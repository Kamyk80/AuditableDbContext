using System.ComponentModel.DataAnnotations;
// ReSharper disable UnusedAutoPropertyAccessor.Local

namespace AuditableDbContext.Tests;

public class TestEntity : IEditableEntity
{
    public int Id { get; set; }

    [MaxLength(100)]
    public string Name { get; set; } = null!;

    [NotAuditable]
    [MaxLength(100)]
    public string Secret { get; set; } = null!;

    public string CreatedBy { get; private set; } = null!;

    public DateTimeOffset CreatedOn { get; private set; }

    public string UpdatedBy { get; private set; } = null!;

    public DateTimeOffset UpdatedOn { get; private set; }
}