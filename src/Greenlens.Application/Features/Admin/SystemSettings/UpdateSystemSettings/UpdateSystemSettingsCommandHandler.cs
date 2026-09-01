using System.Text.Json;
using Greenlens.Application.Common;
using Greenlens.Domain.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Features.Admin.SystemSettings;
using Greenlens.Application.Features.Admin.SystemSettings.GetSystemSettings;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Admin.SystemSettings.UpdateSystemSettings;

/// <summary>Bulk update settings within one module.</summary>
/// <remarks>Implements: BR-ADM-010.</remarks>
public sealed record UpdateSystemSettingsCommand(
    string Module,
    IReadOnlyDictionary<string, string> Values) : IRequest<Result<UpdateSystemSettingsResponse>>;

public sealed record UpdateSystemSettingsResponse(
    IReadOnlyList<SystemSettingItemDto> Updated);

public sealed class UpdateSystemSettingsCommandHandler(
    IApplicationDbContext db,
    ISystemSettingsCacheInvalidationCollector settingsCacheInvalidationCollector,
    IAuditLogger auditLogger,
    ILogger<UpdateSystemSettingsCommandHandler> logger)
    : IRequestHandler<UpdateSystemSettingsCommand, Result<UpdateSystemSettingsResponse>>
{
    public async Task<Result<UpdateSystemSettingsResponse>> Handle(
        UpdateSystemSettingsCommand request,
        CancellationToken ct)
    {
        if (!SystemSettingModuleCatalog.TryParseModule(request.Module, out var module))
            return Result<UpdateSystemSettingsResponse>.Failure(Errors.Admin.SystemSettingModuleNotFound);

        if (request.Values.Count == 0)
            return Result<UpdateSystemSettingsResponse>.Failure(Errors.Admin.SystemSettingUpdateEmpty);

        var settings = await db.Set<SystemSetting>()
            .Where(s => s.Module == module)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var byKey = settings.ToDictionary(s => s.Key, StringComparer.Ordinal);
        var updated = new List<SystemSettingItemDto>();
        var auditChanges = new List<object>();

        foreach (var (key, rawValue) in request.Values)
        {
            if (!byKey.TryGetValue(key, out var setting))
                return Result<UpdateSystemSettingsResponse>.Failure(Errors.Admin.SystemSettingKeyNotFound(key));

            if (!SystemSettingValueValidator.TryValidate(
                    setting.ValueType,
                    rawValue,
                    setting.MinValue,
                    setting.MaxValue,
                    out var normalized,
                    out var validationError))
            {
                return Result<UpdateSystemSettingsResponse>.Failure(
                    Errors.Admin.SystemSettingInvalidValue(key, validationError!));
            }

            if (string.Equals(setting.Value, normalized, StringComparison.Ordinal))
                continue;

            var oldValue = setting.Value;
            setting.UpdateValue(normalized!);
            auditChanges.Add(new { key, oldValue, newValue = normalized });

            updated.Add(new SystemSettingItemDto(
                setting.Id,
                setting.Module.ToString(),
                setting.Key,
                setting.Title,
                setting.Unit,
                setting.ValueType.ToString(),
                setting.Value,
                setting.DefaultValue,
                setting.Description,
                setting.MinValue,
                setting.MaxValue,
                setting.IsActive));
        }

        // Admin gửi giá trị trùng DB (vd. DB đã 200m, bấm Lưu lại 200m) — vẫn re-sync cache
        // để sửa trường hợp RAM còn giá trị cũ (100km) dù DB đã đúng.
        if (updated.Count == 0)
        {
            // Schedule — TransactionBehavior sẽ InvalidateAsync sau commit.
            settingsCacheInvalidationCollector.Schedule();

            logger.LogInformation(
                "System settings PATCH for module {Module} had no DB changes; cache re-sync scheduled",
                module);

            return Result<UpdateSystemSettingsResponse>.Success(new UpdateSystemSettingsResponse([]));
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        await auditLogger.LogAsync(
            "UpdateSystemSettings",
            "SystemSetting",
            module.ToString(),
            oldValues: JsonSerializer.Serialize(auditChanges.Select(c => new { type = "before", c })),
            newValues: JsonSerializer.Serialize(auditChanges),
            ct).ConfigureAwait(false);

        // Phải schedule (không InvalidateAsync trực tiếp): refresh trong transaction đọc DB cũ.
        settingsCacheInvalidationCollector.Schedule();

        logger.LogInformation(
            "Updated {Count} system setting(s) in module {Module}",
            updated.Count,
            module);

        return Result<UpdateSystemSettingsResponse>.Success(new UpdateSystemSettingsResponse(updated));
    }
}
