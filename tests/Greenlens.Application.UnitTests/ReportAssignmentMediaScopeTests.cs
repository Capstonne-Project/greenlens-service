using FluentAssertions;
using Greenlens.Application.Features.Reports.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;

namespace Greenlens.Application.UnitTests;

public sealed class ReportAssignmentMediaScopeTests
{
    [Fact]
    public void FilterForAssignment_ExcludesBeforeAfterFromPriorCycle_BR_REP_015()
    {
        var assignmentStart = DateTime.UtcNow.AddDays(-1);
        var assignment = ReportAssignment.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        SetAssignedAt(assignment, assignmentStart);

        var oldBefore = CreateMedia(MediaType.Before, assignmentStart.AddHours(-2), "https://old/before.jpg");
        var newBefore = CreateMedia(MediaType.Before, assignmentStart.AddHours(1), "https://new/before.jpg");
        var oldAfter = CreateMedia(MediaType.After, assignmentStart.AddHours(-1), "https://old/after.jpg");
        var newAfter = CreateMedia(MediaType.After, assignmentStart.AddHours(2), "https://new/after.jpg");

        var before = ReportAssignmentMediaScope.FilterForAssignment(
            [oldBefore, newBefore, oldAfter, newAfter], assignment, MediaType.Before);
        var after = ReportAssignmentMediaScope.FilterForAssignment(
            [oldBefore, newBefore, oldAfter, newAfter], assignment, MediaType.After);

        before.Should().ContainSingle().Which.Url.Should().Be("https://new/before.jpg");
        after.Should().ContainSingle().Which.Url.Should().Be("https://new/after.jpg");
    }

    [Fact]
    public void FilterForAssignment_AfterImagesAtResolve_IncludesMediaAfterCompletedAt_BR_CLN_005()
    {
        var assignmentStart = DateTime.UtcNow.AddDays(-2);
        var completedAt = DateTime.UtcNow.AddMinutes(-5);
        var assignment = ReportAssignment.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        SetAssignedAt(assignment, assignmentStart);
        assignment.Accept();
        assignment.Complete();
        SetCompletedAt(assignment, completedAt);

        // Simulates legacy resolve order: Complete() then persist after (UploadedAt > CompletedAt).
        var after1 = CreateMedia(MediaType.After, completedAt.AddSeconds(2), "https://cdn/after1.jpg");
        var after2 = CreateMedia(MediaType.After, completedAt.AddSeconds(3), "https://cdn/after2.jpg");

        var after = ReportAssignmentMediaScope.FilterForAssignment(
            [after1, after2], assignment, MediaType.After);

        after.Should().HaveCount(2);
        after.Select(m => m.Url).Should().BeEquivalentTo(["https://cdn/after1.jpg", "https://cdn/after2.jpg"]);
    }

    [Fact]
    public void FilterForAssignment_WhenAssignmentNull_ReturnsEmpty()
    {
        var media = CreateMedia(MediaType.Before, DateTime.UtcNow, "https://x/b.jpg");

        ReportAssignmentMediaScope.FilterForAssignment([media], null, MediaType.Before)
            .Should().BeEmpty();
    }

    [Fact]
    public void ResolveLatestProgressNote_PrefersNewestProgressUpdate()
    {
        var reportId = Guid.NewGuid();
        var assignment = ReportAssignment.Create(reportId, Guid.NewGuid(), Guid.NewGuid());
        assignment.Accept();
        assignment.UpdateProgress(10, "snapshot note", Guid.NewGuid());

        var older = AssignmentProgressUpdate.Create(assignment.Id, reportId, 40, "older note", Guid.NewGuid());
        SetCreatedAt(older, DateTime.UtcNow.AddHours(-2));
        var newer = AssignmentProgressUpdate.Create(assignment.Id, reportId, 80, "newest note", Guid.NewGuid());
        SetCreatedAt(newer, DateTime.UtcNow.AddHours(-1));

        assignment.ProgressUpdates.Add(older);
        assignment.ProgressUpdates.Add(newer);

        ReportAssignmentMediaScope.ResolveLatestProgressNote(assignment)
            .Should().Be("newest note");
    }

    private static ReportMedia CreateMedia(MediaType type, DateTime uploadedAt, string url)
    {
        var media = ReportMedia.Create(
            Guid.NewGuid(), type, url, "image/jpeg", 1024, Guid.NewGuid());
        typeof(ReportMedia)
            .GetProperty(nameof(ReportMedia.UploadedAt))!
            .SetValue(media, uploadedAt);
        return media;
    }

    private static void SetAssignedAt(ReportAssignment assignment, DateTime assignedAt)
    {
        typeof(ReportAssignment)
            .GetProperty(nameof(ReportAssignment.AssignedAt))!
            .SetValue(assignment, assignedAt);
    }

    private static void SetCompletedAt(ReportAssignment assignment, DateTime completedAt)
    {
        typeof(ReportAssignment)
            .GetProperty(nameof(ReportAssignment.CompletedAt))!
            .SetValue(assignment, completedAt);
    }

    private static void SetCreatedAt(AssignmentProgressUpdate update, DateTime createdAt)
    {
        typeof(AssignmentProgressUpdate)
            .GetProperty(nameof(AssignmentProgressUpdate.CreatedAt))!
            .SetValue(update, createdAt);
    }
}
