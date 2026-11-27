using System.Globalization;
using DOMAIN.Entities.Forms;
using SHARED;

#nullable enable
namespace APP.Services.Forms.Microbial;

/// <summary>Checks, prints and evaluates typed microbial limits. Pure: no database access.</summary>
public static class MicrobialLimits
{
    public enum Verdict { Complies, DoesNotComply, Unresolved }

    public static Result Validate(MicrobialLimitRequest limit, QuestionType fieldType, string? fieldUnit)
    {
        switch (limit.Kind)
        {
            case MicrobialLimitKind.NotMoreThan:
                if (fieldType != QuestionType.CfuCalculation) return Invalid("A count limit goes on a CFU calculation field.");
                if (limit.Value is not >= 0) return Invalid("Enter the count limit as a number of zero or more.");
                if (!MicrobialUnits.Cfu.Contains(limit.Unit ?? "")) return Invalid("Choose the unit of the count limit.");
                if (fieldUnit is not null && limit.Unit != fieldUnit) return Invalid($"The field reports in {fieldUnit}; set the limit in the same unit.");
                return Result.Success();
            case MicrobialLimitKind.Absent:
                if (fieldType != QuestionType.GrowthObservation) return Invalid("An absence limit goes on a growth observation field.");
                if (!MicrobialOrganisms.IsKnown(limit.Organism ?? "")) return Invalid("Choose the organism that must be absent.");
                if (limit.Quantity is not > 0) return Invalid("Enter the quantity tested, greater than zero.");
                if (!MicrobialUnits.Quantity.Contains(limit.QuantityUnit ?? "")) return Invalid("Choose g or mL for the quantity tested.");
                return Result.Success();
            default:
                return Invalid("Choose the kind of limit.");
        }
    }

    private static Result Invalid(string message) => Error.Validation("MicrobialLimit.Invalid", message);

    public static string Text(MicrobialLimitKind kind, decimal? value, string? unit, string? organism, decimal? quantity, string? quantityUnit) => kind switch
    {
        MicrobialLimitKind.NotMoreThan => $"NMT {Format(value)} {unit}",
        MicrobialLimitKind.Absent => $"Absence of {MicrobialOrganisms.Names.GetValueOrDefault(organism ?? "", organism ?? "the organism")} in {Format(quantity)} {quantityUnit}",
        _ => "",
    };

    public static string Text(SpecificationMicrobialLimit limit) =>
        Text(limit.Kind, limit.Value, limit.Unit, limit.Organism, limit.Quantity, limit.QuantityUnit);

    /// <summary>The verdict for a saved answer. A missing or unreadable result is Unresolved, never a pass.</summary>
    public static Verdict Evaluate(SpecificationMicrobialLimit limit, string? answer)
    {
        if (limit.Kind == MicrobialLimitKind.Absent)
            return answer?.Trim() switch { "Absent" => Verdict.Complies, "Present" => Verdict.DoesNotComply, _ => Verdict.Unresolved };
        var cfu = MicrobialValues.ReadCfu(answer);
        if (cfu is null) return Verdict.Unresolved;
        if (cfu.Unit != limit.Unit) return Verdict.Unresolved;
        if (cfu.TooNumerousToCount) return Verdict.DoesNotComply;
        return cfu.Result <= limit.Value ? Verdict.Complies : Verdict.DoesNotComply;
    }

    private static string Format(decimal? number) => number?.ToString("0.############", CultureInfo.InvariantCulture) ?? "";
}
