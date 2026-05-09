# Hook: afterFileEdit
# Purpose: Detect (and warn) when a file in Domain or Application contains forbidden imports.
#          Returns additional_context the agent will see; does not block.

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

# Only inspect Domain or Application source files.
$inDomain      = $normalized -match '/src/Greenlens\.Domain/'
$inApplication = $normalized -match '/src/Greenlens\.Application/'
if (-not ($inDomain -or $inApplication)) { Write-Output '{}'; exit 0 }

$content = Get-Content -Path $filePath -Raw

$violations = @()

if ($inDomain) {
    if ($content -match 'using\s+Microsoft\.EntityFrameworkCore') {
        $violations += 'Domain file imports Microsoft.EntityFrameworkCore.'
    }
    if ($content -match 'using\s+Microsoft\.AspNetCore') {
        $violations += 'Domain file imports Microsoft.AspNetCore.*.'
    }
    if ($content -match 'using\s+MediatR') {
        $violations += 'Domain file imports MediatR (use a local IDomainEvent marker instead).'
    }
    if ($content -match 'using\s+(AutoMapper|Mapster)') {
        $violations += 'Domain file imports a mapper (mapping belongs in Application/Infrastructure).'
    }
}

if ($inApplication) {
    # Allow only IApplicationDbContext to surface EF Core types; otherwise EF should not appear.
    if ($content -match 'using\s+Microsoft\.EntityFrameworkCore' `
            -and -not ($filePath -match 'IApplicationDbContext\.cs$')) {
        $violations += 'Application file imports Microsoft.EntityFrameworkCore (only IApplicationDbContext is allowed).'
    }
    if ($content -match 'IHttpContextAccessor') {
        $violations += 'Application file references IHttpContextAccessor (use ICurrentUser abstraction).'
    }
}

if ($violations.Count -eq 0) {
    Write-Output '{}'
    exit 0
}

$msg = "Clean Architecture leak detected in '$filePath':`n - " + ($violations -join "`n - ") +
       "`nFix before continuing. See .cursor/rules/00-clean-architecture.mdc and 10-domain-purity.mdc."

@{ additional_context = $msg } | ConvertTo-Json -Compress | Write-Output
exit 0
