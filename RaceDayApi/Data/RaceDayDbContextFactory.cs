using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace RaceDayApi.Data;

/// <summary>
/// Lets `dotnet ef` create the context without starting the web app. It reads the same
/// connection string as the running API.
/// </summary>
public class RaceDayDbContextFactory : IDesignTimeDbContextFactory<RaceDayDbContext>
{
    public RaceDayDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddJsonFile("appsettings.Local.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var options = new DbContextOptionsBuilder<RaceDayDbContext>()
            .UseSqlServer(configuration.GetConnectionString("RaceDayDb"))
            .Options;

        return new RaceDayDbContext(options);
    }
}
