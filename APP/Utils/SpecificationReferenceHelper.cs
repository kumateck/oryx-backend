namespace APP.Utils;

public static class SpecificationReferenceHelper
{
    public static string FormatReference(string reference)
    {
        if (string.IsNullOrWhiteSpace(reference))
            return reference;

        var trimmed = reference.Trim();
        var normalized = trimmed.Replace("-", "").Replace(" ", "").ToLower();
        if (normalized.Contains("inhouse"))
        {
            return trimmed;
        }

        var currentYear = DateTime.UtcNow.Year.ToString();
        if (trimmed.EndsWith(currentYear))
        {
            return trimmed;
        }

        return $"{trimmed} {currentYear}";
    }
}
