#Requires -Version 5.1
<#
.SYNOPSIS
    Runs the MangoAspire test suite. Thin wrapper over the commands documented in AGENTS.md.
.PARAMETER Project
    Optional single test project (e.g. tests/Services/Products.API.Tests/Products.API.Tests.csproj).
.PARAMETER Filter
    Optional xUnit filter passed through as --filter (e.g. "FullyQualifiedName~GetProductByIdTests").
.EXAMPLE
    ./scripts/test.ps1
.EXAMPLE
    ./scripts/test.ps1 -Project tests/Services/Products.API.Tests/Products.API.Tests.csproj -Filter "ClassName~GetProductByIdTests"
#>
[CmdletBinding()]
param(
    [string]$Project,
    [string]$Filter
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repoRoot
try {
    $target = if ($Project) { $Project } else { 'MangoAspire.sln' }
    $dotnetArgs = @('test', $target)
    if ($Filter) { $dotnetArgs += @('--filter', $Filter) }

    Write-Host "dotnet $($dotnetArgs -join ' ')" -ForegroundColor Cyan
    dotnet @dotnetArgs
    exit $LASTEXITCODE
}
finally {
    Pop-Location
}
