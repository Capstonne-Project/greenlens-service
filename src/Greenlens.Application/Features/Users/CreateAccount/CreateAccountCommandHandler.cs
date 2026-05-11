using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using MediatR;

namespace Greenlens.Application.Features.Users.CreateAccount;

/// <summary>
/// Admin creates a new user account with email pre-verified.
/// </summary>
/// <remarks>
/// Implements: BR-ADM (admin provisions Officer / CleanupTeam / Citizen accounts).
/// IsEmailVerified defaults to true — admin has already confirmed the user's identity.
/// </remarks>
public sealed class CreateAccountCommandHandler(
    IUserRepository users,
    IUnitOfWork uow,
    IPasswordHasher passwordHasher)
    : IRequestHandler<CreateAccountCommand, Result<CreateAccountResponse>>
{
    public async Task<Result<CreateAccountResponse>> Handle(
        CreateAccountCommand request,
        CancellationToken cancellationToken)
    {
        // ── Check email uniqueness ──
        var emailExists = await users.ExistsAsync(
            u => u.Email == request.Email.ToLowerInvariant(),
            cancellationToken).ConfigureAwait(false);

        if (emailExists)
            return Errors.Auth.EmailTaken;

        // ── Create user with email pre-verified ──
        var passwordHash = passwordHasher.Hash(request.Password);

        var user = User.CreateByAdmin(
            request.Email,
            passwordHash,
            request.FullName,
            request.Role);

        users.Add(user);
        await uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new CreateAccountResponse(
            user.Id,
            user.Email,
            user.FullName,
            user.Role,
            "Tạo tài khoản thành công.");
    }
}
