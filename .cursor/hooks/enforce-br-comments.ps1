# Hook: afterFileEdit
# Purpose: When a Handler file in Application is edited, check it has a BR XML doc comment
#          ("Implements: BR-XXX-NNN"). If missing, warn the agent via additional_context.

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

$filePath = [string]$payload.file_path
if ([string]::IsNullOrWhiteSpace($filePath)) { Write-Output '{}'; exit 0 }
if (-not $filePath.EndsWith('.cs')) { Write-Output '{}'; exit 0 }
if (-not (Test-Path $filePath)) { Write-Output '{}'; exit 0 }

$normalized = $filePath.Replace('\', '/')

# Only inspect Handler.cs files inside Application/Features.
$isHandler = $normalized -match '/src/Greenlens\.Application/Features/.+/(.+)Handler\.cs$'
if (-not $isHandler) { Write-Output '{}'; exit 0 }

$content = Get-Content -Path $filePath -Raw

# Must contain at least one "Implements: BR-XXX-NNN" reference inside an XML doc <remarks> block.
$hasBr = $content -match 'Implements:\s*BR-[A-Z]+-\d{3}'

if ($hasBr) {
    Write-Output '{}'
    exit 0
}

$msg = @"
Handler file '$filePath' is missing a BR traceability comment.
Add an XML doc comment to the handler class:

/// <summary>...</summary>
/// <remarks>
/// Implements: BR-REP-001 (photo required), BR-REP-003 (Vietnam GPS bounds), ...
/// </remarks>

See .cursor/rules/04-business-rules-traceability.mdc for the required format.
"@

@{ additional_context = $msg } | ConvertTo-Json -Compress | Write-Output
exit 0
