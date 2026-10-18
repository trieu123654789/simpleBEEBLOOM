param([int]$Port = 6871, [string]$GamaHome = 'C:\Program Files\Gama')
$ErrorActionPreference = 'Stop'
$runtime = Join-Path $PSScriptRoot 'Runtime'
$config = Join-Path $runtime 'headless-config'
New-Item -ItemType Directory -Force $runtime,$config | Out-Null
# Shared installation is read-only. All caches/workspaces/logs stay inside this demo.
$shared = (Join-Path $GamaHome 'headless/configuration').Replace('\','/')
@("osgi.sharedConfiguration.area=file:/$shared/", 'osgi.configuration.cascaded=true') | Set-Content (Join-Path $config 'config.ini')
$launcher = Get-ChildItem (Join-Path $GamaHome 'plugins') -Filter 'org.eclipse.equinox.launcher_*.jar' | Select-Object -First 1
$ini = Get-Content (Join-Path $GamaHome 'Gama.ini')
$vm = @($ini | Where-Object { $_ -like '--add-*' })
$arguments = @('-Xms256m','-Xmx2g','-Djava.awt.headless=true','-Duse_global_preference_store=false') + $vm + @(
    '-cp', ('"' + $launcher.FullName + '"'), 'org.eclipse.equinox.launcher.Main',
    '-install', ('"' + $GamaHome + '"'), '-configuration', ('"' + $config + '"'),
    '-application', 'gama.headless.product', '-data', ('"' + (Join-Path $runtime 'gama-workspace') + '"'),
    '-socket', "$Port")
$listener = [System.Net.Sockets.TcpClient]::new()
try { $listener.Connect('127.0.0.1',$Port); throw "Port $Port is already occupied. No process was changed." }
catch [System.Net.Sockets.SocketException] { }
finally { $listener.Dispose() }
$process = Start-Process (Join-Path $GamaHome 'jdk/bin/java.exe') -ArgumentList $arguments -WorkingDirectory $runtime -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $runtime 'gama-server.log') -RedirectStandardError (Join-Path $runtime 'gama-server-error.log')
$process.Id | Set-Content (Join-Path $runtime 'gama-server.pid')
Write-Host "GAMA demo PID $($process.Id), ws://127.0.0.1:$Port. Logs: $runtime"
