#Requires -Version 5.1
<#
.SYNOPSIS
    Formats (or verifies formatting of) the solution using .editorconfig. Wrapper over AGENTS.md commands.
.PARAMETER Verify
    Verify-only: runs `dotnet format --verify-no-changes` and fails if changes are needed (no files modified).
#>
[CmdletBinding()]
param(
    [switch]$Verify
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repoRoot
try {
    if ($Verify) {
        Write-Host "dotnet format --verify-no-changes MangoAspire.sln" -ForegroundColor Cyan
        dotnet format --verify-no-changes MangoAspire.sln
    }
    else {
        Write-Host "dotnet format MangoAspire.sln" -ForegroundColor Cyan
        dotnet format MangoAspire.sln
    }
    exit $LASTEXITCODE
}
finally {
    Pop-Location
}
