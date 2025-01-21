namespace APP.Utils;

public static class UnitOfMeasureUtils
{
    public static IEnumerable<(string Name, string Symbol, string Description, bool IsScalable)> All()
    {
        return new List<(string Name, string Symbol, string Description, bool IsScalable)>
        {
            ("Gram", "g", "Base unit of mass in the metric system", true),
            ("Liter", "l", "Base unit of volume in the metric system", true),
            ("Meter", "m", "Base unit of length in the metric system", true),
            ("Piece", "piece", "Unit for counting individual items", false),
            ("Tablet", "tablet", "Unit for counting tablets", false),
            ("Pack", "pack", "Unit for counting packs of items", false),
            ("Box", "box", "Unit for counting boxes of items", false),
            ("Bottle", "bottle", "Unit for counting bottles of liquid", false),
            ("Square Meter", "m²", "Unit of area in the metric system", false),
            ("Cubic Meter", "m³", "Unit of volume in the metric system", true)
        };
    }
}
