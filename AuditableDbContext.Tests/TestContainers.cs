using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Testcontainers.MsSql;

namespace AuditableDbContext.Tests;

public static class TestContainers
{
    public static async Task<MsSqlContainer> StartDatabaseAsync(int port)
    {
        var container = new MsSqlBuilder()
            .WithPortBinding(port, 1433)
            .Build();

        await container.StartAsync();

        return container;
    }

    public static DbContextOptions BuildContextOptions(MsSqlContainer database) =>
        new DbContextOptionsBuilder()
            .UseSqlServer(database.GetConnectionString())
            .LogTo(message => Debug.WriteLine(message), LogLevel.Information)
            .EnableSensitiveDataLogging()
            .Options;
}