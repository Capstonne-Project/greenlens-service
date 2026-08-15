using FluentAssertions;
using Greenlens.Application.Features.Users.GetAllUsersWithPaged;
using Greenlens.Application.IntegrationTests.Fixtures;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;

namespace Greenlens.Application.IntegrationTests.Features.Users;

[Collection("Postgres")]
public sealed class GetAllUsersWithPagedQueryTests(PostgresContainerFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task GetAllUsersWithPaged_SearchByVietnameseFullNamePartial_ReturnsMatch()
    {
        const string email = "leo.23158@greenlens.dev";

        await WithDbAsync(async db =>
        {
            db.Set<User>().Add(User.CreateByAdmin(
                email,
                "hash",
                "LEO Nghị Đức",
                UserRole.LEO));
            await db.SaveChangesAsync();
        });

        var result = await Mediator.Send(new GetAllUsersWithPagedQuery(Search: "Nghị Đức"));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().ContainSingle(u => u.Email == email && u.FullName == "LEO Nghị Đức");
    }

    [Fact]
    public async Task GetAllUsersWithPaged_SearchByProvinceInFullName_ReturnsDeoMatch()
    {
        const string email = "deo.75@greenlens.dev";

        await WithDbAsync(async db =>
        {
            db.Set<User>().Add(User.CreateByAdmin(
                email,
                "hash",
                "DEO Đồng Nai",
                UserRole.DEO));
            await db.SaveChangesAsync();
        });

        var result = await Mediator.Send(new GetAllUsersWithPagedQuery(Search: "Đồng Nai"));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().ContainSingle(u => u.Email == email);
    }
}
