using System.Diagnostics;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AuditableDbContext.Tests;

public static class TestContainers
{
    public static async Task<IContainer> StartDatabaseAsync(int port)
    {
        var container = new ContainerBuilder()
            .WithImage("mcr.microsoft.com/azure-sql-edge")
            .WithEnvironment("ACCEPT_EULA", "Y")
            .WithEnvironment("MSSQL_SA_PASSWORD", "Password1!")
            .WithExposedPort(port)
            .WithPortBinding(port, 1433)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(1433))
            .Build();

        await container.StartAsync();

        return container;
    }

    public static DbContextOptions BuildContextOptions(int port)
    {
        return new DbContextOptionsBuilder()
            .UseSqlServer($"Server=localhost,{port}; Initial Catalog=Test; User Id=sa; Password=Password1!;")
            .LogTo(message => Debug.WriteLine(message), LogLevel.Information)
            .EnableSensitiveDataLogging()
            .Options;
    }
}