using DOMAIN.Entities.Configurations;
using INFRASTRUCTURE.Context;

namespace APP.Utils;

public static class CodeGenerator
{
    public static string GenerateCode(ApplicationDbContext context, string modelType, int seriesCounter = 1)
    {
        var config = context.Configurations
            .FirstOrDefault(c => c.ModelType == modelType);

        if (config == null)
        {
            return $"PO-{Guid.NewGuid()}";
        }

        string generatedCode;

        switch (config.NamingType)
        {
            case NamingType.Time:
                generatedCode = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
                break;
            case NamingType.Random:
                var random = new Random();
                generatedCode = random.Next(0, (int)Math.Pow(10, config.MaximumNameLength)).ToString();
                break;
            case NamingType.Series:
                generatedCode = seriesCounter.ToString().PadLeft(config.MinimumNameLength, '0');
                break;
            default:
                return $"PO-{Guid.NewGuid()}";
        }

        if (generatedCode.Length < config.MinimumNameLength)
        {
            generatedCode = generatedCode.PadLeft(config.MinimumNameLength, '0');
        }
        else if (generatedCode.Length > config.MaximumNameLength)
        {
            generatedCode = generatedCode[..config.MaximumNameLength];
        }

        return $"{config.Prefix}{generatedCode}";
    }
}
