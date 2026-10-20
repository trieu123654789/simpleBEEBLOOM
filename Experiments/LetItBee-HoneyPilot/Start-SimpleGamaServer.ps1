param([int]$Port = 6875, [string]$GamaHome = (Join-Path $env:ProgramFiles 'Gama'))
$ErrorActionPreference = 'Stop'
$runtime = Join-Path $PSScriptRoot 'Runtime/simple-gama'
$config = Join-Path $runtime 'config'
New-Item -ItemType Directory -Force $runtime,$config | Out-Null
$probe = [System.Net.Sockets.TcpClient]::new()
try { $probe.Connect('127.0.0.1',$Port); Write-Host "GAMA port $Port already listening; no process changed."; return }
catch [System.Net.Sockets.SocketException] { }
finally { $probe.Dispose() }
$plugin = Get-ChildItem -Path "$env:USERPROFILE/.eclipse/*/plugins/gaml.extension.unity_*.jar" -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 1
if (!$plugin) { throw 'Install the GAMA LinkToUnity plugin first. No installation was changed.' }
$bundleDir = Join-Path $config 'org.eclipse.equinox.simpleconfigurator'
New-Item -ItemType Directory -Force $bundleDir | Out-Null
$bundles = Get-Content (Join-Path $GamaHome 'configuration/org.eclipse.equinox.simpleconfigurator/bundles.info')
$version = $plugin.BaseName.Substring('gaml.extension.unity_'.Length)
$bundles += "gaml.extension.unity,$version,$([Uri]$plugin.FullName),4,false"
$bundlePath = Join-Path $bundleDir 'bundles.info'
$bundles | Set-Content -LiteralPath $bundlePath
@("osgi.sharedConfiguration.area=$([Uri](Join-Path $GamaHome 'configuration'))/", 'osgi.configuration.cascaded=true',
 "org.eclipse.equinox.simpleconfigurator.configUrl=$([Uri]$bundlePath)") | Set-Content (Join-Path $config 'config.ini')
$launcher = Get-ChildItem (Join-Path $GamaHome 'plugins') -Filter 'org.eclipse.equinox.launcher_*.jar' | Select-Object -First 1
$vm = @(Get-Content (Join-Path $GamaHome 'Gama.ini') | Where-Object { $_ -like '--add-*' })
$arguments = @('-Xms256m','-Xmx2g','-Djava.awt.headless=true','-Duse_global_preference_store=false') + $vm + @(
 '-cp', ('"'+$launcher.FullName+'"'), 'org.eclipse.equinox.launcher.Main',
 '-install', ('"'+$GamaHome+'"'), '-configuration', ('"'+$config+'"'),
 '-application', 'gama.headless.product', '-data', ('"'+(Join-Path $runtime 'workspace')+'"'), '-socket', "$Port")
$process = Start-Process (Join-Path $GamaHome 'jdk/bin/java.exe') -ArgumentList $arguments -WorkingDirectory $runtime -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $runtime 'server.log') -RedirectStandardError (Join-Path $runtime 'error.log')
$process.Id | Set-Content (Join-Path $runtime 'server.pid')
Write-Host "SIMPLE GAMA + LinkToUnity PID $($process.Id), ws://127.0.0.1:$Port"
