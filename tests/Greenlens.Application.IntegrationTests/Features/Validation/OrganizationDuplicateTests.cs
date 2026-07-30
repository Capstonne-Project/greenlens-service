using FluentAssertions;
using Greenlens.Application.Features.Organization.AcceptInvitation;
using Greenlens.Application.Features.Organization.RenewContract;
using Greenlens.Application.IntegrationTests.Fixtures;
using Greenlens.Application.IntegrationTests.Helpers;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;

namespace Greenlens.Application.IntegrationTests.Features.Validation;

[Collection("Postgres")]
public sealed class OrganizationDuplicateTests(PostgresContainerFixture fixture)
    : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task RenewContract_DuplicateContractNumber_ReturnsConflict_BR_CMP_006()
    {
        const string existingContract = "HD-DUP-001";
        var companyToRenew = await WithDbAsync(async db =>
        {
            await IntegrationDataSeeder.SeedBiddingCompanyAsync(db, existingContract);
            return await IntegrationDataSeeder.SeedBiddingCompanyAsync(db, "HD-OTHER-002");
        });

        var result = await Mediator.Send(new RenewContractCommand(
            companyToRenew.Id,
            DateTime.UtcNow.Date,
            DateTime.UtcNow.Date.AddYears(1),
            existingContract));

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("CONTRACT_NUMBER_ALREADY_USED");
        result.Error.Type.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public async Task AcceptInvitation_UserAlreadyInTeam_ReturnsMemberAlreadyInTeam_BR_ORG_021()
    {
        var (invitationId, invitedUserId) = await WithDbAsync(async db =>
        {
            var leo = await IntegrationDataSeeder.SeedUserAsync(db, UserRole.LEO);
            var citizen = await IntegrationDataSeeder.SeedUserAsync(db, UserRole.Citizen);
            var office = await IntegrationDataSeeder.SeedLocalOfficeAsync(db);
            var team = EnvironmentalTeam.Create("Cleanup Team", office.Id, TeamType.Cleanup);
            db.Set<EnvironmentalTeam>().Add(team);
            await db.SaveChangesAsync();

            db.Set<TeamMember>().Add(TeamMember.Create(team.Id, citizen.Id));
            var invitation = StaffInvitation.Create(
                leo.Id,
                citizen.Id,
                office.Id,
                UserRole.Cleaner,
                team.Id);
            db.Set<StaffInvitation>().Add(invitation);
            await db.SaveChangesAsync();
            return (invitation.Id, citizen.Id);
        });

        CurrentUser.UserId = invitedUserId;
        CurrentUser.Role = UserRole.Citizen.ToString();

        var result = await Mediator.Send(new AcceptInvitationCommand(invitationId));

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("MEMBER_ALREADY_IN_TEAM");
        result.Error.Type.Should().Be(ErrorType.Conflict);
    }
}
