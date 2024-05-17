using Microsoft.EntityFrameworkCore;

namespace AuditableDbContext.Tests;

public class TestDbContext : AuditableDbContext
{
    public DbSet<TestEntity> TestEntities { get; set; } = null!;
    public DbSet<IgnoredEntity> IgnoredEntities { get; set; } = null!;

    public TestDbContext(DbContextOptions options)
        : base(options)
    {
    }
}