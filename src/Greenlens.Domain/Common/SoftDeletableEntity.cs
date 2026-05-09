namespace Greenlens.Domain.Common;

public abstract class SoftDeletableEntity : AuditableEntity
{
    public DateTime? DeletedAt { get; private set; }
    public string? DeletedBy { get; private set; }

    public bool IsDeleted => DeletedAt is not null;

    public void SoftDelete(string? deletedBy = null)
    {
        DeletedAt = DateTime.UtcNow;
        DeletedBy = deletedBy;
    }

    public void Restore()
    {
        DeletedAt = null;
        DeletedBy = null;
    }
}
