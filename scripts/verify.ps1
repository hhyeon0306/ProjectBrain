[CmdletBinding()]
param([switch]$IncludeBrain)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
& (Join-Path $PSScriptRoot 'docs-check.ps1')
& git -C $root diff --check
if ($LASTEXITCODE -ne 0) { throw 'Working tree whitespace check failed' }
& git -C $root diff --cached --check
if ($LASTEXITCODE -ne 0) { throw 'Staged whitespace check failed' }
if ($IncludeBrain) {
    $child = Join-Path $root 'Project Brain/scripts/verify.ps1'
    if (!(Test-Path -LiteralPath $child)) { throw 'No child Brain workspace here' }
    & $child
}
Write-Output 'Management verification passed. Unity build/tests were not run by this script.'
