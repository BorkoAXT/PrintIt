using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace PrintIt.Tests.Integration;

public class PrintItFactory : WebApplicationFactory<WebProgram>
{
    private SqliteConnection _connection = null!;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Remove existing DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (descriptor != null) services.Remove(descriptor);

            // Open shared SQLite in-memory connection
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            // Enable foreign keys
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "PRAGMA foreign_keys = ON;";
            cmd.ExecuteNonQuery();

            // Register DbContext with same connection
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite(_connection));

            services.AddSingleton<IMemoryCache, MemoryCache>();

            // Build service provider and create schema
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Database.EnsureCreated();
        });
    }

    protected override void Dispose(bool disposing)
    {
        _connection?.Dispose();
        base.Dispose(disposing);
    }
}