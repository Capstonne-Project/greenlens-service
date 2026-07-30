using FluentAssertions;
using Greenlens.Application.Common;
using Greenlens.Application.Features.Organization.DeleteEnvironmentalCompany;
using Greenlens.Application.Features.Organization.SoftDeleteCompanyTeam;
using Greenlens.Application.IntegrationTests.Fixtures;
using Greenlens.Application.IntegrationTests.Helpers;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;

namespace Greenlens.Application.IntegrationTests.Features.SoftDelete;

[Collection("Postgres")]
public sealed class OrganizationSoftDeleteTests(PostgresContainerFixture fixture)
    : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task DeleteCompany_WhenActive_ReturnsMustTerminateFirst_BR_CMP_004()
    {
        var companyId = await WithDbAsync(async db =>
        {
            var company = await IntegrationDataSeeder.SeedCompanyAsync(db, CompanyStatus.Active);
            return company.Id;
        });

        var result = await Mediator.Send(new DeleteEnvironmentalCompanyCommand(companyId));

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("COMPANY_MUST_TERMINATE_FIRST");
        result.Error.Type.Should().Be(ErrorType.BusinessRule);
    }

    [Fact]
    public async Task DeleteCompany_WhenAlreadyDeleted_ReturnsConflict_BR_CMP_004()
    {
        var companyId = await WithDbAsync(async db =>
        {
            var company = await IntegrationDataSeeder.SeedCompanyAsync(
                    db,
                    CompanyStatus.Terminated,
                    softDeleted: true);
            return company.Id;
        });

        var result = await Mediator.Send(new DeleteEnvironmentalCompanyCommand(companyId));

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("COMPANY_ALREADY_DELETED");
        result.Error.Type.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public async Task DeleteTeam_WithInProgressAssignment_ReturnsConflict_BR_CMP_004()
    {
        var teamId = await WithDbAsync(async db =>
        {
            var company = await IntegrationDataSeeder.SeedCompanyAsync(db, CompanyStatus.Active);
            var team = await IntegrationDataSeeder.SeedCompanyTeamAsync(db, company);
            var category = await IntegrationDataSeeder.SeedCategoryAsync(db, $"CAT-{Guid.NewGuid():N}"[..8]);
            await IntegrationDataSeeder.SeedInProgressAssignmentAsync(db, team, category);
            return team.Id;
        });

        var result = await Mediator.Send(new SoftDeleteCompanyTeamCommand(teamId));

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("TEAM_HAS_ACTIVE_ASSIGNMENTS");
        result.Error.Type.Should().Be(ErrorType.BusinessRule);
    }

    [Fact]
    public async Task DeleteTeam_WhenAlreadyDeleted_ReturnsConflict_BR_CMP_004()
    {
        var teamId = await WithDbAsync(async db =>
        {
            var company = await IntegrationDataSeeder.SeedCompanyAsync(db, CompanyStatus.Active);
            var team = await IntegrationDataSeeder.SeedCompanyTeamAsync(db, company, softDeleted: true);
            return team.Id;
        });

        var result = await Mediator.Send(new SoftDeleteCompanyTeamCommand(teamId));

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("TEAM_ALREADY_DELETED");
        result.Error.Type.Should().Be(ErrorType.Conflict);
    }
}
