using Manufacturing.Application;

namespace Manufacturing.Api;

public static class ProductionEndpoints
{
    public static void MapProductionEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/lines");
        group.MapGet("", async (IProductionReadStore store, CancellationToken ct) =>
            TypedResults.Ok(new LinesResponse(DemoScenario.Date, DemoScenario.ScenarioLineId,
                DemoScenario.DefaultWindow, await store.LinesAsync(ct))));

        foreach (var resource in new[] { "metrics", "quality-trend", "alarms", "defects", "batches" })
        {
            group.MapGet($"/{{id:int}}/{resource}", async (int id, string? from, string? to,
                HttpContext context, IProductionReadStore store, ProductionAnalyticsService analytics,
                CancellationToken ct) =>
            {
                if (!TimeWindow.TryParse(from, to, out var window, out var error))
                    return Results.ValidationProblem(new Dictionary<string, string[]> { ["window"] = [error!] });
                if (!await store.LineExistsAsync(id, ct))
                    return Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Production line not found.");
                var source = context.Request.Path + context.Request.QueryString;
                return resource switch
                {
                    "metrics" => Results.Ok(new EvidenceResponse<MetricsDto>(id, window, source,
                        await analytics.MetricsAsync(id, window, ct))),
                    "quality-trend" => Results.Ok(new EvidenceResponse<IReadOnlyList<TrendPoint>>(id, window, source,
                        await analytics.TrendAsync(id, window, ct))),
                    "alarms" => Results.Ok(new EvidenceResponse<IReadOnlyList<AlarmDto>>(id, window, source,
                        await analytics.AlarmsAsync(id, window, ct))),
                    "defects" => Results.Ok(new EvidenceResponse<IReadOnlyList<DefectDto>>(id, window, source,
                        await analytics.DefectsAsync(id, window, ct))),
                    "batches" => Results.Ok(new EvidenceResponse<IReadOnlyList<BatchDto>>(id, window, source,
                        await analytics.BatchesAsync(id, window, ct))),
                    _ => Results.NotFound()
                };
            });
        }
    }
}
