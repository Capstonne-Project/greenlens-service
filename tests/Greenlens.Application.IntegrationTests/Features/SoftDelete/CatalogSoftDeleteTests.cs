using FluentAssertions;
using Greenlens.Application.Common;
using Greenlens.Application.Features.Admin.CreateCategory;
using Greenlens.Application.Features.Admin.DeleteCategory;
using Greenlens.Application.Features.Admin.DeleteWasteTag;
using Greenlens.Application.IntegrationTests.Fixtures;
using Greenlens.Application.IntegrationTests.Helpers;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;

namespace Greenlens.Application.IntegrationTests.Features.SoftDelete;

[Collection("Postgres")]
public sealed class CatalogSoftDeleteTests(PostgresContainerFixture fixture)
    : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task DeleteCategory_WhenInUse_ReturnsCategoryInUse_BR_ADM_003()
    {
        var categoryId = await WithDbAsync(async db =>
        {
            var category = await IntegrationDataSeeder.SeedCategoryAsync(db, $"CAT-{Guid.NewGuid():N}"[..8])
                .ConfigureAwait(false);
            await IntegrationDataSeeder.SeedReportAsync(db, category).ConfigureAwait(false);
            return category.Id;
        }).ConfigureAwait(false);

        var result = await Mediator.Send(new DeleteCategoryCommand(categoryId)).ConfigureAwait(false);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("CATEGORY_IN_USE");
        result.Error.Type.Should().Be(ErrorType.BusinessRule);
    }

    [Fact]
    public async Task DeleteCategory_WhenAlreadyDeleted_ReturnsConflict_BR_ADM_003()
    {
        var categoryId = await WithDbAsync(async db =>
        {
            var category = await IntegrationDataSeeder.SeedCategoryAsync(
                    db,
                    $"CAT-{Guid.NewGuid():N}"[..8],
                    softDeleted: true)
                .ConfigureAwait(false);
            return category.Id;
        }).ConfigureAwait(false);

        var result = await Mediator.Send(new DeleteCategoryCommand(categoryId)).ConfigureAwait(false);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("CATEGORY_ALREADY_DELETED");
        result.Error.Type.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public async Task DeleteWasteTag_WhenInUse_ReturnsWasteTagInUse_BR_ADM_003()
    {
        var tagId = await WithDbAsync(async db =>
        {
            var tag = await IntegrationDataSeeder.SeedWasteTagAsync(db, $"TAG-{Guid.NewGuid():N}"[..8])
                .ConfigureAwait(false);
            var category = await IntegrationDataSeeder.SeedCategoryAsync(db, $"CAT-{Guid.NewGuid():N}"[..8])
                .ConfigureAwait(false);
            var (reporter, report) = await IntegrationDataSeeder.SeedReportAsync(db, category)
                .ConfigureAwait(false);
            db.Set<ReportWasteTag>().Add(ReportWasteTag.Create(report.Id, tag.Id, reporter.Id));
            await db.SaveChangesAsync().ConfigureAwait(false);
            return tag.Id;
        }).ConfigureAwait(false);

        var result = await Mediator.Send(new DeleteWasteTagCommand(tagId)).ConfigureAwait(false);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("WASTE_TAG_IN_USE");
        result.Error.Type.Should().Be(ErrorType.BusinessRule);
    }

    [Fact]
    public async Task DeleteWasteTag_WhenAlreadyDeleted_ReturnsConflict_BR_ADM_003()
    {
        var tagId = await WithDbAsync(async db =>
        {
            var tag = await IntegrationDataSeeder.SeedWasteTagAsync(
                    db,
                    $"TAG-{Guid.NewGuid():N}"[..8],
                    softDeleted: true)
                .ConfigureAwait(false);
            return tag.Id;
        }).ConfigureAwait(false);

        var result = await Mediator.Send(new DeleteWasteTagCommand(tagId)).ConfigureAwait(false);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("WASTE_TAG_ALREADY_DELETED");
        result.Error.Type.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public async Task CreateCategory_DuplicateCodeIncludingSoftDeleted_ReturnsConflict_BR_ADM_003()
    {
        const string code = "DUPE-CAT";
        await WithDbAsync(async db =>
        {
            await IntegrationDataSeeder.SeedCategoryAsync(db, code, softDeleted: true)
                .ConfigureAwait(false);
        }).ConfigureAwait(false);

        var result = await Mediator.Send(new CreateCategoryCommand(code, "Mới", "New", null))
            .ConfigureAwait(false);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("CATEGORY_CODE_EXISTS");
        result.Error.Type.Should().Be(ErrorType.Conflict);
    }
}
