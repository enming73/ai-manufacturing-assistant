using Manufacturing.Application;
using Manufacturing.Api;
using Manufacturing.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DatabaseExceptionHandler>();
var connectionString = builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException("ConnectionStrings__Postgres must be configured.");
builder.Services.AddSingleton(NpgsqlDataSource.Create(connectionString));
builder.Services.AddDbContext<ManufacturingDbContext>((services, options) =>
    options.UseNpgsql(services.GetRequiredService<NpgsqlDataSource>()));
builder.Services.AddScoped<IProductionReadStore, ProductionReadStore>();
builder.Services.AddScoped<ProductionAnalyticsService>();
builder.Services.AddHealthChecks().AddCheck<PostgresHealthCheck>("postgres");
var app = builder.Build();
app.UseExceptionHandler();
app.MapHealthChecks("/api/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        await context.Response.WriteAsJsonAsync(new
        {
            status = report.Status == HealthStatus.Healthy ? "healthy" : "unhealthy",
            service = "manufacturing-api",
            checks = report.Entries.ToDictionary(
                entry => entry.Key,
                entry => entry.Value.Status == HealthStatus.Healthy ? "healthy" : "unhealthy")
        });
    }
});
app.MapProductionEndpoints();
if (app.Configuration.GetValue<bool>("Database:ApplyMigrations"))
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<ManufacturingDbContext>();
    await db.Database.MigrateAsync();
    if (app.Configuration.GetValue<bool>("Demo:SeedOnStartup"))
    {
        await DemoSeeder.SeedAsync(db);
        app.Logger.LogInformation("Synthetic demo scenario ready for {DemoDate}", DemoScenario.Date);
    }
}
else if (app.Configuration.GetValue<bool>("Demo:SeedOnStartup"))
{
    throw new InvalidOperationException("Demo:SeedOnStartup requires Database:ApplyMigrations.");
}
app.Run();

public partial class Program;
