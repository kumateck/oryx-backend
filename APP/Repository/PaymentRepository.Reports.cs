using DOMAIN.Entities.Payments;
using SHARED;

namespace APP.Repository;

public partial class PaymentRepository
{
    public async Task<Result<AgingReportDto>> GetApAging(DateTime? asOf = null)
    {
        var date = (asOf ?? DateTime.UtcNow).Date;
        var raw = await GetApLines(date);
        return await BuildAging(raw.Lines, raw.Warnings, date);
    }

    public async Task<Result<AgingReportDto>> GetArAging(DateTime? asOf = null)
    {
        var date = (asOf ?? DateTime.UtcNow).Date;
        var raw = await GetArLines(date);
        return await BuildAging(raw.Lines, raw.Warnings, date);
    }

    public async Task<Result<CashflowSummaryDto>> GetCashflowSummary(DateTime? asOf = null)
    {
        var date = (asOf ?? DateTime.UtcNow).Date;
        var baseCurrency = await GetBaseCurrency();
        if (baseCurrency is null)
            return Error.Validation("Currency.BaseMissing", "Configure a base currency before running cashflow reports.");

        var ap = await GetApLines(date);
        var ar = await GetArLines(date);
        var warnings = ap.Warnings.Concat(ar.Warnings).Distinct().ToList();
        var rateCache = new Dictionary<Guid, decimal> { [baseCurrency.Id] = 1m };

        async Task<CashflowDirectionDto> BuildDirection(IEnumerable<RawLine> lines)
        {
            var result = new CashflowDirectionDto();
            foreach (var line in lines.Where(item => item.Outstanding > 0))
            {
                if (!rateCache.TryGetValue(line.CurrencyId, out var rate))
                {
                    var resolved = await ResolveRate(line.CurrencyId, date);
                    if (!resolved.IsSuccess)
                    {
                        warnings.Add(resolved.Error.Description);
                        continue;
                    }
                    rate = resolved.Value.RateToBase;
                    rateCache[line.CurrencyId] = rate;
                }

                var amount = line.Outstanding * rate;
                result.TotalOutstandingBase += amount;
                if (!line.DueDate.HasValue) result.DueDateUnknown += amount;
                else if (line.DueDate.Value.Date < date) result.Overdue += amount;
                else
                {
                    var days = (line.DueDate.Value.Date - date).Days;
                    if (days <= 7) result.Next7Days += amount;
                    else if (days <= 30) result.Days8To30 += amount;
                    else if (days <= 60) result.Days31To60 += amount;
                    else if (days <= 90) result.Days61To90 += amount;
                    else result.Beyond90Days += amount;
                }
            }
            return result;
        }

        return new CashflowSummaryDto
        {
            BaseCurrencyId = baseCurrency.Id,
            BaseCurrencyName = baseCurrency.Name,
            ProjectedOutflows = await BuildDirection(ap.Lines),
            ProjectedInflows = await BuildDirection(ar.Lines),
            DataQualityWarnings = warnings.Distinct().ToList(),
        };
    }

    private async Task<Result<AgingReportDto>> BuildAging(
        List<RawLine> rawLines,
        List<string> warnings,
        DateTime asOf
    )
    {
        var baseCurrency = await GetBaseCurrency();
        if (baseCurrency is null)
            return Error.Validation("Currency.BaseMissing", "Configure a base currency before running aging reports.");

        var rateCache = new Dictionary<Guid, decimal> { [baseCurrency.Id] = 1m };
        var converted = new List<(RawLine Raw, AgingLineDto Line)>();
        foreach (var raw in rawLines.Where(item => item.Outstanding > 0))
        {
            if (!rateCache.TryGetValue(raw.CurrencyId, out var rate))
            {
                var resolved = await ResolveRate(raw.CurrencyId, asOf);
                if (!resolved.IsSuccess)
                {
                    warnings.Add(resolved.Error.Description);
                    rate = 0m;
                }
                else rate = resolved.Value.RateToBase;
                rateCache[raw.CurrencyId] = rate;
            }

            converted.Add((raw, new AgingLineDto
            {
                PayableType = raw.PayableType,
                PayableId = raw.PayableId,
                DocumentCode = raw.DocumentCode,
                DueDate = raw.DueDate,
                CurrencyId = raw.CurrencyId,
                CurrencyName = raw.CurrencyName,
                OriginalOutstanding = raw.Outstanding,
                RateToBase = rate,
                OutstandingBase = raw.Outstanding * rate,
                Bucket = GetAgingBucket(raw.DueDate, asOf),
            }));
        }

        var report = new AgingReportDto
        {
            BaseCurrencyId = baseCurrency.Id,
            BaseCurrencyName = baseCurrency.Name,
            DataQualityWarnings = warnings.Distinct().ToList(),
        };
        report.Parties = converted
            .GroupBy(item => new { item.Raw.PartyId, item.Raw.PartyName })
            .Select(group =>
            {
                var party = new AgingPartyDto
                {
                    PartyId = group.Key.PartyId,
                    PartyName = group.Key.PartyName,
                    Lines = group.Select(item => item.Line).ToList(),
                    TotalOutstandingBase = group.Sum(item => item.Line.OutstandingBase),
                };
                foreach (var line in party.Lines)
                    AddBucket(party.BucketsBase, line.Bucket, line.OutstandingBase);
                return party;
            })
            .OrderBy(item => item.PartyName)
            .ToList();
        report.TotalOutstandingBase = report.Parties.Sum(item => item.TotalOutstandingBase);
        return report;
    }

    internal static AgingBucket GetAgingBucket(DateTime? dueDate, DateTime asOf)
    {
        if (!dueDate.HasValue) return AgingBucket.DueDateUnknown;
        var overdueDays = (asOf.Date - dueDate.Value.Date).Days;
        if (overdueDays < 0) return AgingBucket.Current;
        if (overdueDays <= 30) return AgingBucket.Days0To30;
        if (overdueDays <= 60) return AgingBucket.Days31To60;
        if (overdueDays <= 90) return AgingBucket.Days61To90;
        return AgingBucket.DaysOver90;
    }

    private static void AddBucket(AgingBucketsDto buckets, AgingBucket bucket, decimal amount)
    {
        switch (bucket)
        {
            case AgingBucket.Current: buckets.Current += amount; break;
            case AgingBucket.Days0To30: buckets.Days0To30 += amount; break;
            case AgingBucket.Days31To60: buckets.Days31To60 += amount; break;
            case AgingBucket.Days61To90: buckets.Days61To90 += amount; break;
            case AgingBucket.DaysOver90: buckets.DaysOver90 += amount; break;
            case AgingBucket.DueDateUnknown: buckets.DueDateUnknown += amount; break;
        }
    }

}
