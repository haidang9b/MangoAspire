#Requires -Version 5.1
<#
.SYNOPSIS
    Syncs the Copilot mirror of the agent harness and lints the harness.
.DESCRIPTION
    .claude/ is canonical: rules, skills and agents live there, and Claude Code and VS Code Copilot
    read .claude/skills natively. This script generates the parts Copilot can only read from .github/:
      .claude/rules/**/*.md  -> .github/instructions/**/*.instructions.md   (paths -> applyTo)
      .claude/agents/*.md    -> .github/agents/*.agent.md                   (name + description only)
    Only those two .github/ folders are ever written.

    -Check lints instead of writing: Budget, Rules, Skills, Agents, Links, Staleness, Drift, Facts,
    Specs, Evals. It exits 1 on any finding. CI runs it.
.PARAMETER Check
    Lint only; no files are written.
.PARAMETER Category
    With -Check, limit the lint to these categories (comma-separated or repeated).
.EXAMPLE
    pwsh ./scripts/sync-agent-harness.ps1
.EXAMPLE
    powershell -NoProfile -ExecutionPolicy Bypass -File scripts/sync-agent-harness.ps1 -Check -Category Drift,Agents
#>
[CmdletBinding()]
param(
    [switch]$Check,
    [string[]]$Category
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
Import-Module (Join-Path $PSScriptRoot 'harness/AgentHarness.psm1') -Force

if ($Check) {
    # powershell -File passes "-Category A,B" as the single string "A,B".
    $cats = @($Category | ForEach-Object { $_ -split ',' } | ForEach-Object { $_.Trim() } | Where-Object { $_ })
    $findings = if ($cats.Count -gt 0) { @(Invoke-HarnessLint -RepoRoot $repoRoot -Category $cats) } else { @(Invoke-HarnessLint -RepoRoot $repoRoot) }
    foreach ($f in $findings) { Write-Host ("[{0}] {1}: {2}" -f $f.Category, $f.File, $f.Message) }
    if ($findings.Count -gt 0) {
        Write-Host ("harness check: {0} finding(s)" -f $findings.Count) -ForegroundColor Yellow
        exit 1
    }
    Write-Host 'harness check: OK' -ForegroundColor Green
    exit 0
}

Invoke-HarnessSync -RepoRoot $repoRoot
$report = Get-HarnessContextReport -RepoRoot $repoRoot
Write-Host 'Synced .github/instructions and .github/agents from .claude/.' -ForegroundColor Green
Write-Host ("AlwaysOnBytes: {0}  ModelSkillCount: {1}  UserSkillCount: {2}  SkillListingChars: {3}" -f `
        $report.AlwaysOnBytes, $report.ModelSkillCount, $report.UserSkillCount, $report.SkillListingChars)
exit 0
