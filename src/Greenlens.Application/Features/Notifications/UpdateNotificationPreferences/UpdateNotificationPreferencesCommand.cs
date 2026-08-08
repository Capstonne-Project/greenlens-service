using FluentValidation;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Notifications.UpdateNotificationPreferences;

/// <summary>Update notification channel preferences for the current user (BR-NTF-001).</summary>
public sealed record UpdateNotificationPreferencesCommand(
    IReadOnlyList<PreferenceUpdate> Preferences) : IRequest<Result>;

public sealed record PreferenceUpdate(
    NotificationType Type,
    bool PushEnabled,
    bool EmailEnabled);

public sealed class UpdateNotificationPreferencesCommandValidator
    : AbstractValidator<UpdateNotificationPreferencesCommand>
{
    public UpdateNotificationPreferencesCommandValidator()
    {
        RuleFor(x => x.Preferences).NotEmpty()
            .WithMessage("Danh sách preferences không được rỗng.");

        RuleForEach(x => x.Preferences)
            .Must(p => Enum.IsDefined(p.Type))
            .WithMessage("Loại thông báo không hợp lệ.");
    }
}

/// <remarks>Implements: BR-NTF-001 (user configures push/email per notification type).</remarks>
internal sealed class UpdateNotificationPreferencesCommandHandler(
    ICurrentUser currentUser,
    INotificationPreferenceRepository prefRepo,
    IUnitOfWork unitOfWork,
    ILogger<UpdateNotificationPreferencesCommandHandler> logger)
    : IRequestHandler<UpdateNotificationPreferencesCommand, Result>
{
    public async Task<Result> Handle(
        UpdateNotificationPreferencesCommand request, CancellationToken ct)
    {
        logger.LogInformation("Updating notification preferences");

        var userId = currentUser.UserId;

        var existing = await prefRepo.Query()
            .Where(p => p.UserId == userId)
            .ToListAsync(ct).ConfigureAwait(false);

        logger.LogInformation("Existing preferences: {Existing}", existing);

        foreach (var update in request.Preferences)
        {
            var pref = existing.FirstOrDefault(p => p.Type == update.Type);

            if (pref is not null)
            {
                logger.LogInformation("Updating preference for type {Type}", update.Type);
                pref.Update(update.PushEnabled, update.EmailEnabled);
            }
            else
            {
                logger.LogInformation("Adding new preference for type {Type}", update.Type);
                prefRepo.Add(NotificationPreference.Create(
                    userId, update.Type, update.PushEnabled, update.EmailEnabled));
            }
        }

        await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation("Notification preferences updated");

        return Result.Success();
    }
}
