#Requires -Version 5.1
<#
.SYNOPSIS
    One-shot developer onboarding for MangoAspire: verifies prerequisites, restores dependencies,
    generates the Claude Code harness, and starts the Serena MCP server.
.PARAMETER SkipMcp
    Skip starting the Serena MCP container (e.g. when Docker is unavailable).
.PARAMETER SkipRestore
    Skip dotnet restore / pnpm install (faster re-runs when dependencies are already present).
#>
[CmdletBinding()]
param(
    [switch]$SkipMcp,
    [switch]$SkipRestore
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$scripts = $PSScriptRoot

function Test-Tool {
    param([string]$Name, [string]$Hint)
    if (Get-Command $Name -ErrorAction SilentlyContinue) {
        Write-Host ("  [ok]   {0}" -f $Name) -ForegroundColor Green
        return $true
    }
    Write-Host ("  [MISS] {0} - {1}" -f $Name, $Hint) -ForegroundColor Yellow
    return $false
}

Write-Host "== MangoAspire bootstrap ==" -ForegroundColor Cyan
Write-Host "Checking prerequisites..."
$haveDotnet = Test-Tool 'dotnet' 'install the .NET 10 SDK'
$haveNode = Test-Tool 'node'   'install Node.js 18+ (for pnpm via corepack)'
$havePnpm = Test-Tool 'pnpm'   "run 'corepack enable' or install pnpm"
$haveDocker = Test-Tool 'docker' 'install Docker Desktop (needed for the Serena MCP server + infra)'

if (-not $haveDotnet) { Write-Error "dotnet is required. Aborting."; exit 1 }

if (-not $SkipRestore) {
    Write-Host "`nRestoring .NET dependencies..." -ForegroundColor Cyan
    Push-Location $repoRoot
    try { dotnet restore MangoAspire.sln } finally { Pop-Location }

    if ($havePnpm) {
        Write-Host "`nInstalling frontend dependencies (pnpm)..." -ForegroundColor Cyan
        pnpm install --dir (Join-Path $repoRoot 'src/UI/mango-ui')
    }
    else {
        Write-Warning "pnpm not found - skipping frontend install. Enable it with 'corepack enable'."
    }
}

Write-Host "`nGenerating Claude Code harness from .agent/ ..." -ForegroundColor Cyan
& (Join-Path $scripts 'sync-agent-harness.ps1')

Write-Host "`nRendering the ticket board from .agent/tickets/ ..." -ForegroundColor Cyan
& (Join-Path $scripts 'update-ticket-board.ps1')

if (-not $SkipMcp) {
    if ($haveDocker) {
        Write-Host "`nStarting Serena MCP server..." -ForegroundColor Cyan
        & (Join-Path $scripts 'start-mcp.ps1')
    }
    else {
        Write-Warning "docker not found - skipping Serena MCP start. Run scripts/start-mcp.ps1 later."
    }
}

Write-Host "`n== Bootstrap complete ==" -ForegroundColor Green
Write-Host "Next steps:"
Write-Host "  1. Open this repo in Claude Code; approve the project MCP server ('serena') when prompted."
Write-Host "  2. Run '/mcp' to confirm Serena is connected, and check skills/commands are listed."
Write-Host "  3. Launch the app:   ./scripts/run-app.ps1"
Write-Host "  4. Run tests:        ./scripts/test.ps1"
Write-Host "  5. Current work:     open .agent/ui/board.html (and .agent/README.md for the harness layout)"
