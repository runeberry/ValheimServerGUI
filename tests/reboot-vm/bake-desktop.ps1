# Configures the reboot-test desktop image (scripts/validate-reboot-windows.sh, desktop layer on the integration OS
# layer). Runs over SSH as the image's admin user. Makes the VM look like a user's machine as far as launching VSG
# goes, and adds the hooks the host drives it through.
param(
    [Parameter(Mandatory)] [string] $RuntimeUrl,
    [Parameter(Mandatory)] [string] $RuntimeSha512,
    [string] $User = 'vsg'
)
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'   # Invoke-WebRequest is ~10x slower with the progress bar on

function Step($name) { "=== $name ($(Get-Date -Format HH:mm:ss))" }

Step '.NET Desktop Runtime (official installer)'
$installer = 'C:\windowsdesktop-runtime.exe'
Invoke-WebRequest $RuntimeUrl -OutFile $installer -UseBasicParsing
$hash = (Get-FileHash $installer -Algorithm SHA512).Hash
if ($hash -ne $RuntimeSha512) { throw "runtime installer sha512 mismatch: $hash" }
$p = Start-Process $installer -ArgumentList '/install', '/quiet', '/norestart' -Wait -PassThru
if ($p.ExitCode -notin 0, 3010) { throw "runtime installer failed: $($p.ExitCode)" }
Remove-Item $installer

Step 'no SDK on the app path'
# The OS layer's SDK sits in C:\dotnet behind a machine-wide DOTNET_ROOT and PATH entry. A user's machine has
# neither, so the app must find the runtime the way it does there: through the installer's registration (in the
# 32-bit registry view, where the apphost looks).
[Environment]::SetEnvironmentVariable('DOTNET_ROOT', $null, 'Machine')
$machinePath = ([Environment]::GetEnvironmentVariable('Path', 'Machine') -split ';' | Where-Object { $_ -and $_ -ne 'C:\dotnet' }) -join ';'
[Environment]::SetEnvironmentVariable('Path', $machinePath, 'Machine')
(Get-ItemProperty 'HKLM:\SOFTWARE\WOW6432Node\dotnet\Setup\InstalledVersions\x64').InstallLocation

Step 'UI driver task (interactive session only)'
# Like vsg-runner: /it runs it only in the user's interactive session, where the app's windows are. The host
# triggers it with `schtasks /run`; the driver script and its arguments arrive with each run. A headless console
# keeps a terminal window from opening on top of the app, where it would take the driver's clicks.
New-Item -ItemType Directory -Force C:\vsg\ui | Out-Null
schtasks /create /tn vsg-ui /tr 'conhost.exe --headless powershell -NoProfile -ExecutionPolicy Bypass -File C:\vsg\ui\drive.ps1' `
    /sc once /st 00:00 /ru $User /it /f
if ($LASTEXITCODE -ne 0) { throw "schtasks /create failed: $LASTEXITCODE" }

Step 'canary startup entry'
# A second Run value that goes through the same login startup pass (and delay) as the app's. When it has fired,
# Windows has processed the Run key, so an app that has not started by then (plus a grace period) never will.
New-Item -ItemType Directory -Force C:\vsg | Out-Null
# Headless, so no console window is left over the app's.
$canary = 'conhost.exe --headless powershell -NoProfile -Command "Add-Content C:\vsg\canary.txt (Get-Date -Format o)"'
# Written into the user's hive, since this script runs as that user.
Set-ItemProperty 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run' VsgCanary $canary

Step 'done'
