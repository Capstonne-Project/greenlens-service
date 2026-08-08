using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Common.Models;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Reports.GetUserPublicReports;

/// <summary>
/// List another user's public reports — hiển thị trên hồ sơ công khai.
/// </summary>
/// <remarks>
/// Implements: BR-REP-012 (bỏ báo cáo ẩn danh), BR-ADM-006 (bỏ báo cáo bị ẩn),
/// BR-REP-032 (thumb chiếu từ primary sau khi gộp), BR-DAT-002 (không trả địa chỉ chi tiết),
/// BR-AUTH-015/022 (tài khoản bị ban hoặc đã xóa → NotFound).
/// </remarks>
public sealed class GetUserPublicReportsQueryHandler(
    IReportRepository reports,
    IReportMediaRepository reportMedia,
    IUserRepository users,
    ILogger<GetUserPublicReportsQueryHandler> logger)
    : IRequestHandler<GetUserPublicReportsQuery, Result<GetUserPublicReportsResponse>>
{
    public async Task<Result<GetUserPublicReportsResponse>> Handle(
        GetUserPublicReportsQuery request, CancellationToken ct)
    {
        logger.LogInformation("Getting public reports for user {UserId}", request.UserId);

        // Tài khoản đã xóa bị global query filter loại bỏ → NotFound thay vì trả list rỗng.
        var owner = await users.QueryAsNoTracking()
            .Where(u => u.Id == request.UserId)
            .Select(u => new { u.IsBanned })
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (owner is null || owner.IsBanned)
        {
            logger.LogWarning("Public reports requested for missing/banned user {UserId}", request.UserId);
            return Errors.Users.UserNotFound;
        }

        var query = reports.QueryAsNoTracking()
            .Include(r => r.Category)
            .Include(r => r.Media)
            .Where(r => r.ReporterId == request.UserId
                        && !r.IsHidden
                        && !r.HideReporterName);

        var totalCount = await query.CountAsync(ct).ConfigureAwait(false);
        var pagination = PaginationMeta.Create(request.Page, request.PageSize, totalCount);

        var pageRows = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(r => new
            {
                r.Id,
                r.Code,
                CategoryName = r.Category.NameVi,
                r.Severity,
                r.Status,
                r.CreatedAt,
                OwnImageUrl = r.Media
                    .OrderBy(m => m.UploadedAt)
                    .Select(m => m.ThumbnailUrl ?? m.Url)
                    .FirstOrDefault(),
                r.ParentReportId
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        // BR-REP-032: Duplicate mất Media sau reassign — chiếu thumb từ primary theo SourceReportId.
        var missingThumbIds = pageRows
            .Where(r => string.IsNullOrEmpty(r.OwnImageUrl)
                        && r.ParentReportId.HasValue
                        && r.Status == ReportStatus.Duplicate)
            .Select(r => r.Id)
            .ToList();

        Dictionary<Guid, string> sourceThumbs = [];
        if (missingThumbIds.Count > 0)
        {
            var thumbRows = await reportMedia.QueryAsNoTracking()
                .Where(m => m.SourceReportId != null && missingThumbIds.Contains(m.SourceReportId.Value))
                .OrderBy(m => m.UploadedAt)
                .Select(m => new { SourceId = m.SourceReportId!.Value, Url = m.ThumbnailUrl ?? m.Url })
                .ToListAsync(ct)
                .ConfigureAwait(false);

            sourceThumbs = thumbRows
                .GroupBy(x => x.SourceId)
                .ToDictionary(g => g.Key, g => g.First().Url);
        }

        var items = pageRows
            .Select(r => new UserPublicReportItem(
                r.Id, r.Code, r.CategoryName,
                r.Severity, r.Status, r.CreatedAt,
                !string.IsNullOrEmpty(r.OwnImageUrl)
                    ? r.OwnImageUrl
                    : sourceThumbs.GetValueOrDefault(r.Id)))
            .ToList();

        logger.LogInformation(
            "Lấy danh sách báo cáo công khai thành công. Số lượng: {Count}", items.Count);
        return new GetUserPublicReportsResponse(items, pagination);
    }
}
