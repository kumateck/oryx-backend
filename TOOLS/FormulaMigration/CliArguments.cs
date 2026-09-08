namespace FormulaMigration;

internal sealed class CliArguments
{
    private readonly Dictionary<string, string> values;

    private CliArguments(string command, Dictionary<string, string> values)
    {
        Command = command;
        this.values = values;
    }

    public string Command { get; }

    public static CliArguments Parse(string[] args)
    {
        if (args.Length == 0)
            throw new ArgumentException(Usage.Text);
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var index = 1; index < args.Length; index += 2)
        {
            if (!args[index].StartsWith("--", StringComparison.Ordinal) ||
                index + 1 >= args.Length)
                throw new ArgumentException($"Invalid option near '{args[index]}'.\n{Usage.Text}");
            var name = args[index][2..];
            if (!values.TryAdd(name, args[index + 1]))
                throw new ArgumentException($"Option '--{name}' was supplied more than once.");
        }
        return new CliArguments(args[0], values);
    }

    public string Require(string name) =>
        values.TryGetValue(name, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : throw new ArgumentException($"Option '--{name}' is required.");

    public string? Optional(string name) => values.GetValueOrDefault(name);

    public Guid RequireGuid(string name) => Guid.TryParse(Require(name), out var value)
        ? value
        : throw new ArgumentException($"Option '--{name}' must be a GUID.");

    public void EnsureOnly(params string[] allowed)
    {
        var allowedNames = allowed.ToHashSet(StringComparer.Ordinal);
        var unknown = values.Keys.Where(name => !allowedNames.Contains(name)).Order().ToList();
        if (unknown.Count > 0)
            throw new ArgumentException(
                $"Unknown option(s): {string.Join(", ", unknown.Select(name => $"--{name}"))}.");
    }
}

internal static class Usage
{
    public const string Text = """
        Usage:
          formula-migration seal --draft FILE --signed-report FILE --report-location REF --output FILE
          formula-migration validate --package FILE [--signed-report FILE]
          formula-migration dry-run --request FILE --expect-database NAME --expect-server HOST:PORT --output FILE
          formula-migration record-dry-run --request FILE --expect-database NAME --expect-server HOST:PORT --confirm TOKEN
          formula-migration apply --package FILE --signed-report FILE --expect-database NAME --expect-server HOST:PORT --confirm TOKEN

        Database commands read ORYX_FORMULA_DB_CONNECTION from the environment.
        Write commands also require ORYX_FORMULA_OPERATOR_TOKEN, ORYX_FORMULA_JWT_KEY,
        and ORYX_FORMULA_ENVIRONMENT. The token supplies the importer identity.
        """;
}
