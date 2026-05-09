# Hook: afterFileEdit
# Purpose: Format edited C# files via `dotnet format` so the agent's output stays
#          consistent with the project style. Fails OPEN; never blocks.

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

# Only format files that belong to a project (skip top-level scratch files).
$normalized = $filePath.Replace('\', '/')
if ($normalized -notmatch '/(src|tests)/') { Write-Output '{}'; exit 0 }

# Verify dotnet is on PATH; if missing, do nothing.
$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
if ($null -eq $dotnet) { Write-Output '{}'; exit 0 }

# Find the nearest .csproj (walk up from the file).
$dir = Split-Path -Parent $filePath
$proj = $null
while ($dir -and (Split-Path -Parent $dir) -ne $dir) {
    $found = Get-ChildItem -Path $dir -Filter *.csproj -File -ErrorAction SilentlyContinue
    if ($found) { $proj = $found[0].FullName; break }
    $dir = Split-Path -Parent $dir
}

if (-not $proj) { Write-Output '{}'; exit 0 }

# Run `dotnet format` for whitespace + style on this single file. Capture errors silently.
& dotnet format $proj --include $filePath --severity info --verbosity quiet 2>&1 | Out-Null

Write-Output '{}'
exit 0
