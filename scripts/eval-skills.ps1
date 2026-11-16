#Requires -Version 5.1
<#
.SYNOPSIS
    Runs skill output evals: each eval once with the skill and once without, graded and benchmarked.
.DESCRIPTION
    Reads tests/skill-evals/<skill>/evals.json. Every run executes `claude -p` in a disposable git
    worktree of the committed ref, so the working tree is never touched. Deterministic checks run
    first, then a grader session judges the LLM-graded expectations. Results land in
    tests/skill-evals/_results/<timestamp>/ (gitignored) with benchmark.json and summary.md.

    Costs tokens: never run in CI. Use -DryRun to list the planned invocations.
.EXAMPLE
    powershell -NoProfile -ExecutionPolicy Bypass -File scripts/eval-skills.ps1 -Skill generate-commit -EvalId 1
.EXAMPLE
    pwsh ./scripts/eval-skills.ps1 -Skill code-review,run-tests -DryRun
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string[]]$Skill,
    [string[]]$EvalId,
    [int]$Runs = 1,
    [string]$Model,
    [int]$MaxTurns = 30,
    [switch]$DryRun
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
Import-Module (Join-Path $PSScriptRoot 'harness/SkillEval.psm1') -Force

# powershell -File passes "-Skill a,b" as the single string "a,b".
$params = @{
    RepoRoot = $repoRoot
    Skill    = @($Skill | ForEach-Object { $_ -split ',' } | ForEach-Object { $_.Trim() } | Where-Object { $_ })
    Runs     = $Runs
    MaxTurns = $MaxTurns
}
$ids = @($EvalId | ForEach-Object { $_ -split ',' } | Where-Object { $_ } | ForEach-Object { [int]$_ })
if ($ids.Count -gt 0) { $params.EvalId = $ids }
if ($Model) { $params.Model = $Model }
if ($DryRun) { $params.DryRun = $true }

$results = Invoke-SkillEvals @params
if ($results) {
    Write-Host "Results: $results"
    Get-Content -LiteralPath (Join-Path $results 'summary.md') -Encoding UTF8
}
