param([Parameter(Mandatory=$true)][ValidatePattern('^[0-9]{8}-[0-9]{4}$')][string]$Stamp)
$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$deliveryRoot = Join-Path $projectRoot 'Builds/Presentation'
Add-Type -AssemblyName System.IO.Compression.FileSystem
$manifest = @(Get-Content (Join-Path $deliveryRoot "handoff-sha256-$Stamp.json") -Raw | ConvertFrom-Json)
if ($manifest.Count -ne 2) { throw 'Expected playtest and source packages.' }
$checks = @()
foreach ($item in $manifest) {
    $archivePath = Join-Path $deliveryRoot $item.file
    if ((Get-Item $archivePath).Length -ne $item.bytes -or (Get-FileHash $archivePath -Algorithm SHA256).Hash -ne $item.sha256) { throw "Checksum mismatch: $($item.file)" }
    $archive = [IO.Compression.ZipFile]::OpenRead($archivePath)
    try {
        $names = @($archive.Entries.FullName)
        $required = @('docs/PRESENTATION_HANDOFF.md','docs/PRESENTATION_VERIFICATION.md','AUDIO_SOURCES.md','CREDITS.md','UPGRADE_AUDIT.md','tests/Results/presentation_acceptance_summary.json')
        if ($item.file -like '*Playtest*') {
            $required += @('Builds/Presentation/Windows/SaKabilangPinto.exe','Builds/Presentation/Windows/UnityPlayer.dll','Builds/Presentation/Windows/SaKabilangPinto_Data/Managed/mscorlib.dll','Builds/Presentation/Android/SaKabilangPinto-playtest.apk','Builds/Presentation/Windows/Licences/PressStart2P-OFL.txt','Builds/Presentation/Windows/Licences/LiberationSans - OFL.txt','Builds/Presentation/Windows/Licences/church_bell_CC-BY-SA-3.0.wav','Builds/Presentation/Windows/Licences/churchbell_original_CC-BY-SA-3.0.flac','Builds/Presentation/Windows/Licences/RECORDING_CREDITS.txt')
            if (-not ($names -like '*MonoBleedingEdge/*')) { throw 'Missing Windows Mono runtime.' }
        } else {
            $required += @('Assets/Scenes/Boot.unity','Packages/manifest.json','ProjectSettings/ProjectVersion.txt','Assets/Data/Player/TalaRecovered_girl.asset','Assets/Data/Player/TalaRecovered_boy.asset','Assets/Art/Fonts/PressStart2P-OFL.txt')
            if (-not ($names -like 'Sa-Kabilang-Pinto/Assets/Audio/Licensed/*') -or -not ($names -like 'Sa-Kabilang-Pinto/Assets/Art/Recovered/*.meta')) { throw 'Missing source art/audio metadata.' }
            if ($names -match '^Sa-Kabilang-Pinto/(Library|Builds|\.git)/') { throw 'Generated cache or repository data leaked into source archive.' }
        }
        foreach ($relative in $required) {
            $entry = $archive.GetEntry('Sa-Kabilang-Pinto/' + $relative)
            if (-not $entry -or $entry.Length -eq 0) { throw "Missing or empty package entry: $relative" }
        }
        $checks += [pscustomobject]@{file=$item.file;entries=$names.Count;bytes=$item.bytes;sha256=$item.sha256;requiredEntriesPassed=$true}
    } finally { $archive.Dispose() }
}
$checks | ConvertTo-Json -Depth 4 | Set-Content (Join-Path $projectRoot 'tests/Results/presentation_package_verification.json')
$checks | ConvertTo-Json -Depth 4
