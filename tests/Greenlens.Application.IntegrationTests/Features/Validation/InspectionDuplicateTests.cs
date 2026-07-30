using FluentAssertions;
using Greenlens.Application.Common;
using Greenlens.Application.Features.Inspection.CreateViolatingEntity;
using Greenlens.Application.IntegrationTests.Fixtures;
using Greenlens.Application.IntegrationTests.Helpers;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;

namespace Greenlens.Application.IntegrationTests.Features.Validation;

[Collection("Postgres")]
public sealed class InspectionDuplicateTests(PostgresContainerFixture fixture)
    : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task CreateViolatingEntity_DuplicateIdentityNumber_ReturnsConflict_BR_INS_010()
    {
        const string identity = "001234567890";
        await WithDbAsync(async db =>
        {
            var entity = await IntegrationDataSeeder.SeedViolatingEntityAsync(db);
            entity.Update(name: "Existing", identityNumber: identity);
            await db.SaveChangesAsync();
        });

        var result = await Mediator.Send(new CreateViolatingEntityCommand(
            "New Violator",
            ViolatorType.Individual,
            null,
            null,
            identity,
            null));

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("VIOLATING_ENTITY_DUPLICATE_IDENTITY");
        result.Error.Type.Should().Be(ErrorType.Conflict);
    }
}
