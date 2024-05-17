using System.ComponentModel.DataAnnotations;
// ReSharper disable EntityFramework.ModelValidation.UnlimitedStringLength

namespace AuditableDbContext;

public class AuditLog
{
    public long Id { get; set; }

    [MaxLength(254)]
    public string Username { get; init; } = null!;

    public DateTimeOffset Timestamp { get; init; }

    [MaxLength(100)]
    public string Entity { get; init; } = null!;

    [MaxLength(100)]
    public string Operation { get; init; } = null!;

    public int? EntityId { get; init; }

    public string? OldValues { get; init; }

    public string? NewValues { get; init; }

    public string? Changes { get; init; }
}