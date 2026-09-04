using APP.Utils;
using Xunit;

namespace APP.Tests.Utils;

public class ShiftTimeHelperTests
{
    [Fact]
    public void HasOverlap_DetectsTwoOvernightShiftsThatOverlap()
    {
        // Both assigned to the same calendar date: 20:00-04:00 and 22:00-06:00 overlap
        // from 22:00 to 04:00 the next day - the previous naive (non-wraparound)
        // comparison missed this because 04:00 < 22:00 when compared as bare times.
        var shiftAStart = new TimeOnly(20, 0);
        var shiftAEnd = new TimeOnly(4, 0);
        var shiftBStart = new TimeOnly(22, 0);
        var shiftBEnd = new TimeOnly(6, 0);

        Assert.True(ShiftTimeHelper.HasOverlap(shiftAStart, shiftAEnd, shiftBStart, shiftBEnd));
    }

    [Fact]
    public void HasOverlap_ReturnsFalseWhenAnOvernightShiftEndsBeforeTheNextOneStarts()
    {
        // Same calendar date: a 22:00-06:00 night shift ends the next morning before
        // a 08:00-16:00 day shift (assigned to that same original date) begins.
        var nightStart = new TimeOnly(22, 0);
        var nightEnd = new TimeOnly(6, 0);
        var dayStart = new TimeOnly(8, 0);
        var dayEnd = new TimeOnly(16, 0);

        Assert.False(ShiftTimeHelper.HasOverlap(nightStart, nightEnd, dayStart, dayEnd));
    }

    [Fact]
    public void HasOverlap_ReturnsFalseForBackToBackShiftsWithNoGap()
    {
        var morningStart = new TimeOnly(6, 0);
        var morningEnd = new TimeOnly(14, 0);
        var afternoonStart = new TimeOnly(14, 0);
        var afternoonEnd = new TimeOnly(22, 0);

        Assert.False(ShiftTimeHelper.HasOverlap(morningStart, morningEnd, afternoonStart, afternoonEnd));
    }

    [Fact]
    public void Duration_HandlesOvernightShiftCorrectly()
    {
        var start = new TimeOnly(22, 0);
        var end = new TimeOnly(6, 0);

        Assert.Equal(TimeSpan.FromHours(8), ShiftTimeHelper.Duration(start, end));
    }

    [Fact]
    public void Duration_HandlesSameDayShiftCorrectly()
    {
        var start = new TimeOnly(8, 0);
        var end = new TimeOnly(16, 30);

        Assert.Equal(TimeSpan.FromHours(8.5), ShiftTimeHelper.Duration(start, end));
    }

    [Fact]
    public void FormatAndParse_RoundTripThroughTheWireFormat()
    {
        var time = new TimeOnly(6, 30);
        var formatted = ShiftTimeHelper.Format(time);

        Assert.Equal("06:30 AM", formatted);
        Assert.Equal(time, ShiftTimeHelper.Parse(formatted));
    }
}
