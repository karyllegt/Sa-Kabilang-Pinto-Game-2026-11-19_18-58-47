$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
Set-Location $projectRoot
if (Get-Process Unity -ErrorAction SilentlyContinue) { throw 'Finish Unity runs before formatting generated source metadata.' }
$changed = ((git -c core.safecrlf=false diff --name-only -z) -join "`n")
$added = ((git -c core.safecrlf=false ls-files --others --exclude-standard -z) -join "`n")
if ($LASTEXITCODE -ne 0) { throw 'Could not enumerate source changes.' }
$paths = (($changed + [char]0 + $added).Split([char]0) | Where-Object { $_ } | Sort-Object -Unique)
$yaml = @('.asset','.meta','.prefab','.unity','.mat','.mixer','.controller','.anim')
$encoding = New-Object Text.UTF8Encoding($false)
$count = 0
foreach ($relative in $paths) {
    if ([IO.Path]::GetExtension($relative) -notin $yaml) { continue }
    $absolute = [IO.Path]::GetFullPath((Join-Path $projectRoot $relative))
    if (-not $absolute.StartsWith($projectRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Formatting target escaped the project.'
    }
    if (-not (Test-Path -LiteralPath $absolute -PathType Leaf)) { continue }
    $before = [IO.File]::ReadAllText($absolute)
    $after = [Text.RegularExpressions.Regex]::Replace($before, '(?m)[\t ]+(?=\r?$)', '')
    if ($after -ne $before) { [IO.File]::WriteAllText($absolute, $after, $encoding); $count++ }
}
git -c core.safecrlf=false diff --check
if ($LASTEXITCODE -ne 0) { throw 'Resolve remaining diff-check issues before packaging.' }
Write-Output "Generated YAML files mechanically trimmed: $count; git diff --check passed."
