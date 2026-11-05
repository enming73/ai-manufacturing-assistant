using Manufacturing.Application;
using Manufacturing.Domain;
using Xunit;

namespace Manufacturing.Api.Tests;

public sealed class QualityAnalyticsTests
{
    private static readonly TimeWindow Window = DemoScenario.DefaultWindow;

    private static QualityInspection Inspection(long id, string serial, bool accepted, int attempt = 1,
        int batch = 1, InspectionStage stage = InspectionStage.Aoi, DateTimeOffset? at = null) => new()
    {
        Id = id, ProductionLineId = 1, ProductionBatchId = batch, MachineId = 1,
        UnitSerial = serial, Stage = stage, Attempt = attempt, InspectedAt = at ?? Window.From,
        Accepted = accepted, DefectCode = accepted ? null : "SOLDER_BRIDGE"
    };

    [Fact]
    public void Fpy_counts_units_by_first_final_aoi_instead_of_all_inspections()
    {
        QualityInspection[] rows = [Inspection(1, "A", true), Inspection(2, "B", false),
            Inspection(3, "B", true, attempt: 2), Inspection(4, "A", false, stage: InspectionStage.Spi)];
        Assert.Equal(new YieldResult(2, 1, 50m), QualityAnalytics.Yield(rows, Window));
    }

    [Fact]
    public void Repeated_and_conflicting_duplicate_first_pass_results_count_once_in_deterministic_order()
    {
        var first = Inspection(1, "A", false);
        QualityInspection[] rows = [first, first, Inspection(2, "A", true, at: Window.From.AddMinutes(1)),
            Inspection(3, "A", true, attempt: 2), Inspection(4, "B", true)];
        Assert.Equal(new YieldResult(2, 1, 50m), QualityAnalytics.Yield(rows.Reverse(), Window));
    }

    [Fact]
    public void Same_serial_in_different_batches_identifies_different_units()
        => Assert.Equal(new YieldResult(2, 1, 50m), QualityAnalytics.Yield(
            [Inspection(1, "A", true), Inspection(2, "A", false, batch: 2)], Window));

    [Fact]
    public void Empty_window_has_no_fpy_instead_of_zero_or_division_by_zero()
        => Assert.Equal(new YieldResult(0, 0, null), QualityAnalytics.Yield([], Window));

    [Fact]
    public void Window_is_half_open_and_reinspection_does_not_move_a_unit_into_another_window()
    {
        QualityInspection[] rows = [Inspection(1, "A", false, at: Window.From.AddSeconds(-1)),
            Inspection(2, "A", true, attempt: 2), Inspection(3, "B", true, at: Window.To),
            Inspection(4, "C", true, at: Window.From), Inspection(5, "D", true, attempt: 2)];
        Assert.Equal(new YieldResult(1, 1, 100m), QualityAnalytics.Yield(rows, Window));
    }

    [Fact]
    public void Deduplication_precedes_window_filtering()
    {
        QualityInspection[] rows = [Inspection(1, "A", false, at: Window.From.AddSeconds(-1)), Inspection(2, "A", true)];
        Assert.Equal(new YieldResult(0, 0, null), QualityAnalytics.Yield(rows, Window));
    }

    [Theory]
    [InlineData("bad", null)]
    [InlineData("2026-10-15", null)]
    [InlineData("2026-10-15T06:00:00", null)]
    [InlineData("2026-10-15T12:00:00Z", "2026-10-15T06:00:00Z")]
    [InlineData("2026-10-15T06:00:00Z", "2026-10-15T06:00:00Z")]
    [InlineData("2026-10-01T00:00:00Z", "2026-10-15T00:00:00Z")]
    [InlineData("", null)]
    public void Invalid_windows_are_rejected(string from, string? to)
        => Assert.False(TimeWindow.TryParse(from, to, out _, out _));

    [Fact]
    public void Explicit_offsets_are_normalized_to_utc()
    {
        Assert.True(TimeWindow.TryParse("2026-10-15T08:00:00+02:00", "2026-10-15T14:00:00+02:00", out var window, out _));
        Assert.Equal(Window, window);
    }
}
