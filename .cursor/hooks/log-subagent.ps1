# Hook: subagentStart
# Purpose: Log every subagent invocation to .cursor/logs/subagents.log so the team can
#          audit which agents were used during a session. Always allows the subagent.

$ErrorActionPreference = 'Stop'

$inputJson = [Console]::In.ReadToEnd()
if ([string]::IsNullOrWhiteSpace($inputJson)) {
    Write-Output '{ "permission": "allow" }'
    exit 0
}

try {
    $payload = $inputJson | ConvertFrom-Json
} catch {
    Write-Output '{ "permission": "allow" }'
    exit 0
}

$logDir = Join-Path (Get-Location) '.cursor/logs'
if (-not (Test-Path $logDir)) {
    New-Item -ItemType Directory -Path $logDir -Force | Out-Null
}

$logFile = Join-Path $logDir 'subagents.log'

$ts = (Get-Date).ToString('yyyy-MM-ddTHH:mm:ssK')
$type = [string]$payload.subagent_type
$desc = [string]$payload.description
$line = "$ts`t$type`t$desc"

try {
    Add-Content -Path $logFile -Value $line -ErrorAction SilentlyContinue
} catch { }

Write-Output '{ "permission": "allow" }'
exit 0
