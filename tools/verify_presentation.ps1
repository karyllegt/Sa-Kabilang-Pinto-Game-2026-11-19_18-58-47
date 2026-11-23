param([int]$AfterProcessId = 0)
$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$editorPath = 'C:/Program Files/Unity/Hub/Editor/6000.6.6f1/Editor/Unity.exe'
$resultsRoot = Join-Path $projectRoot 'tests/Results'
New-Item -ItemType Directory -Force $resultsRoot | Out-Null
if ($AfterProcessId -gt 0) {
    $prior = Get-Process -Id $AfterProcessId -ErrorAction SilentlyContinue
    if ($prior) { $prior.WaitForExit() }
    $captureLog = Get-Content (Join-Path $projectRoot 'Logs/PresentationFinalCaptures.log') -Raw
    if ($captureLog -notmatch 'return code 0') { throw 'Capture integration did not finish successfully; tests not started.' }
}
$outcomes = @()
foreach ($mode in @('EditMode','PlayMode')) {
    $stem = 'presentation_' + $mode.ToLowerInvariant()
    $reportPath = Join-Path $resultsRoot ($stem + '.xml')
    $logPath = Join-Path $projectRoot ('Logs/' + $stem + '.log')
    $arguments = @('-batchmode','-projectPath',('"' + $projectRoot + '"'),'-runTests','-testPlatform',$mode,
        '-testResults',('"' + $reportPath + '"'),'-logFile',('"' + $logPath + '"'),
        '-force-d3d11','--burst-disable-compilation','-job-worker-count','2','-desiredWorkerCount','1')
    $process = Start-Process -FilePath $editorPath -ArgumentList $arguments -WindowStyle Hidden -PassThru
    [IO.File]::WriteAllText((Join-Path $resultsRoot 'presentation_active_phase.txt'), "$mode PID $($process.Id)")
    Write-Output "$mode started: PID $($process.Id)"
    $process.WaitForExit()
    $summary = [ordered]@{ mode = $mode; exitCode = $process.ExitCode; total = 0; passed = 0; failed = 0 }
    if (Test-Path $reportPath) {
        [xml]$report = Get-Content $reportPath -Raw
        $summary.total = [int]$report.'test-run'.total
        $summary.passed = [int]$report.'test-run'.passed
        $summary.failed = [int]$report.'test-run'.failed
    }
    $outcomes += [pscustomobject]$summary
    Write-Output ($summary | ConvertTo-Json -Compress)
    $outcomes | ConvertTo-Json | Set-Content (Join-Path $resultsRoot 'presentation_suite_summary.json')
}
if ($outcomes.Where({ $_.exitCode -ne 0 -or $_.failed -gt 0 -or $_.total -eq 0 }).Count -gt 0) {
    [IO.File]::WriteAllText((Join-Path $resultsRoot 'presentation_active_phase.txt'), 'Tests finished with failures; build gate closed.')
    throw 'Inspect suite reports and repair failures before building.'
}
$arguments = @('-batchmode','-quit','-projectPath',('"' + $projectRoot + '"'),'-executeMethod','SkpHandoffBuild.Windows',
    '-logFile',('"' + (Join-Path $projectRoot 'Logs/PresentationWindows.log') + '"'),
    '-force-d3d11','--burst-disable-compilation','-job-worker-count','2','-desiredWorkerCount','1')
$process = Start-Process -FilePath $editorPath -ArgumentList $arguments -WindowStyle Hidden -PassThru
[IO.File]::WriteAllText((Join-Path $resultsRoot 'presentation_active_phase.txt'), "Windows build PID $($process.Id)")
Write-Output "Windows build started: PID $($process.Id)"
$process.WaitForExit()
if ($process.ExitCode -ne 0) { throw "Windows build exited $($process.ExitCode)" }
[IO.File]::WriteAllText((Join-Path $resultsRoot 'presentation_active_phase.txt'), 'Full suites and Windows build finished; inspect reports and player.')
