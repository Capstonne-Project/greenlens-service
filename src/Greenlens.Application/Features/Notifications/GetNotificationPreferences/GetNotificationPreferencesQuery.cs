using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Application.Features.Notifications.GetNotificationPreferences;

/// <summary>Get the current user's notification channel preferences.</summary>
public sealed record GetNotificationPreferencesQuery
    : IRequest<Result<IReadOnlyList<PreferenceItem>>>;

public sealed record PreferenceItem(
    NotificationType Type,
    bool PushEnabled,
    bool EmailEnabled);

/// <remarks>Implements: BR-NTF-001 (user configures channels per type).</remarks>
internal sealed class GetNotificationPreferencesQueryHandler(
    ICurrentUser currentUser,
    INotificationPreferenceRepository prefRepo)
    : IRequestHandler<GetNotificationPreferencesQuery, Result<IReadOnlyList<PreferenceItem>>>
{
    public async Task<Result<IReadOnlyList<PreferenceItem>>> Handle(
        GetNotificationPreferencesQuery request, CancellationToken ct)
    {
        var userId = currentUser.UserId;

        var existing = await prefRepo.QueryAsNoTracking()
            .Where(p => p.UserId == userId)
            .ToListAsync(ct).ConfigureAwait(false);

        // Return all types, filling defaults for types not yet customized
        var allTypes = Enum.GetValues<NotificationType>();
        var result = allTypes.Select(type =>
        {
            var pref = existing.FirstOrDefault(p => p.Type == type);
            return new PreferenceItem(
                type,
                pref?.PushEnabled ?? true,
                pref?.EmailEnabled ?? true);
        }).ToList();

        return result;
    }
}
