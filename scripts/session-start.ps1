[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
foreach ($path in @('AGENTS.md','Docs/task.md','Docs/session_handoff.md','Docs/work_log.md')) {
    if (!(Test-Path -LiteralPath (Join-Path $root $path))) { throw "Missing: $path" }
}
Write-Output "Workspace: $root"
Get-Content -LiteralPath (Join-Path $root 'Docs/session_handoff.md') -TotalCount 28
Write-Output 'Git changes:'
& git -C $root status --short
if ($LASTEXITCODE -ne 0) { throw 'Git status failed' }
Write-Output 'Read: AGENTS -> task -> session_handoff -> relevant spec -> latest work_log -> source.'
