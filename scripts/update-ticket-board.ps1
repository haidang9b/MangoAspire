#Requires -Version 5.1
<#
.SYNOPSIS
    Regenerates the self-contained ticket board (.agent/ui/board.html) from .agent/tickets/*/ticket.json.
.DESCRIPTION
    ticket.json is the canonical state of a ticket. This script validates every ticket, inlines the
    whole set into .agent/ui/board.template.html and writes .agent/ui/board.html - a single file with
    no external requests, so it opens correctly by double-click (file://).

    Hard problems (bad status, id/directory mismatch, a step whose done flag contradicts its tasks)
    fail the run. Soft problems (a missing blueprint, checkboxes in notes.md) are warnings.

    The output is deterministic: no render timestamp, no random ids, stable ordering. That is what
    makes -Check meaningful.
.PARAMETER Check
    Dry-run. Renders to a temp file and compares it against the committed board.html; exits non-zero
    if they differ (for pre-commit / CI). Nothing is written to .agent/.
.PARAMETER Open
    Open the generated board in the default browser afterwards.
.EXAMPLE
    pwsh ./scripts/update-ticket-board.ps1
.EXAMPLE
    pwsh ./scripts/update-ticket-board.ps1 -Check
#>
[CmdletBinding()]
param(
    [switch]$Check,
    [switch]$Open
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$agentRoot = Join-Path $repoRoot '.agent'
$ticketsRoot = Join-Path $agentRoot 'tickets'
$memoryRoot = Join-Path $agentRoot 'memory'
$templatePath = Join-Path $agentRoot 'ui/board.template.html'
$boardPath = Join-Path $agentRoot 'ui/board.html'

$Banner = '<!-- GENERATED from .agent/tickets/ by scripts/update-ticket-board.ps1 - do not edit directly -->'
$Utf8NoBom = New-Object System.Text.UTF8Encoding($false)

$ValidStatus = @('not_started', 'in_progress', 'awaiting_approval', 'blocked', 'completed', 'abandoned')
# Board display order; also the tie-break for deterministic serialization.
$StatusRank = @{ 'in_progress' = 0; 'blocked' = 1; 'awaiting_approval' = 2; 'not_started' = 3; 'completed' = 4; 'abandoned' = 5 }

$Problems = New-Object System.Collections.ArrayList
$Warnings = New-Object System.Collections.ArrayList

function Write-FileNoBom {
    param([string]$Path, [string]$Content)
    $dir = Split-Path -Parent $Path
    if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }
    [System.IO.File]::WriteAllText($Path, $Content, $Utf8NoBom)
}

function Add-Problem { param([string]$Message) [void]$Problems.Add($Message) }
function Add-Warn { param([string]$Message) [void]$Warnings.Add($Message) }

# Reads and validates every .agent/tickets/<ID>/ticket.json (directories starting with _ are templates).
function Read-Tickets {
    $result = @()
    if (-not (Test-Path $ticketsRoot)) { return $result }

    $dirs = Get-ChildItem -Path $ticketsRoot -Directory |
        Where-Object { $_.Name -notlike '_*' } |
        Sort-Object Name

    foreach ($dir in $dirs) {
        $jsonPath = Join-Path $dir.FullName 'ticket.json'
        if (-not (Test-Path $jsonPath)) {
            Add-Problem "$($dir.Name): missing ticket.json"
            continue
        }

        try {
            $ticket = [System.IO.File]::ReadAllText($jsonPath) | ConvertFrom-Json
        }
        catch {
            Add-Problem "$($dir.Name): invalid JSON - $($_.Exception.Message)"
            continue
        }

        if ($ticket.id -ne $dir.Name) { Add-Problem "$($dir.Name): id '$($ticket.id)' does not match the directory name" }
        if ($ValidStatus -notcontains $ticket.status) { Add-Problem "$($dir.Name): unknown status '$($ticket.status)'" }
        if (-not $ticket.title) { Add-Problem "$($dir.Name): title is empty" }

        # A step is done if and only if every task under it is done or explicitly skipped.
        foreach ($step in @($ticket.steps)) {
            $tasks = @($step.tasks)
            if ($tasks.Count -eq 0) { continue }
            $allClosed = -not ($tasks | Where-Object { -not $_.done -and -not $_.skipped })
            if ([bool]$step.done -ne [bool]$allClosed) {
                Add-Problem "$($ticket.id)/$($step.id): done=$($step.done) contradicts its tasks"
            }
            foreach ($task in $tasks | Where-Object { $_.skipped -and -not $_.skipReason }) {
                Add-Warn "$($ticket.id)/$($task.id): skipped without a skipReason"
            }
        }

        $openBlockers = @($ticket.blockers | Where-Object { $_ -and -not $_.resolvedUtc })
        if ($openBlockers.Count -gt 0 -and $ticket.status -ne 'blocked') {
            Add-Warn "$($ticket.id): has an open blocker but status is '$($ticket.status)'"
        }
        if ($openBlockers.Count -eq 0 -and $ticket.status -eq 'blocked') {
            Add-Warn "$($ticket.id): status is 'blocked' but no blocker is open"
        }
        if ($ticket.status -eq 'completed' -and -not $ticket.completedUtc) {
            Add-Warn "$($ticket.id): completed but completedUtc is not set"
        }
        if ($ticket.status -ne 'completed' -and $ticket.completedUtc) {
            Add-Warn "$($ticket.id): completedUtc is set but status is '$($ticket.status)'"
        }

        if ($ticket.links -and $ticket.links.plan) {
            if (-not (Test-Path (Join-Path $repoRoot $ticket.links.plan))) {
                Add-Warn "$($ticket.id): links.plan '$($ticket.links.plan)' does not exist"
            }
        }

        if ($ticket.notes) {
            $notesPath = Join-Path $repoRoot $ticket.notes
            if (-not (Test-Path $notesPath)) {
                Add-Warn "$($ticket.id): notes '$($ticket.notes)' does not exist"
            }
            else {
                $notesText = [System.IO.File]::ReadAllText($notesPath)
                if ($notesText -match '(?m)^\s*[-*]\s\[( |x|X)\]') {
                    Add-Warn "$($ticket.id): notes.md contains checkboxes - progress belongs in ticket.json"
                }
                if ($notesText -match '(?m)^\s*\*{0,2}Current Status') {
                    Add-Warn "$($ticket.id): notes.md declares a status - status belongs in ticket.json"
                }
            }
        }

        $result += $ticket
    }

    # Deterministic order: board grouping first, then id.
    return @($result | Sort-Object @{ Expression = { $StatusRank[$_.status] } }, @{ Expression = { $_.id } })
}

# Enumerates .agent/memory/**/*.md as { path, title }, index.md first, everything else by path.
function Read-MemoryIndex {
    $items = @()
    if (-not (Test-Path $memoryRoot)) { return $items }

    foreach ($file in Get-ChildItem -Path $memoryRoot -Recurse -File -Filter '*.md') {
        $rel = $file.FullName.Substring($repoRoot.Length).TrimStart('\', '/').Replace('\', '/')
        $title = $file.BaseName
        foreach ($line in [System.IO.File]::ReadAllLines($file.FullName)) {
            if ($line -match '^#\s+(.+)$') { $title = $Matches[1].Trim(); break }
        }
        $items += [ordered]@{ path = $rel; title = $title }
    }

    return @($items |
        Sort-Object @{ Expression = { if ($_.path -eq '.agent/memory/index.md') { 0 } else { 1 } } }, @{ Expression = { $_.path } })
}

function Get-ActiveTicketId {
    $statePath = Join-Path $agentRoot 'state/current.json'
    if (-not (Test-Path $statePath)) { return $null }
    try {
        $state = [System.IO.File]::ReadAllText($statePath) | ConvertFrom-Json
        return $state.activeTicketId
    }
    catch {
        Add-Warn "state/current.json is not valid JSON - falling back to status-based detection"
        return $null
    }
}

# JSON structural syntax never contains < > &, so escaping them only ever touches string values.
# This makes '</script>' impossible inside the payload while staying valid, parseable JSON.
function ConvertTo-EmbeddableJson {
    param($Data)
    $json = $Data | ConvertTo-Json -Depth 12 -Compress
    $esc = @{ '<' = 'u003c'; '>' = 'u003e'; '&' = 'u0026' }
    foreach ($ch in $esc.Keys) {
        $json = $json.Replace($ch, ('\' + $esc[$ch]))
    }
    return $json
}

$script:TicketCount = 0

function Build-Board {
    $tickets = Read-Tickets
    $memory = Read-MemoryIndex
    $script:TicketCount = @($tickets).Count

    if ($Problems.Count -gt 0) {
        Write-Host "Ticket validation failed:" -ForegroundColor Red
        $Problems | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
        exit 1
    }

    # Data-derived, not wall-clock: a render timestamp would break -Check on every run.
    $latest = [string](@($tickets | ForEach-Object { $_.updatedUtc }) | Sort-Object -Descending | Select-Object -First 1)

    $payload = [ordered]@{
        generatedFrom  = '.agent/tickets'
        activeTicketId = Get-ActiveTicketId
        updatedUtc     = $latest
        tickets        = @($tickets)
        memory         = @($memory)
    }

    if (-not (Test-Path $templatePath)) { throw "Board template not found at $templatePath" }
    $template = [System.IO.File]::ReadAllText($templatePath)

    return $template.
        Replace('/*__BOARD_DATA__*/ null', (ConvertTo-EmbeddableJson $payload)).
        Replace('<!--__BANNER__-->', $Banner)
}

$html = Build-Board

if ($Check) {
    $tmp = Join-Path ([System.IO.Path]::GetTempPath()) ("mango-board-" + [System.Guid]::NewGuid().ToString('N') + ".html")
    try {
        Write-FileNoBom -Path $tmp -Content $html
        $expected = (Get-FileHash -Path $tmp -Algorithm SHA256).Hash
        $actual = if (Test-Path $boardPath) { (Get-FileHash -Path $boardPath -Algorithm SHA256).Hash } else { '' }
        if ($expected -ne $actual) {
            Write-Warning "'.agent/ui/board.html' is STALE. Run: pwsh ./scripts/update-ticket-board.ps1"
            exit 1
        }
        Write-Host ".agent/ui/board.html is up to date." -ForegroundColor Green
        exit 0
    }
    finally {
        Remove-Item -Path $tmp -Force -ErrorAction SilentlyContinue
    }
}

Write-FileNoBom -Path $boardPath -Content $html

Write-Host "Generated .agent/ui/board.html ($script:TicketCount ticket(s))." -ForegroundColor Green
if ($Warnings.Count -gt 0) {
    Write-Warning "Ticket data warnings:"
    $Warnings | Sort-Object -Unique | ForEach-Object { Write-Host "  $_" -ForegroundColor Yellow }
}

if ($Open) { Start-Process $boardPath }
