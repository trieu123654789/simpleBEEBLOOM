#Requires -RunAsAdministrator
param(
 [Parameter(Mandatory=$true)][string]$RemoteAddress,
 [Parameter(Mandatory=$true)][string]$LocalAddress,
 [ValidateSet('Private','Public')][string]$Profile = 'Private'
)
$ErrorActionPreference='Stop'
$localIp=$null
if (![Net.IPAddress]::TryParse($LocalAddress,[ref]$localIp) -or
    $localIp.AddressFamily -ne [Net.Sockets.AddressFamily]::InterNetwork) {
 throw 'LocalAddress must be the PC IPv4 address.'
}
$parts=$RemoteAddress.Split('/')
$remoteIp=$null
if ($parts.Count -gt 2 -or ![Net.IPAddress]::TryParse($parts[0],[ref]$remoteIp) -or
    $remoteIp.AddressFamily -ne [Net.Sockets.AddressFamily]::InterNetwork) {
 throw 'RemoteAddress must be a headset IPv4 address or an IPv4 subnet.'
}
if ($parts.Count -eq 2 -and ($parts[1] -notmatch '^\d+$' -or [int]$parts[1] -lt 16 -or [int]$parts[1] -gt 32)) {
 throw 'Use a local subnet with a prefix from /16 to /32.'
}
$ruleName='BeeBloom SIMPLE Quest TCP 8080'
$existing=@(& netsh advfirewall firewall show rule "name=$ruleName")
if ($LASTEXITCODE -eq 0) {
 & netsh advfirewall firewall set rule "name=$ruleName" new dir=in action=allow enable=yes protocol=TCP localport=8080 "localip=$LocalAddress" "remoteip=$RemoteAddress" "profile=$($Profile.ToLowerInvariant())"
} else {
 & netsh advfirewall firewall add rule "name=$ruleName" dir=in action=allow enable=yes protocol=TCP localport=8080 "localip=$LocalAddress" "remoteip=$RemoteAddress" "profile=$($Profile.ToLowerInvariant())"
}
if ($LASTEXITCODE -ne 0) { throw 'Windows did not apply the firewall rule.' }
& netsh advfirewall firewall show rule "name=$ruleName" verbose
if ($LASTEXITCODE -ne 0) { throw 'Cannot read back the firewall rule.' }
Write-Host "Expected: $Profile profile, inbound TCP 8080, PC $LocalAddress, headset/subnet $RemoteAddress"
