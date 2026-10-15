# Runs the complete suite inside a Windows integration VM (scripts/integration-windows.sh). Started by the vsg-runner
# scheduled task, which Windows only runs in the logged-on user's interactive session: the graceful stop the
# lifecycle tests rely on fails from session 0. Reads the configuration name from C:\vsg\config.txt and writes
# everything to C:\vsg\out (transcript, test results, captured server logs, exit_code.txt).
#
# -AllowServicesSession is only for the negative control (integration-windows.sh --control), which runs this
# script directly over SSH, in session 0, on purpose.
param([switch] $AllowServicesSession)
$ErrorActionPreference = 'Stop'
$out = 'C:\vsg\out'
$src = 'C:\vsg\src'
New-Item -ItemType Directory -Force $out | Out-Null
Start-Transcript -Path "$out\run.log" -Force | Out-Null
$exit = 1
try {
    $session = (Get-Process -Id $PID).SessionId
    "session: $session"
    if ($session -eq 0 -and -not $AllowServicesSession) { throw 'Running in session 0, where a graceful stop cannot work.' }

    $cfg = (Get-Content C:\vsg\config.txt -Raw).Trim()
    $env:VSG_IT_SERVER_EXE = "C:\valheim\$cfg\valheim_server.exe"
    $env:VSG_IT_SERVER_CONFIG = $cfg
    if ($cfg -eq 'bepinex') {
        $line = Select-String -Path "$src\tests\integration-image\server-configs.env" -Pattern '^EXPECTED_PLUGIN=(.*)$'
        $env:VSG_IT_EXPECTED_PLUGIN = $line.Matches[0].Groups[1].Value.Trim('"')
    }
    $env:VSG_IT_SAVEDIR = "$out\savedir"
    $env:VSG_IT_ARTIFACT_DIR = "$out\artifacts"
    New-Item -ItemType Directory -Force $env:VSG_IT_SAVEDIR, $env:VSG_IT_ARTIFACT_DIR | Out-Null
    "config: $cfg  server build: $(Get-Content C:\valheim\buildid.txt)"

    # Windows PowerShell turns a native command's stderr into a terminating error under 'Stop'.
    $ErrorActionPreference = 'Continue'
    & C:\dotnet\dotnet.exe test "$src\ValheimServerGUI.slnx" -p:Integration=true --logger trx `
        --blame-hang --blame-hang-timeout 360s --blame-hang-dump-type mini --results-directory "$out\results" 2>&1 | Out-Host
    $exit = $LASTEXITCODE
    $ErrorActionPreference = 'Stop'
}
catch {
    "ERROR: $_"
}
finally {
    Get-CimInstance Win32_Process -Filter "Name='valheim_server.exe'" |
        ForEach-Object { "cleanup: killing leftover server $($_.ProcessId)"; Stop-Process -Id $_.ProcessId -Force }
    Stop-Transcript | Out-Null
    Set-Content "$out\exit_code.txt" $exit
}
