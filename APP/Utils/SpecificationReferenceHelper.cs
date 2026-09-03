using System.Text.RegularExpressions;

namespace APP.Utils;

public static class SpecificationReferenceHelper
{
    private static readonly Regex TrailingYearPattern = new(@"\s+\d{4}$", RegexOptions.Compiled);

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
        var withoutTrailingYear = TrailingYearPattern.Replace(trimmed, "").TrimEnd();
        if (withoutTrailingYear.Length == 0)
        {
            withoutTrailingYear = trimmed;
        }

        return $"{withoutTrailingYear} {currentYear}";
    }
}
