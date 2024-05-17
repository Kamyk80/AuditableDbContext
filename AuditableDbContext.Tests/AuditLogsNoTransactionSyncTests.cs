using DotNet.Testcontainers.Containers;
using Microsoft.EntityFrameworkCore;

using static AuditableDbContext.Tests.TestContainers;

namespace AuditableDbContext.Tests;

[TestClass]
public class AuditLogsNoTransactionSyncTests : AuditLogsBaseTests
{
    private const int Port = 60002;

    private static IContainer _database = null!;
    private static DbContextOptions _options = null!;

    [ClassInitialize]
    public static async Task TestFixtureSetup(TestContext _)
    {
        _database = await StartDatabaseAsync(Port);
        _options = BuildContextOptions(Port);

        await using var context = new TestDbContext(_options);
        await context.Database.EnsureCreatedAsync();

        var testEntity = new TestEntity { Name = "OldName", Secret = "OldSecret" };
        context.Add(testEntity);
        var ignoredEntity = new IgnoredEntity { Name = "IgnoredEntity" };
        context.Add(ignoredEntity);
        // ReSharper disable once MethodHasAsyncOverload
        context.SaveChanges("creator@test.com");

        testEntity.Name = "NewName";
        testEntity.Secret = "NewSecret";
        // ReSharper disable once MethodHasAsyncOverload
        context.SaveChanges("updater@test.com");

        context.Remove(testEntity);
        // ReSharper disable once MethodHasAsyncOverload
        context.SaveChanges("deleter@test.com");
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