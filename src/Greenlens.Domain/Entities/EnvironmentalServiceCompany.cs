using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;

namespace Greenlens.Domain.Entities;

/// <summary>
/// Công ty Dịch vụ Môi trường — created by DEO per contract.
/// Company receives CleanupTasks dispatched by LEO for urban areas.
/// </summary>
/// <remarks>
/// Implements: BR-CMP-001 → BR-CMP-007.
/// Contract-window authorization: requests only accepted when
/// now ∈ [ContractStartDate, ContractEndDate] AND Status == Active.
/// </remarks>
public sealed class EnvironmentalServiceCompany : AuditableEntity
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
    public DateTime ContractEndDate { get; private set; }
    /// <summary>Subsidiary = trực thuộc, Bidding = đấu thầu.</summary>
    public ContractType ContractType { get; private set; }
    public CompanyStatus Status { get; private set; } = CompanyStatus.PendingActivation;

    // ── Activation ──
    /// <summary>One-time activation token (hashed). 7 days, single-use. BR-CMP-002/003.</summary>
    public string? ActivationTokenHash { get; private set; }
    public DateTime? ActivationTokenExpiresAt { get; private set; }
    public DateTime? ActivatedAt { get; private set; }

    // ── Organization ──
    public Guid DepartmentId { get; private set; }

    // ── Navigation ──
    public Department? Department { get; private set; }
    public ICollection<CompanyStaff> Staff { get; private set; } = [];

    // ────────────────────────────────────────────────────
    // Factory
    // ────────────────────────────────────────────────────

    /// <summary>BR-CMP-001: DEO creates a company profile with contract info.</summary>
    public static EnvironmentalServiceCompany Create(
        string name,
        Guid departmentId,
        string contractNumber,
        DateTime contractStartDate,
        DateTime contractEndDate,
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

    /// <summary>BR-CMP-002: Set activation token for company onboarding.</summary>
    public void SetActivationToken(string tokenHash, DateTime expiresAt)
    {
        ActivationTokenHash = tokenHash;
        ActivationTokenExpiresAt = expiresAt;
    }

    /// <summary>BR-CMP-003: Company Manager activates the company via token.</summary>
    public void Activate()
    {
        if (Status != CompanyStatus.PendingActivation)
            throw new InvalidOperationException(
                $"Cannot activate company from status {Status}.");

        Status = CompanyStatus.Active;
        ActivatedAt = DateTime.UtcNow;
        ActivationTokenHash = null; // single-use
        ActivationTokenExpiresAt = null;
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

    /// <summary>BR-CMP-005: Check if company is within active contract window.</summary>
    public bool IsWithinContractWindow(DateTime now) =>
        Status == CompanyStatus.Active &&
        now >= ContractStartDate &&
        now <= ContractEndDate;

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
}

/// <summary>Status of an Environmental Service Company.</summary>
public enum CompanyStatus
{
    PendingActivation,
    Active,
    Suspended,
    Expired
}
