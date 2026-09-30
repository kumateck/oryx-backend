using System.Text.RegularExpressions;
using DOMAIN.Entities.QcWorksheets;

namespace APP.Services.QcWorksheets.WorksheetDocxImport;

/// <summary>
/// EM worksheets print one limit per tier (the Action limit); the completed EM certificates
/// print Alert and Action per room ("NMT 80 | NMT 100", "NMT 3 | NMT 5"). When a certificate is
/// uploaded with a worksheet, its Alert limit is suggested on the worksheet's tier proposals —
/// cross-checked, never a template source. The certificate is matched by the rooms it shares
/// with the worksheet, not by file name.
/// </summary>
public static partial class EmCoaCrossCheck
{
    public sealed record CoaLimits(string Alert, string Action);

    [GeneratedRegex(@"^\s*NMT\s*(?<number>\d+(?:\.\d+)?)\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex NmtRegex();

    [GeneratedRegex(@"\d+(?:\.\d+)?\s*(?<unit>\S.*)$")]
    private static partial Regex UnitAfterNumberRegex();

    public static void Apply(IReadOnlyList<WorksheetImportProposal> batch)
    {
        var certificates = batch.Where(proposal => proposal.Family == ArdFamily.CompletedCertificate && proposal.Source is not null)
            .Select(proposal => (Proposal: proposal, Rooms: ReadRooms(proposal.Source)))
            .Where(item => item.Rooms.Count > 0)
            .ToList();
        if (certificates.Count == 0)
            return;

        foreach (var worksheet in batch.Where(proposal => proposal.Family == ArdFamily.EnvironmentalMonitoring))
        {
            var codes = worksheet.SamplingPointProposals.Select(point => ImportText.Canonical(point.Code)).ToHashSet();
            var (coa, rooms) = certificates.OrderByDescending(item => item.Rooms.Keys.Count(codes.Contains)).First();
            if (!rooms.Keys.Any(codes.Contains))
                continue;

            foreach (var proposal in worksheet.SpecificationProposals.Where(item => item.GroupName is not null))
                Suggest(worksheet, proposal, coa, rooms);
        }
    }

    private static void Suggest(WorksheetImportProposal worksheet, SpecificationCharacteristicProposal proposal,
        WorksheetImportProposal coa, IReadOnlyDictionary<string, CoaLimits> rooms)
    {
        var group = worksheet.SamplingPointGroupProposals.FirstOrDefault(item => item.Name == proposal.GroupName);
        var limits = (group?.PointCodes ?? [])
            .Select(code => rooms.GetValueOrDefault(ImportText.Canonical(code)))
            .Where(item => item is not null)
            .ToList();
        if (limits.Count == 0)
            return;

        var action = Number(proposal.AcceptanceCriteria);
        var disagreeing = limits.Where(item => Number(item.Action) != action).Select(item => item.Action).Distinct().ToList();
        if (disagreeing.Count > 0)
        {
            worksheet.Flags.Add(new WorksheetImportFlag
            {
                Code = WorksheetImportFlagCodes.UnrecognizedContent,
                Message = $"'{coa.FileName}' prints Action {string.Join(" / ", disagreeing)} for some '{proposal.GroupName}' points; "
                          + $"the worksheet prints {proposal.AcceptanceCriteria}. No Alert limit suggested."
            });
            return;
        }

        var alerts = limits.Select(item => item.Alert).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (alerts.Count != 1)
            return;

        var unit = UnitAfterNumberRegex().Match(proposal.AcceptanceCriteria ?? string.Empty) is { Success: true } match
            ? " " + match.Groups["unit"].Value.Trim()
            : string.Empty;
        proposal.AlertLimit = alerts[0] + unit;
        proposal.AlertLimitSource = $"from COA '{coa.FileName}' ({limits.Count} of {group!.PointCodes.Count} points)";
    }

    /// <summary>Room code → (Alert, Action) from the certificate's result tables.</summary>
    public static IReadOnlyDictionary<string, CoaLimits> ReadRooms(ImportSourceDocument source)
    {
        var rooms = new Dictionary<string, CoaLimits>();
        foreach (var row in source.Blocks.Where(block => block.Rows is not null).SelectMany(block => block.Rows))
        {
            // Spanned cells repeat their text across the grid; consecutive repeats are one cell.
            var limits = row.Skip(2).Where(cell => NmtRegex().IsMatch(cell)).ToList();
            var collapsed = limits.Where((cell, index) => index == 0 || !string.Equals(cell, limits[index - 1], StringComparison.OrdinalIgnoreCase)).ToList();
            if (collapsed.Count < 2 || row.Count == 0 || ImportText.IsBlank(row[0]))
                continue;
            rooms.TryAdd(ImportText.Canonical(row[0]), new CoaLimits(ImportText.Normalize(collapsed[0]), ImportText.Normalize(collapsed[1])));
        }

        return rooms;
    }

    private static double? Number(string limit)
    {
        var match = Regex.Match(limit ?? string.Empty, @"\d+(?:\.\d+)?");
        return match.Success ? double.Parse(match.Value, System.Globalization.CultureInfo.InvariantCulture) : null;
    }
}
