namespace AuditableDbContext;

public interface IEditableEntity
{
    public string CreatedBy { get; }

    public DateTimeOffset CreatedOn { get; }

    public string UpdatedBy { get; }

    public DateTimeOffset UpdatedOn { get; }
}