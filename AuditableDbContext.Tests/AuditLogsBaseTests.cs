using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace AuditableDbContext.Tests;

public abstract class AuditLogsBaseTests
{
    protected DbContextOptions Options = null!;

    [TestMethod]
    public async Task AccountsDbContext_SavingChanges_ShouldCreateAddedAuditLogs()
    {
        await using var context = new TestDbContext(Options);

        var auditLogs = await context.AuditLogs
            .Where(log => log.Operation == "Added")
            .ToListAsync();

        auditLogs.Should().NotBeEmpty().And.HaveCount(1)
            .And.ContainSingle(log => log.Entity == "TestEntity");
    }

    [TestMethod]
    public async Task AccountsDbContext_SavingChanges_ShouldCreateModifiedAuditLogs()
    {
        await using var context = new TestDbContext(Options);

        var auditLogs = await context.AuditLogs
            .Where(log => log.Operation == "Modified")
            .ToListAsync();

        auditLogs.Should().NotBeEmpty().And.HaveCount(1)
            .And.ContainSingle(log => log.Entity == "TestEntity");
    }

    [TestMethod]
    public async Task AccountsDbContext_SavingChanges_ShouldCreateDeletedAuditLogs()
    {
        await using var context = new TestDbContext(Options);

        var auditLogs = await context.AuditLogs
            .Where(log => log.Operation == "Deleted")
            .ToListAsync();

        auditLogs.Should().NotBeEmpty().And.HaveCount(1)
            .And.ContainSingle(log => log.Entity == "TestEntity");
    }

    [TestMethod]
    public async Task AccountsDbContext_SavingChanges_ShouldSetPropertiesForAddedAuditLog()
    {
        await using var context = new TestDbContext(Options);

        var auditLog = await context.AuditLogs
            .Where(log => log.Operation == "Added")
            .Where(log => log.Entity == "TestEntity")
            .SingleAsync();

        auditLog.Username.Should().Be("creator@test.com");
        auditLog.Timestamp.Should().BeCloseTo(DateTimeOffset.Now, TimeSpan.FromMinutes(1));
        auditLog.EntityId.Should().Be(1);
        auditLog.OldValues.Should().BeNull();
        auditLog.NewValues.Should().NotBeNull();
        auditLog.Changes.Should().BeNull();

        var testEntity = JsonSerializer.Deserialize<TestEntity>(auditLog.NewValues!)!;
        testEntity.Id.Should().Be(1);
        testEntity.Name.Should().Be("OldName");
        testEntity.Secret.Should().BeNull();
    }

    [TestMethod]
    public async Task AccountsDbContext_SavingChanges_ShouldSetPropertiesForModifiedAuditLog()
    {
        await using var context = new TestDbContext(Options);

        var auditLog = await context.AuditLogs
            .Where(log => log.Operation == "Modified")
            .Where(log => log.Entity == "TestEntity")
            .SingleAsync();

        auditLog.Username.Should().Be("updater@test.com");
        auditLog.Timestamp.Should().BeCloseTo(DateTimeOffset.Now, TimeSpan.FromMinutes(1));
        auditLog.EntityId.Should().Be(1);
        auditLog.OldValues.Should().NotBeNull();
        auditLog.NewValues.Should().NotBeNull();
        auditLog.Changes.Should().NotBeNull();

        var oldTestEntity = JsonSerializer.Deserialize<TestEntity>(auditLog.OldValues!)!;
        oldTestEntity.Id.Should().Be(1);
        oldTestEntity.Name.Should().Be("OldName");
        oldTestEntity.Secret.Should().BeNull();

        var newTestEntity = JsonSerializer.Deserialize<TestEntity>(auditLog.NewValues!)!;
        newTestEntity.Id.Should().Be(1);
        newTestEntity.Name.Should().Be("NewName");
        newTestEntity.Secret.Should().BeNull();

        var changes = JsonSerializer.Deserialize<string[]>(auditLog.Changes!)!;
        changes.Should().Contain(new[] { "Name", "Secret" });
    }

    [TestMethod]
    public async Task AccountsDbContext_SavingChanges_ShouldSetPropertiesForDeletedAuditLog()
    {
        await using var context = new TestDbContext(Options);

        var auditLog = await context.AuditLogs
            .Where(log => log.Operation == "Deleted")
            .Where(log => log.Entity == "TestEntity")
            .SingleAsync();

        auditLog.Username.Should().Be("deleter@test.com");
        auditLog.Timestamp.Should().BeCloseTo(DateTimeOffset.Now, TimeSpan.FromMinutes(1));
        auditLog.EntityId.Should().Be(1);
        auditLog.OldValues.Should().NotBeNull();
        auditLog.NewValues.Should().BeNull();
        auditLog.Changes.Should().BeNull();

        var testEntity = JsonSerializer.Deserialize<TestEntity>(auditLog.OldValues!)!;
        testEntity.Id.Should().Be(1);
        testEntity.Name.Should().Be("NewName");
        testEntity.Secret.Should().BeNull();
    }
}