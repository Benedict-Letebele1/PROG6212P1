using System.Globalization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RaceDayApi.Data;

namespace RaceDayApi.Tests.Infrastructure;

/// <summary>
/// Hosts the API in memory. Each test class gets its own factory, and so its own database.
/// The database is in-memory SQLite, so the tests run without SQL Server or LocalDB on any machine.
/// </summary>
public class RaceDayApiFactory : WebApplicationFactory<Program>
{
    // A unique name per factory keeps each test class's data separate.
    private readonly SqliteConnection _connection = new($"Data Source=RaceDayTests-{Guid.NewGuid():N};Mode=Memory;Cache=Shared");

    public RaceDayApiFactory()
    {
        // The schema uses GETDATE() as a default, which SQLite does not have, so I register an equivalent.
        _connection.CreateFunction("GETDATE", () => DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture));
        _connection.Open();

        // Tables come from the model. The migrations are written for SQL Server, so the tests skip them.
        using var scope = Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<RaceDayDbContext>().Database.EnsureCreated();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Database:ApplyMigrationsOnStartup"] = "false",
            ["Database:SeedDemoData"] = "false"
        }));

        builder.ConfigureTestServices(services =>
        {
            // The SQL Server configuration from Program.cs is removed, so only SQLite is configured for the context.
            services.RemoveAll<DbContextOptions<RaceDayDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<RaceDayDbContext>>();

            // EF Core allows one database provider per service provider, so SQLite gets its own internal provider.
            var sqliteServices = new ServiceCollection().AddEntityFrameworkSqlite().BuildServiceProvider();
            services.AddDbContext<RaceDayDbContext>(options => options
                .UseSqlite(_connection)
                .UseInternalServiceProvider(sqliteServices));
        });
    }

    /// <summary>Runs a database action in its own scope, so tests can check what the API stored.</summary>
    public async Task<T> WithDbAsync<T>(Func<RaceDayDbContext, Task<T>> action)
    {
        using var scope = Services.CreateScope();
        return await action(scope.ServiceProvider.GetRequiredService<RaceDayDbContext>());
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection.Dispose();
        }
    }
}
