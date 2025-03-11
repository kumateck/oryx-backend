namespace APP.Utils;

public static class UnitOfMeasureUtils
{
    public static IEnumerable<(string Name, string Symbol, string Description, bool IsScalable, bool IsRawMaterial)> All()
    {
        return new List<(string Name, string Symbol, string Description, bool IsScalable, bool IsRawMaterial)>
        {
            ("Milligram", "mg", "Base unit of mass in the metric system", true, true),
            ("Milliliter", "ml", "Base unit of volume in the metric system", true, true),
            ("Meter", "m", "Base unit of length in the metric system", true, false),
            ("Piece", "piece", "Unit for counting individual items", false, false),
            ("Tablet", "tablet", "Unit for counting tablets", false, false),
            ("Pack", "pack", "Unit for counting packs of items", false, false),
            ("Box", "box", "Unit for counting boxes of items", false, false),
            ("Bottle", "bottle", "Unit for counting bottles of liquid", false, false),
            ("Square Meter", "m²", "Unit of area in the metric system", false, false),
            ("Cubic Meter", "m³", "Unit of volume in the metric system", true, false)
        };
    }
}
