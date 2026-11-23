$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$resultsRoot = Join-Path $projectRoot 'tests/Results'
$reports = @{}
foreach ($name in @('presentation_editmode','presentation_playmode','presentation_repaired_editmode','presentation_baseline_compare','presentation_ui_final')) {
    [xml]$report = Get-Content (Join-Path $resultsRoot ($name + '.xml')) -Raw
    if ([int]$report.'test-run'.total -eq 0) { throw "Empty report: $name" }
    $reports[$name] = $report
}
$resolved = @()
foreach ($failure in $reports.presentation_editmode.SelectNodes('//test-case[@result="Failed"]')) {
    $match = $reports.presentation_repaired_editmode.SelectNodes('//test-case') | Where-Object { $_.fullname -eq $failure.fullname -and $_.result -eq 'Passed' }
    if (-not $match) { throw "Original failure has no passing repair result: $($failure.fullname)" }
    $resolved += $failure.fullname
}
foreach ($name in @('presentation_playmode','presentation_repaired_editmode','presentation_baseline_compare','presentation_ui_final')) {
    if ([int]$reports[$name].'test-run'.failed -ne 0) { throw "Unresolved failures: $name" }
}
$summary = [ordered]@{
    description = 'Full suites plus targeted repairs; the full expensive EditMode suite was not repeated after UI/audio-only repairs.'
    fullEditMode = @{ total=[int]$reports.presentation_editmode.'test-run'.total; initiallyPassed=[int]$reports.presentation_editmode.'test-run'.passed; repairedFailures=$resolved }
    fullPlayMode = @{ passed=[int]$reports.presentation_playmode.'test-run'.passed; failed=[int]$reports.presentation_playmode.'test-run'.failed; skipped=[int]$reports.presentation_playmode.'test-run'.skipped }
    repairedEditMode = @{ passed=[int]$reports.presentation_repaired_editmode.'test-run'.passed; failed=[int]$reports.presentation_repaired_editmode.'test-run'.failed }
    baselineComparison = @{ passed=[int]$reports.presentation_baseline_compare.'test-run'.passed; failed=[int]$reports.presentation_baseline_compare.'test-run'.failed }
    finalUiCapture = @{ passed=[int]$reports.presentation_ui_final.'test-run'.passed; failed=[int]$reports.presentation_ui_final.'test-run'.failed }
    unverified = @('Physical Android device: performance, touch, safe areas, app-kill persistence','Headphone and phone-speaker listening acceptance','Physical gamepad acceptance')
}
$summary | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $resultsRoot 'presentation_acceptance_summary.json')
$summary | ConvertTo-Json -Depth 6
