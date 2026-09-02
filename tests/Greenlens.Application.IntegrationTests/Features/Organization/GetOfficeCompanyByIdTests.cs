using FluentAssertions;
using Greenlens.Application.Features.Organization.GetOfficeCompanyById;
using Greenlens.Application.IntegrationTests.Fixtures;
using Greenlens.Application.IntegrationTests.Helpers;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;

namespace Greenlens.Application.IntegrationTests.Features.Organization;

[Collection("Postgres")]
public sealed class GetOfficeCompanyByIdTests(PostgresContainerFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task Handle_CompanyServesLeoWard_ReturnsDetail_BR_CMP_008()
    {
        var (companyId, wardCode) = await WithDbAsync(async db =>
        {
            var ward = await IntegrationDataSeeder.SeedWardAsync(db, "26944");
            var dept = await IntegrationDataSeeder.SeedDepartmentAsync(db);
            var office = LocalOffice.Create("VP MTĐT Test", dept.Id, ward.Code);
            db.Set<LocalOffice>().Add(office);

            var leo = User.CreateByAdmin(
                CurrentUser.Email!, "hash", "LEO Test", UserRole.LEO);
            typeof(Greenlens.Domain.Common.BaseEntity)
                .GetProperty(nameof(Greenlens.Domain.Common.BaseEntity.Id))!
                .SetValue(leo, CurrentUser.UserId);
            leo.AssignToLocalOffice(office.Id);
            db.Set<User>().Add(leo);

            var company = await IntegrationDataSeeder.SeedCompanyAsync(db);
            db.Set<CompanyServiceArea>().Add(CompanyServiceArea.Create(company.Id, ward.Code));

            await db.SaveChangesAsync();
            return (company.Id, ward.Code);
        });

        CurrentUser.Role = UserRole.LEO.ToString();

        var result = await Mediator.Send(new GetOfficeCompanyByIdQuery(companyId));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(companyId);
        result.Value.WardCode.Should().Be(wardCode);
        result.Value.AllServiceAreas.Should().ContainSingle(sa => sa.WardCode == wardCode);
    }

    [Fact]
    public async Task Handle_CompanyNotInLeoWard_ReturnsNotFound_BR_CMP_008()
    {
        var companyId = await WithDbAsync(async db =>
        {
            var leoWard = await IntegrationDataSeeder.SeedWardAsync(db, "26944");
            var otherWard = await IntegrationDataSeeder.SeedWardAsync(db, "26945");
            var dept = await IntegrationDataSeeder.SeedDepartmentAsync(db);
            var office = LocalOffice.Create("VP MTĐT Test", dept.Id, leoWard.Code);
            db.Set<LocalOffice>().Add(office);

            var leo = User.CreateByAdmin(
                CurrentUser.Email!, "hash", "LEO Test", UserRole.LEO);
            typeof(Greenlens.Domain.Common.BaseEntity)
                .GetProperty(nameof(Greenlens.Domain.Common.BaseEntity.Id))!
                .SetValue(leo, CurrentUser.UserId);
            leo.AssignToLocalOffice(office.Id);
            db.Set<User>().Add(leo);

            var company = await IntegrationDataSeeder.SeedCompanyAsync(db);
            db.Set<CompanyServiceArea>().Add(CompanyServiceArea.Create(company.Id, otherWard.Code));

            await db.SaveChangesAsync();
            return company.Id;
        });

        CurrentUser.Role = UserRole.LEO.ToString();

        var result = await Mediator.Send(new GetOfficeCompanyByIdQuery(companyId));

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("COMPANY_NOT_FOUND");
    }
}
