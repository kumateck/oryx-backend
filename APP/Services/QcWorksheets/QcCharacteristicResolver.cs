using DOMAIN.Entities.QcWorksheets;

namespace APP.Services.QcWorksheets;

/// <summary>
/// Picks which <see cref="SpecificationCharacteristic"/> a given worksheet field is judged
/// against for a given Subject.
/// <para>
/// Extracted so the OOS detector (Milestone 4) and the COA engine (Milestone 5) resolve
/// identically. They must: a certificate row that printed a different acceptance criterion from
/// the one the result was actually judged against would be a certificate that contradicts its own
/// investigation record.
/// </para>
/// </summary>
public static class QcCharacteristicResolver
{
    /// <summary>
    /// The same (template, field) pair may appear on several Characteristics differentiated by
    /// <see cref="SpecificationCharacteristic.SamplingPointGroupId"/> — that is how one EM test
    /// carries a different Alert/Action tier per room classification. The Subject's own group
    /// wins; a group-less Characteristic is the fallback. A Subject whose group matches nothing
    /// deliberately falls back rather than failing, because a general limit is still a limit.
    /// </summary>
    public static SpecificationCharacteristic Resolve(
        IReadOnlyCollection<SpecificationCharacteristic> characteristics,
        string fieldKey,
        Guid? samplingPointGroupId)
    {
        var candidates = characteristics
            .Where(item => string.Equals(item.SourceFieldKey, fieldKey, StringComparison.Ordinal))
            .ToList();

        return ResolveAmong(candidates, samplingPointGroupId);
    }

    /// <summary>
    /// The same choice, made over a set of candidates that has already been narrowed to one
    /// (template, field) pair — the shape the COA engine works in, where the Characteristics are
    /// grouped once and each group resolved per Subject.
    /// </summary>
    public static SpecificationCharacteristic ResolveAmong(
        IReadOnlyCollection<SpecificationCharacteristic> candidates,
        Guid? samplingPointGroupId)
    {
        if (candidates is null || candidates.Count == 0)
            return null;

        if (samplingPointGroupId.HasValue)
        {
            var tiered = candidates.FirstOrDefault(
                item => item.SamplingPointGroupId == samplingPointGroupId.Value);

            if (tiered is not null) return tiered;
        }

        return candidates.FirstOrDefault(item => !item.SamplingPointGroupId.HasValue)
            ?? candidates.First();
    }
}
