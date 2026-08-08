using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using Greenlens.Domain.Exceptions;

namespace Greenlens.Domain.UnitTests;

public sealed class EnvironmentalServiceCompanyTests
{
    private static EnvironmentalServiceCompany CreateCompany(CompanyStatus status = CompanyStatus.PendingActivation)
    {
        var company = EnvironmentalServiceCompany.Create(
            "Test Co",
            Guid.NewGuid(),
            "HD-TEST-001",
            DateTime.UtcNow.Date,
            null,
            ContractType.Subsidiary);

        if (status == CompanyStatus.Active)
            company.Activate();
        else if (status == CompanyStatus.Suspended)
        {
            company.Activate();
            company.Suspend();
        }
        else if (status == CompanyStatus.Terminated)
        {
            company.Activate();
            company.Terminate();
        }

        return company;
    }

    [Fact]
    public void Archive_WhenTerminated_SoftDeletes_BR_CMP_004()
    {
        var company = CreateCompany(CompanyStatus.Terminated);

        company.Archive("admin-id");

        Assert.True(company.IsDeleted);
    }

    [Fact]
    public void Archive_PendingActivationWithoutStaff_SoftDeletes_BR_CMP_004()
    {
        var company = CreateCompany(CompanyStatus.PendingActivation);

        company.Archive("admin-id", hasStaff: false);

        Assert.True(company.IsDeleted);
    }

    [Fact]
    public void Archive_WhenActive_ThrowsDomainException_BR_CMP_004()
    {
        var company = CreateCompany(CompanyStatus.Active);

        var act = () => company.Archive("admin-id");

        Assert.Throws<DomainException>(act);
        Assert.False(company.IsDeleted);
    }

    [Fact]
    public void Archive_PendingActivationWithStaff_ThrowsDomainException_BR_CMP_004()
    {
        var company = CreateCompany(CompanyStatus.PendingActivation);

        var act = () => company.Archive("admin-id", hasStaff: true);

        Assert.Throws<DomainException>(act);
        Assert.False(company.IsDeleted);
    }

    [Fact]
    public void Archive_WhenAlreadyDeleted_ThrowsDomainException_BR_CMP_004()
    {
        var company = CreateCompany(CompanyStatus.Terminated);
        company.Archive("admin-id");

        var act = () => company.Archive("admin-id");

        Assert.Throws<DomainException>(act);
    }
}

public sealed class EnvironmentalTeamArchiveTests
{
    [Fact]
    public void Archive_NoActiveAssignments_DeactivatesAndSoftDeletes_BR_CMP_004()
    {
        var team = EnvironmentalTeam.CreateCompanyTeam("Alpha", TeamType.Cleanup, Guid.NewGuid());

        team.Archive("cm-id", hasActiveAssignments: false);

        Assert.True(team.IsDeleted);
        Assert.False(team.IsActive);
    }

    [Fact]
    public void Archive_WithActiveAssignments_ThrowsDomainException_BR_CMP_004()
    {
        var team = EnvironmentalTeam.CreateCompanyTeam("Alpha", TeamType.Cleanup, Guid.NewGuid());

        var act = () => team.Archive("cm-id", hasActiveAssignments: true);

        Assert.Throws<DomainException>(act);
        Assert.False(team.IsDeleted);
    }

    [Fact]
    public void Archive_WhenAlreadyDeleted_ThrowsDomainException_BR_CMP_004()
    {
        var team = EnvironmentalTeam.CreateCompanyTeam("Alpha", TeamType.Cleanup, Guid.NewGuid());
        team.Archive("cm-id");

        var act = () => team.Archive("cm-id");

        Assert.Throws<DomainException>(act);
    }
}
