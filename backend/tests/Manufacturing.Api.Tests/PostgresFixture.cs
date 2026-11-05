using Manufacturing.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace Manufacturing.Api.Tests;

public sealed class PostgresFactAttribute : FactAttribute
{
    public PostgresFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("TEST_POSTGRES_CONNECTION")))
            Skip = "Set TEST_POSTGRES_CONNECTION to an isolated PostgreSQL admin connection to run real database tests.";
    }
}

public sealed class PostgresTheoryAttribute : TheoryAttribute
{
    public PostgresTheoryAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("TEST_POSTGRES_CONNECTION")))
            Skip = "Set TEST_POSTGRES_CONNECTION to an isolated PostgreSQL admin connection to run real database tests.";
    }
}

[CollectionDefinition("postgres")]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>;

public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly string? adminConnection = Environment.GetEnvironmentVariable("TEST_POSTGRES_CONNECTION");
    private readonly string database = $"manufacturing_test_{Guid.NewGuid():N}";
    private string? connection;
    public WebApplicationFactory<Program> Factory { get; private set; } = null!;

    public ManufacturingDbContext CreateContext() => new(new DbContextOptionsBuilder<ManufacturingDbContext>()
        .UseNpgsql(connection ?? throw new InvalidOperationException("Test database has not been initialized.")).Options);

    public async Task InitializeAsync()
    {
        if (string.IsNullOrWhiteSpace(adminConnection)) return;
        await using var admin = new NpgsqlConnection(adminConnection);
        await admin.OpenAsync();
        await using var command = new NpgsqlCommand($"CREATE DATABASE \"{database}\"", admin);
        await command.ExecuteNonQueryAsync();
        connection = new NpgsqlConnectionStringBuilder(adminConnection) { Database = database }.ConnectionString;
        await using var db = CreateContext();
        await db.Database.MigrateAsync();
        await DemoSeeder.SeedAsync(db);
        Factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:Postgres", connection);
            builder.UseSetting("Database:ApplyMigrations", "false");
            builder.UseSetting("Demo:SeedOnStartup", "false");
        });
    }

    public async Task DisposeAsync()
    {
        if (connection is null || adminConnection is null) return;
        if (Factory is not null) await Factory.DisposeAsync();
        NpgsqlConnection.ClearAllPools();
        await using var admin = new NpgsqlConnection(adminConnection);
        await admin.OpenAsync();
        await using var command = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{database}\" WITH (FORCE)", admin);
        await command.ExecuteNonQueryAsync();
    }
}
