using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using moto_micro_tours;

namespace moto_micro_tours.Tests;

/// <summary>
/// Boots the real Program with an in-memory SQLite database instead of MySQL,
/// so integration tests run self-contained (no MySQL service needed).
/// DbSeeder detects the SQLite provider and creates the schema directly.
/// </summary>
public class MotoTestFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public MotoTestFactory()
    {
        _connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureServices(services =>
        {
            // Replace the MySQL context and its options with SQLite.
            // (EF Core 8 registers options directly; from EF 9 also remove
            // IDbContextOptionsConfiguration<AppDbContext>.)
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.AddDbContext<AppDbContext>(options => options.UseSqlite(_connection));
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _connection.Dispose();
        }
        base.Dispose(disposing);
    }
}
