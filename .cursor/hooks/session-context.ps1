# Hook: sessionStart
# Purpose: Inject GreenLens project context banner so the agent always knows
# which project, BR docx, and conventions apply.

$ErrorActionPreference = 'Stop'

# Read stdin (sessionStart sends an empty/lightweight payload, but Cursor expects us to consume it).
$null = [Console]::In.ReadToEnd()

$projectRoot = (Get-Location).Path
$gitBranch = ''
try {
    $gitBranch = (git rev-parse --abbrev-ref HEAD 2>$null).Trim()
} catch { }

$brDocPath = Join-Path $projectRoot 'SU26SE049_BusinessRules_v1_0.docx'
$brDocStatus = if (Test-Path $brDocPath) { 'present' } else { 'MISSING - ask user to provide' }

$banner = @"
GreenLens Backend (SU26SE049)
.NET 9 / ASP.NET Core 9 / EF Core 9 / PostgreSQL 18 + PostGIS
Clean Architecture: Domain -> Application -> Infrastructure -> Api
Vertical slice + CQRS + Result pattern
API envelope: { code, message, status, data }
BR docx: $brDocStatus
Branch: $gitBranch

Active subagents: scout, debug, api-actor, research, performance, security, fix, test
Slash skills: /execute (full pipeline), /fix (debug+fix+test)

Reminders:
- Every business handler needs BR XML doc.
- No Microsoft.EntityFrameworkCore in Domain or Application (except IApplicationDbContext).
- All I/O methods take CancellationToken; library projects use ConfigureAwait(false).
- Never commit secrets. Use dotnet user-secrets in dev.
"@

$payload = @{
    additional_context = $banner
} | ConvertTo-Json -Depth 5 -Compress

Write-Output $payload
exit 0
