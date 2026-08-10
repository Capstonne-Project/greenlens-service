using FluentAssertions;
using Greenlens.Application.Features.Organization.GetCompanyTeamById;
using Greenlens.Application.IntegrationTests.Fixtures;
using Greenlens.Application.IntegrationTests.Helpers;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;

namespace Greenlens.Application.IntegrationTests.Features.Organization;

[Collection("Postgres")]
public sealed class GetCompanyTeamByIdTests(PostgresContainerFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task Handle_ExistingCompanyTeam_ReturnsDetailWithMembers_BR_CMP_004()
    {
        var teamId = await WithDbAsync(async db =>
        {
            var company = await IntegrationDataSeeder.SeedCompanyAsync(db);

            var cm = User.CreateByAdmin(
                CurrentUser.Email!, "hash", "CM User", UserRole.CompanyManager);
            typeof(Greenlens.Domain.Common.BaseEntity)
                .GetProperty(nameof(Greenlens.Domain.Common.BaseEntity.Id))!
                .SetValue(cm, CurrentUser.UserId);
            db.Set<User>().Add(cm);
            db.Set<CompanyStaff>().Add(CompanyStaff.Create(cm.Id, company.Id));

            var worker = await IntegrationDataSeeder.SeedUserAsync(db, UserRole.CompanyStaff);
            db.Set<CompanyStaff>().Add(CompanyStaff.Create(worker.Id, company.Id));

            var team = EnvironmentalTeam.CreateCompanyTeam(
                "Đội công ty A", TeamType.Cleanup, company.Id);
            db.Set<EnvironmentalTeam>().Add(team);
            db.Set<TeamMember>().Add(TeamMember.Create(team.Id, worker.Id, isLeader: true));

            await db.SaveChangesAsync();
            return team.Id;
        });

        CurrentUser.Role = UserRole.CompanyManager.ToString();

        var result = await Mediator.Send(new GetCompanyTeamByIdQuery(teamId));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Đội công ty A");
        result.Value.MemberCount.Should().Be(1);
        result.Value.Members.Should().HaveCount(1);
        result.Value.Members[0].IsLeader.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_TeamFromOtherCompany_ReturnsForbidden_BR_CMP_004()
    {
        var otherTeamId = await WithDbAsync(async db =>
        {
            var myCompany = await IntegrationDataSeeder.SeedCompanyAsync(db);
            var otherCompany = await IntegrationDataSeeder.SeedCompanyAsync(db);

            var cm = User.CreateByAdmin(
                CurrentUser.Email!, "hash", "CM User", UserRole.CompanyManager);
            typeof(Greenlens.Domain.Common.BaseEntity)
                .GetProperty(nameof(Greenlens.Domain.Common.BaseEntity.Id))!
                .SetValue(cm, CurrentUser.UserId);
            db.Set<User>().Add(cm);
            db.Set<CompanyStaff>().Add(CompanyStaff.Create(cm.Id, myCompany.Id));

            var otherTeam = EnvironmentalTeam.CreateCompanyTeam(
                "Other team", TeamType.Cleanup, otherCompany.Id);
            db.Set<EnvironmentalTeam>().Add(otherTeam);

            await db.SaveChangesAsync();
            return otherTeam.Id;
        });

        CurrentUser.Role = UserRole.CompanyManager.ToString();

        var result = await Mediator.Send(new GetCompanyTeamByIdQuery(otherTeamId));

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("TEAM_NOT_IN_COMPANY");
    }
}
