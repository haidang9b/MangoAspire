#Requires -Version 5.1
<#
.SYNOPSIS
    Builds the MangoAspire solution. Thin wrapper over the command documented in AGENTS.md.
.PARAMETER Project
    Optional path to a single .csproj to build instead of the whole solution.
#>
[CmdletBinding()]
param(
    [string]$Project
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repoRoot
try {
    if ($Project) {
        Write-Host "dotnet build $Project" -ForegroundColor Cyan
        dotnet build $Project
    }
    else {
        Write-Host "dotnet build MangoAspire.sln" -ForegroundColor Cyan
        dotnet build MangoAspire.sln
    }
    exit $LASTEXITCODE
}
finally {
    Pop-Location
}
