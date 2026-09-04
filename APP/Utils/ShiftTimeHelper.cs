using System.Globalization;

namespace APP.Utils;

/// <summary>
/// Single source of truth for shift time-range comparisons, so an overnight shift
/// (end &lt;= start, e.g. 22:00-06:00) is handled consistently everywhere a conflict
/// is checked. Previously each call site re-implemented this independently — some
/// correctly, some not.
/// </summary>
public static class ShiftTimeHelper
{
    public const string WireFormat = "hh:mm tt";

    public static TimeOnly Parse(string time) =>
        TimeOnly.ParseExact(time, WireFormat, CultureInfo.InvariantCulture);

    public static string Format(TimeOnly time) => time.ToString(WireFormat, CultureInfo.InvariantCulture);

    public static bool HasOverlap(TimeOnly startA, TimeOnly endA, TimeOnly startB, TimeOnly endB)
    {
        var spanAStart = startA.ToTimeSpan();
        var spanAEnd = endA.ToTimeSpan();
        var spanBStart = startB.ToTimeSpan();
        var spanBEnd = endB.ToTimeSpan();

        if (spanAEnd <= spanAStart)
            spanAEnd = spanAEnd.Add(TimeSpan.FromDays(1));

        if (spanBEnd <= spanBStart)
            spanBEnd = spanBEnd.Add(TimeSpan.FromDays(1));

        return spanAStart < spanBEnd && spanAEnd > spanBStart;
    }

    /// <summary>Shift length, correctly crossing midnight when end &lt;= start.</summary>
    public static TimeSpan Duration(TimeOnly start, TimeOnly end)
    {
        var span = end.ToTimeSpan() - start.ToTimeSpan();
        return span <= TimeSpan.Zero ? span.Add(TimeSpan.FromDays(1)) : span;
    }
}
