$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$codecPath = Join-Path $PSScriptRoot 'package/ffmpeg.exe'
$musicRoot = Join-Path $projectRoot 'Assets/Audio/Licensed/Music'
$loopRoot = Join-Path $musicRoot 'Loops'
if (-not (Test-Path -LiteralPath $codecPath)) { throw 'The local FFmpeg tool is required; see AUDIO_SOURCES.md.' }
New-Item -ItemType Directory -Path $loopRoot -Force | Out-Null
Add-Type -Path (Join-Path $PSScriptRoot 'MusicLoop.cs')
$decodedRoot = Join-Path $projectRoot 'reference/licensed-audio/decoded'
New-Item -ItemType Directory -Path $decodedRoot -Force | Out-Null
foreach ($track in Get-ChildItem -LiteralPath $musicRoot -Filter '*.mp3') {
    $loopPath = Join-Path $loopRoot ($track.BaseName + '_loop.wav')
    $decodedPath = Join-Path $decodedRoot ($track.BaseName + '.wav')
    & $codecPath -hide_banner -loglevel error -y -i $track.FullName -ar 44100 -c:a pcm_s16le $decodedPath
    if ($LASTEXITCODE -ne 0) { throw "Music loop conversion failed: $($track.Name)" }
    # Equal-power crossfade is computed by frame, preserving stereo channel alignment. Unity
    # imports the resulting WAVs as streamed Vorbis, rather than shipping uncompressed PCM.
    [RecordedMusicLoop]::Prepare($decodedPath, $loopPath)
    Write-Output "Loop prepared: $($track.Name)"
}
