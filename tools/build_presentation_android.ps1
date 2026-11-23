param(
    [string]$EditorRoot = 'C:/Program Files/Unity/Hub/Editor/6000.6.2f1',
    [string]$EditorVersion = '6000.6.2f1'
)
$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$copyRoot = [IO.Path]::GetFullPath((Join-Path $projectRoot 'Builds/Presentation/AndroidSource'))
if (-not $copyRoot.StartsWith($projectRoot + [IO.Path]::DirectorySeparatorChar)) { throw 'Build copy escaped workspace' }
$editorPath = Join-Path $EditorRoot 'Editor/Unity.exe'
$androidTools = Join-Path $EditorRoot 'Editor/Data/PlaybackEngines/AndroidPlayer'
if (-not (Test-Path $editorPath) -or -not (Test-Path $androidTools)) { throw 'Android-capable editor required' }
New-Item -ItemType Directory -Force $copyRoot | Out-Null
Set-Content (Join-Path $projectRoot 'tests/Results/presentation_active_phase.txt') 'Preparing isolated Android source copy.'
foreach ($directory in @('Assets','Packages','ProjectSettings')) {
    & robocopy (Join-Path $projectRoot $directory) (Join-Path $copyRoot $directory) /E /R:1 /W:1 /NFL /NDL /NJH /NJS
    if ($LASTEXITCODE -gt 7) { throw "Source copy failed: $directory" }
}
# Only this disposable build copy uses the installed Android-capable patch version.
[IO.File]::WriteAllText((Join-Path $copyRoot 'ProjectSettings/ProjectVersion.txt'), "m_EditorVersion: $EditorVersion`n")
$copyLibrary = Join-Path $copyRoot 'Library'
New-Item -ItemType Directory -Force $copyLibrary | Out-Null
# Let this editor resolve its own generated package cache. Sharing it across editor patches
# can leave the other project's source/assembly index stale.
$packageLink = Join-Path $copyLibrary 'PackageCache'
$cacheItem = Get-Item -LiteralPath $packageLink -Force -ErrorAction SilentlyContinue
if ($cacheItem -and $cacheItem.LinkType -eq 'Junction') {
    $preservedLink = Join-Path $projectRoot ('Builds/PackageRepair/AndroidCacheLink-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
    New-Item -ItemType Directory -Force (Split-Path $preservedLink) | Out-Null
    [IO.Directory]::Move($packageLink, $preservedLink)
}
$env:SKP_ANDROID_SDK = Join-Path $androidTools 'SDK'
$env:SKP_ANDROID_NDK = Join-Path $androidTools 'NDK'
$env:SKP_ANDROID_JDK = Join-Path $androidTools 'OpenJDK'
$logPath = Join-Path $projectRoot 'Logs/PresentationAndroid.log'
$arguments = @('-batchmode','-nographics','-quit','-projectPath',('"' + $copyRoot + '"'),'-buildTarget','Android',
    '-executeMethod','SkpHandoffBuild.Android','-force-d3d11','--burst-disable-compilation',
    '-job-worker-count','2','-desiredWorkerCount','1','-logFile',('"' + $logPath + '"'))
$process = Start-Process -FilePath $editorPath -ArgumentList $arguments -WindowStyle Hidden -PassThru
Set-Content (Join-Path $projectRoot 'tests/Results/presentation_active_phase.txt') "Android build PID $($process.Id)"
$process.WaitForExit()
if ($process.ExitCode -ne 0) { throw "Android build editor exited $($process.ExitCode); inspect $logPath" }
$builtApk = Join-Path $copyRoot 'Builds/Presentation/Android/SaKabilangPinto-playtest.apk'
if (-not (Test-Path $builtApk)) { throw 'Editor returned without producing the APK' }
$deliveryRoot = Join-Path $projectRoot 'Builds/Presentation/Android'
New-Item -ItemType Directory -Force $deliveryRoot | Out-Null
Copy-Item -LiteralPath $builtApk -Destination (Join-Path $deliveryRoot 'SaKabilangPinto-playtest.apk') -Force
$resultsRoot = Join-Path $projectRoot 'tests/Results'
New-Item -ItemType Directory -Force $resultsRoot | Out-Null
Copy-Item -LiteralPath (Join-Path $copyRoot 'tests/Results/presentation_android_build.txt') -Destination $resultsRoot -Force
$licences = Join-Path $deliveryRoot 'Licences'
New-Item -ItemType Directory -Force $licences | Out-Null
foreach ($file in @('AUDIO_SOURCES.md','CREDITS.md','Assets/Audio/Licensed/Foley/LICENSE.txt',
    'Assets/Audio/Licensed/Foley/RECORDING_CREDITS.txt','Assets/Audio/Licensed/Music/ATTRIBUTION.txt',
    'Assets/Audio/Licensed/Foley/church_bell.wav','reference/audio-licenses/churchbell.flac')) {
    Copy-Item -LiteralPath (Join-Path $projectRoot $file) -Destination $licences -Force
}
Write-Output "Android APK prepared with $EditorVersion from isolated source copy: $deliveryRoot"
