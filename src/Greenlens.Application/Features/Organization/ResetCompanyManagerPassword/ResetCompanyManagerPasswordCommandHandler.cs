using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Organization.ResetCompanyManagerPassword;

/// <summary>
/// DEO/Admin resets a CM's password to a new temporary password.
/// CM will be forced to change password on next login (MustChangePassword = true).
/// </summary>
public sealed class ResetCompanyManagerPasswordCommandHandler(
    IEnvironmentalServiceCompanyRepository companies,
    ICompanyStaffRepository companyStaff,
    IUserRepository users,
    IUnitOfWork uow,
    IPasswordHasher passwordHasher,
    ILogger<ResetCompanyManagerPasswordCommandHandler> logger)
    : IRequestHandler<ResetCompanyManagerPasswordCommand, Result<ResetCompanyManagerPasswordResponse>>
{
    public async Task<Result<ResetCompanyManagerPasswordResponse>> Handle(
        ResetCompanyManagerPasswordCommand request,
        CancellationToken ct)
    {
        logger.LogInformation("Resetting company manager password for user {UserId}", request.ManagerUserId);

        // ── 1. Verify company exists ──
        var company = await companies.GetByIdAsync(request.CompanyId, ct)
            .ConfigureAwait(false);

        if (company is null)
        {
            logger.LogWarning("Company {CompanyId} not found", request.CompanyId);
            return Errors.Organization.CompanyNotFound;
        }

        // ── 2. Verify user is a staff member of this company ──
        var staff = await companyStaff.QueryAsNoTracking()
            .FirstOrDefaultAsync(
                cs => cs.CompanyId == request.CompanyId && cs.UserId == request.ManagerUserId, ct)
            .ConfigureAwait(false);

        if (staff is null)
        {
            logger.LogWarning("Staff {UserId} not found in company {CompanyId}", request.ManagerUserId, request.CompanyId);
            return Errors.Organization.StaffNotInCompany;
        }

        // ── 3. Get the user (tracked for update) ──
        var user = await users.GetByIdAsync(request.ManagerUserId, ct)
            .ConfigureAwait(false);

        if (user is null)
        {
            logger.LogWarning("User {UserId} not found", request.ManagerUserId);
            return Errors.Organization.StaffNotFound;
        }

        // ── 4. Generate new temp password and reset ──
        var tempPassword = GenerateTempPassword();
        var hashedPassword = passwordHasher.Hash(tempPassword);

        user.ResetToTempPassword(hashedPassword);

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation(
            "DEO reset password for CM {UserId} ({Email}) in company {CompanyId}",
            user.Id, user.Email, request.CompanyId);

        return new ResetCompanyManagerPasswordResponse(
            user.Id,
            user.Email,
            tempPassword);
    }

    /// <summary>Generate a random 10-char password with mixed case, digits, and special chars.</summary>
    private static string GenerateTempPassword()
    {
        const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string lower = "abcdefghjkmnpqrstuvwxyz";
        const string digits = "23456789";
        const string special = "!@#$%";

        var random = Random.Shared;
        var chars = new char[10];

        chars[0] = upper[random.Next(upper.Length)];
        chars[1] = lower[random.Next(lower.Length)];
        chars[2] = digits[random.Next(digits.Length)];
        chars[3] = special[random.Next(special.Length)];

        var all = upper + lower + digits + special;
        for (var i = 4; i < chars.Length; i++)
            chars[i] = all[random.Next(all.Length)];

        random.Shuffle(chars);

        return new string(chars);
    }
}
