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
            // Seed the current user (Admin) into DB so ResolveActorAsync can find them
            var admin = User.CreateByAdmin(CurrentUser.Email!, "hash", "Admin User", UserRole.Admin);
            // Overwrite the Id to match CurrentUser
            typeof(Greenlens.Domain.Common.BaseEntity)
                .GetProperty(nameof(Greenlens.Domain.Common.BaseEntity.Id))!
                .SetValue(admin, CurrentUser.UserId);
            db.Set<User>().Add(admin);

            var office = await IntegrationDataSeeder.SeedLocalOfficeAsync(db);
            admin.AssignToLocalOffice(office.Id);

            var cleaner = await IntegrationDataSeeder.SeedUserAsync(db, UserRole.Cleaner);
            cleaner.AssignToLocalOffice(office.Id);

            var team = EnvironmentalTeam.Create("Tiểu đội Test", office.Id, TeamType.Cleanup);
            db.Set<EnvironmentalTeam>().Add(team);
            db.Set<TeamMember>().Add(TeamMember.Create(team.Id, cleaner.Id, isLeader: true));
            await db.SaveChangesAsync();
            return team.Id;
        });

        var result = await Mediator.Send(new GetTeamByIdQuery(teamId));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Tiểu đội Test");
        result.Value.Members.Should().HaveCount(1);
        result.Value.Members[0].FullName.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Handle_MissingTeam_ReturnsNotFound()
    {
        await WithDbAsync(async db =>
        {
            var admin = User.CreateByAdmin(CurrentUser.Email!, "hash", "Admin User", UserRole.Admin);
            typeof(Greenlens.Domain.Common.BaseEntity)
                .GetProperty(nameof(Greenlens.Domain.Common.BaseEntity.Id))!
                .SetValue(admin, CurrentUser.UserId);
            db.Set<User>().Add(admin);
            await db.SaveChangesAsync();
        });

        var result = await Mediator.Send(new GetTeamByIdQuery(Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("TEAM_NOT_FOUND");
    }
}
