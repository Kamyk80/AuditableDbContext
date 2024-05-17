using DotNet.Testcontainers.Containers;
using Microsoft.EntityFrameworkCore;

using static AuditableDbContext.Tests.TestContainers;

namespace AuditableDbContext.Tests;

[TestClass]
public class AuditLogsTransactionAsyncTests : AuditLogsBaseTests
{
    private const int Port = 60003;

    private static IContainer _database = null!;
    private static DbContextOptions _options = null!;

    [ClassInitialize]
    public static async Task TestFixtureSetup(TestContext _)
    {
        _database = await StartDatabaseAsync(Port);
        _options = BuildContextOptions(Port);

        await using var context = new TestDbContext(_options);
        await context.Database.EnsureCreatedAsync();

        await using var transaction = await context.Database.BeginTransactionAsync();

        var testEntity = new TestEntity { Name = "OldName", Secret = "OldSecret" };
        context.Add(testEntity);
        var ignoredEntity = new IgnoredEntity { Name = "IgnoredEntity" };
        context.Add(ignoredEntity);
        await context.SaveChangesAsync("creator@test.com");

        testEntity.Name = "NewName";
        testEntity.Secret = "NewSecret";
        await context.SaveChangesAsync("updater@test.com");

        context.Remove(testEntity);
        await context.SaveChangesAsync("deleter@test.com");

        await transaction.CommitAsync();
    }

    [TestInitialize]
    public void TestSetup()
    {
        Options = _options;
    }

    [ClassCleanup]
    public static async Task TestFixtureTearDown()
    {
        await _database.StopAsync();
    }
}