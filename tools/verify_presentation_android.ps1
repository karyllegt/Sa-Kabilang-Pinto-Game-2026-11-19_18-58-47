param([string]$EditorRoot = 'C:/Program Files/Unity/Hub/Editor/6000.6.2f1')
$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$apk = Join-Path $projectRoot 'Builds/Presentation/Android/SaKabilangPinto-playtest.apk'
$androidTools = Join-Path $EditorRoot 'Editor/Data/PlaybackEngines/AndroidPlayer'
$buildTools = Join-Path $androidTools 'SDK/build-tools/36.0.0'
$priorJava = $env:JAVA_HOME
try {
    $env:JAVA_HOME = Join-Path $androidTools 'OpenJDK'
    $badging = @(& (Join-Path $buildTools 'aapt.exe') dump badging $apk 2>&1)
    if ($LASTEXITCODE -ne 0) { throw 'APK manifest inspection failed.' }
    $signature = @(& (Join-Path $buildTools 'apksigner.bat') verify --verbose $apk 2>&1)
    if ($LASTEXITCODE -ne 0) { throw 'APK signature verification failed.' }
    $manifestText = $badging -join "`n"
    foreach ($expected in @("name='com.pangkatparol.sakabilangpinto.playtest'", "sdkVersion:'26'", "targetSdkVersion:'36'", "native-code: 'arm64-v8a'")) {
        if (-not $manifestText.Contains($expected)) { throw "Unexpected APK configuration: missing $expected" }
    }
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $archive = [IO.Compression.ZipFile]::OpenRead($apk)
    try {
        $iconPaths = @([regex]::Matches($manifestText, "application-icon-[0-9]+:'([^']+\.xml)'" ) | ForEach-Object { $_.Groups[1].Value })
        $adaptive = @((@($archive.Entries.FullName | Where-Object { $_ -match '^res/mipmap-anydpi-v26/.+\.xml$' }) + $iconPaths) | Sort-Object -Unique)
    }
    finally { $archive.Dispose() }
    $layers = @()
    foreach ($entry in $adaptive) {
        $xmlTree = @(& (Join-Path $buildTools 'aapt.exe') dump xmltree $apk $entry 2>&1)
        if ($LASTEXITCODE -ne 0) { throw "Cannot inspect icon: $entry" }
        $layers += $xmlTree
    }
    $result = [ordered]@{
        apk='Builds/Presentation/Android/SaKabilangPinto-playtest.apk'
        bytes=(Get-Item $apk).Length
        sha256=(Get-FileHash $apk -Algorithm SHA256).Hash
        manifestVerified=$true
        signatureVerified=$true
        adaptiveResources=$adaptive
        adaptiveLayersVerified=(($layers -join "`n") -match 'E: foreground' -and ($layers -join "`n") -match 'E: background')
        physicalDeviceTested=$false
    }
    @('APK manifest:', $badging, 'Signature:', $signature, 'Adaptive resources:', $layers) | ForEach-Object { $_ } | Set-Content (Join-Path $projectRoot 'tests/Results/presentation_android_verification.txt')
    $result | ConvertTo-Json -Depth 4 | Set-Content (Join-Path $projectRoot 'tests/Results/presentation_android_verification.json')
    $result | ConvertTo-Json -Depth 4
} finally { $env:JAVA_HOME = $priorJava }
