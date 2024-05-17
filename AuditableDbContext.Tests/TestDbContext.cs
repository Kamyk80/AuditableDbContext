using Microsoft.EntityFrameworkCore;

namespace AuditableDbContext.Tests;

public class TestDbContext(DbContextOptions options) : AuditableDbContext(options)
{
    public DbSet<TestEntity> TestEntities { get; set; } = null!;

    public DbSet<IgnoredEntity> IgnoredEntities { get; set; } = null!;
}