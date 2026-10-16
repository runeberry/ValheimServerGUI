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

Step 'steamcmd + dedicated server (vanilla)'
New-Item -ItemType Directory -Force C:\steamcmd, C:\valheim | Out-Null
Invoke-WebRequest https://steamcdn-a.akamaihd.net/client/installer/steamcmd.zip -OutFile C:\steamcmd\steamcmd.zip -UseBasicParsing
Expand-Archive C:\steamcmd\steamcmd.zip C:\steamcmd -Force
# steamcmd updates itself on its first run and often exits non-zero doing so ("Missing configuration"), and
# Windows PowerShell turns a native command's stderr into an error under 'Stop'. Retry until the server is there.
$ErrorActionPreference = 'Continue'
for ($i = 1; $i -le 3 -and -not (Test-Path C:\valheim\vanilla\valheim_server.exe); $i++) {
    & C:\steamcmd\steamcmd.exe +force_install_dir C:\valheim\vanilla +login anonymous +app_update 896660 validate +quit
}
$ErrorActionPreference = 'Stop'
if (-not (Test-Path C:\valheim\vanilla\valheim_server.exe)) { throw 'steamcmd did not install the server' }
(Select-String -Path C:\valheim\vanilla\steamapps\appmanifest_896660.acf -Pattern '"buildid"\s+"(\d+)"').Matches[0].Groups[1].Value |
    Set-Content C:\valheim\buildid.txt
"server build: $(Get-Content C:\valheim\buildid.txt)"

Step 'bepinex (copy of vanilla + pack + plugins)'
robocopy C:\valheim\vanilla C:\valheim\bepinex /E /NFL /NDL /NJH /NJS /NP | Out-Null
if ($LASTEXITCODE -ge 8) { throw "robocopy failed: $LASTEXITCODE" }
Get-Verified $BepInExPackUrl $BepInExPackSha256 C:\pack.zip
Expand-Archive C:\pack.zip C:\pack -Force
Copy-Item C:\pack\BepInExPack_Valheim\* C:\valheim\bepinex -Recurse -Force
foreach ($plugin in $Plugins -split ' ') {
    $dir, $url, $sha256 = $plugin -split '\|'
    Get-Verified $url $sha256 C:\plugin.zip
    Expand-Archive C:\plugin.zip C:\plugin -Force
    New-Item -ItemType Directory -Force "C:\valheim\bepinex\BepInEx\plugins\$dir" | Out-Null
    Copy-Item C:\plugin\*.dll "C:\valheim\bepinex\BepInEx\plugins\$dir"
    Remove-Item C:\plugin.zip, C:\plugin -Recurse -Force
}
Remove-Item C:\pack.zip, C:\pack -Recurse -Force

Step 'firewall'
foreach ($cfg in 'vanilla', 'bepinex') {
    New-NetFirewallRule -DisplayName "valheim-$cfg" -Direction Inbound -Program "C:\valheim\$cfg\valheim_server.exe" -Action Allow | Out-Null
}

Step 'done'
$global:LASTEXITCODE = 0
