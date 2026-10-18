# Configures the integration-test OS image (scripts/integration-windows.sh, OS layer). Runs over SSH as the image's
# admin user right after the unattended install. Everything the suite needs that does not depend on Valheim.
param(
    [Parameter(Mandatory)] [string] $Password,
    [string] $User = 'vsg'
)
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'   # Invoke-WebRequest is ~10x slower with the progress bar on

function Step($name) { "=== $name ($(Get-Date -Format HH:mm:ss))" }

Step 'permanent auto-logon'
# The suite must run in an interactive session (a graceful stop fails from session 0), so the user stays logged on
# across every boot. No AutoLogonCount: with it, Windows stops logging on once the count runs out.
$winlogon = 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon'
Set-ItemProperty $winlogon AutoAdminLogon '1'
Set-ItemProperty $winlogon DefaultUserName $User
Set-ItemProperty $winlogon DefaultPassword $Password
Set-ItemProperty $winlogon DefaultDomainName $env:COMPUTERNAME
Remove-ItemProperty $winlogon AutoLogonCount -ErrorAction SilentlyContinue

Step 'no lock screen, sleep or hibernation'
New-Item 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\Personalization' -Force | Out-Null
Set-ItemProperty 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\Personalization' NoLockScreen 1 -Type DWord
powercfg /change monitor-timeout-ac 0
powercfg /change standby-timeout-ac 0
powercfg /hibernate off

Step 'no automatic updates'
# The image is rebuilt on a schedule instead; updates installing mid-run would make runs differ.
New-Item 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU' -Force | Out-Null
Set-ItemProperty 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU' NoAutoUpdate 1 -Type DWord

Step 'Defender exclusions'
# Real-time scanning of build output is the main Windows slowdown. Tamper Protection prevents turning Defender off.
Add-MpPreference -ExclusionPath 'C:\dotnet', 'C:\vsg', 'C:\steamcmd', "C:\Users\$User\.nuget", "C:\Users\$User\.cache"

Step '.NET 10 SDK'
Invoke-WebRequest https://dot.net/v1/dotnet-install.ps1 -OutFile C:\dotnet-install.ps1 -UseBasicParsing
& C:\dotnet-install.ps1 -Channel 10.0 -InstallDir C:\dotnet -NoPath
Remove-Item C:\dotnet-install.ps1
[Environment]::SetEnvironmentVariable('DOTNET_ROOT', 'C:\dotnet', 'Machine')
[Environment]::SetEnvironmentVariable('DOTNET_CLI_TELEMETRY_OPTOUT', '1', 'Machine')
[Environment]::SetEnvironmentVariable('DOTNET_NOLOGO', '1', 'Machine')
$machinePath = [Environment]::GetEnvironmentVariable('Path', 'Machine')
if ($machinePath -notlike '*C:\dotnet*') { [Environment]::SetEnvironmentVariable('Path', "C:\dotnet;$machinePath", 'Machine') }
"dotnet $(& C:\dotnet\dotnet.exe --version)"

Step 'runner task (interactive session only)'
# /it: the task only runs in the user's interactive session. The host triggers it with `schtasks /run` over SSH;
# the schedule itself never fires. The script it runs arrives with each run's source tree.
New-Item -ItemType Directory -Force C:\vsg | Out-Null
schtasks /create /tn vsg-runner /tr 'powershell -NoProfile -ExecutionPolicy Bypass -File C:\vsg\src\tests\integration-vm\run.ps1' `
    /sc once /st 00:00 /ru $User /it /rl highest /f
if ($LASTEXITCODE -ne 0) { throw "schtasks /create failed: $LASTEXITCODE" }

Step 'done'
