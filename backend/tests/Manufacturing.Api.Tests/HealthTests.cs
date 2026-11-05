using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Xunit;

namespace Manufacturing.Api.Tests;

public sealed class HealthTests
{
    [Theory]
    [InlineData(true, HttpStatusCode.OK, "healthy")]
    [InlineData(false, HttpStatusCode.ServiceUnavailable, "unhealthy")]
    public async Task Health_reports_dependency_status(bool healthy, HttpStatusCode expectedCode, string status)
    {
        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:Postgres", "Host=localhost;Username=test;Password=test");
            builder.ConfigureServices(services => services.PostConfigure<HealthCheckServiceOptions>(options =>
            {
                options.Registrations.Clear();
                options.Registrations.Add(new HealthCheckRegistration("postgres",
                    new StubHealthCheck(healthy), null, null));
            }));
        });
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/api/health");
        Assert.Equal(expectedCode, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(status, body.RootElement.GetProperty("status").GetString());
        Assert.Equal(status, body.RootElement.GetProperty("checks").GetProperty("postgres").GetString());
        Assert.Equal("manufacturing-api", body.RootElement.GetProperty("service").GetString());
    }

    [Theory]
    [InlineData("/api/lines")]
    [InlineData("/api/lines/3/metrics")]
    public async Task Unavailable_database_returns_sanitized_problem_details(string path)
    {
        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:Postgres",
                "Host=127.0.0.1;Port=1;Database=test;Username=test;Password=secret-test-password;Timeout=1");
        });
        using var client = factory.CreateClient();
        using var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("secret-test-password", body);
        Assert.DoesNotContain("Npgsql", body);
    }

    private sealed class StubHealthCheck(bool healthy) : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
            => Task.FromResult(healthy ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy("Secret connection details"));
    }
}
