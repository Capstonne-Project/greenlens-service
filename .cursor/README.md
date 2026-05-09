# GreenLens — Cursor Configuration

> Project-level Cursor configuration for the **SU26SE049** GreenLens Pollution Reporting backend.
> Everything in this folder is committed and shared with the team.

## Layout

```
.cursor/
├── rules/                     # Persistent guidance (.mdc) — auto-applied by Cursor
│   ├── 00-clean-architecture.mdc      (alwaysApply)
│   ├── 01-csharp-dotnet9.mdc          (globs **/*.cs)
│   ├── 02-cqrs-result-pattern.mdc     (globs Application/**)
│   ├── 03-api-envelope.mdc            (globs Api/**)
│   ├── 04-business-rules-traceability.mdc  (alwaysApply)
│   ├── 05-database-ef-core.mdc        (globs Infrastructure/**)
│   ├── 06-auth-security.mdc           (alwaysApply)
│   ├── 07-testing.mdc                 (globs tests/**)
│   ├── 08-performance.mdc             (alwaysApply)
│   ├── 09-no-secrets.mdc              (alwaysApply)
│   └── 10-domain-purity.mdc           (globs Domain/**)
│
├── agents/                    # Subagents — each runs in its own context window
│   ├── scout.md               # readonly recon
│   ├── debug.md               # bug investigation
│   ├── api-actor.md           # writes vertical slices end-to-end
│   ├── research.md            # finds best solutions (web + project constraints)
│   ├── performance.md         # readonly perf audit
│   ├── security.md            # readonly OWASP/BR audit
│   ├── fix.md                 # smallest-correct-change bug fixer
│   └── test.md                # writes/runs tests, BR coverage
│
├── skills/                    # Slash commands + reusable workflows
│   ├── execute/SKILL.md               # /execute — scout→api-actor→performance→security→test
│   ├── fix/SKILL.md                   # /fix — debug→fix→test
│   ├── greenlens-vertical-slice/      # scaffold a CQRS slice
│   ├── greenlens-br-trace/            # BR coverage matrix
│   ├── greenlens-api-contract/        # API contract + Swagger + Postman
│   └── greenlens-migration/           # EF Core 9 migration guidance
│
├── hooks.json                 # Hook registration
├── hooks/                     # Hook scripts (PowerShell, Win5.1+ compatible)
│   ├── session-context.ps1            (sessionStart)
│   ├── scan-secrets.ps1               (beforeSubmitPrompt, failClosed)
│   ├── audit-shell.ps1                (beforeShellExecution, ask on destructive)
│   ├── block-domain-ef-leak.ps1       (afterFileEdit, warn on Clean Arch leak)
│   ├── enforce-br-comments.ps1        (afterFileEdit, warn if Handler missing BR XML)
│   ├── format-on-save.ps1             (afterFileEdit, dotnet format)
│   └── log-subagent.ps1               (subagentStart, logs to .cursor/logs/)
│
└── logs/                      # Created at runtime; gitignored
    └── subagents.log
```

## How to use

### Slash commands

In the Cursor chat, type:

| Command | What it does |
|---------|--------------|
| `/execute` | Full feature delivery: scout → implement → perf → security → test |
| `/fix` | Bug pipeline: debug → fix → regression test |
| `/greenlens-vertical-slice` | Scaffold a new CQRS slice (Command/Handler/Validator) |
| `/greenlens-br-trace` | Generate BR coverage matrix |
| `/greenlens-api-contract` | Produce API contract + Swagger + Postman |
| `/greenlens-migration` | EF Core 9 migration guide |

### Invoking a single subagent directly

```
@scout map the code for adding the cleanup-team check-in endpoint
@debug investigate why POST /v1/pollution-reports returns 500 on valid payload
@api-actor implement the SubmitReport vertical slice per the scout report
@research compare Hangfire vs Quartz.NET vs Coravel for our background jobs
@performance audit the changes I just made to the map endpoints
@security audit the new admin user-impersonation endpoint
@fix apply the regression fix described in the debug report
@test add coverage for BR-OFF-002 (officer SLA verify within 24h)
```

Each subagent runs in its OWN context window — the parent chat only carries forward the
structured summary, keeping the main context lean.

### Rules

Rules apply automatically based on `globs` and `alwaysApply` flags. You don't invoke them
manually. Cursor injects them into the system prompt when relevant files are open.

## Hooks behavior

| Event | Hook | Mode | Notes |
|-------|------|------|-------|
| sessionStart | `session-context.ps1` | inject context | Prints project banner + reminders. |
| beforeSubmitPrompt | `scan-secrets.ps1` | **failClosed** | Blocks prompts containing AWS/GCP/JWT/PEM/Slack/GitHub keys. |
| beforeShellExecution | `audit-shell.ps1` | ask | Asks before destructive commands (force push, hard reset, db drop, rm -rf, DROP TABLE, etc.). |
| afterFileEdit | `block-domain-ef-leak.ps1` | warn | Surfaces a warning if a Domain/Application file imports forbidden namespaces. |
| afterFileEdit | `enforce-br-comments.ps1` | warn | Warns if a Handler file lacks `Implements: BR-XXX-NNN` XML doc. |
| afterFileEdit | `format-on-save.ps1` | silent | Runs `dotnet format` on the edited C# file. |
| subagentStart | `log-subagent.ps1` | log | Appends to `.cursor/logs/subagents.log`. |

### Hooks requirements

- Windows PowerShell **5.1+** (already on every Windows 10/11 machine).
- `dotnet` CLI on PATH for `format-on-save.ps1` (skipped silently if missing).
- All scripts run from the **project root** (Cursor sets the cwd automatically for project hooks).

### Verifying hooks loaded

In Cursor, open **Settings → Hooks** tab to confirm the 6 hooks are listed without errors.
You can also tail the hook output channel for live diagnostics.

## Pre-existing skills

The repository also has skills under `.agents/skills/` (older project layout):

- `greenlens-csharp-conventions`
- `greenlens-plan`
- `greenlens-build`
- `greenlens-test`
- `greenlens-release`

These are still discovered by Cursor (it scans both `.agents/skills/` and `.cursor/skills/`).
Treat them as the **upstream "process" skills** (Plan → Build → Test → Release lifecycle) and
the new `.cursor/skills/` as **task-level slash commands**. They complement each other.

## When to add a new...

| Need | Add a... | Where |
|------|----------|-------|
| Persistent rule that applies to many tasks | Rule | `.cursor/rules/` |
| Specialized "persona" for a recurring task type | Subagent | `.cursor/agents/` |
| Multi-step workflow user can invoke with `/name` | Skill | `.cursor/skills/` |
| Automated check on agent events | Hook | `.cursor/hooks/` + register in `hooks.json` |

## Definition of "merge ready" (the rules + skills agree)

- [ ] `dotnet build` clean (0 warnings)
- [ ] `dotnet test` green
- [ ] No EF Core in Domain/Application (hook warned, fixed)
- [ ] Handler has `Implements: BR-XXX-NNN` XML doc (hook warned, fixed)
- [ ] No secrets in source (hook + manual review)
- [ ] BR coverage matrix is 100% green for the change
- [ ] API contract documented (Swagger + Postman)
- [ ] Migration named `yyyyMMddHHmm_VerbNoun`, reversible
- [ ] Conventional Commit message lists BR IDs

## Troubleshooting

- **Hook didn't fire** → Restart Cursor after editing `hooks.json`. Check **Settings → Hooks** tab
  for parse errors.
- **Hook blocks normal work** → All hooks except `scan-secrets` are `failClosed: false` and only
  warn. If `scan-secrets` blocks a legitimate prompt, refine the regex in
  `.cursor/hooks/scan-secrets.ps1`.
- **`format-on-save` slow** → Increase the `timeout` in `hooks.json` or remove the hook entry if
  you prefer `dotnet format` only on commit.
- **Subagent not found** → Subagent files must live directly in `.cursor/agents/` (no nesting).
  See [Cursor docs](https://cursor.com/docs/agent/subagents).

## Related project files

- `CLAUDE.md` — project conventions and BR-module mapping.
- `00_API_CONVENTIONS.md` — API envelope, error codes, pagination, headers.
- `SU26SE049_BusinessRules_v1_0.docx` — source of truth for BR-XXX-NNN.
