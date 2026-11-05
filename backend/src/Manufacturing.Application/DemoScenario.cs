namespace Manufacturing.Application;

public static class DemoScenario
{
    public static readonly DateOnly Date = new(2026, 10, 15);
    public static readonly TimeWindow DefaultWindow = new(
        new DateTimeOffset(2026, 10, 15, 6, 0, 0, TimeSpan.Zero),
        new DateTimeOffset(2026, 10, 15, 12, 0, 0, TimeSpan.Zero));
    public const int ScenarioLineId = 3;
}
