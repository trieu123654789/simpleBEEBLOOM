<#
.SYNOPSIS
Reset the two-flower test loop with an automatic checkpoint backup.
.DESCRIPTION
Close Let It Bee on Quest and Stop Play in Unity first. SIMPLE/GAMA must be
running. The script stops only LetItBeeVR, then resets progress. Launch that
experiment again in SIMPLE afterwards. It never writes Unity scene settings.
.PARAMETER UseCurrentLayout
Archive the whole checkpoint so the next Unity/APK configure supplies geometry.
Use this after intentionally moving flowers/hive or changing interaction radii.
.PARAMETER Preview
Show the target session and proposed reset without changing disk or the server.
#>
param(
 [string]$Session,
 [string]$CheckpointDirectory,
 [switch]$UseCurrentLayout,
 [switch]$Preview
)
$ErrorActionPreference='Stop'

function New-HoneyProgress($saved) {
 # Only gameplay progress changes. Scene geometry and tuning remain authoritative.
 $fresh=$saved | ConvertTo-Json -Depth 64 | ConvertFrom-Json
 if ($fresh.state.schema -ne 'letitbee-honey-pilot-v1' -and $fresh.state.schema -ne 'beebloom-honey-pilot-v1' -or @($fresh.state.flowers).Count -ne 2) {
  throw 'Unsupported checkpoint. This reset tool is for the current two-flower pilot.'
 }
 $fresh.state.revision=0
 $fresh.state.nectar=@(1,1)
 $fresh.state.pollinated=@($false,$false)
 $fresh.state.pollen_source=-1
 $fresh.state.visits=0
 $fresh.state.last_contact=-1
 $fresh.state.chunk_id=''
 $fresh.state.chunk_state='none'
 $fresh.state.chunk_pos=@(0.0,0.0,0.0)
 $fresh.state.stored=0
 $fresh.state.PSObject.Properties.Remove('chunk_flower')
 $fresh.receipts=@{}
 return $fresh
}

function Write-HoneyCheckpoint([string]$path, [string]$json) {
 if (!('LetItBee.ConnectionAtomicFile' -as [type])) {
  Add-Type -TypeDefinition @'
using System.Runtime.InteropServices;
namespace LetItBee {
 public static class ConnectionAtomicFile {
  [DllImport("kernel32.dll", CharSet=CharSet.Unicode, SetLastError=true)]
  [return: MarshalAs(UnmanagedType.Bool)]
  public static extern bool MoveFileEx(string source, string destination, uint flags);
 }
}
'@
 }
 $temporary=$path+'.'+[Guid]::NewGuid().ToString('N')+'.tmp'
 try {
  [IO.File]::WriteAllText($temporary,$json,[Text.UTF8Encoding]::new($false))
  if (![LetItBee.ConnectionAtomicFile]::MoveFileEx($temporary,$path,9)) {
   throw [ComponentModel.Win32Exception]::new([Runtime.InteropServices.Marshal]::GetLastWin32Error(),'Checkpoint is locked. Original progress and backup were kept; close the locking program and retry.')
  }
 } finally {
  if ([IO.File]::Exists($temporary)) { [IO.File]::Delete($temporary) }
 }
}

if (!$Session) {
 $scene=Join-Path $PSScriptRoot '../../Assets/Scenes/VU2/SCN_LetItBee.unity'
 $found=[regex]::Matches([IO.File]::ReadAllText($scene),'(?m)^\s*sessionId:\s*([a-f0-9]{32})\s*$')
 if ($found.Count -ne 1) { throw 'Cannot identify one saved pilot session. Pass -Session <32-character ID>.' }
 $Session=$found[0].Groups[1].Value
}
if ($Session -cnotmatch '^[a-f0-9]{32}$') { throw 'Session must be a 32-character lowercase hexadecimal ID.' }
if (!$CheckpointDirectory) {
 $CheckpointDirectory=Join-Path $PSScriptRoot 'Runtime/checkpoints'
 $localSettings=Join-Path $PSScriptRoot 'GAMA/server.local.json'
 if (Test-Path -LiteralPath $localSettings) {
  $settings=Get-Content -LiteralPath $localSettings -Raw | ConvertFrom-Json
  if (!$settings.checkpoint_directory) { throw 'server.local.json has no checkpoint_directory.' }
  $CheckpointDirectory=[string]$settings.checkpoint_directory
  if (![IO.Path]::IsPathRooted($CheckpointDirectory)) { $CheckpointDirectory=Join-Path (Join-Path $PSScriptRoot 'GAMA') $CheckpointDirectory }
 }
}
$checkpoint=Join-Path ([IO.Path]::GetFullPath($CheckpointDirectory)) ($Session+'.json')
if (!(Test-Path -LiteralPath $checkpoint -PathType Leaf)) {
 Write-Host "No saved progress for session $Session. The next GAMA connection starts fresh."
 return
}
$saved=Get-Content -LiteralPath $checkpoint -Raw | ConvertFrom-Json
if ($saved.state.session -cne $Session) { throw 'Checkpoint session does not match its filename; nothing changed.' }
$fresh=New-HoneyProgress $saved
if ($Preview) {
 [pscustomobject]@{session=$Session;checkpoint=$checkpoint;useCurrentLayout=[bool]$UseCurrentLayout;
  oldRevision=$saved.state.revision;oldChunk=$saved.state.chunk_state;oldStored=$saved.state.stored;
  nextState=if($UseCurrentLayout){'Created from the next Unity/APK configure'}else{$fresh.state}} | ConvertTo-Json -Depth 64
 return
}

# Coordinate through SIMPLE, so an in-memory GAMA state cannot overwrite reset data.
$socket=[Net.WebSockets.ClientWebSocket]::new()
$state=$null
function Read-HoneyMonitor {
 do {
  $buffer=New-Object byte[] 8192
  $message=[IO.MemoryStream]::new()
  $timeout=[Threading.CancellationTokenSource]::new(10000)
  try {
   do {
    $part=$socket.ReceiveAsync([ArraySegment[byte]]::new($buffer),$timeout.Token).GetAwaiter().GetResult()
    if ($part.MessageType -eq [Net.WebSockets.WebSocketMessageType]::Close) { throw 'SIMPLE closed the monitor connection.' }
    $message.Write($buffer,0,$part.Count)
   } while (!$part.EndOfMessage)
   $item=[Text.Encoding]::UTF8.GetString($message.ToArray()) | ConvertFrom-Json
  } finally { $message.Dispose();$timeout.Dispose() }
 } while ($item.type -ne 'json_state')
 return $item
}
function Send-HoneyMonitor($value) {
 $bytes=[Text.Encoding]::UTF8.GetBytes(($value|ConvertTo-Json -Compress))
 $timeout=[Threading.CancellationTokenSource]::new(5000)
 try { $socket.SendAsync([ArraySegment[byte]]::new($bytes),[Net.WebSockets.WebSocketMessageType]::Text,$true,$timeout.Token).GetAwaiter().GetResult() | Out-Null }
 finally { $timeout.Dispose() }
}
try {
 $timeout=[Threading.CancellationTokenSource]::new(5000)
 try { $socket.ConnectAsync([Uri]'ws://127.0.0.1:8001',$timeout.Token).GetAwaiter().GetResult() | Out-Null }
 catch { throw 'Start SIMPLE first. No progress was changed.' }
 finally { $timeout.Dispose() }
 $state=Read-HoneyMonitor
 $players=if ($state.player -is [Array]) { @($state.player) } else { @($state.player.PSObject.Properties | ForEach-Object {$_.Value}) }
 if (@($players | Where-Object {$_.connected}).Count -gt 0) {
  throw 'Close Let It Bee on the headset and Stop Play in Unity first. Wait 15 seconds, then retry. No progress was changed.'
 }
 if (!$state.gama.connected) { throw 'GAMA is not connected. Start GAMA/SIMPLE first; no progress was changed.' }
 if ($state.gama.experiment_state -ne 'NONE') {
  if ($state.gama.experiment_name -ne 'LetItBeeVR' -and $state.gama.experiment_name -ne 'BeeBloomVR') { throw 'Another experiment is running. It was left unchanged.' }
  Send-HoneyMonitor @{type='stop_experiment'}
  $deadline=[DateTime]::UtcNow.AddSeconds(25)
  do {
   $state=Read-HoneyMonitor
   if ([DateTime]::UtcNow -gt $deadline) { throw 'GAMA did not stop. No checkpoint was changed.' }
  } while ($state.gama.experiment_state -ne 'NONE')
 }
 # Remove stale headset records while GAMA is stopped. The next connection gets
 # a fresh admission instead of inheriting a previous headset's identity.
 foreach ($player in $players) { Send-HoneyMonitor @{type='remove_player_headset';id=$player.id} }
 # Read again only after GAMA has stopped; back up exactly that latest state.
 $saved=Get-Content -LiteralPath $checkpoint -Raw | ConvertFrom-Json
 if ($saved.state.session -cne $Session) { throw 'Checkpoint changed session; reset cancelled.' }
 $fresh=New-HoneyProgress $saved
 $backupFolder=Join-Path ([IO.Path]::GetDirectoryName($checkpoint)) 'backups'
 [IO.Directory]::CreateDirectory($backupFolder) | Out-Null
 $backup=Join-Path $backupFolder ($Session+'-before-reset-'+(Get-Date -Format 'yyyyMMdd-HHmmss-fff')+'-'+[Guid]::NewGuid().ToString('N').Substring(0,6)+'.json')
 if ($UseCurrentLayout) {
  [IO.File]::Move($checkpoint,$backup)
 } else {
  [IO.File]::Copy($checkpoint,$backup,$false)
  Write-HoneyCheckpoint $checkpoint ($fresh|ConvertTo-Json -Depth 64)
 }
 Write-Host "Backup: $backup"
 if ($UseCurrentLayout) { Write-Host 'Progress cleared. GAMA will accept geometry/tuning from the next Unity/APK connection.' }
 else { Write-Host 'Reset complete: both flowers have nectar, no carried pollen/chunk, hive empty. Saved geometry/tuning kept.' }
 Write-Host 'Next: launch LetItBeeVR in http://localhost:8000, then open Let It Bee and add the headset to the experiment.'
 Write-Host 'No Unity scene, prefab, Inspector value, connection IP or APK was changed.'
} finally { $socket.Dispose() }
