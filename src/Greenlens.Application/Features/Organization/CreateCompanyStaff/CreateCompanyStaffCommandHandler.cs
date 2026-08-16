using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Organization.CreateCompanyStaff;

/// <summary>
/// CM creates a CompanyStaff account with temporary password.
/// Optionally assigns the new staff to a company team.
/// Staff must change password on first login (MustChangePassword=true).
/// </summary>
/// <remarks>Implements: BR-CMP-004, BR-CMP-010, BR-NTF-002.</remarks>
public sealed class CreateCompanyStaffCommandHandler(
    ICompanyStaffRepository companyStaffRepo,
    IEnvironmentalServiceCompanyRepository companies,
    IUserRepository users,
    IEnvironmentalTeamRepository teams,
    ITeamMemberRepository teamMembers,
    IUnitOfWork uow,
    ICurrentUser currentUser,
    IPasswordHasher passwordHasher,
    INotificationService notifications,
    ILogger<CreateCompanyStaffCommandHandler> logger)
    : IRequestHandler<CreateCompanyStaffCommand, Result<CreateCompanyStaffResponse>>
{
    public async Task<Result<CreateCompanyStaffResponse>> Handle(
        CreateCompanyStaffCommand request,
        CancellationToken ct)
    {
        logger.LogInformation("Creating company staff for user {Email}", request.Email);

        // ── 1. Resolve CM's company ──
        var cmStaff = await companyStaffRepo.GetByUserIdAsync(currentUser.UserId, ct)
            .ConfigureAwait(false);

        if (cmStaff is null)
        {
            logger.LogWarning("Company manager not found for user ID {UserId}", currentUser.UserId);
            return Errors.Organization.NotCompanyManager;
        }

        var companyId = cmStaff.CompanyId;

        // ── 2. Check email uniqueness ──
        var emailError = await UserRegistrationGuard
            .ValidateNewEmailForProvisioningAsync(users, request.Email, ct)
            .ConfigureAwait(false);
        if (emailError is not null)
        {
            logger.LogWarning("Email {Email} is already in use", request.Email);
            return emailError;
        }

        // ── 3. Validate team belongs to this company (if specified) ──
        EnvironmentalTeam? team = null;
        if (request.TeamId.HasValue)
        {
            team = await teams.GetByIdAsync(request.TeamId.Value, ct)
                .ConfigureAwait(false);

            if (team is null)
            {
                logger.LogWarning("Team {TeamId} not found", request.TeamId.Value);
                return Errors.Organization.TeamNotFound;
            }

            if (team.CompanyId != companyId)
            {
                logger.LogWarning("Team {TeamId} is not in company {CompanyId}", request.TeamId.Value, companyId);
                return Errors.Organization.TeamNotInCompany;
            }
        }

        // ── 4. Create User (role=CompanyStaff, MustChangePassword=true) ──
        var tempPassword = GenerateTempPassword();
        var hashedPassword = passwordHasher.Hash(tempPassword);

        var newUser = User.CreateWithTempPassword(
            request.Email,
            hashedPassword,
            request.FullName,
            UserRole.CompanyStaff);

        users.Add(newUser);

        // ── 5. Link to company via CompanyStaff ──
        var staffLink = CompanyStaff.Create(newUser.Id, companyId, request.Position);
        companyStaffRepo.Add(staffLink);

        // ── 6. Optionally add to team ──
        Guid? assignedTeamId = null;
        if (team is not null)
        {
            var member = TeamMember.Create(team.Id, newUser.Id);
            teamMembers.Add(member);
            assignedTeamId = team.Id;
        }

        try
        {
            await uow.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        catch (DbUpdateException ex)
        {
            var mapped = PostgresUniqueViolationMapper.TryMap(ex);
            if (mapped is not null)
                return mapped;
            throw;
        }

        logger.LogInformation(
            "CM {CmId} created staff {StaffEmail} for company {CompanyId}, team={TeamId}",
            currentUser.UserId, newUser.Email, companyId, assignedTeamId);

        var company = await companies.GetByIdAsync(companyId, ct).ConfigureAwait(false);
        if (company is not null)
        {
            await notifications.SendFromTemplateAsync(
                newUser.Id,
                NotificationType.CompanyStaffAccountCreated,
                CompanyStaffAccountNotificationPlaceholders.ForCreated(
                    company.Name,
                    newUser.FullName,
                    newUser.Email,
                    tempPassword),
                companyId,
                ct).ConfigureAwait(false);
        }
        else
        {
            logger.LogWarning(
                "Company {CompanyId} not found after staff creation — welcome email skipped",
                companyId);
        }

        return new CreateCompanyStaffResponse(
            newUser.Id,
            newUser.Email,
            newUser.FullName,
            tempPassword,
            companyId,
            request.Position,
            assignedTeamId);
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
