---
name: greenlens-vertical-slice
description: >
  Scaffolds a complete vertical slice in the GreenLens Application layer following CQRS, Result
  pattern, and BR traceability. Generates Command/Handler/Validator/Response files in the correct
  folder with consistent BR XML doc, FluentValidation, MediatR registration, and pipeline behavior
  hooks. Use when adding a new use case under `src/Greenlens.Application/Features/<Module>/`.
  Triggers: "scaffold slice", "new use case", "create command", "add handler".
---

# GreenLens — Vertical Slice Scaffold

## Inputs

| Field | Example |
|-------|---------|
| Module | `Reports` |
| Use case | `SubmitReport` |
| Type | `Command` (mutate) or `Query` (read) |
| Result type | `Guid`, `ReportDto`, `PagedList<ReportDto>` |
| BR IDs | `BR-REP-001, BR-REP-003, BR-REP-005, BR-REP-010, BR-REP-013` |
| Auth policy | `Policies.CanSubmitReport` (or `[AllowAnonymous]`) |

## Folder layout

```
src/Greenlens.Application/Features/<Module>/<UseCase>/
├── <UseCase>Command.cs        // or <UseCase>Query.cs
├── <UseCase>CommandHandler.cs // or <UseCase>QueryHandler.cs
├── <UseCase>CommandValidator.cs
└── <UseCase>Response.cs       // optional
```

## Templates

### Command record

```csharp
namespace Greenlens.Application.Features.Reports.SubmitReport;

public sealed record SubmitReportCommand(
    PollutionType Type,
    double Latitude,
    double Longitude,
    IReadOnlyList<Guid> MediaIds,
    string? Description) : IRequest<Result<Guid>>;
```

### Validator (FluentValidation)

```csharp
namespace Greenlens.Application.Features.Reports.SubmitReport;

public sealed class SubmitReportCommandValidator : AbstractValidator<SubmitReportCommand>
{
    public SubmitReportCommandValidator()
    {
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Latitude).InclusiveBetween(8.0, 24.0)
            .WithErrorCode("INVALID_GPS")                                       // BR-REP-003
            .WithMessage("Latitude must be within Vietnam (8.0 - 24.0).");
        RuleFor(x => x.Longitude).InclusiveBetween(102.0, 110.0)
            .WithErrorCode("INVALID_GPS")                                       // BR-REP-003
            .WithMessage("Longitude must be within Vietnam (102.0 - 110.0).");
        RuleFor(x => x.MediaIds).NotEmpty().WithErrorCode("PHOTO_REQUIRED");    // BR-REP-001
        RuleFor(x => x.MediaIds.Count).LessThanOrEqualTo(5)
            .WithErrorCode("TOO_MANY_IMAGES");                                  // BR-REP-002
        RuleFor(x => x.Description).MaximumLength(2000);
    }
}
```

### Handler with BR XML doc

```csharp
namespace Greenlens.Application.Features.Reports.SubmitReport;

/// <summary>
/// Submit a new pollution report from a Citizen.
/// </summary>
/// <remarks>
/// Implements: BR-REP-001 (photo required), BR-REP-003 (Vietnam GPS bounds),
/// BR-REP-005 (category required), BR-REP-010 (rate limit 5/h, 20/24h),
/// BR-REP-013 (initial state Submitted), BR-REP-030 (duplicate detection).
/// </remarks>
public sealed class SubmitReportCommandHandler(
    IApplicationDbContext db,
    ICurrentUser currentUser,
    IRateLimiter rateLimiter,
    IReportDeduplicator dedup)
    : IRequestHandler<SubmitReportCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(SubmitReportCommand cmd, CancellationToken ct)
    {
        var allowed = await rateLimiter
            .AllowAsync($"report:{currentUser.Id}", limit: 5, window: TimeSpan.FromHours(1), ct)
            .ConfigureAwait(false);
        if (!allowed)
            return Result.Failure<Guid>(Errors.Reports.RateLimitExceeded);      // BR-REP-010

        var location = GeoLocation.Create(cmd.Latitude, cmd.Longitude);         // BR-REP-003

        var report = Report.Create(
            cmd.Type,
            location,
            currentUser.Id,
            cmd.Description,
            cmd.MediaIds);

        if (await dedup.IsDuplicateAsync(report, ct).ConfigureAwait(false))
            return Result.Failure<Guid>(Errors.Reports.Duplicate);              // BR-REP-030

        db.Reports.Add(report);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return Result.Success(report.Id);
    }
}
```

### Response (optional)

Only create when the API needs a custom shape distinct from `Result<T>`:

```csharp
namespace Greenlens.Application.Features.Reports.SubmitReport;

public sealed record SubmitReportResponse(Guid Id, string Status, DateTime CreatedAt);
```

## Post-scaffold checklist

- [ ] MediatR auto-discovers the handler (no manual registration needed if `AddMediatR` scans the assembly).
- [ ] FluentValidation auto-discovers the validator via `AddValidatorsFromAssembly`.
- [ ] Errors added to `Application/Common/Errors/Errors.<Module>.cs` (`Errors.Reports.RateLimitExceeded`, etc.).
- [ ] If a new interface (`IRateLimiter`, `IReportDeduplicator`) was used, define it in
      `Application/Common/Interfaces/` and implement in `Infrastructure/`.
- [ ] BR XML doc lists every BR ID this handler implements.
- [ ] All async paths flow `CancellationToken` and use `ConfigureAwait(false)`.
- [ ] Add a controller action in `src/Greenlens.Api/Controllers/<Module>Controller.cs`.

## Hand-off

After scaffolding, hand off to `test` agent to add unit + integration tests for every BR ID listed.
