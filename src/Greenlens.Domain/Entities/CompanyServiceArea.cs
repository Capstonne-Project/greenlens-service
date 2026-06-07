using Greenlens.Domain.Common;
using Greenlens.Domain.Entities.Location;

namespace Greenlens.Domain.Entities;

/// <summary>
/// Vùng phục vụ của công ty — mapping CompanyId ↔ WardCode.
/// 1 công ty phụ trách nhiều phường/xã (VD: DVCI Quận 3 → P.Bàn Cờ, P.Xuân Hòa, P.Nhiêu Lộc).
/// DEO gán ward cho company khi tạo/cập nhật hợp đồng.
/// </summary>
/// <remarks>Implements: BR-CMP-008 (service area definition).</remarks>
public sealed class CompanyServiceArea : AuditableEntity
{
    private CompanyServiceArea() { } // EF Core constructor

    public Guid CompanyId { get; private set; }
    public string WardCode { get; private set; } = default!;

    // ── Navigation ──
    public EnvironmentalServiceCompany? Company { get; private set; }
    public Ward? Ward { get; private set; }

    /// <summary>Assign a ward to a company's service area.</summary>
    public static CompanyServiceArea Create(Guid companyId, string wardCode)
    {
        return new CompanyServiceArea
        {
            CompanyId = companyId,
            WardCode = wardCode,
            CreatedAt = DateTime.UtcNow
        };
    }
}
