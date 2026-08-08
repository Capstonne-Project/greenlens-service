using Greenlens.Application.Common;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;

namespace Greenlens.Application.Features.Reports.Common;

internal static class ReportReviewCandidateFilters
{
    /// <summary>
    /// Scope review lists to the caller's org: LEO → ward, DEO → department, Admin → all.
    /// </summary>
    public static IQueryable<Report> ApplyOfficerScope(
        IQueryable<Report> query,
        User user,
        string role)
    {
        if (role == UserRole.Admin.ToString())
            return query;

        if (role == UserRole.LEO.ToString() && user.LocalOfficeId.HasValue)
            return query.Where(r => r.AssignedOfficeId == user.LocalOfficeId);

        if (role == UserRole.DEO.ToString() && user.DepartmentId.HasValue)
            return query.Where(r => r.AssignedDepartmentId == user.DepartmentId);

        return query;
    }

    /// <summary>
    /// Validates single-report access for LEO/DEO/Admin officer dashboards (BR-ORG-012).
    /// </summary>
    public static Error? ValidateReportAccess(Report report, User user, string role)
    {
        if (role == UserRole.Admin.ToString())
            return null;

        if (role == UserRole.LEO.ToString())
        {
            if (!user.LocalOfficeId.HasValue)
                return Errors.Organization.OfficerNoOffice;

            if (report.AssignedOfficeId != user.LocalOfficeId)
                return Errors.Reports.OutsideJurisdiction;

            return null;
        }

        if (role == UserRole.DEO.ToString())
        {
            if (!user.DepartmentId.HasValue)
                return Errors.Reports.OutsideJurisdiction;

            if (report.AssignedDepartmentId != user.DepartmentId)
                return Errors.Reports.OutsideJurisdiction;

            return null;
        }

        return null;
    }

    /// <summary>LEO-only scope for progress endpoints (Admin bypasses).</summary>
    public static Error? ValidateLeoReportAccess(Report report, User user, string role)
    {
        if (role == UserRole.Admin.ToString())
            return null;

        if (role != UserRole.LEO.ToString())
            return Errors.Auth.Forbidden;

        if (!user.LocalOfficeId.HasValue)
            return Errors.Organization.OfficerNoOffice;

        if (report.AssignedOfficeId != user.LocalOfficeId)
            return Errors.Reports.OutsideJurisdiction;

        return null;
    }

    public static IQueryable<Report> ApplyCommon(
        IQueryable<Report> query,
        ReportStatus? status,
        Severity? severity,
        Guid? categoryId,
        string? wardCode,
        DateTime? fromDate,
        DateTime? toDate,
        string? search)
    {
        if (status.HasValue)
            query = query.Where(r => r.Status == status.Value);

        if (severity.HasValue)
            query = query.Where(r => r.Severity == severity.Value);

        if (categoryId.HasValue)
            query = query.Where(r => r.CategoryId == categoryId.Value);

        if (!string.IsNullOrWhiteSpace(wardCode))
            query = query.Where(r => r.WardCode == wardCode);

        if (fromDate.HasValue)
            query = query.Where(r => r.CreatedAt >= DateTime.SpecifyKind(fromDate.Value, DateTimeKind.Utc));

        if (toDate.HasValue)
            query = query.Where(r => r.CreatedAt <= DateTime.SpecifyKind(toDate.Value, DateTimeKind.Utc));

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim().ToLower();
            query = query.Where(r =>
                r.Code.ToLower().Contains(keyword) ||
                (r.Address != null && r.Address.ToLower().Contains(keyword)) ||
                r.Category.NameVi.ToLower().Contains(keyword) ||
                r.Category.Code.ToLower().Contains(keyword));
        }

        return query;
    }
}
