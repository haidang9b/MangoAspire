#Requires -Version 5.1
<#
.SYNOPSIS
    Runs a pnpm script for the React SPA (src/UI/mango-ui). Wrapper over the commands documented in AGENTS.md.
.PARAMETER Task
    The pnpm script to run: dev | build | lint | preview | install.
.EXAMPLE
    ./scripts/ui.ps1 dev
.EXAMPLE
    ./scripts/ui.ps1 lint
#>
[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [ValidateSet('dev', 'build', 'lint', 'preview', 'install')]
    [string]$Task = 'dev'
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$uiDir = Join-Path $repoRoot 'src/UI/mango-ui'

if (-not (Get-Command pnpm -ErrorAction SilentlyContinue)) {
    Write-Error "pnpm is not on PATH. Enable it with 'corepack enable' (Node 18+) or install pnpm."
    exit 1
}

if ($Task -eq 'install') {
    Write-Host "pnpm install --dir $uiDir" -ForegroundColor Cyan
    pnpm install --dir $uiDir
}
else {
    Write-Host "pnpm --dir $uiDir $Task" -ForegroundColor Cyan
    pnpm --dir $uiDir $Task
}
exit $LASTEXITCODE
