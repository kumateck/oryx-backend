using APP.Services.OnlyOffice;
using Xunit;

namespace APP.Tests.Repository;

public sealed class OnlyOfficeConfigServiceTests
{
    private static readonly OnlyOfficeSettings Settings = new(
        new string('s', 32),
        "https://office.example.test",
        "https://api.internal.test",
        "https://office.internal.test"
    );

    [Fact]
    public void Editor_file_url_uses_a_bound_short_lived_token()
    {
        var service = new OnlyOfficeConfigService(Settings);
        var documentId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var result = service.BuildEditorConfig(Request(documentId, versionId));
        var document = Assert.IsType<Dictionary<string, object>>(result.Config["document"]);
        var url = new Uri(Assert.IsType<string>(document["url"]));
        var token = Uri.UnescapeDataString(url.Query.Split("accessToken=")[1]);

        Assert.True(service.VerifyFileAccessToken(token, documentId, versionId).IsSuccess);
        Assert.True(service.VerifyFileAccessToken(token, documentId, Guid.NewGuid()).IsFailure);

        var editor = Assert.IsType<Dictionary<string, object>>(result.Config["editorConfig"]);
        Assert.IsType<Dictionary<string, object>>(editor["customization"]);
        var user = Assert.IsType<Dictionary<string, object>>(editor["user"]);
        Assert.False(string.IsNullOrWhiteSpace(Assert.IsType<string>(user["id"])));
        var configToken = Assert.IsType<string>(result.Config["token"]);
        var signedConfig = service.VerifyCallbackToken(configToken);
        Assert.True(signedConfig.IsSuccess);
        Assert.Equal(
            System.Text.Json.JsonValueKind.Object,
            signedConfig.Value.GetProperty("editorConfig").GetProperty("customization").ValueKind
        );
        var callbackUrl = new Uri(Assert.IsType<string>(editor["callbackUrl"]));
        var callbackToken = Uri.UnescapeDataString(callbackUrl.Query.Split("accessToken=")[1]);
        Assert.True(service.VerifyCallbackAccessToken(callbackToken, documentId).IsSuccess);
        Assert.True(service.VerifyCallbackAccessToken(callbackToken, Guid.NewGuid()).IsFailure);
    }

    [Fact]
    public void Callback_download_is_restricted_to_the_configured_origin()
    {
        var service = new OnlyOfficeConfigService(Settings);

        Assert.True(service.ValidateDownloadUrl("https://office.internal.test/cache/file.docx").IsSuccess);
        Assert.True(service.ValidateDownloadUrl("http://169.254.169.254/latest/meta-data").IsFailure);
        Assert.True(service.ValidateDownloadUrl("https://office.internal.test.evil.test/file").IsFailure);
    }

    private static OnlyOfficeEditorConfigRequest Request(Guid documentId, Guid versionId) => new()
    {
        DocumentId = documentId,
        VersionId = versionId,
        VersionCreatedAt = DateTime.UtcNow,
        FileName = "SOP.docx",
        Edit = true,
        Mode = "edit",
        UserId = Guid.NewGuid(),
        UserName = "Test User"
    };
}
