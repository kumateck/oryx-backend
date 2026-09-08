using System.Text.Json;
using SHARED;

namespace APP.Services.OnlyOffice;

/// <summary>
/// Parameters used to build a signed ONLYOFFICE editor-open config for one document version.
/// </summary>
public class OnlyOfficeEditorConfigRequest
{
    public Guid DocumentId { get; set; }

    public Guid VersionId { get; set; }

    /// <summary>
    /// Used to seed the document.key so it changes whenever the underlying version changes.
    /// The version's CreatedAt is a good source - versions are immutable once created.
    /// </summary>
    public DateTime VersionCreatedAt { get; set; }

    public string FileName { get; set; }

    public bool Edit { get; set; }

    public bool Review { get; set; }

    public bool Download { get; set; } = true;

    /// <summary>"edit" or "view" - see ONLYOFFICE editorConfig.mode.</summary>
    public string Mode { get; set; }

    public Guid UserId { get; set; }

    public string UserName { get; set; }

    public string Lang { get; set; } = "en";
}

public class OnlyOfficeEditorConfigResult
{
    /// <summary>
    /// The full JSON object ({document, documentType, editorConfig, token}) the frontend
    /// passes straight into `new DocsAPI.DocEditor(...)`.
    /// </summary>
    public Dictionary<string, object> Config { get; set; }

    /// <summary>The browser-reachable ONLYOFFICE Document Server origin, for loading its JS API script.</summary>
    public string DocumentServerUrl { get; set; }
}

public interface IOnlyOfficeConfigService
{
    OnlyOfficeEditorConfigResult BuildEditorConfig(OnlyOfficeEditorConfigRequest request);

    /// <summary>
    /// Verifies a JWT produced by the Document Server (from the Authorization header, or the
    /// body's own "token" field) and returns the trusted payload it signed. Never trust the
    /// callback's raw JSON body fields directly - only what comes back from here.
    /// </summary>
    Result<JsonElement> VerifyCallbackToken(string token);

    Result VerifyFileAccessToken(string token, Guid documentId, Guid versionId);

    Result VerifyCallbackAccessToken(string token, Guid documentId);

    Result ValidateDownloadUrl(string url);
}
