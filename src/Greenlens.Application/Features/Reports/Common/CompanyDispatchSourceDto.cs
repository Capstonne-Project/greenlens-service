using Greenlens.Domain.Entities;

namespace Greenlens.Application.Features.Reports.Common;

/// <summary>
/// Ward LEO and local office that dispatched a report to a company (BR-CMP-005).
/// </summary>
public sealed record CompanyDispatchSourceDto(
    Guid? LocalOfficeId,
    string? LocalOfficeName,
    string? WardCode,
    string? WardName,
    Guid? LeoUserId,
    string? LeoFullName);

public static class CompanyDispatchSourceMapper
{
    public static CompanyDispatchSourceDto Map(Report report)
    {
        var office = report.AssignedOffice;
        var leoUser = report.DispatchedByUser ?? report.VerifiedByUser;

        return new CompanyDispatchSourceDto(
            report.AssignedOfficeId,
            office?.Name,
            report.WardCode ?? office?.WardCode,
            office?.Ward?.Name,
            report.DispatchedByOfficerId ?? report.VerifiedBy,
            leoUser?.FullName);
    }
}
