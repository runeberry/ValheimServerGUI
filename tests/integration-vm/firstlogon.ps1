# Runs once at the first logon of a fresh install, from the answer CD (see autounattend.xml). Enables the inbox
# OpenSSH server and authorizes the host's key; the rest of the image is configured over SSH by bake-os.ps1.
$ErrorActionPreference = 'Stop'
Start-Transcript -Path C:\firstlogon.log -Force | Out-Null
try {
    Add-WindowsCapability -Online -Name OpenSSH.Server~~~~0.0.1.0 | Out-Null
    Set-Service sshd -StartupType Automatic
    Start-Service sshd
    # The feature's own rule allows only the Private profile, and Windows puts libvirt's NAT network in Public.
    if (Get-NetFirewallRule -Name OpenSSH-Server-In-TCP -ErrorAction SilentlyContinue) {
        Set-NetFirewallRule -Name OpenSSH-Server-In-TCP -Profile Any
    }
    else {
        New-NetFirewallRule -Name OpenSSH-Server-In-TCP -DisplayName 'OpenSSH Server (sshd)' -Protocol TCP `
            -LocalPort 22 -Direction Inbound -Action Allow -Profile Any | Out-Null
    }

    # The stock sshd_config reads administrators' keys from this file, which must be writable by admins only.
    $keys = 'C:\ProgramData\ssh\administrators_authorized_keys'
    Copy-Item (Join-Path $PSScriptRoot 'authorized_key.pub') $keys -Force
    icacls $keys /inheritance:r /grant 'Administrators:F' /grant 'SYSTEM:F' | Out-Null
    'firstlogon: ok'
}
catch {
    "firstlogon: FAILED: $_"
}
finally {
    Stop-Transcript | Out-Null
}
