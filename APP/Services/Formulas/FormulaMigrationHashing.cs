using System.Security.Cryptography;
using System.Text;

#nullable enable

namespace APP.Services.Formulas;

internal static class FormulaMigrationHashing
{
    private static readonly Guid FormulaNamespace =
        new("d5599567-13e4-5b66-b6ee-8eb8709e4202");

    public static string Sha256(string value) => Convert
        .ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value ?? string.Empty)))
        .ToLowerInvariant();

    public static bool IsSha256(string? value) => value?.Length == 64 &&
        value.All(character => character is >= '0' and <= '9' or >= 'a' and <= 'f');

    public static string FormatTimestamp(DateTime? value) => value?.ToUniversalTime()
        .ToString("O", System.Globalization.CultureInfo.InvariantCulture) ?? "-";

    public static Guid DeterministicId(string releaseId, string entityKind, string sourceIdentity)
    {
        var releaseNamespace = CreateV5(FormulaNamespace, releaseId);
        return CreateV5(releaseNamespace, $"{entityKind}:{sourceIdentity}");
    }

    private static Guid CreateV5(Guid namespaceId, string name)
    {
        var namespaceBytes = namespaceId.ToByteArray();
        SwapGuidByteOrder(namespaceBytes);
        var nameBytes = Encoding.UTF8.GetBytes(name);
        var input = new byte[namespaceBytes.Length + nameBytes.Length];
        Buffer.BlockCopy(namespaceBytes, 0, input, 0, namespaceBytes.Length);
        Buffer.BlockCopy(nameBytes, 0, input, namespaceBytes.Length, nameBytes.Length);
        var hash = SHA1.HashData(input);
        hash[6] = (byte)((hash[6] & 0x0f) | 0x50);
        hash[8] = (byte)((hash[8] & 0x3f) | 0x80);
        var result = hash[..16];
        SwapGuidByteOrder(result);
        return new Guid(result);
    }

    private static void SwapGuidByteOrder(byte[] value)
    {
        (value[0], value[3]) = (value[3], value[0]);
        (value[1], value[2]) = (value[2], value[1]);
        (value[4], value[5]) = (value[5], value[4]);
        (value[6], value[7]) = (value[7], value[6]);
    }
}
