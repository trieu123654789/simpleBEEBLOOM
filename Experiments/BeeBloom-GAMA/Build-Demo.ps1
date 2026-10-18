param([string]$UnityExe = 'C:\Program Files\Unity\Hub\Editor\6000.6.2f1\Editor\Unity.exe')
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$plugins = Join-Path $PSScriptRoot 'UnityDemo/Assets/Plugins'
New-Item -ItemType Directory -Force $plugins,(Join-Path $PSScriptRoot 'Runtime') | Out-Null
$json = Get-ChildItem (Join-Path $repo 'Library/PackageCache') -Directory -Filter 'com.unity.nuget.newtonsoft-json@*' | Select-Object -First 1
if (!$json) { throw 'Open the main project once to restore its existing Newtonsoft package, then retry.' }
Copy-Item -LiteralPath (Join-Path $json.FullName 'Runtime/Newtonsoft.Json.dll') -Destination $plugins
Copy-Item -LiteralPath (Join-Path $repo 'Assets/GAMA_Resources/Plugins/websocket-sharp.dll') -Destination $plugins
$argsBuild = @('-batchmode','-nographics','-quit','-projectPath',('"'+$PSScriptRoot+'\UnityDemo"'),'-executeMethod','DemoBuild.Build','-logFile',('"'+$PSScriptRoot+'\Runtime\unity-build.log"'))
$process = Start-Process $UnityExe -ArgumentList $argsBuild -WindowStyle Hidden -PassThru -Wait
if ($process.ExitCode -ne 0) { throw "Unity build failed ($($process.ExitCode)). See Runtime/unity-build.log" }
Write-Host "Built: $PSScriptRoot\Build\BeeBloomGamaDemo.exe"
