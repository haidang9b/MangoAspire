#Requires -Version 5.1
<#
.SYNOPSIS
    Starts (or stops) the Serena MCP server used by Claude Code, via docker-compose.mcp.yaml.
.DESCRIPTION
    The server is reachable at http://localhost:9121 and is wired into Claude Code through the
    project-scoped .mcp.json. Claude Code reads .mcp.json at startup, so the container must be up
    BEFORE launching Claude Code in this repo.
.PARAMETER Down
    Tears the MCP server down instead of starting it.
.PARAMETER TimeoutSeconds
    How long to wait for the server to answer on port 9121 (default 60).
#>
[CmdletBinding()]
param(
    [switch]$Down,
    [int]$TimeoutSeconds = 60
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$composeFile = Join-Path $repoRoot 'docker-compose.mcp.yaml'

if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    Write-Error "docker is not on PATH. Install Docker Desktop (or the docker CLI) and retry."
    exit 1
}
if (-not (Test-Path $composeFile)) {
    Write-Error "Missing $composeFile"
    exit 1
}

if ($Down) {
    Write-Host "docker compose -f docker-compose.mcp.yaml down" -ForegroundColor Cyan
    docker compose -f $composeFile down
    exit $LASTEXITCODE
}

Write-Host "docker compose -f docker-compose.mcp.yaml up -d" -ForegroundColor Cyan
docker compose -f $composeFile up -d
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# Poll the port until the server answers (bounded retry; no fixed sleep).
$deadline = (Get-Date).AddSeconds($TimeoutSeconds)
$ready = $false
Write-Host "Waiting for Serena on http://localhost:9121 ..." -NoNewline
while ((Get-Date) -lt $deadline) {
    try {
        $conn = Test-NetConnection -ComputerName 'localhost' -Port 9121 -WarningAction SilentlyContinue
        if ($conn.TcpTestSucceeded) { $ready = $true; break }
    }
    catch {
        # Test-NetConnection unavailable (e.g. Linux pwsh) — fall back to a raw TcpClient probe.
        try {
            $client = New-Object System.Net.Sockets.TcpClient
            $client.Connect('localhost', 9121)
            if ($client.Connected) { $client.Close(); $ready = $true; break }
        }
        catch { }
    }
    Write-Host "." -NoNewline
    Start-Sleep -Milliseconds 1000
}
Write-Host ""

if ($ready) {
    Write-Host "Serena MCP server is up on http://localhost:9121" -ForegroundColor Green
    Write-Host "In Claude Code, run '/mcp' to confirm 'serena' is connected." -ForegroundColor Green
    exit 0
}
else {
    Write-Warning "Serena did not answer on port 9121 within $TimeoutSeconds s. Check 'docker logs serena'."
    exit 1
}
