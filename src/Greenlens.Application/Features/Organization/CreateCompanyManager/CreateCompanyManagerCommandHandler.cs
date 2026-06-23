using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Organization.CreateCompanyManager;

/// <summary>
/// DEO creates a CompanyManager account for an existing company.
/// Supports deferred onboarding: company can be created first, CM account created later.
/// </summary>
/// <remarks>Implements: BR-CMP-001, BR-CMP-002.</remarks>
public sealed class CreateCompanyManagerCommandHandler(
    IEnvironmentalServiceCompanyRepository companies,
    IUserRepository users,
    ICompanyStaffRepository companyStaff,
    IUnitOfWork uow,
    IPasswordHasher passwordHasher,
    ILogger<CreateCompanyManagerCommandHandler> logger)
    : IRequestHandler<CreateCompanyManagerCommand, Result<CreateCompanyManagerResponse>>
{
    public async Task<Result<CreateCompanyManagerResponse>> Handle(
        CreateCompanyManagerCommand request,
        CancellationToken ct)
    {
        // ── 1. Verify company exists ──
        var company = await companies.GetByIdAsync(request.CompanyId, ct)
            .ConfigureAwait(false);

        if (company is null)
            return Errors.Organization.CompanyNotFound;

        // ── 2. Check manager email uniqueness ──
        var emailExists = await users.ExistsAsync(
            u => u.Email == request.ManagerEmail.ToLowerInvariant(), ct)
            .ConfigureAwait(false);

        if (emailExists)
            return Errors.Organization.ManagerEmailAlreadyExists;

        // ── 3. Create CM user with temporary password ──
        var tempPassword = GenerateTempPassword();
        var hashedPassword = passwordHasher.Hash(tempPassword);

        var managerUser = User.CreateWithTempPassword(
            request.ManagerEmail,
            hashedPassword,
            request.ManagerFullName,
            Domain.Enums.UserRole.CompanyManager);

        users.Add(managerUser);

        // ── 4. Link CM to company via CompanyStaff ──
        var staffLink = CompanyStaff.Create(managerUser.Id, company.Id, "Manager");
        companyStaff.Add(staffLink);

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation(
            "CM account {ManagerEmail} created for company {CompanyId} '{CompanyName}'",
            request.ManagerEmail, company.Id, company.Name);

        return new CreateCompanyManagerResponse(
            managerUser.Id,
            managerUser.Email,
            managerUser.FullName,
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
