param(
 [Parameter(Mandatory=$true)][string]$Endpoint,
 [string]$OutputPath = (Join-Path $PSScriptRoot 'Runtime/beebloom-connection.json'),
 [switch]$PushToQuest,
 [string]$AdbPath = 'adb',
 [string]$Serial,
 [string]$PackageName = 'com.quoctrieu.beebloom'
)
$ErrorActionPreference='Stop'
$address=$null
if (![Uri]::TryCreate($Endpoint.Trim(),[UriKind]::Absolute,[ref]$address) -or $address.Scheme -ne 'ws' -or
    !$address.Host -or $address.HostNameType -eq [UriHostNameType]::IPv6 -or $address.Port -lt 1 -or
    $address.AbsolutePath -ne '/' -or $address.Query -or $address.Fragment -or $address.UserInfo) {
 throw 'Endpoint must be ws://<PC IPv4 or hostname>:8080 with no path, query or credentials.'
}
if ($PushToQuest -and $address.Host -in @('localhost','127.0.0.1','0.0.0.0')) {
 throw 'Use the PC LAN address for Quest. localhost/127.0.0.1 on Quest refers to the headset itself.'
}
$destination=[IO.Path]::GetFullPath($OutputPath)
[IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($destination)) | Out-Null
$normalizedEndpoint="ws://$($address.Host):$($address.Port)"
$profile=@{schema='beebloom-connection-v1';simpleEndpoint=$normalizedEndpoint} | ConvertTo-Json
$unchanged=$false
if ([IO.File]::Exists($destination)) {
 try {
  $existing=[IO.File]::ReadAllText($destination) | ConvertFrom-Json
  $unchanged=($null -ne $existing -and @($existing.PSObject.Properties).Count -eq 2 -and
   $existing.schema -ceq 'beebloom-connection-v1' -and $existing.simpleEndpoint -ceq $normalizedEndpoint)
 } catch { $unchanged=$false }
}
if (!$unchanged) {
 # File.Replace attempts to merge the old file's Windows metadata and can fail
 # with ERROR_UNABLE_TO_REMOVE_REPLACED. Rename a complete file on the same
 # volume instead; never delete/truncate the active profile as a fallback.
 if ([Environment]::OSVersion.Platform -eq [PlatformID]::Win32NT -and
     !('BeeBloom.ConnectionAtomicFile' -as [type])) {
  Add-Type -TypeDefinition @'
using System.Runtime.InteropServices;
namespace BeeBloom {
 public static class ConnectionAtomicFile {
  [DllImport("kernel32.dll", CharSet=CharSet.Unicode, SetLastError=true)]
  [return: MarshalAs(UnmanagedType.Bool)]
  public static extern bool MoveFileEx(string source, string destination, uint flags);
 }
}
'@
 }
 $temporary=$destination+'.'+[Guid]::NewGuid().ToString('N')+'.tmp'
 try {
  [IO.File]::WriteAllText($temporary,$profile,[Text.UTF8Encoding]::new($false))
  for ($attempt=0; $attempt -lt 6; $attempt++) {
   try {
    if ([Environment]::OSVersion.Platform -eq [PlatformID]::Win32NT) {
     # MOVEFILE_REPLACE_EXISTING | MOVEFILE_WRITE_THROUGH
     if (![BeeBloom.ConnectionAtomicFile]::MoveFileEx($temporary,$destination,9)) {
      $nativeError=[Runtime.InteropServices.Marshal]::GetLastWin32Error()
      throw [ComponentModel.Win32Exception]::new($nativeError,"Cannot atomically update connection file '$destination' (Windows error $nativeError). Close programs locking this file and retry.")
     }
    } elseif ([IO.File]::Exists($destination)) { [IO.File]::Replace($temporary,$destination,[NullString]::Value) }
    else { [IO.File]::Move($temporary,$destination) }
    break
   } catch {
    if ($attempt -eq 5) { throw }
    Start-Sleep -Milliseconds 150
   }
  }
 } finally {
  if ([IO.File]::Exists($temporary)) { [IO.File]::Delete($temporary) }
 }
} else { Write-Host 'Connection endpoint unchanged; existing file kept.' }
Write-Host "Connection file: $destination"
if (!$PushToQuest) { return }
if ($PackageName -notmatch '^[A-Za-z][A-Za-z0-9_]*(\.[A-Za-z][A-Za-z0-9_]*)+$') { throw 'Invalid Android package name.' }
$adb=(Get-Command $AdbPath -ErrorAction Stop).Source
$devices=& $adb devices
if ($LASTEXITCODE -ne 0) { throw 'ADB device query failed.' }
$available=@($devices | ForEach-Object { if ($_ -match '^(\S+)\s+device$') { $Matches[1] } })
if (!$Serial) {
 if ($available.Count -ne 1) { throw 'Connect exactly one authorized Quest, or pass -Serial.' }
 $Serial=$available[0]
} elseif ($Serial -notin $available) { throw 'The selected device is not authorized/connected.' }
$target="/sdcard/Android/data/$PackageName/files"
& $adb -s $Serial shell mkdir -p $target
if ($LASTEXITCODE -ne 0) { throw 'Cannot create the app config directory. Install and open the app once first.' }
& $adb -s $Serial push $destination "$target/beebloom-connection.json.tmp"
if ($LASTEXITCODE -ne 0) { throw 'ADB config upload failed.' }
& $adb -s $Serial shell mv "$target/beebloom-connection.json.tmp" "$target/beebloom-connection.json"
if ($LASTEXITCODE -ne 0) { throw 'ADB config activation failed.' }
Write-Host 'Quest connection updated. No APK rebuild needed; a running client reconnects automatically.'
