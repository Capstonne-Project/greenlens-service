using Greenlens.Application.Common;
using Greenlens.Domain.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Features.Admin.SystemSettings.GetSystemSettings;
using Greenlens.Domain.Entities;
using Greenlens.Application.Features.Admin.SystemSettings;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Greenlens.Application.Features.Admin.SystemSettings.ResetSystemSettingsModule;

/// <summary>Reset all settings in a module to seeded defaults.</summary>
/// <remarks>Implements: BR-ADM-010.</remarks>
public sealed record ResetSystemSettingsModuleCommand(string Module)
    : IRequest<Result<GetSystemSettingsResponse>>;

public sealed class ResetSystemSettingsModuleCommandHandler(
    IApplicationDbContext db,
    ISystemSettingsCache settingsCache,
    IAuditLogger auditLogger,
    ILogger<ResetSystemSettingsModuleCommandHandler> logger)
    : IRequestHandler<ResetSystemSettingsModuleCommand, Result<GetSystemSettingsResponse>>
{
    public async Task<Result<GetSystemSettingsResponse>> Handle(
        ResetSystemSettingsModuleCommand request,
        CancellationToken ct)
    {
        if (!SystemSettingModuleCatalog.TryParseModule(request.Module, out var module))
            return Result<GetSystemSettingsResponse>.Failure(Errors.Admin.SystemSettingModuleNotFound);

        var settings = await db.Set<SystemSetting>()
            .Where(s => s.Module == module)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        foreach (var setting in settings)
            setting.ResetToDefault();

        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        await auditLogger.LogAsync(
            "ResetSystemSettingsModule",
            "SystemSetting",
            module.ToString(),
            oldValues: null,
            newValues: JsonSerializer.Serialize(new { module = module.ToString(), count = settings.Count }),
            ct: ct).ConfigureAwait(false);

        await settingsCache.RefreshAsync(ct).ConfigureAwait(false);

        logger.LogInformation("Reset {Count} system setting(s) in module {Module}", settings.Count, module);

        var items = settings
            .OrderBy(s => s.Key)
            .Select(s => new SystemSettingItemDto(
                s.Id,
                s.Module.ToString(),
                s.Key,
                s.ValueType.ToString(),
                s.Value,
                s.DefaultValue,
                s.Description,
                s.MinValue,
                s.MaxValue,
                s.IsActive))
            .ToList();

        return Result<GetSystemSettingsResponse>.Success(new GetSystemSettingsResponse(items));
    }
}
