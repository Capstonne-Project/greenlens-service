using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using Greenlens.Domain.Exceptions;

namespace Greenlens.Domain.Entities;

/// <summary>
/// Công ty Dịch vụ Môi trường — created by DEO per contract.
/// Company receives CleanupTasks dispatched by LEO for urban areas.
/// </summary>
/// <remarks>
/// Implements: BR-CMP-001 → BR-CMP-007.
/// Hiệu lực tác nghiệp chỉ dựa Company.Status == Active (BR-CMP-005).
/// ContractStartDate/EndDate là metadata (hiển thị + job expire). Subsidiary vô thời hạn (EndDate null).
/// Onboarding: CM đặt mật khẩu lần đầu qua cơ chế reset-password chung (BR-CMP-002).
/// BR-CMP-006: Lịch sử kỳ hợp đồng lưu qua ContractPeriod (1-N).
/// </remarks>
public sealed class EnvironmentalServiceCompany : SoftDeletableEntity
{
    private EnvironmentalServiceCompany() { } // EF Core constructor

    public string Name { get; private set; } = default!;
    public string? TaxCode { get; private set; }
    public string? Address { get; private set; }
    public string? Phone { get; private set; }
    public string? Email { get; private set; }

    // ── Contract ──
    public string ContractNumber { get; private set; } = default!;
    public DateTime ContractStartDate { get; private set; }
    /// <summary>Null = vô thời hạn (Subsidiary). Chỉ Bidding có giá trị.</summary>
    public DateTime? ContractEndDate { get; private set; }
    /// <summary>Subsidiary = trực thuộc, Bidding = đấu thầu.</summary>
    public ContractType ContractType { get; private set; }
    public CompanyStatus Status { get; private set; } = CompanyStatus.PendingActivation;

    /// <summary>Timestamp khi công ty được kích hoạt (DEO approve).</summary>
    public DateTime? ActivatedAt { get; private set; }

    /// <summary>BR-CMP-007: Timestamp of last expiry warning sent (30d/7d/1d). Used for idempotency.</summary>
    public DateTime? LastExpiryWarningAt { get; private set; }

    // ── Organization ──
    public Guid DepartmentId { get; private set; }

    // ── Navigation ──
    public Department? Department { get; private set; }
    public ICollection<CompanyStaff> Staff { get; private set; } = [];
    public ICollection<CompanyServiceArea> ServiceAreas { get; private set; } = [];
    public ICollection<ContractPeriod> ContractPeriods { get; private set; } = [];

    // ────────────────────────────────────────────────────
    // Factory
    // ────────────────────────────────────────────────────

    /// <summary>BR-CMP-001: DEO creates a company profile with contract info.</summary>
    public static EnvironmentalServiceCompany Create(
        string name,
        Guid departmentId,
        string contractNumber,
        DateTime contractStartDate,
        DateTime? contractEndDate,
        ContractType contractType,
        string? taxCode = null,
        string? address = null,
        string? phone = null,
        string? email = null)
    {
        return new EnvironmentalServiceCompany
        {
            Name = name,
            DepartmentId = departmentId,
            ContractNumber = contractNumber,
            ContractStartDate = contractStartDate,
            ContractEndDate = contractEndDate,
            ContractType = contractType,
            TaxCode = taxCode,
            Address = address,
            Phone = phone,
            Email = email,
            Status = CompanyStatus.PendingActivation,
            CreatedAt = DateTime.UtcNow
        };
    }

    // ────────────────────────────────────────────────────
    // State transitions
    // ────────────────────────────────────────────────────

    /// <summary>BR-CMP-003: DEO activates the company after CM sets password via reset-password flow.</summary>
    public void Activate()
    {
        if (Status != CompanyStatus.PendingActivation)
            throw new InvalidOperationException(
                $"Cannot activate company from status {Status}.");

        Status = CompanyStatus.Active;
        ActivatedAt = DateTime.UtcNow;
    }

    /// <summary>BR-CMP-006: Suspend company (e.g. contract violation).</summary>
    public void Suspend()
    {
        if (Status != CompanyStatus.Active)
            throw new InvalidOperationException(
                $"Cannot suspend company from status {Status}.");

        Status = CompanyStatus.Suspended;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>BR-CMP-007: Mark company as expired (background job at contract end).</summary>
    public void Expire()
    {
        Status = CompanyStatus.Expired;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Reactivate a suspended company.</summary>
    public void Reactivate()
    {
        if (Status != CompanyStatus.Suspended)
            throw new InvalidOperationException(
                $"Cannot reactivate company from status {Status}.");

        Status = CompanyStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>BR-CMP-004: Chấm dứt hợp đồng sớm (DEO/Admin quyết định).</summary>
    public void Terminate()
    {
        if (Status is CompanyStatus.Terminated or CompanyStatus.PendingActivation)
            throw new InvalidOperationException(
                $"Cannot terminate company from status {Status}.");

        Status = CompanyStatus.Terminated;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>BR-CMP-005: Hiệu lực tác nghiệp chỉ dựa Status (KHÔNG dùng cửa sổ hợp đồng khóa routing).</summary>
    public bool IsActive => Status == CompanyStatus.Active;

    /// <summary>BR-CMP-007: Mark that an expiry warning was sent. Prevents duplicate notifications.</summary>
    public void MarkExpiryWarned()
    {
        LastExpiryWarningAt = DateTime.UtcNow;
    }

    /// <summary>
    /// BR-CMP-006: DEO gia hạn/tái ký hợp đồng Bidding.
    /// Tạo kỳ hợp đồng mới, cập nhật metadata trên Company, auto-reactivate từ Expired.
    /// </summary>
    public ContractPeriod RenewContract(
        DateTime newStartDate,
        DateTime newEndDate,
        string newContractNumber,
        Guid renewedByUserId,
        string? note = null)
    {
        if (ContractType != ContractType.Bidding)
            throw new InvalidOperationException(
                "Subsidiary contracts are indefinite — cannot renew.");

        if (newEndDate <= newStartDate)
            throw new InvalidOperationException(
                "New contract end date must be after start date.");

        // Update current contract metadata on Company
        ContractStartDate = newStartDate;
        ContractEndDate = newEndDate;
        ContractNumber = newContractNumber;
        LastExpiryWarningAt = null; // Reset warning tracker

        // Auto-reactivate from Expired
        if (Status == CompanyStatus.Expired)
            Status = CompanyStatus.Active;

        UpdatedAt = DateTime.UtcNow;

        // Create history record
        var period = ContractPeriod.Create(
            Id, newContractNumber, ContractType,
            newStartDate, newEndDate, renewedByUserId, note);
        ContractPeriods.Add(period);

        return period;
    }

    public void UpdateProfile(
        string? name = null,
        string? taxCode = null,
        string? address = null,
        string? phone = null,
        string? email = null)
    {
        if (name is not null) Name = name;
        if (taxCode is not null) TaxCode = taxCode;
        if (address is not null) Address = address;
        if (phone is not null) Phone = phone;
        if (email is not null) Email = email;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// BR-CMP-004: Archive (soft-delete) only after contract terminated,
    /// or while still a staff-less pending draft.
    /// </summary>
    public void Archive(string? deletedBy = null, bool hasStaff = false)
    {
        if (IsDeleted)
            throw new DomainException("Company is already archived.");

        var canArchive = Status == CompanyStatus.Terminated
            || (Status == CompanyStatus.PendingActivation && !hasStaff);

        if (!canArchive)
            throw new DomainException(
                "Company must be terminated before archiving, or pending activation with no staff.");

        SoftDelete(deletedBy);
        UpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>Status of an Environmental Service Company (BR-CMP-004).</summary>
public enum CompanyStatus
{
    PendingActivation,
    Active,
    Suspended,
    Expired,

    /// <summary>Chấm dứt hợp đồng sớm (DEO/Admin quyết định). Khác Expired (hết hạn tự nhiên).</summary>
    Terminated
}
