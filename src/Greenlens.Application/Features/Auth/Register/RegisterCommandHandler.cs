using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using MediatR;

namespace Greenlens.Application.Features.Auth.Register;

/// <summary>
/// Register a new citizen account.
/// </summary>
/// <remarks>
/// Implements: BR-AUTH-005 (password strength), BR-DAT-001 (bcrypt ≥12).
/// </remarks>
public sealed class RegisterCommandHandler(
    IUserRepository users,
    IUnitOfWork uow,
    IPasswordHasher passwordHasher)
    : IRequestHandler<RegisterCommand, Result<RegisterResponse>>
{
    public async Task<Result<RegisterResponse>> Handle(
        RegisterCommand request,
        CancellationToken cancellationToken)
    {
        var emailExists = await users.ExistsAsync(
            u => u.Email == request.Email.ToLowerInvariant(),
            cancellationToken).ConfigureAwait(false);

        if (emailExists)
            return Errors.Auth.EmailTaken;

        var passwordHash = passwordHasher.Hash(request.Password);

        var user = User.Create(
            request.Email,
            passwordHash,
            request.FullName);

        users.Add(user);
        await uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new RegisterResponse(
            user.Id,
            user.Email,
            "Đăng ký thành công. Vui lòng kiểm tra email để xác thực tài khoản.");
    }
}
