namespace APP.Services.OnlyOffice;

public sealed record OnlyOfficeSettings(
    string JwtSecret,
    string PublicUrl,
    string ApiInternalBaseUrl,
    string DocumentServerInternalUrl
)
{
    public static OnlyOfficeSettings Load()
    {
        var publicUrl = Environment.GetEnvironmentVariable("ONLYOFFICE_PUBLIC_URL")
            ?? "http://localhost:8082";
        return new OnlyOfficeSettings(
            Environment.GetEnvironmentVariable("ONLYOFFICE_JWT_SECRET"),
            publicUrl.TrimEnd('/'),
            (Environment.GetEnvironmentVariable("API_INTERNAL_BASE_URL")
                ?? "http://localhost:5270").TrimEnd('/'),
            (Environment.GetEnvironmentVariable("ONLYOFFICE_INTERNAL_URL")
                ?? publicUrl).TrimEnd('/')
        );
    }
}
