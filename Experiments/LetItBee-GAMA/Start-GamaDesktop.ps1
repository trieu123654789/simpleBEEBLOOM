param([int]$Port = 6872, [string]$GamaHome = 'C:\Program Files\Gama')
$ErrorActionPreference = 'Stop'
$runtime = Join-Path $PSScriptRoot 'Runtime'
$config = Join-Path $runtime 'desktop-config'
New-Item -ItemType Directory -Force $runtime,$config | Out-Null
$workspace = Join-Path $runtime 'gama-view-workspace'
New-Item -ItemType Directory -Force $workspace | Out-Null
New-Item -ItemType File -Force (Join-Path $workspace '.gama_application_workspace') | Out-Null
# GAMA stamps the extracted built-in library, not the source jar.
# On a fresh installation GAMA may ask once to initialize that library.
$models = Get-ChildItem (Join-Path $config 'org.eclipse.osgi') -Directory -Recurse -Filter models -ErrorAction SilentlyContinue | Where-Object { $_.Parent.Name -eq '.cp' -and (Test-Path (Join-Path $_.FullName 'Tutorials')) } | Select-Object -First 1
if ($models) {
    $stamp = ([DateTimeOffset]$models.LastWriteTimeUtc).ToUnixTimeMilliseconds()
    New-Item -ItemType File -Force (Join-Path $workspace ".built_in_models_$stamp") | Out-Null
}
$shared = (Join-Path $GamaHome 'configuration').Replace('\','/')
@("osgi.sharedConfiguration.area=file:/$shared/", 'osgi.configuration.cascaded=true') | Set-Content (Join-Path $config 'config.ini')
$prefsDir = Join-Path $config '.settings'
New-Item -ItemType Directory -Force $prefsDir | Out-Null
@('eclipse.preferences.version=1',"pref_server_port=$Port",'pref_enable_server=true','pref_server_no_delay=false','pref_experiment_ask_closing=false') | Set-Content (Join-Path $prefsDir 'gama.prefs')
$launcher = Get-ChildItem (Join-Path $GamaHome 'plugins') -Filter 'org.eclipse.equinox.launcher_*.jar' | Select-Object -First 1
$vm = @(Get-Content (Join-Path $GamaHome 'Gama.ini') | Where-Object { $_ -like '--add-*' })
$arguments = @('-Xms256m','-Xmx2g','-Duse_global_preference_store=false',"-Dpref_server_port=$Port",'-Dpref_enable_server=true','-Dpref_experiment_ask_closing=false', '-Dpref_workspace_remember=true', ('"-Dpref_workspace_path='+$workspace+'"')) + $vm + @(
    '-cp', ('"' + $launcher.FullName + '"'), 'org.eclipse.equinox.launcher.Main',
    '-install', ('"' + $GamaHome + '"'), '-configuration', ('"' + $config + '"'),
    '-data', '@noDefault')
$listener = [System.Net.Sockets.TcpClient]::new()
try { $listener.Connect('127.0.0.1',$Port); throw "Port $Port is already occupied. No process was changed." }
catch [System.Net.Sockets.SocketException] { }
finally { $listener.Dispose() }
# This is the visible GAMA half of the requested interactive demo.
$process = Start-Process (Join-Path $GamaHome 'jdk/bin/javaw.exe') -ArgumentList $arguments -WorkingDirectory $runtime -PassThru -RedirectStandardOutput (Join-Path $runtime 'gama-desktop.log') -RedirectStandardError (Join-Path $runtime 'gama-desktop-error.log')
$process.Id | Set-Content (Join-Path $runtime 'gama-desktop.pid')
Write-Host "Demo GAMA desktop PID $($process.Id), port $Port. Existing GAMA workspace untouched."
