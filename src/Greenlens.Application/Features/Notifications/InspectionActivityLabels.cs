using Greenlens.Domain.Enums;

namespace Greenlens.Application.Features.Notifications;

internal static class InspectionActivityLabels
{
    internal static string FormatEvidenceUpload(InspectionEvidenceCategory category) =>
        category switch
        {
            InspectionEvidenceCategory.ScenePhoto => "đã bổ sung ảnh hiện trường",
            InspectionEvidenceCategory.Video => "đã bổ sung video minh chứng",
            InspectionEvidenceCategory.Audio => "đã bổ sung ghi âm minh chứng",
            InspectionEvidenceCategory.Other => "đã bổ sung minh chứng khác",
            _ => "đã cập nhật minh chứng"
        };

    internal const string ChecklistUpdated = "đã cập nhật checklist điều tra";
    internal const string ArrivalConfirmed = "đã xác nhận có mặt hiện trường";
    internal const string FieldReportSubmitted = "đã nộp biên bản điều tra hiện trường";
    internal const string PenaltyIssued = "đã ban hành quyết định xử phạt";
    internal const string ClosedNoViolation = "đã kết luận không vi phạm";
}
