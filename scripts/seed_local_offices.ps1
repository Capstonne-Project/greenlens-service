$ErrorActionPreference = "Stop"

$WardsJson = "D:\LEARNING\S9SU26\SEP490\dataPostgre\wards_202605301348.json"
$DeptsJson = "D:\LEARNING\S9SU26\SEP490\dataPostgre\departments_202605301359.json"
$OutputSql = "D:\LEARNING\S9SU26\SEP490\dataPostgre\seed_local_offices.sql"

$wards = (Get-Content $WardsJson -Raw -Encoding UTF8 | ConvertFrom-Json).wards
$depts = (Get-Content $DeptsJson -Raw -Encoding UTF8 | ConvertFrom-Json).departments

Write-Host "Loaded $($wards.Count) wards, $($depts.Count) departments"

# Build lookup: province_code -> department id
$deptByProvince = @{}
foreach ($d in $depts) {
    $deptByProvince[$d.province_code] = $d.id
}

# Group wards by province
$wardsByProvince = $wards | Group-Object province_code

$lines = [System.Collections.Generic.List[string]]::new()
$lines.Add("-- Seed: LocalOffices (1 per ward, linked to real department IDs)")
$lines.Add("-- Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')")
$lines.Add("-- Wards: $($wards.Count), Departments: $($depts.Count)")
$lines.Add("")
$lines.Add("BEGIN;")
$lines.Add("")
$lines.Add("INSERT INTO local_offices (id, name, department_id, ward_code, officer_id, is_onboarded, created_at)")
$lines.Add("VALUES")

$values = [System.Collections.Generic.List[string]]::new()
$skipped = 0

foreach ($group in ($wardsByProvince | Sort-Object Name)) {
    $pc = $group.Name
    $deptId = $deptByProvince[$pc]

    if (-not $deptId) {
        Write-Host "WARN: No department for province_code=$pc, skipping $($group.Group.Count) wards"
        $skipped += $group.Group.Count
        continue
    }

    foreach ($ward in ($group.Group | Sort-Object code)) {
        $wc = $ward.code
        $wn = $ward.name -replace "'", "''"
        $id = [guid]::NewGuid()
        $values.Add("  ('$id', 'VP MTDT $wn', '$deptId', '$wc', null, true, NOW())")
    }
}

$lines.Add(($values -join ",`r`n"))
$lines.Add("ON CONFLICT (ward_code) DO NOTHING;")
$lines.Add("")
$lines.Add("COMMIT;")
$lines.Add("")
$lines.Add("-- Summary: $($values.Count) offices inserted, $skipped skipped")

$content = $lines -join "`r`n"
[System.IO.File]::WriteAllText($OutputSql, $content, [System.Text.UTF8Encoding]::new($false))
Write-Host "Done: $($values.Count) local offices -> $OutputSql (skipped: $skipped)"
