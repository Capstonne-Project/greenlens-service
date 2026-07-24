using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Auth.Login;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Users.CreateAccount;

/// <summary>
/// Admin creates a new user account with email pre-verified.
/// </summary>
/// <remarks>
/// Implements: BR-ADM (admin provisions Officer / CleanupTeam / Citizen accounts).
/// </remarks>
public sealed class CreateAccountCommandHandler(
    IUserRepository users,
    IUnitOfWork uow,
    IPasswordHasher passwordHasher,
    ILogger<CreateAccountCommandHandler> logger)
    : IRequestHandler<CreateAccountCommand, Result<CreateAccountResponse>>
{
    public async Task<Result<CreateAccountResponse>> Handle(
        CreateAccountCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Creating account for user {Email} with role {Role}", request.Email, request.Role);

        var emailError = await UserRegistrationGuard
            .ValidateNewEmailForRegistrationAsync(users, request.Email, cancellationToken)
            .ConfigureAwait(false);
        if (emailError is not null)
        {
            logger.LogWarning("Email validation failed for user {Email}", request.Email);
            return emailError;
        }

        var passwordHash = passwordHasher.Hash(request.Password);
        var user = User.CreateByAdmin(
            request.Email,
            passwordHash,
            request.FullName,
            request.Role);

        users.Add(user);

        try
        {
            await uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException ex)
        {
            logger.LogWarning("Database error occurred while creating account for user {Email}", request.Email);
            var mapped = PostgresUniqueViolationMapper.TryMap(ex);
            if (mapped is not null)
            {
                logger.LogWarning("Database unique violation error occurred while creating account for user {Email}", request.Email);
                return mapped;
            }
            throw;
        }

        logger.LogInformation("Admin created account {UserId} with role {Role}", user.Id, user.Role);

        return new CreateAccountResponse(
            user.Id,
            user.Email,
            user.FullName,
            user.Role,
            "Tạo tài khoản thành công.");
    }
}
