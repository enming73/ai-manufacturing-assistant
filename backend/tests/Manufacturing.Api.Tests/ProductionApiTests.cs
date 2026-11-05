using System.Net;
using System.Net.Http.Json;
using Manufacturing.Application;
using Manufacturing.Domain;
using Manufacturing.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace Manufacturing.Api.Tests;

[Collection("postgres")]
public sealed class ProductionApiTests(PostgresFixture fixture)
{
    private static async Task<EvidenceResponse<T>> Get<T>(HttpClient client, string path)
    {
        using var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<EvidenceResponse<T>>())!;
    }

    [PostgresFact]
    public async Task Seeded_dashboard_values_are_consistent_across_all_endpoints()
    {
        using var client = fixture.Factory.CreateClient();
        var lines = await client.GetFromJsonAsync<LinesResponse>("/api/lines");
        Assert.Equal(new DateOnly(2026, 10, 15), lines!.DemoDate);
        Assert.Equal([1, 2, 3], lines.Items.Select(row => row.Id));
        var metrics = await Get<MetricsDto>(client, "/api/lines/3/metrics");
        Assert.Equal(3, metrics.LineId);
        Assert.Equal(DemoScenario.DefaultWindow, metrics.Window);
        Assert.Equal("/api/lines/3/metrics", metrics.Source);
        Assert.Equal(6000, metrics.Data.EligibleUnits);
        Assert.Equal(5780, metrics.Data.FirstPassAcceptedUnits);
        Assert.Equal(96.3333m, metrics.Data.FpyPercent);
        Assert.Equal(6000, metrics.Data.SpiInspectedUnits);
        Assert.Equal(350, metrics.Data.SpiAnomalyUnits);
        Assert.Equal(2.975m, metrics.Data.AverageCycleTimeSeconds);

        var trend = (await Get<TrendPoint[]>(client, "/api/lines/3/quality-trend")).Data;
        Assert.Equal(6, trend.Length);
        Assert.Equal(98.5m, trend[0].FpyPercent);
        Assert.Equal(94.2m, trend[^1].FpyPercent);
        Assert.All(trend, row => Assert.Equal(1000, row.EligibleUnits));
        Assert.Equal(metrics.Data.FirstPassAcceptedUnits, trend.Sum(row => row.FirstPassAcceptedUnits));
        Assert.True(trend.Zip(trend.Skip(1)).All(pair => pair.First.FpyPercent > pair.Second.FpyPercent));
        Assert.True(trend.Zip(trend.Skip(1)).All(pair => pair.First.SpiAnomalyUnits < pair.Second.SpiAnomalyUnits));

        var defects = (await Get<DefectDto[]>(client, "/api/lines/3/defects")).Data;
        Assert.Equal(220, defects.Where(row => row.Stage == "AOI").Sum(row => row.Count));
        Assert.Equal(350, defects.Where(row => row.Stage == "SPI").Sum(row => row.Count));
        var alarms = (await Get<AlarmDto[]>(client, "/api/lines/3/alarms")).Data;
        Assert.Equal(16, alarms.Length);
        Assert.Equal(alarms.Length, metrics.Data.AlarmCount);
        Assert.All(alarms, alarm => { Assert.Equal("M03", alarm.MachineCode); Assert.Equal("NOZZLE_PICKUP_FAILURE", alarm.Code); });
        var batches = (await Get<BatchDto[]>(client, "/api/lines/3/batches")).Data;
        Assert.Equal(6, batches.Length);
        Assert.Equal(metrics.Data.EligibleUnits, batches.Sum(row => row.EligibleUnits));
        Assert.Equal(metrics.Data.FirstPassAcceptedUnits, batches.Sum(row => row.FirstPassAcceptedUnits));
    }

    [PostgresFact]
    public async Task Time_and_line_filters_are_applied_to_each_resource()
    {
        using var client = fixture.Factory.CreateClient();
        var query = "?from=" + Uri.EscapeDataString("2026-10-15T08:00:00+02:00") + "&to=" + Uri.EscapeDataString("2026-10-15T09:00:00+02:00");
        var metrics = (await Get<MetricsDto>(client, "/api/lines/3/metrics" + query)).Data;
        Assert.Equal(1000, metrics.EligibleUnits);
        Assert.Equal(98.5m, metrics.FpyPercent);
        Assert.Single((await Get<TrendPoint[]>(client, "/api/lines/3/quality-trend" + query)).Data);
        Assert.Single((await Get<BatchDto[]>(client, "/api/lines/3/batches" + query)).Data);
        Assert.Single((await Get<AlarmDto[]>(client, "/api/lines/3/alarms" + query)).Data);
        Assert.Equal(15, (await Get<DefectDto[]>(client, "/api/lines/3/defects" + query)).Data.Where(row => row.Stage == "AOI").Sum(row => row.Count));
        var other = (await Get<MetricsDto>(client, "/api/lines/1/metrics")).Data;
        Assert.Equal(99m, other.FpyPercent);
        Assert.Equal(0, other.AlarmCount);
        Assert.Empty((await Get<AlarmDto[]>(client, "/api/lines/1/alarms")).Data);
    }

    [PostgresFact]
    public async Task Empty_window_returns_null_fpy_and_empty_evidence()
    {
        using var client = fixture.Factory.CreateClient();
        const string query = "?from=2026-10-16T06:00:00Z&to=2026-10-16T07:00:00Z";
        var metrics = (await Get<MetricsDto>(client, "/api/lines/3/metrics" + query)).Data;
        Assert.Null(metrics.FpyPercent);
        Assert.Equal(0, metrics.EligibleUnits);
        Assert.Null(metrics.SpiAnomalyPercent);
        Assert.Null(metrics.AverageCycleTimeSeconds);
        Assert.Empty((await Get<AlarmDto[]>(client, "/api/lines/3/alarms" + query)).Data);
        Assert.Empty((await Get<DefectDto[]>(client, "/api/lines/3/defects" + query)).Data);
        Assert.Empty((await Get<BatchDto[]>(client, "/api/lines/3/batches" + query)).Data);
        Assert.All((await Get<TrendPoint[]>(client, "/api/lines/3/quality-trend" + query)).Data,
            point => { Assert.Null(point.FpyPercent); Assert.Equal(0, point.EligibleUnits); });
    }

    [PostgresTheory]
    [InlineData("metrics")]
    [InlineData("quality-trend")]
    [InlineData("alarms")]
    [InlineData("defects")]
    [InlineData("batches")]
    public async Task Unknown_line_returns_problem_details(string resource)
    {
        using var client = fixture.Factory.CreateClient();
        using var response = await client.GetAsync($"/api/lines/999/{resource}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
    }

    [PostgresTheory]
    [InlineData("metrics")]
    [InlineData("quality-trend")]
    [InlineData("alarms")]
    [InlineData("defects")]
    [InlineData("batches")]
    public async Task Invalid_range_returns_validation_problem(string resource)
    {
        using var client = fixture.Factory.CreateClient();
        using var response = await client.GetAsync($"/api/lines/3/{resource}?from=2026-10-15T12:00:00Z&to=2026-10-15T06:00:00Z");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
    }

    [PostgresFact]
    public async Task Malformed_and_oversized_windows_are_rejected_over_http()
    {
        using var client = fixture.Factory.CreateClient();
        foreach (var query in new[] { "from=bad", "from=2026-10-15T06:00:00", "from=2026-10-01T00:00:00Z&to=2026-10-15T00:00:00Z" })
        {
            using var response = await client.GetAsync("/api/lines/3/metrics?" + query);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }

    [PostgresFact]
    public async Task Demo_seed_is_idempotent_and_keeps_raw_measurements_and_reinspections()
    {
        await using var db = fixture.CreateContext();
        Assert.Equal(36352, await db.QualityInspections.CountAsync());
        await DemoSeeder.SeedAsync(db);
        Assert.Equal(36352, await db.QualityInspections.CountAsync());
        Assert.Equal(18, await db.ProductionBatches.CountAsync());
        Assert.Equal(18, await db.ProductionMetrics.CountAsync());
        Assert.Equal(350, await db.QualityInspections.CountAsync(row => row.ProductionLineId == 3 && row.Stage == InspectionStage.Spi && row.PasteVolumePercent > 125m));
        Assert.Equal(220, await db.QualityInspections.CountAsync(row => row.ProductionLineId == 3 && row.Stage == InspectionStage.Aoi && row.Attempt == 2));
        Assert.Single(await db.Database.GetAppliedMigrationsAsync());
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
    }

    [PostgresFact]
    public async Task Database_rejects_duplicate_inspection_attempts()
    {
        await using var db = fixture.CreateContext();
        var row = await db.QualityInspections.AsNoTracking().OrderBy(row => row.Id).FirstAsync();
        row.Id = 900000;
        db.QualityInspections.Add(row);
        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        Assert.Equal(PostgresErrorCodes.UniqueViolation, Assert.IsType<PostgresException>(exception.InnerException).SqlState);
    }

    [PostgresFact]
    public async Task Database_prevents_an_inspection_from_linking_a_different_lines_machine()
    {
        await using var db = fixture.CreateContext();
        var row = await db.QualityInspections.AsNoTracking().FirstAsync(row => row.ProductionLineId == 3);
        row.Id = 900001;
        row.UnitSerial = "CROSS-LINE";
        row.MachineId = 11;
        db.QualityInspections.Add(row);
        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, Assert.IsType<PostgresException>(exception.InnerException).SqlState);
    }

    [PostgresFact]
    public async Task Analytics_does_not_accept_writes()
    {
        using var client = fixture.Factory.CreateClient();
        using var response = await client.PostAsJsonAsync("/api/lines/3/metrics", new { value = 100 });
        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }
}
