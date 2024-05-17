using System.ComponentModel.DataAnnotations;

namespace AuditableDbContext.Tests;

[NotAuditable]
public class IgnoredEntity
{
    public int Id { get; set; }

    [MaxLength(100)]
    public string Name { get; set; } = null!;
}