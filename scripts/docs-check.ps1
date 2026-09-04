[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$brain = Test-Path -LiteralPath (Join-Path $root 'ProjectSettings/ProjectVersion.txt')
$spec = if ($brain) { 'Docs/product_spec.md' } else { 'Docs/plan.md' }
$required = @('AGENTS.md','Docs/README.md','Docs/task.md',$spec,'Docs/session_handoff.md','Docs/work_log.md')
foreach ($path in $required) {
    if (!(Test-Path -LiteralPath (Join-Path $root $path))) { throw "Missing document: $path" }
}
$handoff = Get-Content -LiteralPath (Join-Path $root 'Docs/session_handoff.md') -Raw
foreach ($heading in @('## Current state','## Decisions','## Next action','## Verification')) {
    if (!$handoff.Contains($heading)) { throw "Missing handoff section: $heading" }
}
if ($handoff -notmatch 'Updated: (\d{4}-\d{2}-\d{2})') { throw 'Missing handoff date' }
$null = [datetime]::ParseExact($Matches[1], 'yyyy-MM-dd', [Globalization.CultureInfo]::InvariantCulture)
foreach ($path in $required) {
    $file = Join-Path $root $path
    $body = Get-Content -LiteralPath $file -Raw
    foreach ($match in [regex]::Matches($body, '\[[^\]]+\]\(([^)]+)\)')) {
        $link = $match.Groups[1].Value.Trim('<','>')
        if ($link -match '^[a-zA-Z]+:|^#') { continue }
        $link = [Uri]::UnescapeDataString(($link -split '#')[0])
        if (!(Test-Path -LiteralPath (Join-Path (Split-Path $file -Parent) $link))) { throw "Broken link in ${path}: $link" }
    }
}
& git -C $root rev-parse --verify HEAD 2>$null | Out-Null
if ($LASTEXITCODE -eq 0) {
    $changed = @(& git -C $root -c core.quotepath=false diff --name-only HEAD)
} else { $changed = @(& git -C $root -c core.quotepath=false ls-files) }
$changed += @(& git -C $root -c core.quotepath=false ls-files --others --exclude-standard)
if ($LASTEXITCODE -ne 0) { throw 'Cannot inspect Git changes' }
$changed = @($changed | Sort-Object -Unique)
$meaningful = @($changed | Where-Object {
    $_ -notmatch '^Docs/(archive/|work_log\.md$|session_handoff\.md$)' -and
    $_ -match '^(AGENTS\.md$|\.gitignore$|scripts/|Assets/|Packages/|ProjectSettings/|Docs/)'
})
if ($meaningful.Count) {
    foreach ($record in @('Docs/work_log.md','Docs/session_handoff.md')) {
        if ($changed -notcontains $record) { throw "Changes require updated record: $record" }
    }
}
$log = Get-Content -LiteralPath (Join-Path $root 'Docs/work_log.md') -Raw
$entry = [regex]::Match($log, '(?ms)^## \d{4}-\d{2}-\d{2}[^\r\n]*\r?\n(.*?)(?=^## |\z)')
if (!$entry.Success) { throw 'Missing dated work log entry' }
foreach ($field in @('- Goal:','- Changes:','- Files:','- Verification:','- Decisions:','- Next:','- Limitations:')) {
    if (!$entry.Value.Contains($field)) { throw "Missing work log field: $field" }
}
Write-Output "Document checks passed. Meaningful changed paths: $($meaningful.Count)."
Write-Output 'Checks cover record presence and structure, not semantic correctness or Unity test results.'
