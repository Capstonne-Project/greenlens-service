using FluentAssertions;
using Greenlens.Application.Features.Organization.GetTeamById;
using Greenlens.Application.IntegrationTests.Fixtures;
using Greenlens.Application.IntegrationTests.Helpers;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;

namespace Greenlens.Application.IntegrationTests.Features.Organization;

[Collection("Postgres")]
public sealed class GetTeamByIdTests(PostgresContainerFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task Handle_ExistingCommunityTeam_ReturnsDetailWithMembers()
    {
        var teamId = await WithDbAsync(async db =>
        {
            var office = await IntegrationDataSeeder.SeedLocalOfficeAsync(db).ConfigureAwait(false);
            var cleaner = await IntegrationDataSeeder.SeedUserAsync(db, UserRole.Cleaner).ConfigureAwait(false);
            cleaner.AssignToLocalOffice(office.Id);

            var team = EnvironmentalTeam.Create("Tiểu đội Test", office.Id, TeamType.Cleanup);
            db.Set<EnvironmentalTeam>().Add(team);
            db.Set<TeamMember>().Add(TeamMember.Create(team.Id, cleaner.Id, isLeader: true));
            await db.SaveChangesAsync().ConfigureAwait(false);
            return team.Id;
        }).ConfigureAwait(false);

        var result = await Mediator.Send(new GetTeamByIdQuery(teamId)).ConfigureAwait(false);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Tiểu đội Test");
        result.Value.Members.Should().HaveCount(1);
        result.Value.Members[0].FullName.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Handle_MissingTeam_ReturnsNotFound()
    {
        var result = await Mediator.Send(new GetTeamByIdQuery(Guid.NewGuid())).ConfigureAwait(false);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("TEAM_NOT_FOUND");
    }
}
