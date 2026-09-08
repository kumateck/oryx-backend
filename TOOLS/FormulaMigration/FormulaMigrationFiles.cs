using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using APP.Services.Formulas;

namespace FormulaMigration;

internal static class FormulaMigrationFiles
{
    private const long JsonByteLimit = FormulaMigrationApplyManifest.ManifestByteLimit;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static async Task<T> ReadJsonAsync<T>(string path,
        CancellationToken cancellationToken)
    {
        var file = new FileInfo(Path.GetFullPath(path));
        if (!file.Exists)
            throw new FileNotFoundException("Migration JSON file was not found.", file.FullName);
        if (file.Length is 0 or > JsonByteLimit)
            throw new InvalidOperationException("Migration JSON file has an invalid size.");
        await using var stream = file.OpenRead();
        return await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Migration JSON file is empty or invalid.");
    }

    public static async Task WriteJsonAsync<T>(string path, T value,
        CancellationToken cancellationToken)
    {
        var fullPath = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await using var stream = new FileStream(fullPath, FileMode.CreateNew,
            FileAccess.Write, FileShare.None);
        await JsonSerializer.SerializeAsync(stream, value, JsonOptions, cancellationToken);
        await stream.FlushAsync(cancellationToken);
    }

    public static string HashSignedReport(string path)
    {
        var file = new FileInfo(Path.GetFullPath(path));
        if (!file.Exists)
            throw new FileNotFoundException("Signed approval report was not found.", file.FullName);
        if (file.Length is 0 or > FormulaMigrationPackage.SignedReportByteLimit)
            throw new InvalidOperationException("Signed approval report has an invalid size.");
        using var stream = file.OpenRead();
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }
}
