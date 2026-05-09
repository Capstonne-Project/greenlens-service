# Hook: beforeSubmitPrompt
# Purpose: Block prompts that contain obvious secrets (connection strings, JWT keys, AWS keys,
#          Firebase service account JSON, private keys). Fails CLOSED when matched.

$ErrorActionPreference = 'Stop'

$inputJson = [Console]::In.ReadToEnd()
if ([string]::IsNullOrWhiteSpace($inputJson)) {
    Write-Output '{}'
    exit 0
}

try {
    $payload = $inputJson | ConvertFrom-Json
} catch {
    Write-Output '{}'
    exit 0
}

$prompt = $payload.prompt
if ([string]::IsNullOrWhiteSpace($prompt)) {
    Write-Output '{}'
    exit 0
}

# Patterns that almost certainly indicate a real secret (not a placeholder).
$patterns = @(
    @{ Name = 'AWS access key';       Pattern = 'AKIA[0-9A-Z]{16}' },
    @{ Name = 'AWS temp key';         Pattern = 'ASIA[0-9A-Z]{16}' },
    @{ Name = 'Google API key';       Pattern = 'AIza[0-9A-Za-z\-_]{35}' },
    @{ Name = 'Firebase service JSON';Pattern = '"private_key":\s*"-----BEGIN PRIVATE KEY-----' },
    @{ Name = 'PEM private key';      Pattern = '-----BEGIN (RSA |EC |OPENSSH |DSA )?PRIVATE KEY-----' },
    @{ Name = 'JWT signing key (Jwt:Key)'; Pattern = '"Jwt:Key"\s*:\s*"[^"]{32,}"' },
    @{ Name = 'Postgres URL with password'; Pattern = 'postgres(ql)?://[^:\s]+:[^@\s]+@' },
    @{ Name = 'Generic Bearer token (long)'; Pattern = 'Bearer\s+[A-Za-z0-9\-_\.]{80,}' },
    @{ Name = 'Slack token';          Pattern = 'xox[baprs]-[A-Za-z0-9-]{10,}' },
    @{ Name = 'GitHub PAT';           Pattern = 'ghp_[A-Za-z0-9]{36}' }
)

$matched = @()
foreach ($p in $patterns) {
    if ($prompt -match $p.Pattern) {
        $matched += $p.Name
    }
}

if ($matched.Count -gt 0) {
    $message = "Blocked: prompt appears to contain secrets ({0}). Use 'dotnet user-secrets' or env vars instead." -f ($matched -join ', ')
    @{
        permission     = 'deny'
        user_message   = $message
        agent_message  = "Hook scan-secrets blocked the prompt. Detected: $($matched -join ', ')."
    } | ConvertTo-Json -Compress | Write-Output
    exit 0
}

Write-Output '{ "permission": "allow" }'
exit 0
