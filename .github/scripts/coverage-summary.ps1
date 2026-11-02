<#
.SYNOPSIS
    Renders Cobertura coverage XML files as markdown for the GitHub Actions job summary.
.PARAMETER Path
    Directory containing *.cobertura.xml files.
#>
param(
    [Parameter(Mandatory)]
    [string] $Path
)

$ErrorActionPreference = 'Stop'

function Format-Rate([string] $rate) {
    if ([string]::IsNullOrEmpty($rate)) { return 'n/a' }
    return ('{0:N1}%' -f ([double]::Parse($rate, [cultureinfo]::InvariantCulture) * 100))
}

$files = @(Get-ChildItem -Path $Path -Filter '*.cobertura.xml' -Recurse -File | Sort-Object FullName)
if ($files.Count -eq 0) {
    "## Code coverage`n`n:warning: No Cobertura coverage file was found."
    return
}

foreach ($file in $files) {
    $root = ([xml] (Get-Content -Raw -Path $file.FullName)).coverage

    '## Code coverage'
    ''
    '| Metric | Covered | Total | Rate |'
    '|---|---:|---:|---:|'
    "| Lines | $($root.'lines-covered') | $($root.'lines-valid') | $(Format-Rate $root.'line-rate') |"
    "| Branches | $($root.'branches-covered') | $($root.'branches-valid') | $(Format-Rate $root.'branch-rate') |"

    $rows = $root.SelectNodes('//class') |
        ForEach-Object {
            $lines = @($_.SelectNodes('./lines/line'))
            if ($lines.Count -gt 0) {
                $name = if ($_.filename) { $_.filename } else { $_.name }
                [pscustomobject]@{
                    Name    = $name
                    Covered = @($lines | Where-Object { [int] $_.hits -gt 0 }).Count
                    Total   = $lines.Count
                }
            }
        } |
        Group-Object Name |
        ForEach-Object {
            # A file can hold several classes; combine them into a single row.
            [pscustomobject]@{
                Name    = $_.Name
                Covered = ($_.Group | Measure-Object Covered -Sum).Sum
                Total   = ($_.Group | Measure-Object Total -Sum).Sum
            }
        } |
        Sort-Object { $_.Covered / $_.Total }, Name   # lowest coverage first

    if ($rows) {
        ''
        '<details><summary>Line coverage by file</summary>'
        ''
        '| File | Covered | Total | Rate |'
        '|---|---:|---:|---:|'
        foreach ($row in $rows) {
            '| `{0}` | {1} | {2} | {3:N1}% |' -f $row.Name, $row.Covered, $row.Total, ($row.Covered / $row.Total * 100)
        }
        ''
        '</details>'
    }
}
