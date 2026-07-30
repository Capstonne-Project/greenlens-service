using System.Text.Json;
using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Users.UpdateUser;

/// <summary>
/// Admin updates a user's details (name, phone, role, verification status).
/// </summary>
/// <remarks>
/// Implements: BR-ADM (admin user management), BR-ADM-010 (audit snapshot).
/// </remarks>
public sealed class UpdateUserCommandHandler(
    IUserRepository users,
    IUnitOfWork uow,
    IAuditLogger auditLogger,
    ILogger<UpdateUserCommandHandler> logger)
    : IRequestHandler<UpdateUserCommand, Result<UpdateUserResponse>>
{
    public async Task<Result<UpdateUserResponse>> Handle(
        UpdateUserCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Updating user {UserId}", request.UserId);

        var user = await users.GetByIdAsync(request.UserId, cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
        {
            logger.LogWarning("User not found for ID {UserId}", request.UserId);
            return Errors.Users.UserNotFound;
        }

        if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            var normalized = PhoneNumberNormalizer.Normalize(request.PhoneNumber);
            if (normalized is not null)
            {
                var phoneInUse = await users
                    .PhoneExistsIncludingDeletedAsync(normalized, request.UserId, cancellationToken)
                    .ConfigureAwait(false);
                if (phoneInUse)
                {
                    logger.LogWarning("Phone number {PhoneNumber} is already in use", normalized);
                    return Errors.Phone.PhoneAlreadyUsed;
                }
            }
        }

        var oldSnapshot = JsonSerializer.Serialize(new
        {
            role = user.Role.ToString(),
            fullName = user.FullName,
            isEmailVerified = user.IsEmailVerified,
            isBanned = user.IsBanned
        });

        user.AdminUpdate(
            request.FullName,
            request.PhoneNumber is null ? null : PhoneNumberNormalizer.Normalize(request.PhoneNumber),
            request.Role,
            request.IsEmailVerified);

        try
        {
            await uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException ex)
        {
            var mapped = PostgresUniqueViolationMapper.TryMap(ex);
            if (mapped is not null)
            {
                logger.LogWarning("Database unique violation error occurred while updating user {UserId}", request.UserId);
                return mapped;
            }
            logger.LogWarning("Database error occurred while updating user {UserId}", request.UserId);
            throw;
        }

        await auditLogger.LogAsync(
            "UpdateUser",
            "User",
            user.Id.ToString(),
            oldValues: oldSnapshot,
            newValues: JsonSerializer.Serialize(new
            {
                role = user.Role.ToString(),
                fullName = user.FullName,
                isEmailVerified = user.IsEmailVerified,
                isBanned = user.IsBanned
            }),
            cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Admin updated user {UserId}", request.UserId);

        return new UpdateUserResponse(user.Id, "Cập nhật người dùng thành công.");
    }
}
