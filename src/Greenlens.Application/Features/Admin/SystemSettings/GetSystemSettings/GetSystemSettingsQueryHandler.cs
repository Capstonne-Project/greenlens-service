using Greenlens.Application.Common;
using Greenlens.Domain.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Features.Admin.SystemSettings;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Application.Features.Admin.SystemSettings.GetSystemSettings;

/// <summary>List system settings, optionally filtered by module.</summary>
/// <remarks>Implements: BR-ADM-010.</remarks>
public sealed record GetSystemSettingsQuery(string? Module = null) : IRequest<Result<GetSystemSettingsResponse>>;

public sealed record GetSystemSettingsResponse(
    IReadOnlyList<SystemSettingItemDto> Items);

public sealed record SystemSettingItemDto(
    Guid Id,
    string Module,
    string Key,
    string Title,
    string? Unit,
    string ValueType,
    string Value,
    string DefaultValue,
    string Description,
    decimal? MinValue,
    decimal? MaxValue,
    bool IsActive);

public sealed class GetSystemSettingsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetSystemSettingsQuery, Result<GetSystemSettingsResponse>>
{
    public async Task<Result<GetSystemSettingsResponse>> Handle(
        GetSystemSettingsQuery request,
        CancellationToken ct)
    {
        SystemSettingModule? moduleFilter = null;
        if (!string.IsNullOrWhiteSpace(request.Module))
        {
            if (!SystemSettingModuleCatalog.TryParseModule(request.Module, out var parsed))
                return Result<GetSystemSettingsResponse>.Failure(Errors.Admin.SystemSettingModuleNotFound);

            moduleFilter = parsed;
        }

        var query = db.Set<SystemSetting>().AsNoTracking();
        if (moduleFilter.HasValue)
            query = query.Where(s => s.Module == moduleFilter.Value);

        var items = await query
            .OrderBy(s => s.Module)
            .ThenBy(s => s.Key)
            .Select(s => new SystemSettingItemDto(
                s.Id,
                s.Module.ToString(),
                s.Key,
                s.Title,
                s.Unit,
                s.ValueType.ToString(),
                s.Value,
                s.DefaultValue,
                s.Description,
                s.MinValue,
                s.MaxValue,
                s.IsActive))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return Result<GetSystemSettingsResponse>.Success(new GetSystemSettingsResponse(items));
    }
}
