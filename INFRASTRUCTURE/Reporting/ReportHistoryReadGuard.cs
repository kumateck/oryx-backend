using System.Data.Common;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace INFRASTRUCTURE.Reporting;

/// <summary>Fail closed if a report starts joining an unversioned table or attempts a write.</summary>
public sealed partial class ReportHistoryReadGuard : DbCommandInterceptor
{
    public static readonly ReportHistoryReadGuard Instance = new();
    [GeneratedRegex("""\b(?:FROM|JOIN)\s+(?:(?:"(?<schema>[^"]+)"|(?<schema>[a-zA-Z_][a-zA-Z0-9_$]*))\s*\.\s*)?(?:"(?<table>[^"]+)"|(?<table>[a-zA-Z_][a-zA-Z0-9_$]*))""", RegexOptions.IgnoreCase)]
    private static partial Regex Tables();
    [GeneratedRegex("\\b(?:INSERT|UPDATE|DELETE|MERGE|TRUNCATE|ALTER|CREATE|DROP|CALL)\\b", RegexOptions.IgnoreCase)]
    private static partial Regex Writes();
    private static void Validate(DbCommand command, DbContext? context)
    {
        if (context is not Context.ApplicationDbContext { ReportAsOfUtc: not null } reporting) return;
        if (Writes().IsMatch(command.CommandText)) throw new ReportHistoryUnavailableException("Historical reporting cannot change business data.");
        var mapped = reporting.Model.GetEntityTypes().Select(type => type.GetTableName()).Where(name => name is not null).ToHashSet(StringComparer.Ordinal);
        foreach (Match match in Tables().Matches(command.CommandText))
        {
            var table = match.Groups["table"].Value;
            if (mapped.Contains(table) && (reporting.ReportHistoryTables?.Contains(table) != true || match.Groups["schema"].Success))
                throw new ReportHistoryUnavailableException("A dependency required by this report has no verified historical snapshot.");
        }
    }
    public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
    { Validate(command, eventData.Context); return result; }
    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
    { Validate(command, eventData.Context); return ValueTask.FromResult(result); }
    public override InterceptionResult<object> ScalarExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<object> result)
    { Validate(command, eventData.Context); return result; }
    public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<object> result, CancellationToken cancellationToken = default)
    { Validate(command, eventData.Context); return ValueTask.FromResult(result); }
    public override InterceptionResult<int> NonQueryExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<int> result)
    { Validate(command, eventData.Context); return result; }
    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    { Validate(command, eventData.Context); return ValueTask.FromResult(result); }
}
