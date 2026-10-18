param([switch]$HeadlessGama)
$ErrorActionPreference = 'Stop'
# Only stop processes recorded by this package. Never close the user's GAMA GUI.
$targets = @(
 @{ File='Runtime/simple-platform/platform.pid'; Name='node'; Marker='simple-compat.mjs' },
 @{ File='Runtime/simple-platform/web.pid'; Name='node'; Marker='vite.config.mjs' }
)
if ($HeadlessGama) { $targets += @{ File='Runtime/simple-gama/server.pid'; Name='java'; Marker='Runtime\simple-gama' } }
foreach ($target in $targets) {
 $record = Join-Path $PSScriptRoot $target.File
 if (!(Test-Path -LiteralPath $record)) { continue }
 $processId = [int](Get-Content -LiteralPath $record)
 $process = Get-Process -Id $processId -ErrorAction SilentlyContinue
 if (!$process) { continue }
 $command = (Get-CimInstance Win32_Process -Filter "ProcessId=$processId").CommandLine
 $sameStart = [Math]::Abs(($process.StartTime - (Get-Item -LiteralPath $record).LastWriteTime).TotalSeconds) -lt 30
 if ($process.ProcessName -ne $target.Name -or !$sameStart -or !$command -or !$command.Contains($target.Marker)) {
  throw "PID $processId no longer matches this launcher. It was left running."
 }
 $process.Kill()
 if (!$process.WaitForExit(5000)) { throw "PID $processId did not stop within 5 seconds." }
 Write-Host "Stopped $($target.Name) PID $processId"
}
