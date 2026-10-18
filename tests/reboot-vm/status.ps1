# Reports what happened at the latest login (scripts/validate-reboot-windows.sh): whether the canary startup entry
# has fired since boot, and whether the app under test (C:\vsg\ui\exe.txt) is running in the interactive session.
# Prints one line: "canary=yes|no app=yes|no".
$boot = (Get-CimInstance Win32_OperatingSystem).LastBootUpTime
$exe = (Get-Content C:\vsg\ui\exe.txt -Raw).Trim()
$canary = Get-Content C:\vsg\canary.txt -ErrorAction SilentlyContinue |
    Where-Object { $_ -and [datetime]::Parse($_) -gt $boot }
$app = Get-CimInstance Win32_Process -Filter "Name='ValheimServerGUI.exe'" |
    Where-Object { $_.ExecutablePath -eq $exe -and $_.SessionId -eq 1 }
"canary=$(if ($canary) { 'yes' } else { 'no' }) app=$(if ($app) { 'yes' } else { 'no' })"
