using FluentAssertions;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Reports.SubmitPollutionReport;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Entities.Location;
using Greenlens.Domain.Enums;
using Greenlens.Infrastructure.Persistence;
using Greenlens.Infrastructure.Persistence.Repositories;
using Greenlens.Infrastructure.Persistence.Repositories.Location;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Greenlens.Application.UnitTests.Features.Reports.SubmitPollutionReport;

/// <summary>
/// BR-ORG-004, BR-ORG-010, BR-ORG-011, BR-ORG-016: handler must always derive WardCode/ProvinceCode
/// via point-in-polygon lookup from GPS (<see cref="IWardBoundaryLookupService.FindWardCodeByPointAsync"/>),
/// never from the (legacy, backward-compat only) WardCode/ProvinceCode fields on the request.
/// </summary>
public sealed class SubmitPollutionReportCommandHandlerWardAutoDetectTests
{
    private readonly IWardBoundaryLookupService _wardBoundaryLookup = Substitute.For<IWardBoundaryLookupService>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly ITempImageStore _tempStore = Substitute.For<ITempImageStore>();
    private readonly IFileStorageService _fileStorage = Substitute.For<IFileStorageService>();
    private readonly IProfanityFilter _profanityFilter = Substitute.For<IProfanityFilter>();
    private readonly IReportSubmissionRateLimiter _rateLimiter = Substitute.For<IReportSubmissionRateLimiter>();
    private readonly IIdempotencyContext _idempotencyContext = Substitute.For<IIdempotencyContext>();
    private readonly IImageExifAnalyzer _exifAnalyzer = Substitute.For<IImageExifAnalyzer>();
    private readonly IImageBytesFetcher _imageBytesFetcher = Substitute.For<IImageBytesFetcher>();

    private const decimal Lat = 10.7626m;
    private const decimal Lng = 106.6602m;

    /// <summary>
    /// Builds a handler wired to a fresh EF Core InMemory ApplicationDbContext (via the real
    /// repository implementations) so that the handler's QueryAsNoTracking()/EF LINQ calls
    /// (LocalOffice/Department routing, Tier-1 duplicate lookup) execute against a real
    /// IQueryable provider instead of a hand-mocked IQueryable that EF's async LINQ can't run.
    /// Only true boundary dependencies (rate limiter, profanity filter, storage, EXIF, temp
    /// store) are NSubstitute mocks, set up to happy-path/no-op through everything unrelated
    /// to ward/GPS auto-detection.
    /// </summary>
    private (SubmitPollutionReportCommandHandler Sut, ApplicationDbContext Ctx, Guid ReporterId, Guid CategoryId) CreateSut()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"submit-report-ward-{Guid.NewGuid():N}")
            .Options;
        var ctx = new ApplicationDbContext(options);

        var reporter = User.Create("citizen@test.local", "hash", "Citizen Test", UserRole.Citizen);
        reporter.AcceptDataConsent();
        ctx.Users.Add(reporter);

        var category = PollutionCategory.Create("TRASH", "Rác thải", "Trash");
        ctx.PollutionCategories.Add(category);
        ctx.SaveChanges();

        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(reporter.Id);

        _fileStorage.IsOwnedPublicUrl(Arg.Any<string>()).Returns(true);
        _fileStorage.IsOwnedPublicUrl(Arg.Any<string>(), Arg.Any<string>()).Returns(true);

        _rateLimiter.TryAcquireAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new ReportSubmissionRateLimitResult(true, 0));
        _idempotencyContext.IsReplay.Returns(false);
        _profanityFilter.ContainsProfanity(Arg.Any<string>()).Returns(false);
        _imageBytesFetcher.TryFetchAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((byte[]?)null); // manual flow, no bytes needed → EXIF block skipped

        var unitOfWork = new UnitOfWork(
            ctx,
            Substitute.For<IPublisher>(),
            Substitute.For<ITransactionManager>(),
            Substitute.For<IDomainEventCollector>());

        var sut = new SubmitPollutionReportCommandHandler(
            new PollutionCategoryRepository(ctx),
            new ReportRepository(ctx),
            new ReportMediaRepository(ctx),
            new ReportStatusHistoryRepository(ctx),
            new WasteTagRepository(ctx),
            new ReportWasteTagRepository(ctx),
            new UserRepository(ctx),
            new WardRepository(ctx),
            new DepartmentRepository(ctx),
            new LocalOfficeRepository(ctx),
            _wardBoundaryLookup,
            unitOfWork,
            _currentUser,
            _tempStore,
            _fileStorage,
            _profanityFilter,
            _rateLimiter,
            _idempotencyContext,
            _exifAnalyzer,
            _imageBytesFetcher,
            NullLogger<SubmitPollutionReportCommandHandler>.Instance);

        return (sut, ctx, reporter.Id, category.Id);
    }

    private static SubmitPollutionReportCommand BuildCommand(
        Guid categoryId,
        string? requestWardCode = null,
        string? requestProvinceCode = null) => new(
            categoryId,
            Severity.Medium,
            "Rác thải sinh hoạt vứt bừa bãi ngoài đường",
            Lat,
            Lng,
            "123 Test Street",
            requestWardCode,
            requestProvinceCode,
            TempImageId: null,
            Images: [new SubmitPollutionReportImageItem("https://cdn.test.local/reports/img1.jpg", "image/jpeg", 1024)],
            WasteTagIds: null);

    /// <summary>
    /// GPS falls within a ward that has boundary data → handler must persist the report with
    /// the WardCode/ProvinceCode resolved from the point-in-polygon lookup (BR-ORG-016).
    /// </summary>
    [Fact]
    public async Task Handle_GpsInsideKnownWardBoundary_UsesDetectedWardAndProvinceCode_BR_ORG_016()
    {
        var (sut, ctx, _, categoryId) = CreateSut();

        var ward = Ward.Seed("00001", "Phường Bến Nghé", "79", administrativeUnitId: 1);
        ctx.Set<Ward>().Add(ward);
        await ctx.SaveChangesAsync();

        _wardBoundaryLookup
            .FindWardCodeByPointAsync(Lat, Lng, Arg.Any<CancellationToken>())
            .Returns(ward.Code);
        _wardBoundaryLookup
            .FindProvinceCodeByPointAsync(Lat, Lng, Arg.Any<CancellationToken>())
            .Returns((string?)null);

        var result = await sut.Handle(BuildCommand(categoryId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.WardCode.Should().Be(ward.Code);
        result.Value.ProvinceCode.Should().Be(ward.ProvinceCode);

        var persisted = await ctx.Reports.SingleAsync();
        persisted.WardCode.Should().Be(ward.Code);
        persisted.ProvinceCode.Should().Be(ward.ProvinceCode);
    }

    /// <summary>
    /// Client sends legacy WardCode/ProvinceCode fields with a bogus, non-existent WardCode.
    /// Rule change: those fields must NOT influence the outcome at all — the handler must
    /// always call the point-in-polygon lookup and use its result instead (BR-ORG-004/016).
    /// </summary>
    [Fact]
    public async Task Handle_ClientSuppliedWardCodeIgnored_AlwaysUsesGpsPointInPolygon_BR_ORG_004()
    {
        var (sut, ctx, _, categoryId) = CreateSut();

        var realWard = Ward.Seed("00002", "Phường Thủ Đức", "79", administrativeUnitId: 2);
        ctx.Set<Ward>().Add(realWard);
        await ctx.SaveChangesAsync();

        _wardBoundaryLookup
            .FindWardCodeByPointAsync(Lat, Lng, Arg.Any<CancellationToken>())
            .Returns(realWard.Code);
        _wardBoundaryLookup
            .FindProvinceCodeByPointAsync(Lat, Lng, Arg.Any<CancellationToken>())
            .Returns((string?)null);

        // Client claims a bogus ward/province that does not exist in the DB at all.
        var command = BuildCommand(categoryId, requestWardCode: "99999", requestProvinceCode: "01");

        var result = await sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.WardCode.Should().Be(realWard.Code);
        result.Value.ProvinceCode.Should().Be(realWard.ProvinceCode);
        result.Value.WardCode.Should().NotBe("99999");
        result.Value.ProvinceCode.Should().NotBe("01");

        await _wardBoundaryLookup.Received(1)
            .FindWardCodeByPointAsync(Lat, Lng, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// GPS point is not contained by any ward boundary but falls inside a province polygon
    /// → ProvinceCode is set and report routes to Department queue (BR-ORG-011).
    /// </summary>
    [Fact]
    public async Task Handle_GpsOutsideWardButInsideProvince_RoutesToDepartmentQueue_BR_ORG_011()
    {
        var (sut, ctx, _, categoryId) = CreateSut();

        var department = Department.Create("Sở TNMT TP.HCM", "79");
        ctx.Departments.Add(department);
        await ctx.SaveChangesAsync();

        _wardBoundaryLookup
            .FindWardCodeByPointAsync(Lat, Lng, Arg.Any<CancellationToken>())
            .Returns((string?)null);
        _wardBoundaryLookup
            .FindProvinceCodeByPointAsync(Lat, Lng, Arg.Any<CancellationToken>())
            .Returns("79");

        var result = await sut.Handle(BuildCommand(categoryId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.WardCode.Should().BeNull();
        result.Value.ProvinceCode.Should().Be("79");

        var persisted = await ctx.Reports.SingleAsync();
        persisted.WardCode.Should().BeNull();
        persisted.ProvinceCode.Should().Be("79");
        persisted.AssignedOfficeId.Should().BeNull();
        persisted.AssignedDepartmentId.Should().Be(department.Id);

        await _wardBoundaryLookup.Received(1)
            .FindProvinceCodeByPointAsync(Lat, Lng, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// GPS point is not contained by any ward or province boundary → report is still created
    /// successfully with null WardCode/ProvinceCode and no auto-routing target.
    /// </summary>
    [Fact]
    public async Task Handle_GpsOutsideAnyWardBoundary_CreatesReportWithNullWardAndProvince_BR_ORG_011()
    {
        var (sut, ctx, _, categoryId) = CreateSut();

        _wardBoundaryLookup
            .FindWardCodeByPointAsync(Lat, Lng, Arg.Any<CancellationToken>())
            .Returns((string?)null);
        _wardBoundaryLookup
            .FindProvinceCodeByPointAsync(Lat, Lng, Arg.Any<CancellationToken>())
            .Returns((string?)null);

        var result = await sut.Handle(BuildCommand(categoryId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.WardCode.Should().BeNull();
        result.Value.ProvinceCode.Should().BeNull();

        var persisted = await ctx.Reports.SingleAsync();
        persisted.WardCode.Should().BeNull();
        persisted.ProvinceCode.Should().BeNull();
        persisted.AssignedOfficeId.Should().BeNull();
        persisted.AssignedDepartmentId.Should().BeNull();

        await _wardBoundaryLookup.Received(1)
            .FindWardCodeByPointAsync(Lat, Lng, Arg.Any<CancellationToken>());
        await _wardBoundaryLookup.Received(1)
            .FindProvinceCodeByPointAsync(Lat, Lng, Arg.Any<CancellationToken>());
    }
}
