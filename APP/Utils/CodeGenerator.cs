using DOMAIN.Entities.Configurations;

namespace APP.Utils;

public static class CodeGenerator
{
    public static string GenerateCode(Configuration config, int seriesCounter = 1)
    {
        return config.NamingType switch
        {
            NamingType.Time => $"{config.Prefix}-{GenerateTimeBasedCode()}",
            NamingType.Random => $"{config.Prefix}-{GenerateRandomCode(config)}",
            NamingType.Series => $"{config.Prefix}-{GenerateSeriesCode(seriesCounter)}",
            _ => $"{config.Prefix}-{Guid.NewGuid()}"
        };
    }

    private static string GenerateTimeBasedCode()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
    }

    private static string GenerateRandomCode(Configuration config)
    {
        var maxLength = Math.Max(1, config.MaximumNameLength - (config.Prefix.Length + 1)); // Ensure length is at least 1
        var random = new Random();
    
        return string.Concat(Enumerable.Range(0, maxLength)
            .Select(_ => random.Next(0, 10).ToString()));
    }

    private static string GenerateSeriesCode(int seriesCounter)
    {
        return seriesCounter.ToString();
    }
}
