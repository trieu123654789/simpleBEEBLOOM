param(
 [string]$WebPlatform,
 [int]$GamaPort,
 [string]$ConfigPath = (Join-Path $PSScriptRoot 'machine.local.json'),
 [switch]$Headless
)
$ErrorActionPreference = 'Stop'
$machine=@{}
if (Test-Path -LiteralPath $ConfigPath) {
 $json=Get-Content -LiteralPath $ConfigPath -Raw | ConvertFrom-Json
 foreach($property in $json.PSObject.Properties) { $machine[$property.Name]=$property.Value }
}
function Resolve-MachinePath([string]$value) {
 if ([IO.Path]::IsPathRooted($value)) { return [IO.Path]::GetFullPath($value) }
 return [IO.Path]::GetFullPath((Join-Path (Split-Path -Parent ([IO.Path]::GetFullPath($ConfigPath))) $value))
}
if (!$WebPlatform) {
 if (!$machine.webPlatformPath) { throw 'Copy machine.example.json to machine.local.json and set webPlatformPath first (or pass -WebPlatform).' }
 $WebPlatform=Resolve-MachinePath $machine.webPlatformPath
} else { $WebPlatform=[IO.Path]::GetFullPath($WebPlatform) }
if (!$GamaPort) { $GamaPort=if($machine.gamaPort){[int]$machine.gamaPort}else{1000} }
$gamaHost=if($machine.gamaHost){[string]$machine.gamaHost}else{'127.0.0.1'}
if ($gamaHost -notmatch '^[A-Za-z0-9.-]+$' -or $GamaPort -lt 1 -or $GamaPort -gt 65535) { throw 'Invalid GAMA host/port.' }
$node=if($machine.nodePath){Resolve-MachinePath $machine.nodePath}else{(Get-Command node -ErrorAction Stop).Source}
$runtime = Join-Path $PSScriptRoot 'Runtime/simple-platform'
New-Item -ItemType Directory -Force $runtime | Out-Null
# The official dev runtime resolves toolkit assets relative to its working
# directory. Our .env lives here, so mirror the official bundled files here too.
$runtimeToolkit=Join-Path $runtime 'toolkit'
New-Item -ItemType Directory -Force $runtimeToolkit | Out-Null
foreach($assetName in @('scrcpyServer-v3.3.4-rom1v','tdg.oculuswirelessadb.apk','eu.project_simple.adbautoenable.apk','eu.project_simple.no-loft.apk')) {
 $source=Join-Path (Join-Path $WebPlatform 'toolkit') $assetName
 if (Test-Path -LiteralPath $source) { Copy-Item -LiteralPath $source -Destination (Join-Path $runtimeToolkit $assetName) -Force }
 else { Write-Warning "Official SIMPLE toolkit asset missing: $source. Headset setup/streaming may be unavailable; gameplay services can still start." }
}
$ownedRunning = 0
foreach ($entry in @(@{File='platform.pid';Marker='simple-compat.mjs'}, @{File='web.pid';Marker='vite.config.mjs'})) {
 $record = Join-Path $runtime $entry.File
 if (!(Test-Path -LiteralPath $record)) { continue }
 $processId = [int](Get-Content -LiteralPath $record)
 $process = Get-Process -Id $processId -ErrorAction SilentlyContinue
 if (!$process) { continue }
 $command = (Get-CimInstance Win32_Process -Filter "ProcessId=$processId").CommandLine
 if ($process.ProcessName -eq 'node' -and $command -and $command.Contains($entry.Marker) -and
     [Math]::Abs(($process.StartTime - (Get-Item -LiteralPath $record).LastWriteTime).TotalSeconds) -lt 30) { $ownedRunning++ }
}
if ($ownedRunning -eq 2) {
 Write-Host 'SIMPLE is already running: http://localhost:8000. Use Stop-Simple.ps1 before changing GUI/headless mode.'
 return
}
if ($Headless) {
 $GamaPort=if($machine.headlessPort){[int]$machine.headlessPort}else{6875}
 if ($gamaHost -ne '127.0.0.1' -and $gamaHost -ne 'localhost') { throw 'Headless launcher starts a local GAMA server; use gamaHost=127.0.0.1.' }
 $gamaHome=if($machine.gamaHome){Resolve-MachinePath $machine.gamaHome}else{Join-Path $env:ProgramFiles 'Gama'}
 & (Join-Path $PSScriptRoot 'Start-SimpleGamaServer.ps1') -Port $GamaPort -GamaHome $gamaHome
}
if (!(Test-Path -LiteralPath $node)) { throw 'Node.js is required.' }
if (!(Test-Path -LiteralPath (Join-Path $WebPlatform 'node_modules/tsx/dist/loader.mjs'))) { throw 'Install the official SIMPLE WebPlatform and its npm dependencies first.' }
foreach ($port in @(8000,8001,8080)) {
 $probe = [System.Net.Sockets.TcpClient]::new()
 try { $probe.Connect('127.0.0.1',$port); throw "Port $port is already occupied. Existing applications were left running." }
 catch [System.Net.Sockets.SocketException] { }
 finally { $probe.Dispose() }
}
$package = $PSScriptRoot.Replace('\','/')
@("GAMA_WS_PORT=$GamaPort","GAMA_IP_ADDRESS=$gamaHost",'HEADSET_WS_PORT=8080','MONITOR_WS_PORT=8001',
 'WEB_APPLICATION_HOST=0.0.0.0','WEB_APPLICATION_PORT=8000',"LEARNING_PACKAGE_PATH=`"$package`"",'EXTRA_LEARNING_PACKAGE_PATH=',
 'AGGRESSIVE_DISCONNECT=true','ENV_GAMALESS=false','VERBOSE=false','EXTRA_VERBOSE=false') | Set-Content (Join-Path $runtime '.env')
$reactUri = ([Uri](Join-Path $WebPlatform 'node_modules/@vitejs/plugin-react/dist/index.js')).AbsoluteUri
$tailwindUri = ([Uri](Join-Path $WebPlatform 'node_modules/tailwindcss/lib/index.js')).AbsoluteUri
$autoprefixerUri = ([Uri](Join-Path $WebPlatform 'node_modules/autoprefixer/lib/autoprefixer.js')).AbsoluteUri
$tailwindConfigUri = ([Uri](Join-Path $WebPlatform 'tailwind.config.js')).AbsoluteUri
$reactImport=$reactUri | ConvertTo-Json -Compress
$tailwindImport=$tailwindUri | ConvertTo-Json -Compress
$autoprefixerImport=$autoprefixerUri | ConvertTo-Json -Compress
$tailwindConfigImport=$tailwindConfigUri | ConvertTo-Json -Compress
$platformPath = $WebPlatform.Replace('\','/')
$cachePath = (Join-Path $runtime 'vite-cache').Replace('\','/')
$platformJson=$platformPath | ConvertTo-Json -Compress
$cacheJson=$cachePath | ConvertTo-Json -Compress
$contentJson=@("$platformPath/index.html","$platformPath/src/**/*.{js,ts,jsx,tsx}") | ConvertTo-Json -Compress
@"
import react from $reactImport;
import tailwindcss from $tailwindImport;
import autoprefixer from $autoprefixerImport;
import tailwindConfig from $tailwindConfigImport;
import { createRequire } from 'node:module';
// Excluded dependencies remain bare imports in optimized decoder chunks. The
// cache lives outside WebPlatform, so resolve them from its installed packages.
const platformRequire = createRequire($platformJson + '/package.json');
const streamingAliases = ['@yume-chan/adb-scrcpy', '@yume-chan/stream-extra'].map(name => ({
 find: name, replacement: platformRequire.resolve(name)
}));
export default {
 root: $platformJson, cacheDir: $cacheJson, plugins: [react()],
 resolve: { alias: streamingAliases },
 css: { postcss: { plugins: [tailwindcss({ ...tailwindConfig,
   content: $contentJson
 }), autoprefixer()] } },
 server: { host: '0.0.0.0', port: 8000, strictPort: true },
 optimizeDeps: { exclude: ['@yume-chan/adb-scrcpy','@yume-chan/stream-extra'] },
 define: { 'process.env.MONITOR_WS_PORT': '8001', 'process.env.HEADSETS_IP': 'undefined',
   'process.env.ENV_MAX_ELEMENTS': 'undefined', 'process.env.IMAGE_SOURCE_FOLDER': 'undefined' }
};
"@ | Set-Content (Join-Path $runtime 'vite.config.mjs')
$loader = ([Uri](Join-Path $WebPlatform 'node_modules/tsx/dist/loader.mjs')).AbsoluteUri
$connectorUri = ([Uri](Join-Path $WebPlatform 'src/api/simulation/GamaConnector.ts')).AbsoluteUri
$connectorImport=$connectorUri | ConvertTo-Json -Compress
$playersImport=([Uri](Join-Path $WebPlatform 'src/api/multiplayer/PlayerManager.ts')).AbsoluteUri | ConvertTo-Json -Compress
$heartbeatImport=([Uri](Join-Path $PSScriptRoot 'SimpleHeartbeatCompat.mjs')).AbsoluteUri | ConvertTo-Json -Compress
$isHeadless = $Headless.IsPresent.ToString().ToLowerInvariant()
@"
// WebPlatform 4.0 compatibility: GUI load replies with an experiment name,
// not a headless experiment id. GUI commands use its active experiment;
// headless commands require the experiment id returned by load.
import GamaConnector from $connectorImport;
import PlayerManager from $playersImport;
import { installHeartbeatCleanup } from $heartbeatImport;
installHeartbeatCleanup(PlayerManager);
const headless = $isHeadless;
const connectGama = GamaConnector.prototype.connectGama;
GamaConnector.prototype.connectGama = function () {
 connectGama.call(this);
 const socket = this.gama_socket;
 if (headless || !socket || socket.__beeBloomGuiReady) return;
 socket.__beeBloomGuiReady = true;
 const receive = socket.onmessage;
 socket.onmessage = event => {
  const message = JSON.parse(event.data.toString());
  if (message.type === 'CommandExecutedSuccessfully' && message.command?.type === 'load') {
   this.setGamaExperimentName(message.command.experiment);
   this.setGamaExperimentId('0');
   this.setGamaContentError('');
   // GUI ACK precedes simulation init. Wait for native SimulationStatus,
   // otherwise players get marked in-game before unity_linker exists.
   return;
  }
  receive(event);
 };
};
const sendMessages = GamaConnector.prototype.sendMessages;
GamaConnector.prototype.sendMessages = function (callback) {
 const route = (message) => {
  const result = { ...message };
  if (result.type !== 'load') {
   if (headless) result.exp_id = this.jsonGamaState.experiment_id;
   else { delete result.exp_id; delete result.socket_id; }
  }
  return result;
 };
 this.listMessages = this.listMessages.map(message => typeof message === 'function' ? () => route(message()) : route(message));
 return sendMessages.call(this, callback);
};
"@ | Set-Content (Join-Path $runtime 'simple-compat.mjs')
$compat = ([Uri](Join-Path $runtime 'simple-compat.mjs')).AbsoluteUri
$api = Join-Path $WebPlatform 'src/api/index.ts'
$adbCommand=Get-Command adb -ErrorAction SilentlyContinue
$adbExe=if($machine.adbPath){Resolve-MachinePath $machine.adbPath}elseif($adbCommand){$adbCommand.Source}else{$null}
if (!$adbExe) {
 $candidate=Get-ChildItem -Path "$env:ProgramFiles/Unity/Hub/Editor/*/Editor/Data/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb.exe" -File -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 1
 if($candidate){$adbExe=$candidate.FullName}
}
$previousPath=$env:Path
try {
 if($adbExe){$env:Path=(Split-Path -Parent $adbExe)+';'+$previousPath}
 $backend = Start-Process $node -ArgumentList @('--import', $loader, '--import', $compat, ('"'+$api+'"')) -WorkingDirectory $runtime -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $runtime 'platform.log') -RedirectStandardError (Join-Path $runtime 'platform-error.log')
} finally {$env:Path=$previousPath}
$backend.Id | Set-Content (Join-Path $runtime 'platform.pid')
$vite = Join-Path $WebPlatform 'node_modules/vite/bin/vite.js'
$frontend = Start-Process $node -ArgumentList @(('"'+$vite+'"'),'--config',('"'+(Join-Path $runtime 'vite.config.mjs')+'"'),'--configLoader','native') -WorkingDirectory $runtime -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $runtime 'web.log') -RedirectStandardError (Join-Path $runtime 'web-error.log')
$frontend.Id | Set-Content (Join-Path $runtime 'web.pid')
$deadline = (Get-Date).AddSeconds(45)
do {
 $ready = 0
 foreach ($port in @(8000,8001,8080)) {
  $probe = [System.Net.Sockets.TcpClient]::new()
  try { $probe.Connect('127.0.0.1',$port); $ready++ } catch [System.Net.Sockets.SocketException] { }
  finally { $probe.Dispose() }
 }
 if ($ready -eq 3) { break }
 if ($backend.HasExited -or $frontend.HasExited) { throw "SIMPLE failed to start. Read logs in $runtime" }
 Start-Sleep -Milliseconds 250
} while ((Get-Date) -lt $deadline)
if ($ready -ne 3) { throw "SIMPLE startup timed out. Read logs in $runtime" }
Write-Host "SIMPLE WebPlatform: http://localhost:8000 | Quest: ws://<PC LAN IP>:8080 | GAMA port $GamaPort"
Write-Host 'Select Let It Bee - Honey Pilot and launch LetItBeeVR in WebPlatform. For GUI mode, open GAMA first.'
