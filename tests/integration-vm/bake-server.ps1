# Installs the server configurations into the integration-test server image (scripts/integration-windows.sh, server
# layer on top of the OS layer). Runs over SSH. The bepinex pins come from tests/integration-image/server-configs.env,
# passed in by the host script.
param(
    [Parameter(Mandatory)] [string] $BepInExPackUrl,
    [Parameter(Mandatory)] [string] $BepInExPackSha256,
    # server-configs.env's PLUGINS: "<folder>|<url>|<sha256>" entries separated by spaces.
    [Parameter(Mandatory)] [string] $Plugins
)
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

function Step($name) { "=== $name ($(Get-Date -Format HH:mm:ss))" }

function Get-Verified($url, $sha256, $path) {
    Invoke-WebRequest -Uri $url -OutFile $path -UseBasicParsing
    $actual = (Get-FileHash $path -Algorithm SHA256).Hash.ToLower()
    if ($actual -ne $sha256.ToLower()) { throw "sha256 mismatch for ${url}: $actual" }
}

Step 'Visual C++ Redistributable'
# PlayFab Party (crossplay) loads PartyWin32.dll, which needs the VC++ runtime; without it a crossplay server never
# registers and never gets an invite code. Most Windows machines have it already (Steam installs it); a fresh
# evaluation image does not. Microsoft revises the installer in place, so it is checked by signature, not hash.
Invoke-WebRequest https://aka.ms/vs/17/release/vc_redist.x64.exe -OutFile C:\vc_redist.x64.exe -UseBasicParsing
$signature = Get-AuthenticodeSignature C:\vc_redist.x64.exe
if ($signature.Status -ne 'Valid' -or $signature.SignerCertificate.Subject -notmatch 'O=Microsoft Corporation') {
    throw "vc_redist.x64.exe is not validly signed by Microsoft: $($signature.Status) $($signature.SignerCertificate.Subject)"
}
$installer = Start-Process C:\vc_redist.x64.exe -ArgumentList '/install', '/quiet', '/norestart' -Wait -PassThru
# 3010: installed, reboot pending (the image is shut down after this bake anyway); 1638: a newer version is present.
if ($installer.ExitCode -notin 0, 3010, 1638) { throw "vc_redist.x64.exe failed: $($installer.ExitCode)" }
Remove-Item C:\vc_redist.x64.exe

# Each configuration sits where Steam itself puts the server: a library's steamapps\common\"Valheim dedicated server",
# with the app manifest in the library's steamapps\. vanilla is in Steam's default library under Program Files (x86),
# bepinex in a second library like the SteamLibrary folder Steam makes on another drive. The spaces and parentheses
# in those paths are part of what the suite proves. The locations are recorded in C:\vsg-servers.json for run.ps1.
$libraries = @{
    vanilla = 'C:\Program Files (x86)\Steam'
    bepinex = 'C:\SteamLibrary'
}
$servers = @{}
foreach ($cfg in $libraries.Keys) { $servers[$cfg] = Join-Path $libraries[$cfg] 'steamapps\common\Valheim dedicated server' }

Step 'steamcmd + dedicated server (vanilla)'
New-Item -ItemType Directory -Force C:\steamcmd | Out-Null
Invoke-WebRequest https://steamcdn-a.akamaihd.net/client/installer/steamcmd.zip -OutFile C:\steamcmd\steamcmd.zip -UseBasicParsing
Expand-Archive C:\steamcmd\steamcmd.zip C:\steamcmd -Force
# steamcmd updates itself on its first run and often exits non-zero doing so ("Missing configuration"), and
# Windows PowerShell turns a native command's stderr into an error under 'Stop'. Retry until the server is there.
$ErrorActionPreference = 'Continue'
for ($i = 1; $i -le 3 -and -not (Test-Path "$($servers.vanilla)\valheim_server.exe"); $i++) {
    & C:\steamcmd\steamcmd.exe +force_install_dir "$($servers.vanilla)" +login anonymous +app_update 896660 validate +quit
}
$ErrorActionPreference = 'Stop'
if (-not (Test-Path "$($servers.vanilla)\valheim_server.exe")) { throw 'steamcmd did not install the server' }
# steamcmd keeps its manifest in the install folder; Steam keeps it in the library's steamapps\.
Move-Item "$($servers.vanilla)\steamapps\appmanifest_896660.acf" (Join-Path $libraries.vanilla 'steamapps')
Remove-Item "$($servers.vanilla)\steamapps" -Recurse -Force
# Steam grants Users modify rights on its own folder; mods and the game write there without elevation.
icacls $libraries.vanilla /grant '*S-1-5-32-545:(OI)(CI)M' /T /Q | Out-Null
if ($LASTEXITCODE -ne 0) { throw "icacls failed: $LASTEXITCODE" }

Step 'bepinex (copy of vanilla + pack + plugins)'
robocopy $libraries.vanilla $libraries.bepinex /E /NFL /NDL /NJH /NJS /NP | Out-Null
if ($LASTEXITCODE -ge 8) { throw "robocopy failed: $LASTEXITCODE" }
Get-Verified $BepInExPackUrl $BepInExPackSha256 C:\pack.zip
Expand-Archive C:\pack.zip C:\pack -Force
Copy-Item C:\pack\BepInExPack_Valheim\* $servers.bepinex -Recurse -Force
foreach ($plugin in $Plugins -split ' ') {
    $dir, $url, $sha256 = $plugin -split '\|'
    Get-Verified $url $sha256 C:\plugin.zip
    Expand-Archive C:\plugin.zip C:\plugin -Force
    $pluginDir = Join-Path $servers.bepinex "BepInEx\plugins\$dir"
    New-Item -ItemType Directory -Force $pluginDir | Out-Null
    Copy-Item C:\plugin\*.dll $pluginDir
    Remove-Item C:\plugin.zip, C:\plugin -Recurse -Force
}
Remove-Item C:\pack.zip, C:\pack -Recurse -Force

Step 'firewall + Defender exclusions'
foreach ($cfg in $servers.Keys) {
    New-NetFirewallRule -DisplayName "valheim-$cfg" -Direction Inbound -Program "$($servers[$cfg])\valheim_server.exe" -Action Allow | Out-Null
}
Add-MpPreference -ExclusionPath @($libraries.Values)

Step 'record'
$manifest = Join-Path $libraries.vanilla 'steamapps\appmanifest_896660.acf'
$buildId = (Select-String -Path $manifest -Pattern '"buildid"\s+"(\d+)"').Matches[0].Groups[1].Value
@{ buildid = $buildId; servers = $servers } | ConvertTo-Json | Set-Content C:\vsg-servers.json
"server build: $buildId"

Step 'done'
$global:LASTEXITCODE = 0
