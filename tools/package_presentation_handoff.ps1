param([ValidatePattern('^[0-9]{8}-[0-9]{4}$')][string]$Stamp = (Get-Date -Format 'yyyyMMdd-HHmm'))
$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$deliveryRoot = Join-Path $projectRoot 'Builds/Presentation'
if (Get-Process Unity -ErrorAction SilentlyContinue) { throw 'Close Unity before taking the final source/package snapshot.' }
$apk = Join-Path $deliveryRoot 'Android/SaKabilangPinto-playtest.apk'
$apkCheck = Get-Content (Join-Path $projectRoot 'tests/Results/presentation_android_verification.json') -Raw | ConvertFrom-Json
if (-not $apkCheck.signatureVerified -or -not $apkCheck.manifestVerified -or (Get-FileHash $apk -Algorithm SHA256).Hash -ne $apkCheck.sha256) {
    throw 'Verify the current Android APK signature and manifest before packaging.'
}
if (-not (Test-Path (Join-Path $projectRoot 'tests/Results/presentation_acceptance_summary.json'))) { throw 'Reconcile regression evidence before packaging.' }
Add-Type -AssemblyName System.IO.Compression.FileSystem
function Write-Package([string]$destination, [string[]]$files) {
    if (Test-Path -LiteralPath $destination) { throw "Package already exists: $destination. Choose a new dated name." }
    $zip = [IO.Compression.ZipFile]::Open($destination, [IO.Compression.ZipArchiveMode]::Create)
    try {
        foreach ($file in ($files | Sort-Object -Unique)) {
            $absolute = [IO.Path]::GetFullPath($file)
            if (-not $absolute.StartsWith($projectRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
                throw "Package source escaped the project: $absolute"
            }
            $entry = $absolute.Substring($projectRoot.Length + 1).Replace('\','/')
            [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $absolute, ('Sa-Kabilang-Pinto/' + $entry), [IO.Compression.CompressionLevel]::Fastest) | Out-Null
        }
    } finally { $zip.Dispose() }
    Write-Output "Created $destination"
}
$windows = Join-Path $deliveryRoot 'Windows/SaKabilangPinto.exe'
if (-not (Test-Path $windows)) { throw 'A successful Windows player is required before packaging.' }
foreach ($platform in @('Windows','Android')) {
    $platformRoot = Join-Path $deliveryRoot $platform
    if (-not (Test-Path $platformRoot)) { continue }
    $licences = Join-Path $platformRoot 'Licences'
    New-Item -ItemType Directory -Force $licences | Out-Null
    foreach ($notice in @('CREDITS.md','Assets/Art/Fonts/PressStart2P-OFL.txt','Assets/TextMesh Pro/Fonts/LiberationSans - OFL.txt')) {
        Copy-Item -LiteralPath (Join-Path $projectRoot $notice) -Destination $licences -Force
    }
}
$documents = @(Get-ChildItem -LiteralPath $projectRoot -Filter '*.md' -File) + @(Get-ChildItem (Join-Path $projectRoot 'docs') -Filter '*.md' -File -Recurse) + @(Get-ChildItem (Join-Path $projectRoot 'tests') -Filter '*.md' -File -Recurse)
$evidence = @(Get-ChildItem (Join-Path $projectRoot 'tests/Screenshots/presentation') -File) +
    @(Get-ChildItem (Join-Path $projectRoot 'tests/Screenshots/presentation/ui') -Filter 'contact_sheet*.png' -File -Recurse) +
    @(Get-ChildItem (Join-Path $projectRoot 'tests/Results') -Filter 'presentation*' -File | Where-Object { $_.Name -ne 'presentation_package_verification.json' })
$playable = @(Get-ChildItem (Join-Path $deliveryRoot 'Windows') -File -Recurse)
if (Test-Path (Join-Path $deliveryRoot 'Android/SaKabilangPinto-playtest.apk')) {
    $playable += @(Get-ChildItem (Join-Path $deliveryRoot 'Android') -File -Recurse)
}
Write-Package (Join-Path $deliveryRoot ("SaKabilangPinto-Playtest-$stamp.zip")) (($playable + $documents + $evidence).FullName)
$sources = @()
foreach ($directory in @('Assets','Packages','ProjectSettings','reference/audio-licenses')) {
    $sources += @(Get-ChildItem (Join-Path $projectRoot $directory) -File -Recurse)
}
$sources += @(Get-ChildItem (Join-Path $projectRoot 'tools') -File -Recurse | Where-Object {
    $_.FullName -notlike '*\media-codec\package\*' -and $_.Extension -in @('.ps1','.cs','.py','.md','.json')
})
$sources += @(Get-Item (Join-Path $projectRoot '.gitignore'),(Join-Path $projectRoot '.gitattributes'))
Write-Package (Join-Path $deliveryRoot ("SaKabilangPinto-UnitySource-$stamp.zip")) (($sources + $documents + $evidence).FullName)
$hashes = Get-ChildItem $deliveryRoot -Filter "*-$stamp.zip" | ForEach-Object {
    [pscustomobject]@{file=$_.Name;bytes=$_.Length;sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash}
}
$hashes | ConvertTo-Json | Set-Content (Join-Path $deliveryRoot ("handoff-sha256-$stamp.json"))
Write-Output ($hashes | ConvertTo-Json)
