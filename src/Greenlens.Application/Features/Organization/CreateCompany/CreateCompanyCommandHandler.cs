using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Organization.CreateCompany;

/// <summary>
/// DEO creates a new Environmental Service Company + CM account with temporary password.
/// Company starts as PendingActivation. CM must change password on first login → company auto-activates.
/// </summary>
/// <remarks>Implements: BR-CMP-001, BR-CMP-002.</remarks>
public sealed class CreateCompanyCommandHandler(
    IEnvironmentalServiceCompanyRepository companies,
    IDepartmentRepository departments,
    IUserRepository users,
    ICompanyStaffRepository companyStaff,
    IUnitOfWork uow,
    IPasswordHasher passwordHasher,
    ILogger<CreateCompanyCommandHandler> logger)
    : IRequestHandler<CreateCompanyCommand, Result<CreateCompanyResponse>>
{
    public async Task<Result<CreateCompanyResponse>> Handle(
        CreateCompanyCommand request,
        CancellationToken ct)
    {
        // ── 1. Verify department exists ──
        var department = await departments.GetByIdAsync(request.DepartmentId, ct)
            .ConfigureAwait(false);

        if (department is null)
            return Errors.Organization.DepartmentNotFound;

        // ── 2. Check contract number uniqueness ──
        var contractExists = await companies.ExistsAsync(
            c => c.ContractNumber == request.ContractNumber, ct)
            .ConfigureAwait(false);

        if (contractExists)
            return Errors.Organization.CompanyContractNumberExists;

        // ── 3. Check manager email uniqueness ──
        var emailExists = await users.ExistsAsync(
            u => u.Email == request.ManagerEmail.ToLowerInvariant(), ct)
            .ConfigureAwait(false);

        if (emailExists)
            return Errors.Organization.ManagerEmailAlreadyExists;

        // ── 4. Create company entity ──
        var company = EnvironmentalServiceCompany.Create(
            request.Name,
            request.DepartmentId,
            request.ContractNumber,
            request.ContractStartDate,
            request.ContractEndDate,
            request.ContractType,
            request.TaxCode,
            request.Address,
            request.Phone,
            request.Email);

        companies.Add(company);

        // ── 5. Create CM user with temporary password ──
        var tempPassword = GenerateTempPassword();
        var hashedPassword = passwordHasher.Hash(tempPassword);

        var managerUser = User.CreateWithTempPassword(
            request.ManagerEmail,
            hashedPassword,
            request.ManagerFullName,
            Domain.Enums.UserRole.CompanyManager);

        users.Add(managerUser);

        // ── 6. Link CM to company via CompanyStaff ──
        var staffLink = CompanyStaff.Create(managerUser.Id, company.Id, "Manager");
        companyStaff.Add(staffLink);

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation(
            "Company {CompanyId} '{Name}' created with CM {ManagerEmail} under department {DeptId}",
            company.Id, company.Name, request.ManagerEmail, company.DepartmentId);

        return new CreateCompanyResponse(
            company.Id,
            company.Name,
            company.ContractNumber,
            company.ContractType.ToString(),
            company.Status.ToString(),
            managerUser.Id,
            managerUser.Email,
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

        // Guarantee at least 1 of each type
        chars[0] = upper[random.Next(upper.Length)];
        chars[1] = lower[random.Next(lower.Length)];
        chars[2] = digits[random.Next(digits.Length)];
        chars[3] = special[random.Next(special.Length)];

        // Fill remaining
        var all = upper + lower + digits + special;
        for (var i = 4; i < chars.Length; i++)
            chars[i] = all[random.Next(all.Length)];

        // Shuffle
        random.Shuffle(chars);

        return new string(chars);
    }
}
