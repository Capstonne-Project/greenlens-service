---
name: fix
description: >
  Slash command `/fix` — bug-fix pipeline for the GreenLens .NET 9 backend. Chains debug → fix
  → test subagents to triage a defect, apply the smallest correct change, and add a regression
  test. Use when the user reports a bug, a failing test, an exception, a wrong response, or a
  regression. Triggers: `/fix`, "fix this bug", "the test is failing", "I'm getting a 500".
disable-model-invocation: true
---

# /fix — Bug-Fix Pipeline

> Chain: **debug → fix → test**
>
> Each stage runs in its own subagent context window via Task calls.

## Inputs

Ask the user (skip if already in the conversation):

1. **Symptom** — what's wrong? (HTTP status, error message, wrong value, failing test name).
2. **Reproduction** — exact request/payload, or test command.
3. **Expected behavior** — what should happen instead?
4. **Suspected BR** (optional) — if the user knows which BR is involved.

If symptom or reproduction is missing, use `AskQuestion` once.

## Pipeline

### Stage 1 — `debug` (investigate)

```
Task(subagent_type="debug",
     prompt="""Investigate this defect.

Symptom: <symptom>
Reproduction: <command/payload/test>
Expected: <expected behavior>
Suspected BR: <BR ID or "unknown">

Reproduce, locate, classify, and produce the debug report per your output template.""")
```

Carry forward: debug report (root cause classification, file:line, BR mapping, fix plan).

### Decision gate

Read the debug classification:

| Classification | Next stage |
|----------------|------------|
| Code Bug | → Stage 2 (`fix`) |
| Test Bug | → Stage 3 (`test`) directly to fix the test |
| Environment | STOP, surface setup gap to user |
| Flaky | → Stage 2 (`fix`) for root cause, never blind retry |
| BR Misinterpretation | STOP, ask user to clarify BR |
| Performance | Hand off to `/execute` performance stage |
| Security | Hand off to `/execute` security stage |

### Stage 2 — `fix` (apply smallest change)

```
Task(subagent_type="fix",
     prompt="""Apply the smallest correct fix.

Debug report (use as ground truth):
<paste debug report>

Add a regression test FIRST that fails on the bug, passes after the fix.
Run `dotnet build` and `dotnet test` after each change. Report the fix per your output template.""")
```

Carry forward: files touched, regression test name, test results.

### Stage 3 — `test` (verify + extend coverage)

```
Task(subagent_type="test",
     prompt="""Verify the fix and extend coverage if needed.

Fix report:
<paste fix report>

Run the full test suite. Confirm:
- The new regression test passes.
- No previously-passing test now fails.
- BR coverage for the affected BR ID is intact.

Return the BR coverage delta and any failures.""")
```

If any test fails → loop back to `fix`. Maximum 2 loops. If still failing → STOP and surface to user.

## Final report

```markdown
# /fix — <issue title>

## Status: ✅ Fixed

## Root cause
- File: `<file>:<line>`
- Category: Code Bug
- BR impacted: BR-XXX-NNN

## Change
- Files touched: <N>
- Regression test: `<TestName>_BR_XXX_NNN`

## Tests
- Suite: <N>/<N> passing
- New: 1 test added

## Suggested commit message
fix(<scope>): <one-line> (BR-XXX-NNN)
```

## Stop conditions

- Cannot reproduce in `debug` stage → STOP, surface to user.
- Fix loop > 2 iterations → STOP, surface to user.
- Root cause is BR misinterpretation → STOP, do NOT change code, ask user.
- Production code change touches > 5 files → STOP, this is not a "fix" anymore, escalate to `/execute`.

## What this skill does NOT do

- Does not refactor.
- Does not change the response envelope or error codes unless the bug IS that.
- Does not skip the regression test.
