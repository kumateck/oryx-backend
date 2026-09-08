using APP.Services.Formulas;

namespace FormulaMigration;

internal static class FormulaMigrationCommand
{
    public static async Task<int> RunAsync(string[] args,
        CancellationToken cancellationToken = default)
    {
        var input = CliArguments.Parse(args);
        switch (input.Command)
        {
            case "seal":
                await SealAsync(input, cancellationToken);
                break;
            case "validate":
                await ValidateAsync(input, cancellationToken);
                break;
            case "dry-run":
                await DryRunAsync(input, cancellationToken);
                break;
            case "record-dry-run":
                await RecordDryRunAsync(input, cancellationToken);
                break;
            case "apply":
                await ApplyAsync(input, cancellationToken);
                break;
            default:
                throw new ArgumentException($"Unknown command '{input.Command}'.\n{Usage.Text}");
        }
        return 0;
    }

    private static async Task SealAsync(CliArguments input,
        CancellationToken cancellationToken)
    {
        input.EnsureOnly("draft", "signed-report", "report-location", "output");
        var draft = await FormulaMigrationFiles.ReadJsonAsync<FormulaMigrationApplyDraft>(
            input.Require("draft"), cancellationToken);
        var reportHash = FormulaMigrationFiles.HashSignedReport(
            input.Require("signed-report"));
        var package = FormulaMigrationPackage.Seal(
            draft, input.Require("report-location"), reportHash);
        await FormulaMigrationFiles.WriteJsonAsync(
            input.Require("output"), package, cancellationToken);
        Console.WriteLine($"Sealed manifest {package.ManifestHash}.");
        Console.WriteLine($"Apply confirmation token: APPLY:{package.ManifestHash}");
    }

    private static async Task ValidateAsync(CliArguments input,
        CancellationToken cancellationToken)
    {
        input.EnsureOnly("package", "signed-report");
        var package = await FormulaMigrationFiles.ReadJsonAsync<FormulaMigrationApplyRequest>(
            input.Require("package"), cancellationToken);
        var reportPath = input.Optional("signed-report");
        var observedHash = reportPath is null
            ? null
            : FormulaMigrationFiles.HashSignedReport(reportPath);
        var receipt = FormulaMigrationPackage.ValidateOffline(package, observedHash);
        Console.WriteLine($"Valid manifest {receipt.ManifestHash}: " +
            $"{receipt.TargetCount} targets, {receipt.EvidenceCount} evidence records, " +
            $"{receipt.KeyMappingCount} key mappings.");
    }

    private static async Task DryRunAsync(CliArguments input,
        CancellationToken cancellationToken)
    {
        input.EnsureOnly("request", "expect-database", "expect-server", "output");
        var request = await FormulaMigrationFiles.ReadJsonAsync<FormulaMigrationDryRunRequest>(
            input.Require("request"), cancellationToken);
        await using var context = await FormulaMigrationDatabase.OpenAsync(null,
            input.Require("expect-database"), input.Require("expect-server"),
            cancellationToken);
        var report = await new FormulaMigrationInventoryService(context)
            .DryRunAsync(request, cancellationToken);
        await FormulaMigrationFiles.WriteJsonAsync(
            input.Require("output"), report, cancellationToken);
        Console.WriteLine($"Dry run CanApply={report.CanApply}; " +
            $"source fingerprint {report.SourceFingerprint}.");
        Console.WriteLine($"Record confirmation token: " +
            $"RECORD:{report.ReleaseId}:{report.SourceFingerprint}");
    }

    private static async Task RecordDryRunAsync(CliArguments input,
        CancellationToken cancellationToken)
    {
        input.EnsureOnly("request", "expect-database", "expect-server", "confirm");
        var request = await FormulaMigrationFiles.ReadJsonAsync<FormulaMigrationDryRunRequest>(
            input.Require("request"), cancellationToken);
        var actorId = FormulaMigrationOperatorIdentity.Authenticate();
        await using var context = await FormulaMigrationDatabase.OpenAsync(actorId,
            input.Require("expect-database"), input.Require("expect-server"),
            cancellationToken);
        await FormulaMigrationDatabase.RequireAuthorizedOperatorAsync(
            context, actorId, cancellationToken);
        var inventory = new FormulaMigrationInventoryService(context);
        var preview = await inventory.DryRunAsync(request, cancellationToken);
        if (!preview.CanApply)
            throw new InvalidOperationException(
                "Dry run is not apply-ready; no migration evidence was recorded.");
        RequireConfirmation(input,
            $"RECORD:{preview.ReleaseId}:{preview.SourceFingerprint}");
        var service = new FormulaMigrationEvidenceService(
            context, inventory, new OperatorCurrentUser(actorId));
        var receipt = await service.RecordDryRunAsync(request, cancellationToken);
        Console.WriteLine($"Recorded dry-run {receipt.RunId:N}; " +
            $"alreadyRecorded={receipt.AlreadyRecorded}.");
    }

    private static async Task ApplyAsync(CliArguments input,
        CancellationToken cancellationToken)
    {
        input.EnsureOnly("package", "signed-report", "expect-database",
            "expect-server", "confirm");
        var package = await FormulaMigrationFiles.ReadJsonAsync<FormulaMigrationApplyRequest>(
            input.Require("package"), cancellationToken);
        FormulaMigrationPackage.ValidateOffline(package,
            FormulaMigrationFiles.HashSignedReport(input.Require("signed-report")));
        RequireConfirmation(input, $"APPLY:{package.ManifestHash}");
        var actorId = FormulaMigrationOperatorIdentity.Authenticate();
        await using var context = await FormulaMigrationDatabase.OpenAsync(actorId,
            input.Require("expect-database"), input.Require("expect-server"),
            cancellationToken);
        await FormulaMigrationDatabase.RequireAuthorizedOperatorAsync(
            context, actorId, cancellationToken);
        var inventory = new FormulaMigrationInventoryService(context);
        var service = new FormulaMigrationApplyService(
            context, inventory, new OperatorCurrentUser(actorId));
        var receipt = await service.ApplyApprovedDefinitionsAsync(package, cancellationToken);
        Console.WriteLine($"Apply run {receipt.RunId:N}; alreadyApplied={receipt.AlreadyApplied}; " +
            $"definitions={receipt.DefinitionsCreated}; revisions={receipt.RevisionsCreated}; " +
            $"questionLinks={receipt.QuestionLinksCreated}; keyMappings={receipt.KeyMappingsCreated}.");
    }

    private static void RequireConfirmation(CliArguments input, string expected)
    {
        if (!string.Equals(input.Require("confirm"), expected, StringComparison.Ordinal))
            throw new InvalidOperationException(
                $"Confirmation token mismatch. Expected exactly: {expected}");
    }
}
