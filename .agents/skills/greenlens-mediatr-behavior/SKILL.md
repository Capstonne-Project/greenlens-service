---
name: greenlens-mediatr-behavior
description: Scaffold or modify a MediatR pipeline behavior (validation, logging, transaction, caching, performance, authorization) for the Greenlens .NET 9 backend (project SU26SE049). Use this skill whenever the user mentions "pipeline behavior", "MediatR behavior", "IPipelineBehavior", "cross-cutting concern", or asks to add validation/logging/caching/timing across handlers — including casual phrasings like "log every command", "cache the GetReport query", "add a behavior that times each handler", "wire FluentValidation into MediatR", or "auto-rollback on validation failure". Trigger this BEFORE adding the same logic to individual handlers — that's the smell this skill exists to prevent. Produces the behavior class in Application/Common/Behaviors/, plus DI registration order. Pushes back if the request is about a single-handler concern that doesn't belong in a pipeline.
---

# Greenlens MediatR Pipeline Behaviors

MediatR pipeline behaviors are how Greenlens implements **cross-cutting concerns**: validation, logging, transactions, caching, performance monitoring, authorization. The pattern is core to the project (OVERVIEW.md §4.12 references `TransactionBehavior`).

## When to use

Trigger when the user mentions:
- "pipeline behavior", "MediatR behavior", "IPipelineBehavior"
- "cross-cutting concern", "every handler should …"
- "validation behavior", "logging behavior", "transaction behavior", "caching behavior"
- "wire FluentValidation into MediatR"
- "log every command/query", "time each handler", "auto-rollback on …"

## Step 0 — Sanity check

Before scaffolding, ask: **does this concern belong to every handler, or only one?**

- ✅ Pipeline material: validation (every command), transaction (every command), structured logging (every request), caching (every query), authorization gate (every command).
- ❌ Single-handler material: business invariant on Report submission, role check that's specific to one verb, complex multi-step orchestration.

If it's single-handler material, push back:

> "That logic is specific to one handler — putting it in a pipeline behavior would run it for every request and slow them all down. Keep it in the handler. Pipeline behaviors are for concerns that touch all (or all of one kind) of requests."

## Available behaviors in Greenlens

The project's pipeline order matters — concerns wrap each other like onion layers:

```
ValidationBehavior         (1) reject early on bad input shape
  └── AuthorizationBehavior (2) deny early on missing permission
        └── TransactionBehavior (3) open tx if Command
              └── PerformanceBehavior (4) time + warn if slow
                    └── LoggingBehavior (5) structured log around the handler
                          └── CachingBehavior (6) check cache (queries only)
                                └── HANDLER
```

> Order in DI matters. `services.AddMediatR(cfg => { cfg.AddOpenBehavior(typeof(X<,>)); … })` — first registered = outermost.

## Workflow

1. **Identify which behavior** the user wants (or whether it's a new one). Match against `assets/`:
   - `validation-behavior.cs.template` — runs all FluentValidation `IValidator<TRequest>` registered for the request, aggregates errors, returns Result.Failure(Validation) before the handler runs.
   - `transaction-behavior.cs.template` — wraps Commands in a UoW transaction (already in OVERVIEW.md §4.12).
   - `logging-behavior.cs.template` — Serilog scope with RequestId + UserId, logs entry/exit + duration.
   - `performance-behavior.cs.template` — warns when a handler exceeds a threshold (default 500 ms).
   - `caching-behavior.cs.template` — opt-in caching for queries that implement `ICacheable`.
   - `authorization-behavior.cs.template` — reads `[RequireRole]` / `[RequirePolicy]` attributes from the request and short-circuits with Result.Failure(Forbidden).
2. **For an entirely new behavior**, confirm:
   - Trigger condition: every request? Only Commands? Only `IXxx` marker interface?
   - What it does: short-circuits, modifies, just observes?
   - Where in the pipeline: before/after which existing behavior?
3. **Generate the file** under `src/Greenlens.Application/Common/Behaviors/`.
4. **Show the user the DI registration line** and where it goes in the order.

## Conventions

- File location: `src/Greenlens.Application/Common/Behaviors/<Name>Behavior.cs`.
- Class is `sealed`, primary constructor for dependencies.
- Implements `IPipelineBehavior<TRequest, TResponse>` with `where TRequest : notnull`.
- Use `typeof(TRequest).Name` for logging — request DTOs are records, no nice `ToString` by default.
- Behaviors that short-circuit (validation, authorization) construct a failed `TResponse` via reflection or a marker interface (`IResultBase`); see template.
- Never throw `DomainException` from a behavior — return failed Result. Throw only for infrastructure failures (e.g. cache deserialization broken).
- Behaviors are stateless. No instance fields. Inject `IMemoryCache`/`IDistributedCache`/`IUnitOfWork` per request.
- `CancellationToken` flows through every `await`.

## Self-check

- [ ] File in correct folder, named `<Concern>Behavior.cs`
- [ ] Implements `IPipelineBehavior<TRequest, TResponse>`
- [ ] DI registration shown to user with clear order vs existing behaviors
- [ ] Behavior is stateless
- [ ] Short-circuits via Result, not exceptions
- [ ] CancellationToken propagated
- [ ] If the behavior only applies to subset (Commands only, marked-cacheable only) — convention is documented in code

## Templates

- `assets/validation-behavior.cs.template`
- `assets/transaction-behavior.cs.template` (also covered in `greenlens-repository-pattern`)
- `assets/logging-behavior.cs.template`
- `assets/performance-behavior.cs.template`
- `assets/caching-behavior.cs.template` — uses `ICacheable` marker on the query
- `assets/authorization-behavior.cs.template` — uses `[RequireRole]` attribute
- `assets/registration-snippet.cs.template` — the exact `services.AddMediatR(...)` block with order

## Common follow-up traps

- "Add caching to `GetReportByIdQuery`" — don't bake cache into the handler. Mark the query with `ICacheable` (interface from `caching-behavior.cs.template`) and the behavior handles it.
- "Wrap the handler in try/catch for nice errors" — DON'T. Exceptions flow to `ExceptionHandlingMiddleware` (OVERVIEW.md §9). A behavior that swallows exceptions hides bugs.
- "Add a behavior that auto-saves changes" — there's already `TransactionBehavior` + UoW. Don't add `SaveChangesBehavior` — handler explicitly calls `uow.SaveChangesAsync`.
