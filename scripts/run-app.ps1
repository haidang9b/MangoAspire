#Requires -Version 5.1
<#
.SYNOPSIS
    Runs the .NET Aspire AppHost locally, orchestrating all services and infrastructure.
    Wrapper over the command documented in AGENTS.md.
.PARAMETER HttpEndpoints
    If set, exports ASPIRE_USE_HTTP_ENDPOINTS=true so services launch on http profiles.
.PARAMETER IdentityType
    Which identity provider to run: Duende (default) or OpenIddict. Sets the IdentityType env var.
#>
[CmdletBinding()]
param(
    [switch]$HttpEndpoints,
    [ValidateSet('Duende', 'OpenIddict')]
    [string]$IdentityType
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repoRoot
try {
    if ($HttpEndpoints) { $env:ASPIRE_USE_HTTP_ENDPOINTS = 'true' }
    if ($IdentityType) { $env:IdentityType = $IdentityType }

    Write-Host "dotnet run --project src/Mango.AppHost/Mango.AppHost.csproj" -ForegroundColor Cyan
    dotnet run --project src/Mango.AppHost/Mango.AppHost.csproj
    exit $LASTEXITCODE
}
finally {
    Pop-Location
}
