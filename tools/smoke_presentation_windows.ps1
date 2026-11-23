$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$player = Join-Path $projectRoot 'Builds/Presentation/Windows/SaKabilangPinto.exe'
if (-not (Test-Path $player)) { throw 'Build the Windows player first.' }
$log = Join-Path $projectRoot 'Logs/PresentationWindowsPlayer.log'
$arguments = @('-batchmode','-force-d3d11','-screen-fullscreen','0','-screen-width','1280','-screen-height','720',
    '-logFile',('"' + $log + '"'))
$process = Start-Process -FilePath $player -ArgumentList $arguments -WindowStyle Hidden -PassThru
$watch = [Diagnostics.Stopwatch]::StartNew()
try {
    $managedStartup = $false
    while ($watch.Elapsed.TotalSeconds -lt 180) {
        Start-Sleep -Seconds 2
        $process.Refresh()
        if ($process.HasExited) { throw "Windows player exited early: $($process.ExitCode)" }
        if (Test-Path $log) {
            $progress = Get-Content $log -Raw
            $managedStartup = $progress -match 'UnloadTime:'
        }
        if ($watch.Elapsed.TotalSeconds -ge 35 -and $managedStartup) { break }
    }
    $contents = Get-Content $log -Raw
    $errors = @($contents -split '\r?\n' | Where-Object { $_ -match 'Exception:|NullReferenceException|MissingReferenceException|Shader error|Crash!!!|Failed to load player' })
    $graphics = $contents -match 'Direct3D|D3D11'
    $summary = [ordered]@{
        check='Native Windows process and graphics startup; batch mode, no interactive playthrough or listening claim'
        seconds=[int]$watch.Elapsed.TotalSeconds
        stayedRunning=$true
        direct3DInitialized=$graphics
        sceneStartupLogged=$managedStartup
        loggedErrors=$errors
        log='Logs/PresentationWindowsPlayer.log'
    }
    $summary | ConvertTo-Json -Depth 4 | Set-Content (Join-Path $projectRoot 'tests/Results/presentation_windows_smoke.json')
    $summary | ConvertTo-Json -Depth 4
    if (-not $graphics -or -not $managedStartup -or $errors.Count -gt 0) { throw 'Inspect the native player smoke report/log; managed scene startup must finish.' }
} finally {
    $process.Refresh()
    if (-not $process.HasExited) {
        if ($process.CloseMainWindow()) { $null = $process.WaitForExit(5000) }
        $process.Refresh()
        if (-not $process.HasExited) { Stop-Process -Id $process.Id -Force }
    }
}
