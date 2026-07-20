using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;

namespace Greenlens.Domain.Entities;

/// <summary>
/// BR-CMP-006: Kỳ hợp đồng — mỗi lần gia hạn/tái ký tạo 1 record mới.
/// Kỳ đầu tiên tự động tạo khi DEO tạo Company (BR-CMP-001).
/// Cho phép truy vấn lịch sử các kỳ hợp đồng để audit.
/// </summary>
public sealed class ContractPeriod : BaseEntity
{
    private ContractPeriod() { } // EF Core constructor

    public Guid CompanyId { get; private set; }
    public string ContractNumber { get; private set; } = default!;
    public ContractType ContractType { get; private set; }
    public DateTime StartDate { get; private set; }

    /// <summary>Null = vô thời hạn (Subsidiary). Bidding luôn có giá trị.</summary>
    public DateTime? EndDate { get; private set; }

    /// <summary>User (DEO/Admin) thực hiện gia hạn/tạo kỳ.</summary>
    public Guid RenewedByUserId { get; private set; }
    public string? Note { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // ── Navigation ──
    public EnvironmentalServiceCompany? Company { get; private set; }

    /// <summary>
    /// Tạo một kỳ hợp đồng mới.
    /// </summary>
    public static ContractPeriod Create(
        Guid companyId,
        string contractNumber,
        ContractType contractType,
        DateTime startDate,
        DateTime? endDate,
        Guid renewedByUserId,
        string? note = null)
    {
        return new ContractPeriod
        {
            CompanyId = companyId,
            ContractNumber = contractNumber,
            ContractType = contractType,
            StartDate = startDate,
            EndDate = endDate,
            RenewedByUserId = renewedByUserId,
            Note = note,
            CreatedAt = DateTime.UtcNow
        };
    }
}
