using Manutenzione.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Manutenzione.Api.Tests;

/// <summary>Avvia l'API su un database SQLite in memoria e con un orologio fisso.</summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    public static readonly DateTimeOffset Now = new(2026, 3, 10, 9, 0, 0, TimeSpan.Zero);

    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Maintenance", "Server=unused");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<MaintenanceDbContext>>();
            services.RemoveAll<TimeProvider>();

            _connection.Open();
            services.AddDbContext<MaintenanceDbContext>(o => o.UseSqlite(_connection));
            services.AddSingleton<TimeProvider>(new FixedTimeProvider(Now));

            using var scope = services.BuildServiceProvider().CreateScope();
            scope.ServiceProvider.GetRequiredService<MaintenanceDbContext>().Database.EnsureCreated();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _connection.Dispose();
    }
}

public sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
