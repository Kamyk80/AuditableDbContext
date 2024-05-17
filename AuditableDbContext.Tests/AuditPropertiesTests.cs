using DotNet.Testcontainers.Containers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

using static AuditableDbContext.Tests.TestContainers;

namespace AuditableDbContext.Tests;

[TestClass]
public class AuditPropertiesTests
{
    private const int Port = 60005;

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
        await context.SaveChangesAsync("creator@test.com");

        testEntity.Name = "NewName";
        testEntity.Secret = "NewSecret";
        await context.SaveChangesAsync("updater@test.com");
    }
    
    [TestMethod]
    public async Task AccountsDbContext_SavingChanges_ShouldSetTimeStamps()
    {
        await using var context = new TestDbContext(_options);

        var testEntity = await context.TestEntities.SingleAsync();

        testEntity.CreatedBy.Should().Be("creator@test.com");
        testEntity.CreatedOn.Should().BeCloseTo(DateTimeOffset.Now, TimeSpan.FromMinutes(1));
        testEntity.UpdatedBy.Should().Be("updater@test.com");
        testEntity.UpdatedOn.Should().BeCloseTo(DateTimeOffset.Now, TimeSpan.FromMinutes(1));
    }

    [ClassCleanup]
    public static async Task TestFixtureTearDown()
    {
        await _database.StopAsync();
    }
}