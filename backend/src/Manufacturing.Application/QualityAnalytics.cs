using Manufacturing.Domain;

namespace Manufacturing.Application;

public sealed record YieldResult(int EligibleUnits, int FirstPassAcceptedUnits, decimal? FpyPercent);

public static class QualityAnalytics
{
    // Unit identity is (batch, serial). Only an explicit attempt 1 is eligible.
    // Deduplicate before window filtering; a later passing reinspection cannot erase a first-pass failure.
    public static IReadOnlyList<QualityInspection> FirstPass(
        IEnumerable<QualityInspection> inspections, InspectionStage stage, TimeWindow window) => inspections
        .Where(row => row.Stage == stage && row.Attempt == 1)
        .GroupBy(row => (row.ProductionBatchId, row.UnitSerial))
        .Select(group => group.OrderBy(row => row.InspectedAt).ThenBy(row => row.Id).First())
        .Where(row => window.Contains(row.InspectedAt))
        .ToArray();

    public static YieldResult Yield(IEnumerable<QualityInspection> inspections, TimeWindow window)
    {
        var first = FirstPass(inspections, InspectionStage.Aoi, window);
        var accepted = first.Count(row => row.Accepted);
        return new(first.Count, accepted, Percent(accepted, first.Count));
    }

    public static decimal? Percent(int numerator, int denominator) => denominator == 0
        ? null : Math.Round(100m * numerator / denominator, 4, MidpointRounding.AwayFromZero);
}
