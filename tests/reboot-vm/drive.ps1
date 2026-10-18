# Drives VSG's start-with-OS setting through its UI, the way a user does, with Windows UI Automation
# (scripts/validate-reboot-windows.sh). Started by the vsg-ui task in the interactive session; reads the action
# from C:\vsg\ui\args.txt and the app's path from C:\vsg\ui\exe.txt, and writes C:\vsg\out\<action>.log plus
# <action>.result ("PASS" or "FAIL: <reason>") when it is done.
#
# Actions: enable, disable (set the checkbox either way), expect-unchecked-then-enable (fail unless the checkbox
# reads unchecked when the dialog opens, then enable). Each one opens File > Preferences, sets the checkbox, clicks
# OK, then closes the app with File > Close and waits for it to exit. Controls are found by AutomationId, which is
# not localized. Menu items expose no Invoke or ExpandCollapse pattern, so they are clicked with the mouse.
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes
Add-Type -Namespace Vsg -Name Mouse -MemberDefinition @'
[DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
[DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
[DllImport("user32.dll")] public static extern void mouse_event(uint flags, uint dx, uint dy, uint data, System.UIntPtr extra);
[StructLayout(LayoutKind.Sequential)] public struct POINT { public int X; public int Y; }
[DllImport("user32.dll")] public static extern System.IntPtr WindowFromPoint(POINT p);
[DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(System.IntPtr hwnd, out uint processId);
'@
# Physical pixels, the same units UI Automation reports bounding rectangles in.
[Vsg.Mouse]::SetProcessDPIAware() | Out-Null

$action = (Get-Content C:\vsg\ui\args.txt -Raw).Trim()
$exe = (Get-Content C:\vsg\ui\exe.txt -Raw).Trim()
$out = 'C:\vsg\out'
New-Item -ItemType Directory -Force $out | Out-Null
Remove-Item "$out\$action.result" -ErrorAction SilentlyContinue
Start-Transcript -Path "$out\$action.log" -Force | Out-Null

$AE = [System.Windows.Automation.AutomationElement]
$Root = $AE::RootElement

function Wait-Until([scriptblock] $probe, [int] $seconds, [string] $what) {
    $deadline = (Get-Date).AddSeconds($seconds)
    while ((Get-Date) -lt $deadline) {
        $value = & $probe
        if ($value) { return $value }
        Start-Sleep -Milliseconds 250
    }
    throw "timed out after ${seconds}s waiting for $what"
}

# The app's top-level windows. Avalonia shows menus and dialogs as their own top-level windows.
function Get-AppWindows([int] $processId) {
    $cond = New-Object System.Windows.Automation.PropertyCondition($AE::ProcessIdProperty, $processId)
    $Root.FindAll([System.Windows.Automation.TreeScope]::Children, $cond)
}

# A form field wrapper and the control inside it can share an AutomationId, so the control type picks the one
# automation acts on.
function Find-ById([int] $processId, [string] $automationId, $controlType) {
    $cond = New-Object System.Windows.Automation.AndCondition(
        (New-Object System.Windows.Automation.PropertyCondition($AE::AutomationIdProperty, $automationId)),
        (New-Object System.Windows.Automation.PropertyCondition($AE::ControlTypeProperty, $controlType)))
    foreach ($window in Get-AppWindows $processId) {
        $found = $window.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $cond)
        if ($found) { return $found }
    }
    $null
}

function Wait-ById([int] $processId, [string] $automationId, $controlType, [int] $seconds = 30) {
    Wait-Until { Find-ById $processId $automationId $controlType } $seconds "control '$automationId'"
}

$CT = [System.Windows.Automation.ControlType]

function Invoke-Control($element) {
    $element.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
}

# Clicks the middle of the control, after checking that the app's own window is what is under that point (a
# window covering the app would take the click silently).
function Click-Control($element) {
    $r = $element.Current.BoundingRectangle
    if ($r.IsEmpty) { throw "control '$($element.Current.AutomationId)' has no bounds (off screen?)" }
    $point = New-Object Vsg.Mouse+POINT
    $point.X = [int]($r.X + $r.Width / 2)
    $point.Y = [int]($r.Y + $r.Height / 2)
    $owner = [uint32] 0
    [Vsg.Mouse]::GetWindowThreadProcessId([Vsg.Mouse]::WindowFromPoint($point), [ref] $owner) | Out-Null
    if ($owner -ne $element.Current.ProcessId) {
        throw "another window (pid $owner, $((Get-Process -Id $owner -ErrorAction SilentlyContinue).ProcessName)) covers '$($element.Current.AutomationId)'"
    }
    [Vsg.Mouse]::SetCursorPos($point.X, $point.Y) | Out-Null
    [Vsg.Mouse]::mouse_event(0x0002, 0, 0, 0, [System.UIntPtr]::Zero)   # left button down
    [Vsg.Mouse]::mouse_event(0x0004, 0, 0, 0, [System.UIntPtr]::Zero)   # left button up
}

function Open-FileMenuItem([int] $processId, [string] $itemId) {
    Click-Control (Wait-ById $processId 'Menu_File' $CT::MenuItem)
    Click-Control (Wait-ById $processId $itemId $CT::MenuItem 10)
}

function Get-AppProcess {
    # The copy under test, in this session (a copy started at login runs here too).
    Get-CimInstance Win32_Process -Filter "Name='ValheimServerGUI.exe'" |
        Where-Object { $_.ExecutablePath -eq $exe -and $_.SessionId -eq (Get-Process -Id $PID).SessionId } |
        Select-Object -First 1
}

$result = 'FAIL: did not finish'
try {
    "action: $action  exe: $exe  session: $((Get-Process -Id $PID).SessionId)"
    $proc = Get-AppProcess
    if ($proc) { "attaching to the running app (pid $($proc.ProcessId))" }
    else {
        "launching the app"
        Start-Process -FilePath $exe | Out-Null
        $proc = Wait-Until { Get-AppProcess } 30 'the app process'
    }
    $appPid = [int] $proc.ProcessId

    # The main window, then any startup prompt (a fresh install has no server executable). "No" dismisses it.
    Wait-ById $appPid 'Menu_File' $CT::MenuItem 60 | Out-Null
    $prompt = $null
    try { $prompt = Wait-ById $appPid 'MessageBox_False' $CT::Button 5 } catch { }
    if ($prompt) { "dismissing a startup prompt"; Invoke-Control $prompt; Start-Sleep -Milliseconds 500 }

    Open-FileMenuItem $appPid 'Menu_Preferences'
    $checkbox = Wait-ById $appPid 'Preferences_StartWithOs' $CT::CheckBox
    $toggle = $checkbox.GetCurrentPattern([System.Windows.Automation.TogglePattern]::Pattern)
    $state = $toggle.Current.ToggleState
    "checkbox on open: $state"

    $want = switch ($action) {
        'enable' { 'On' }
        'disable' { 'Off' }
        'expect-unchecked-then-enable' {
            if ($state -ne 'Off') { throw "the checkbox opened as $state, but the OS registration is disabled" }
            'On'
        }
        default { throw "unknown action '$action'" }
    }
    if ("$state" -ne $want) { $toggle.Toggle() }
    "checkbox set: $($toggle.Current.ToggleState)"
    Invoke-Control (Wait-ById $appPid 'Preferences_OK' $CT::Button)
    Wait-Until { -not (Find-ById $appPid 'Preferences_StartWithOs' $CT::CheckBox) } 10 'the Preferences dialog to close' | Out-Null

    Open-FileMenuItem $appPid 'Menu_Close'
    Wait-Until { -not (Get-Process -Id $appPid -ErrorAction SilentlyContinue) } 30 'the app to exit' | Out-Null
    "app exited"
    $result = 'PASS'
}
catch {
    $result = "FAIL: $_"
    "ERROR: $_"
}
finally {
    'Run:'
    Get-ItemProperty 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run' | Format-List | Out-String
    'StartupApproved\Run:'
    $approved = Get-Item 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run' -ErrorAction SilentlyContinue
    if ($approved) {
        foreach ($name in $approved.GetValueNames()) { "  $name = $(($approved.GetValue($name) | ForEach-Object { $_.ToString('x2') }) -join ' ')" }
    }
    Stop-Transcript | Out-Null
    Set-Content "$out\$action.result" $result
}
