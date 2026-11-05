using System.Globalization;
using System.Text.RegularExpressions;

namespace Manufacturing.Application;

public sealed record TimeWindow(DateTimeOffset From, DateTimeOffset To)
{
    public bool Contains(DateTimeOffset timestamp) => timestamp >= From && timestamp < To;

    public static bool TryParse(string? from, string? to, out TimeWindow window, out string? error)
    {
        window = DemoScenario.DefaultWindow;
        error = null;
        if (!TryTimestamp(from, window.From, out var start) || !TryTimestamp(to, window.To, out var end))
            error = "from and to must be ISO 8601 timestamps with an explicit UTC offset.";
        else if (start >= end)
            error = "from must be earlier than to; the window is [from, to).";
        else if (end - start > TimeSpan.FromDays(7))
            error = "The maximum time window is 7 days.";
        else
            window = new TimeWindow(start.ToUniversalTime(), end.ToUniversalTime());
        return error is null;
    }

    private static bool TryTimestamp(string? input, DateTimeOffset fallback, out DateTimeOffset result)
    {
        result = fallback;
        return input is null || (input.Contains('T') &&
            (input.EndsWith('Z') || Regex.IsMatch(input, @"[+-]\d{2}:\d{2}$")) &&
            DateTimeOffset.TryParse(input, CultureInfo.InvariantCulture, DateTimeStyles.None, out result));
    }
}
