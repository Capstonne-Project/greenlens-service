---
name: greenlens-br-trace
description: >
  Generates and verifies BR (Business Rule) traceability for GreenLens code. Scans handlers,
  validators, jobs, and tests for BR ID coverage; produces a BR coverage matrix; flags BRs
  implemented without tests, tests without BR IDs, and handlers missing XML doc. Use before
  PR review or when the user asks for "BR coverage", "traceability", "BR matrix", "what BRs
  does this implement".
---

# GreenLens — BR Traceability

## What this skill does

1. Scans `src/Greenlens.Application/Features/**/*Handler.cs` for `BR-*-NNN` IDs in XML docs.
2. Scans `tests/**/*.cs` for test names containing `BR_*_NNN`.
3. Cross-references with the official BR list (module map in `CLAUDE.md §5`).
4. Produces a coverage matrix.

## Coverage matrix template

```markdown
| BR ID | Module | Implementation | Tests | Status |
|-------|--------|---------------|-------|--------|
| BR-REP-001 | Reports | `SubmitReportCommandHandler.cs:18` | `SubmitReportTests.NoPhoto_BR_REP_001` | ✅ |
| BR-REP-003 | Reports | `SubmitReportCommandHandler.cs:18` (validator) | `GeoLocationTests.OutOfVietnam_BR_REP_003` | ✅ |
| BR-REP-030 | Reports | `SubmitReportCommandHandler.cs:18` | — | 🟡 IMPLEMENTED, NO TEST |
| BR-OFF-002 | Officer | — | — | 🔴 NOT IMPLEMENTED |
| BR-AUTH-022 | Auth | `AccountHardDeleteJob.cs:12` | `AccountHardDeleteJobTests.After90Days_BR_AUTH_022` | ✅ |
```

## Status legend

- ✅ **Covered** — handler has BR in XML doc AND ≥ 1 test references the BR ID.
- 🟡 **Implemented, no test** — XML doc references BR but no test name contains the ID.
- 🔵 **Tested, not in handler XML** — test references BR but no handler XML doc lists it (probably OK for cross-cutting BRs like logging).
- 🔴 **Not implemented** — BR exists in `CLAUDE.md §5` but appears in neither handler nor test.

## Search patterns (use Grep)

```
# Find all BR IDs in handler XML docs
rg --type cs "BR-[A-Z]+-\d{3}" src/Greenlens.Application/Features

# Find all BR IDs in test names
rg --type cs "BR_[A-Z]+_\d{3}" tests

# Find handlers missing XML doc
rg --type cs -l "IRequestHandler<" src/Greenlens.Application | while read f; do
    rg -l "Implements: BR-" "$f" || echo "MISSING: $f"
done
```

## Output: pre-PR checklist

```markdown
## BR Coverage Pre-Merge Check

- [ ] Every BR ID in handler XML doc has ≥ 1 test
- [ ] No test references a BR ID that is not also in a handler XML doc (or documented as cross-cutting)
- [ ] No handler implements business logic without XML doc listing BR IDs
- [ ] Commit message lists all BR IDs touched by the change

## Findings
🟡 BR-REP-030 implemented in `SubmitReportCommandHandler` but no test
   → Action: ask `test` agent to add `SubmitReport_DuplicateWithin50m_..._BR_REP_030`

🔵 `LoginRateLimitTests` references BR-AUTH-011 but no handler XML doc lists it
   → Likely OK (rate-limit is enforced in middleware, not handler) — confirm with team

🔴 BR-OFF-002 (verify SLA 24h) appears in `CLAUDE.md §5` but no implementation found
   → Action: schedule for next sprint or remove from current scope
```

## When to run

- Before opening a PR.
- After running `/execute` or `/fix` to confirm BR coverage.
- During `release` checklist.
- Whenever the user asks "what BRs does X cover?"

## What this skill does NOT do

- Does not change the BR docx (source of truth is the docx).
- Does not auto-add XML docs (that's `api-actor`'s job).
- Does not auto-write tests (that's `test`'s job).
