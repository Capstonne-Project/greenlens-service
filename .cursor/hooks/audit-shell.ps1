# Hook: beforeShellExecution
# Purpose: Ask the user before running destructive or schema-mutating commands.
#          Fails OPEN (failClosed=false in hooks.json) so a hook bug never blocks routine work.

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

$command = [string]$payload.command
if ([string]::IsNullOrWhiteSpace($command)) {
    Write-Output '{ "permission": "allow" }'
    exit 0
}

# Patterns that should require explicit user confirmation.
$danger = @(
    @{ Name = 'git push --force';            Pattern = 'git\s+push.*--force\b' },
    @{ Name = 'git reset --hard';            Pattern = 'git\s+reset\s+--hard' },
    @{ Name = 'git clean -fd';               Pattern = 'git\s+clean\s+-[a-z]*f' },
    @{ Name = 'rm / Remove-Item -Recurse';   Pattern = '(\brm\s+-rf\b|\bRemove-Item\s+.*-Recurse)' },
    @{ Name = 'dotnet ef database drop';     Pattern = 'dotnet\s+ef\s+database\s+drop' },
    @{ Name = 'dotnet ef migrations remove'; Pattern = 'dotnet\s+ef\s+migrations\s+remove' },
    @{ Name = 'DROP TABLE / DROP DATABASE';  Pattern = '\bDROP\s+(TABLE|DATABASE|SCHEMA)\b' },
    @{ Name = 'TRUNCATE';                    Pattern = '\bTRUNCATE\s+TABLE\b' },
    @{ Name = 'docker volume rm';            Pattern = 'docker\s+volume\s+rm' },
    @{ Name = 'docker system prune -a';      Pattern = 'docker\s+system\s+prune.*-a' },
    @{ Name = 'kubectl delete';              Pattern = 'kubectl\s+delete\s+(pod|deployment|namespace|pvc)' }
)

foreach ($d in $danger) {
    if ($command -match $d.Pattern) {
        @{
            permission    = 'ask'
            user_message  = "About to run a destructive command ($($d.Name)). Confirm before continuing."
            agent_message = "Hook audit-shell flagged: $($d.Name). Command: $command"
        } | ConvertTo-Json -Compress | Write-Output
        exit 0
    }
}

Write-Output '{ "permission": "allow" }'
exit 0
