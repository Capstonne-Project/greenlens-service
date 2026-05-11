---
name: greenlens-domain-entity
description: Scaffold a Domain entity (aggregate root) with state machine, domain events, and invariant-enforcing methods for the Greenlens .NET 9 backend (project SU26SE049). Use this skill whenever the user asks to create, model, or extend a Domain entity — including casual phrasings like "create a Report entity", "add a state machine for reviews", "model the User aggregate", "add a domain event when X changes", "wire up state transitions for cleanup tasks", or "scaffold the Badge entity". Trigger this BEFORE the user asks for a feature slice that needs the entity, since slice scaffolding assumes the entity already exists. Produces the entity class with private setters, factory method, transition methods returning Result, owned value objects, domain event raising, and IEntityTypeConfiguration<T> hint. Pushes back on anemic entities (public setters everywhere, no behavior).
---

# Greenlens Domain Entity (Aggregate Root)

This skill scaffolds **Domain entities** — the heart of the system. Project rule (OVERVIEW.md §3): Domain layer has zero framework dependencies, and entities own their invariants via methods, not via setters from outside.

## When to use

Trigger when the user mentions:
- "entity", "aggregate", "aggregate root", "Domain model"
- A specific entity name (`Report`, `User`, `CleanupTeam`, `Badge`, `Comment`, `TaskAssignment`, `District`, …)
- "state machine", "state transition", "status workflow"
- "domain event", "raise an event when …"
- "model the X", "add the X entity"

## Step 0 — Push back on anemic entities

If the user asks for "an entity with public properties for everything" or hands you a draft with public setters everywhere, push back:

> "In this codebase, Domain entities own their invariants via methods. Public setters bypass business rules and break the state machine. Let me write the entity with private setters + behavior methods (Verify, Reject, AssignTo, …). State changes happen through these methods, not via property assignment from handlers."

The state machine in OVERVIEW.md §5 is non-negotiable for `Report`, `User`, `CleanupTeam`, `Comment` — these have lifecycle. Lookup tables (`PollutionCategory`, `District`) can be simpler.

## Workflow

1. **Confirm with the user before writing files:**
   - Entity name (PascalCase singular, e.g. `Report`)
   - Aggregate or sub-entity? (Aggregate root has its own repo, lifecycle. Sub-entities like `ReportMedia`, `StatusHistoryEntry` belong inside an aggregate.)
   - Is it `AuditableEntity` (everything except lookup tables) or `SoftDeletableEntity` (User, Report, Comment per OVERVIEW.md §13.9)?
   - Does it have a state machine? If yes, list the states + valid transitions + trigger conditions (which role/event causes each).
   - Which BR IDs the entity implements (e.g. Report → BR-REP-013 initial state, BR-REP-020/021 state machine, BR-REP-030 dedup)
   - Domain events to raise (`ReportSubmittedEvent`, `ReportVerifiedEvent`, …)
   - Owned value objects (`GeoLocation`, `Money`, `Email`) — these go in `Domain/ValueObjects/`

2. **Pick the template:**
   - `assets/aggregate-root.cs.template` — full aggregate with factory + state machine + events
   - `assets/sub-entity.cs.template` — child entity inside an aggregate (no factory, no events of its own)
   - `assets/value-object.cs.template` — record struct or class for value objects
   - `assets/domain-event.cs.template` — `IDomainEvent` record

3. **Generate files in dependency order:**
   - Value objects first (`GeoLocation` before `Report` if `Report.Location: GeoLocation`)
   - Domain events (`ReportSubmittedEvent` before `Report` raises it)
   - Sub-entities before aggregate roots
   - Aggregate root last

4. **Stop and ask** if:
   - The entity references another entity that doesn't exist yet (avoid circular scaffolding)
   - The state machine has > 6 states or > 10 transitions — that often means 2 aggregates, not 1
   - The entity has > 7 properties at the same level — likely missing value objects to group them

## File layout produced

```
src/Greenlens.Domain/
├── Common/
│   ├── Entity.cs                       (base, one-time per project)
│   ├── AuditableEntity.cs              (one-time)
│   ├── SoftDeletableEntity.cs          (one-time)
│   └── IDomainEvent.cs                 (one-time)
├── Entities/
│   └── __ENTITY__.cs                   (the aggregate root)
├── ValueObjects/
│   └── __VALUEOBJECT__.cs
├── Events/
│   └── __ENTITY____EVENT__.cs
├── Enums/
│   └── __ENTITY__Status.cs             (state machine states as enum)
└── Errors/
    └── __ENTITY__Errors.cs             (Errors.<Entity>.<Code> static class)
```

## Conventions enforced (matches OVERVIEW.md §3 / §4)

- **No framework imports.** Zero `using Microsoft.*`, zero `using System.ComponentModel.DataAnnotations`. Domain is pure.
- **Aggregate root = `class`** (has identity + behavior). **Value object = `record` or `record struct`** (immutable, equality by value).
- **Private setters** on every property. State changes go through methods.
- **Factory method `Create(...)` returns `Result<TEntity>`** — enforces creation invariants. Constructor is `private` (or `protected` for EF re-hydration).
- **Transition methods return `Result`** (success/failure with `Error`), never throw `DomainException` for business-rule violations. Throwing is reserved for "this should never happen" programmer bugs.
- **State property has a private setter and is changed only by transition methods.** Status-change methods raise the corresponding domain event.
- **Domain events** are `record` types, accumulated in `Entity.DomainEvents` list, dispatched by `EfUnitOfWork` after `SaveChangesAsync` (OVERVIEW.md §4.12).
- **No public collection mutators.** Expose `IReadOnlyList<T>` views; mutations go through methods (`AddMedia`, `RemoveComment`).
- **No EF attributes** (`[Required]`, `[MaxLength]`). Configuration lives in `Infrastructure/Persistence/Configurations/` (see `greenlens-efcore-best-practices`).
- **Guid v7 for IDs**: `Guid.CreateVersion7()` generated in factory method (sequential, index-friendly on Postgres).

## State machine pattern

For entities with lifecycle (Report, User account status, CleanupTask), encode the state machine **inside the entity**:

```csharp
public Result Verify(Guid officerId, DateTimeOffset now)
{
    if (Status != ReportStatus.Submitted)
        return Result.Failure(Errors.Reports.InvalidTransition(Status, "Verify"));

    if (officerId == ReporterId)
        return Result.Failure(Errors.Officer.ConflictOfInterest);  // BR-OFF-004

    Status = ReportStatus.Verified;
    VerifiedAt = now;
    VerifiedBy = officerId;
    AddStatusHistory(officerId, ReportStatus.Verified, now);
    Raise(new ReportVerifiedEvent(Id, officerId, now));
    return Result.Success();
}
```

Each transition:
1. Guards the current state (no `if (Status == X)` checks scattered in handlers)
2. Validates business rules (`BR-OFF-004` segregation of duty)
3. Mutates state through private setters
4. Records the transition in history (for audit BR-ADM-010)
5. Raises a domain event

## Self-check before handing files back

- [ ] No `Microsoft.*` or framework imports in any Domain file
- [ ] Every property has a private (or protected) setter
- [ ] Constructor is `private` or `protected` — `Create()` factory is the only public construction path
- [ ] Every state transition method returns `Result` and raises a domain event
- [ ] `Status` (or equivalent state property) cannot be set from outside the entity
- [ ] Aggregate exposes child collections as `IReadOnlyList<T>` (backed by `private List<T>`)
- [ ] BR IDs are documented in XML comments on the relevant transition methods
- [ ] `Errors.<Entity>` static class lists all `Error` instances the entity returns

## Templates

- `assets/aggregate-root.cs.template` — full aggregate root
- `assets/sub-entity.cs.template` — child entity
- `assets/value-object.cs.template` — value object (record / record struct)
- `assets/domain-event.cs.template` — IDomainEvent record
- `assets/entity-base.cs.template` — Entity / AuditableEntity / SoftDeletableEntity base classes (one-time setup)

## Example interaction

**User:** "Create the Report entity with the state machine from BR-REP-020/021."

**Your response:**
1. Confirm: states = `Submitted`, `Verified`, `Rejected`, `Duplicate`, `InProgress`, `Resolved`, `Closed`. Transitions per BR-REP-020.
2. Confirm: inherits `SoftDeletableEntity` (BR-AUTH-022), has child collections `Media: IReadOnlyList<ReportMedia>`, `StatusHistory`.
3. Confirm domain events: `ReportSubmittedEvent`, `ReportVerifiedEvent`, `ReportRejectedEvent`, `ReportResolvedEvent`, `ReportClosedEvent`.
4. Generate value objects first (`GeoLocation`), then events, then `Report.cs`.
5. Tell user: "Entity scaffolded. Next: configuration (`greenlens-efcore-best-practices`) and slice (`greenlens-feature-slice` for SubmitReport, VerifyReport, …)."
