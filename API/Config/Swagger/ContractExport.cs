using Microsoft.OpenApi.Writers;
using Swashbuckle.AspNetCore.Swagger;

namespace API.Config.Swagger;

/// <summary>Exports controller metadata before seeding, listeners or hosted workers start.</summary>
public static class ContractExport
{
    public static bool TryExport(WebApplication app, string[] args)
    {
        if (args.Length != 2 || args[0] != "--export-openapi") return false;
        app.MapControllers();
        using var scope = app.Services.CreateScope();
        var provider = scope.ServiceProvider.GetRequiredService<ISwaggerProvider>();
        var document = provider.GetSwagger("v1");
        using var text = new StringWriter(System.Globalization.CultureInfo.InvariantCulture);
        document.SerializeAsV3(new OpenApiJsonWriter(text));
        File.WriteAllText(Path.GetFullPath(args[1]), text.ToString());
        return true;
    }
}
