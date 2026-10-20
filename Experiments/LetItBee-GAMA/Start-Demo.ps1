param([switch]$Headless, [switch]$SelfTest, [switch]$Capture)
$ErrorActionPreference = 'Stop'
$port = if ($Headless -or $SelfTest) { 6871 } else { 6872 }
$exe = Join-Path $PSScriptRoot 'Build/LetItBeeGamaDemo.exe'
if (!(Test-Path -LiteralPath $exe)) { throw 'Run Build-Demo.ps1 first.' }
if (!$SelfTest -and (Test-Path (Join-Path $PSScriptRoot 'Runtime/unity-player.pid'))) {
    $existingPid = [int](Get-Content (Join-Path $PSScriptRoot 'Runtime/unity-player.pid'))
    $existingPlayer = Get-Process -Id $existingPid -ErrorAction SilentlyContinue
    if ($existingPlayer -and $existingPlayer.Path -eq $exe) {
        Write-Host "Unity demo is already running (PID $existingPid). Use its existing window."
        return
    }
}
$tcp = [System.Net.Sockets.TcpClient]::new()
$alive = $false
try { $tcp.Connect('127.0.0.1',$port); $alive=$true } catch [System.Net.Sockets.SocketException] { } finally { $tcp.Dispose() }
if (!$alive) {
    if ($port -eq 6871) { & (Join-Path $PSScriptRoot 'Start-GamaServer.ps1') }
    else { & (Join-Path $PSScriptRoot 'Start-GamaDesktop.ps1') }
    $deadline = [DateTime]::UtcNow.AddSeconds(90)
    do {
        Start-Sleep -Milliseconds 500
        $tcp = [System.Net.Sockets.TcpClient]::new()
        try { $tcp.Connect('127.0.0.1',$port); $alive=$true } catch [System.Net.Sockets.SocketException] { } finally { $tcp.Dispose() }
    } until ($alive -or [DateTime]::UtcNow -gt $deadline)
    if (!$alive) { throw 'GAMA did not start. See Runtime logs.' }
}
$runtime = Join-Path $PSScriptRoot 'Runtime'
$logName = if ($SelfTest) { 'unity-self-test.log' } else { 'unity-player.log' }
$arguments = @('-demoRoot',('"'+$PSScriptRoot+'"'),'-gamaUrl',"ws://127.0.0.1:$port",'-logFile',('"'+(Join-Path $runtime $logName)+'"'))
if ($SelfTest) { $arguments += @('-selfTest','-batchmode','-nographics') }
if ($Capture) { $arguments += '-capture' }
if ($SelfTest) {
    $process = Start-Process $exe -ArgumentList $arguments -WindowStyle Hidden -PassThru
    if (!$process.WaitForExit(120000)) { $process.Kill(); throw 'Demo test timed out after 120 seconds. See Runtime/unity-player.log.' }
    Get-Content (Join-Path $runtime 'unity-test-result.json')
    if ($process.ExitCode -ne 0) { throw 'Unity-GAMA self-test failed.' }
} else {
    # Visible application requested by the user.
    $process = Start-Process $exe -ArgumentList $arguments -PassThru
    $process.Id | Set-Content (Join-Path $runtime 'unity-player.pid')
    Write-Host "Unity demo PID $($process.Id). GAMA port $port. Close the Unity demo window when finished."
}
