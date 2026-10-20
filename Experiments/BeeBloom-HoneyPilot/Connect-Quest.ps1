param(
 [string]$PcAddress,
 [string]$AdbPath,
 [string]$Serial,
 [string]$InstallApk,
 [switch]$UpdateFirewall,
 [switch]$EnableStreaming,
 [switch]$PrepareOnly
)
$ErrorActionPreference='Stop'
if (!$PcAddress) {
 # UDP connect selects the default route without sending any packet.
 $route=[Net.Sockets.Socket]::new([Net.Sockets.AddressFamily]::InterNetwork,[Net.Sockets.SocketType]::Dgram,[Net.Sockets.ProtocolType]::Udp)
 try { $route.Connect('1.1.1.1',80); $PcAddress=$route.LocalEndPoint.Address.ToString() }
 finally { $route.Dispose() }
}
$ip=$null
if (![Net.IPAddress]::TryParse($PcAddress,[ref]$ip) -or $ip.AddressFamily -ne [Net.Sockets.AddressFamily]::InterNetwork -or
    $PcAddress -in @('127.0.0.1','0.0.0.0')) { throw 'PcAddress must be the PC LAN IPv4 address.' }
# Quest and Editor profiles are separate. Updating a headset never overwrites
# the Editor loopback profile or any saved Unity scene/Inspector setting.
$questProfile=Join-Path $PSScriptRoot 'Runtime/quest/beebloom-connection.json'
& (Join-Path $PSScriptRoot 'Set-BeeConnection.ps1') -Endpoint "ws://${PcAddress}:8080" -OutputPath $questProfile
if ($PrepareOnly) { Write-Host 'Prepared Quest profile only; no device/firewall changes.'; return }
if (!$AdbPath) {
 $installed=Get-Command adb -ErrorAction SilentlyContinue
 if ($installed) { $AdbPath=$installed.Source }
 else {
  $editors=Join-Path $env:ProgramFiles 'Unity/Hub/Editor'
  $candidate=Get-ChildItem -Path "$editors/*/Editor/Data/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb.exe" -File -ErrorAction SilentlyContinue |
    Sort-Object LastWriteTime -Descending | Select-Object -First 1
  if ($candidate) { $AdbPath=$candidate.FullName }
 }
}
if (!$AdbPath -or !(Test-Path -LiteralPath $AdbPath)) { throw 'ADB not found. Pass -AdbPath or install Unity Android Build Support.' }
$lines=& $AdbPath devices
if ($LASTEXITCODE -ne 0) { throw 'ADB query failed.' }
$ready=@($lines | ForEach-Object { if ($_ -match '^(\S+)\s+device$') { $Matches[1] } })
if (!$Serial) {
 $wired=@($ready | Where-Object {$_ -notmatch ':\d+$' -and $_ -notmatch '\._adb-tls-'})
 if ($wired.Count -eq 1) { $Serial=$wired[0] }
 elseif ($ready.Count -eq 1) { $Serial=$ready[0] }
 else { throw 'Connect one authorized Quest by USB or select -Serial. Offline headsets cannot be updated.' }
} elseif ($Serial -notin $ready) { throw 'Selected headset is offline or unauthorized.' }
if ($InstallApk) {
 $apk=(Resolve-Path -LiteralPath $InstallApk).Path
 # Keep stdout AND stderr on Windows PowerShell 5.1. A native stderr message
 # must not terminate the script before we retain the actual installer error.
 function Invoke-QuestInstall {
  $previousPreference=$ErrorActionPreference
  try {
   $ErrorActionPreference='Continue'
   $output=@(& $AdbPath -s $Serial install --no-streaming -r $apk 2>&1 | ForEach-Object { $_.ToString() })
   $code=$LASTEXITCODE
  } finally { $ErrorActionPreference=$previousPreference }
  [pscustomobject]@{Code=$code;Lines=$output}
 }
 # Separate transfer/install avoids ADB streamed-install failures. -r keeps
 # existing app data, including the saved connection and gameplay progress.
 $install=Invoke-QuestInstall
 $log=Join-Path $PSScriptRoot ('Runtime/quest/install-'+(Get-Date -Format 'yyyyMMdd-HHmmss-fff')+'.log')
 [IO.File]::WriteAllLines($log,@("ADB: $AdbPath","Device: $Serial","APK: $apk","Exit code: $($install.Code)")+$install.Lines,[Text.UTF8Encoding]::new($false))
 $install.Lines | ForEach-Object { Write-Host $_ }
 Write-Host "Install log: $log"
 if ($install.Code -ne 0) {
  $previousPreference=$ErrorActionPreference
  try {
   $ErrorActionPreference='Continue'
   $state=(@(& $AdbPath -s $Serial get-state 2>&1 | ForEach-Object { $_.ToString() })) -join ' '
   $stateCode=$LASTEXITCODE
  } finally { $ErrorActionPreference=$previousPreference }
  if ($stateCode -ne 0 -or $state.Trim() -ne 'device') {
   throw "Quest lost its ADB connection during installation ($state). Reconnect USB, accept USB debugging, then run this command again. Existing app data was not removed. Details: $log"
  }
  throw "APK installation failed (exit $($install.Code)): $($install.Lines -join ' '). Details: $log. Connection upload and firewall update have not run."
 }
}
& (Join-Path $PSScriptRoot 'Set-BeeConnection.ps1') -Endpoint "ws://${PcAddress}:8080" -OutputPath $questProfile -PushToQuest -AdbPath $AdbPath -Serial $Serial
if ($UpdateFirewall) {
 $identity=[Security.Principal.WindowsIdentity]::GetCurrent()
 $admin=([Security.Principal.WindowsPrincipal]::new($identity)).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
 if (!$admin) { throw 'Configuration was uploaded. Re-run with -UpdateFirewall in PowerShell Administrator to update the restricted TCP 8080 rule.' }
 $routeText=(& $AdbPath -s $Serial shell ip -4 route get $PcAddress) -join ' '
 if ($LASTEXITCODE -ne 0 -or $routeText -notmatch '\bsrc\s+(\d+\.\d+\.\d+\.\d+)') { throw 'Cannot determine headset Wi-Fi IPv4; confirm it is on the same network as the PC.' }
 $questAddress=$Matches[1]
 $questIp=$null
 if (![Net.IPAddress]::TryParse($questAddress,[ref]$questIp) -or $questAddress -eq $PcAddress) { throw 'Invalid headset network address.' }
 $local=Get-NetIPAddress -AddressFamily IPv4 -IPAddress $PcAddress -ErrorAction Stop
 $network=Get-NetConnectionProfile -InterfaceIndex $local.InterfaceIndex -ErrorAction Stop
 $profile=if ($network.NetworkCategory -eq 'Private') {'Private'} elseif ($network.NetworkCategory -eq 'Public') {'Public'} else {throw 'Domain-managed networks require administrator configuration.'}
 & (Join-Path $PSScriptRoot 'Set-BeeFirewall.ps1') -LocalAddress $PcAddress -RemoteAddress $questAddress -Profile $profile
}
if ($EnableStreaming) {
 # SIMPLE streams through wireless ADB, independently of gameplay TCP 8080.
 $routeText=(& $AdbPath -s $Serial shell ip -4 route get $PcAddress) -join ' '
 if ($LASTEXITCODE -ne 0 -or $routeText -notmatch '\bsrc\s+(\d+\.\d+\.\d+\.\d+)') { throw 'Cannot read Quest Wi-Fi IP for streaming.' }
 $streamAddress=$Matches[1]
 $streamIp=$null
 if (![Net.IPAddress]::TryParse($streamAddress,[ref]$streamIp) -or $streamAddress -eq $PcAddress) { throw 'Invalid Quest streaming IP.' }
 if ($Serial -notmatch ':\d+$') {
  & $AdbPath -s $Serial tcpip 5555
  if ($LASTEXITCODE -ne 0) { throw 'Cannot enable wireless ADB. Keep USB connected and accept debugging on Quest.' }
 }
 $streamTarget=if($Serial -match ':\d+$'){$Serial}else{"${streamAddress}:5555"}
 $streamReady=$false
 for($attempt=0;$attempt -lt 5;$attempt++) {
  $previousPreference=$ErrorActionPreference
  try {
   $ErrorActionPreference='Continue'
   $message=@(& $AdbPath connect $streamTarget 2>&1 | ForEach-Object { $_.ToString() })
   $states=@(& $AdbPath devices 2>&1 | ForEach-Object { $_.ToString() })
   $queryCode=$LASTEXITCODE
  } finally {$ErrorActionPreference=$previousPreference}
  $message | ForEach-Object { Write-Host $_ }
  if ($queryCode -eq 0 -and ($states -match ('^'+[regex]::Escape($streamTarget)+'\s+device$'))) {$streamReady=$true;break}
  Start-Sleep -Milliseconds 750
 }
 if (!$streamReady) { throw "Gameplay profile was updated, but wireless ADB $streamTarget is not ready. Keep Quest on the same Wi-Fi; guest-network isolation can block screen streaming." }
 Write-Host "Wireless ADB ready: $streamTarget. Open http://localhost:8000/streamPlayerScreen in Chrome or Edge."
}
Write-Host "Quest now targets ws://${PcAddress}:8080. Open Bee Bloom, run BeeBloomVR in SIMPLE and add the player."
