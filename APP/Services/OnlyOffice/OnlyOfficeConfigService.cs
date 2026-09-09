using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DOMAIN.Entities.StpDocuments;
using Microsoft.IdentityModel.Tokens;
using SHARED;

namespace APP.Services.OnlyOffice;

/// <summary>
/// Builds + JWT-signs the ONLYOFFICE editor-open config, and verifies the JWT ONLYOFFICE sends
/// back on its save callback. Reads config from env vars at construction time, mirroring
/// BlobStorageService's MINIO_* pattern.
/// </summary>
public class OnlyOfficeConfigService(OnlyOfficeSettings settings) : IOnlyOfficeConfigService
{
    private readonly string _jwtSecret = settings.JwtSecret;
    private readonly string _publicUrl = settings.PublicUrl;
    private readonly string _apiInternalBaseUrl = settings.ApiInternalBaseUrl;
    private readonly string _documentServerInternalUrl = settings.DocumentServerInternalUrl;

    private const string DocumentType = "word";
    private const string FileType = "docx";

    public OnlyOfficeEditorConfigResult BuildEditorConfig(OnlyOfficeEditorConfigRequest request)
    {
        var fileToken = SignPayload(
            new Dictionary<string, object>
            {
                ["purpose"] = "stp-file",
                ["documentId"] = request.DocumentId.ToString(),
                ["versionId"] = request.VersionId.ToString(),
                ["exp"] = DateTimeOffset.UtcNow.AddMinutes(15).ToUnixTimeSeconds()
            }
        );
        var callbackToken = SignPayload(
            new Dictionary<string, object>
            {
                ["purpose"] = "stp-callback",
                ["documentId"] = request.DocumentId.ToString(),
                ["exp"] = DateTimeOffset.UtcNow.AddHours(12).ToUnixTimeSeconds()
            }
        );
        var document = new Dictionary<string, object>
        {
            ["fileType"] = FileType,
            ["key"] = BuildDocumentKey(request.VersionId, request.VersionCreatedAt),
            ["title"] = string.IsNullOrWhiteSpace(request.FileName) ? "Document.docx" : request.FileName,
            ["url"] =
                $"{_apiInternalBaseUrl}/api/v1/stp-documents/documents/{request.DocumentId}/versions/{request.VersionId}/editor-file?accessToken={Uri.EscapeDataString(fileToken)}",
            ["permissions"] = new Dictionary<string, object>
            {
                ["edit"] = request.Edit,
                ["review"] = request.Review,
                ["download"] = request.Download,
                ["print"] = request.Download,
                ["copy"] = true,
                ["comment"] = request.Edit || request.Review,
                ["fillForms"] = request.Edit,
                ["modifyContentControl"] = request.Edit,
                ["modifyFilter"] = request.Edit
            }
        };

        var editorConfig = new Dictionary<string, object>
        {
            ["callbackUrl"] =
                $"{_apiInternalBaseUrl}/api/v1/stp-documents/documents/{request.DocumentId}/callback?accessToken={Uri.EscapeDataString(callbackToken)}",
            ["mode"] = string.IsNullOrWhiteSpace(request.Mode) ? "view" : request.Mode,
            ["lang"] = string.IsNullOrWhiteSpace(request.Lang) ? "en" : request.Lang,
            ["user"] = new Dictionary<string, object>
            {
                ["id"] = request.UserId.ToString(),
                ["name"] = string.IsNullOrWhiteSpace(request.UserName) ? request.UserId.ToString() : request.UserName
            },
            // ONLYOFFICE Docs 9.4 dereferences customization while bootstrapping even
            // though the public API documents this block as optional. Sending an empty
            // object keeps the config valid across server versions and prevents the
            // editor from stalling before onDocumentReady.
            ["customization"] = new Dictionary<string, object>()
        };

        var payload = new Dictionary<string, object>
        {
            ["document"] = document,
            ["documentType"] = DocumentType,
            ["editorConfig"] = editorConfig
        };

        var config = new Dictionary<string, object>(payload) { ["token"] = SignPayload(payload) };

        return new OnlyOfficeEditorConfigResult { Config = config, DocumentServerUrl = _publicUrl };
    }

    public Result<JsonElement> VerifyCallbackToken(string token)
    {
        if (string.IsNullOrWhiteSpace(_jwtSecret))
            return StpDocumentErrors.OnlyOfficeNotConfigured;

        if (string.IsNullOrWhiteSpace(token))
            return StpDocumentErrors.CallbackUnauthorized;

        var parts = token.Split('.');
        if (parts.Length != 3)
            return StpDocumentErrors.CallbackUnauthorized;

        try
        {
            var signingInput = Encoding.ASCII.GetBytes($"{parts[0]}.{parts[1]}");
            var signature = Base64UrlDecode(parts[2]);

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_jwtSecret));
            var computedSignature = hmac.ComputeHash(signingInput);

            if (!CryptographicOperations.FixedTimeEquals(computedSignature, signature))
                return StpDocumentErrors.CallbackUnauthorized;

            var payloadJson = Encoding.UTF8.GetString(Base64UrlDecode(parts[1]));
            using var payloadDocument = JsonDocument.Parse(payloadJson);
            var root = payloadDocument.RootElement;

            // ONLYOFFICE wraps the actual callback body under a "payload" claim when the token
            // travels in-body (the default since Document Server 5.2+); header tokens may carry
            // the body's fields directly at the top level depending on version - handle both.
            return root.TryGetProperty("payload", out var inner) ? inner.Clone() : root.Clone();
        }
        catch (Exception)
        {
            return StpDocumentErrors.CallbackUnauthorized;
        }
    }

    public Result VerifyFileAccessToken(string token, Guid documentId, Guid versionId)
    {
        var verification = VerifyCallbackToken(token);
        if (verification.IsFailure)
            return Result.Failure(StpDocumentErrors.InvalidDocumentDownload);

        var payload = verification.Value;
        var validPurpose = payload.TryGetProperty("purpose", out var purpose)
            && purpose.GetString() == "stp-file";
        var validDocument = payload.TryGetProperty("documentId", out var document)
            && Guid.TryParse(document.GetString(), out var tokenDocumentId)
            && tokenDocumentId == documentId;
        var validVersion = payload.TryGetProperty("versionId", out var version)
            && Guid.TryParse(version.GetString(), out var tokenVersionId)
            && tokenVersionId == versionId;
        var validExpiry = payload.TryGetProperty("exp", out var expiry)
            && expiry.TryGetInt64(out var expiresAt)
            && expiresAt >= DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        return validPurpose && validDocument && validVersion && validExpiry
            ? Result.Success()
            : Result.Failure(StpDocumentErrors.InvalidDocumentDownload);
    }

    public Result VerifyCallbackAccessToken(string token, Guid documentId)
    {
        var verification = VerifyCallbackToken(token);
        if (verification.IsFailure)
            return Result.Failure(StpDocumentErrors.CallbackUnauthorized);
        var payload = verification.Value;
        var validPurpose = payload.TryGetProperty("purpose", out var purpose)
            && purpose.GetString() == "stp-callback";
        var validDocument = payload.TryGetProperty("documentId", out var document)
            && Guid.TryParse(document.GetString(), out var tokenDocumentId)
            && tokenDocumentId == documentId;
        var validExpiry = payload.TryGetProperty("exp", out var expiry)
            && expiry.TryGetInt64(out var expiresAt)
            && expiresAt >= DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        return validPurpose && validDocument && validExpiry
            ? Result.Success()
            : Result.Failure(StpDocumentErrors.CallbackUnauthorized);
    }

    public Result ValidateDownloadUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var candidate)
            || !Uri.TryCreate(_documentServerInternalUrl, UriKind.Absolute, out var trusted))
            return Result.Failure(StpDocumentErrors.InvalidEditorDownloadUrl);

        var trustedOrigin = (trusted.Scheme, trusted.Host, trusted.Port);
        var candidateOrigin = (candidate.Scheme, candidate.Host, candidate.Port);
        return candidateOrigin == trustedOrigin
            ? Result.Success()
            : Result.Failure(StpDocumentErrors.InvalidEditorDownloadUrl);
    }

    private string SignPayload(Dictionary<string, object> payload)
    {
        if (string.IsNullOrWhiteSpace(_jwtSecret))
            throw new InvalidOperationException(
                "ONLYOFFICE_JWT_SECRET is not configured; cannot sign editor config");

        var tokenHandler = new JwtSecurityTokenHandler();
        var keyBytes = Encoding.UTF8.GetBytes(_jwtSecret);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Claims = payload,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    /// <summary>
    /// document.key must be 0-9/a-z/A-Z/-._= only and at most 128 characters, and must change
    /// whenever the underlying document changes. Versions are immutable once created, so the
    /// version id plus its creation timestamp ticks satisfies both.
    /// </summary>
    private static string BuildDocumentKey(Guid versionId, DateTime versionCreatedAt)
    {
        var key = $"{versionId:N}-{versionCreatedAt.Ticks}";
        return key.Length > 128 ? key[..128] : key;
    }

    private static byte[] Base64UrlDecode(string input)
    {
        var padded = input.Replace('-', '+').Replace('_', '/');
        switch (padded.Length % 4)
        {
            case 2:
                padded += "==";
                break;
            case 3:
                padded += "=";
                break;
        }
        return Convert.FromBase64String(padded);
    }
}
