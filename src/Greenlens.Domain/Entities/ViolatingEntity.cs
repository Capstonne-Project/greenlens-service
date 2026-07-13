using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;

namespace Greenlens.Domain.Entities;

/// <summary>
/// Đối tượng vi phạm — cá nhân/hộ gia đình hoặc doanh nghiệp/cơ sở kinh doanh.
/// Dùng để chuẩn hóa thông tin violator, tránh string-match khi detect repeat offender.
/// </summary>
/// <remarks>
/// Implements: BR-INS-010 (biên bản hiện trường — thông tin đối tượng vi phạm),
///             BR-INS-022 (tái phạm — query bằng ViolatingEntityId thay vì string-match).
/// «ViolatorType»: Individual (cá nhân/hộ gia đình), Business (doanh nghiệp).
/// TaxCode unique cho Business; IdentityNumber cho Individual (CMND/CCCD).
/// </remarks>
public sealed class ViolatingEntity : SoftDeletableEntity
{
    private ViolatingEntity() { } // EF Core constructor

    /// <summary>Tên đối tượng vi phạm (tên cá nhân hoặc tên doanh nghiệp).</summary>
    public string Name { get; private set; } = default!;

    /// <summary>Địa chỉ (nơi cư trú / trụ sở).</summary>
    public string? Address { get; private set; }

    /// <summary>Mã số thuế / MSDN — chỉ doanh nghiệp có. Unique index (filtered).</summary>
    public string? TaxCode { get; private set; }

    /// <summary>CMND/CCCD — chỉ cá nhân có. Dùng để identify repeat offender cá nhân.</summary>
    public string? IdentityNumber { get; private set; }

    /// <summary>Số điện thoại liên hệ.</summary>
    public string? PhoneNumber { get; private set; }

    /// <summary>Individual (cá nhân/hộ gia đình) hoặc Business (doanh nghiệp).</summary>
    public ViolatorType Type { get; private set; }

    // ── Navigation ──
    public ICollection<InspectionReport> InspectionReports { get; private set; } = [];

    // ────────────────────────────────────────────────────
    // Factory
    // ────────────────────────────────────────────────────

    /// <summary>BR-INS-010: Create a violating entity record.</summary>
    public static ViolatingEntity Create(
        string name,
        ViolatorType type,
        string? address = null,
        string? taxCode = null,
        string? identityNumber = null,
        string? phoneNumber = null)
    {
        return new ViolatingEntity
        {
            Name = name,
            Type = type,
            Address = address,
            TaxCode = taxCode,
            IdentityNumber = identityNumber,
            PhoneNumber = phoneNumber
        };
    }

    /// <summary>Update violator details (e.g. address correction, phone update).</summary>
    public void Update(
        string? name = null,
        string? address = null,
        string? taxCode = null,
        string? identityNumber = null,
        string? phoneNumber = null)
    {
        if (name is not null) Name = name;
        if (address is not null) Address = address;
        if (taxCode is not null) TaxCode = taxCode;
        if (identityNumber is not null) IdentityNumber = identityNumber;
        if (phoneNumber is not null) PhoneNumber = phoneNumber;
    }
}
