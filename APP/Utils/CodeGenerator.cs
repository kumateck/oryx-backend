using DOMAIN.Entities.Configurations;

namespace APP.Utils;

public static class CodeGenerator
{
    public static string GenerateCode(Configuration config, int seriesCounter = 1)
    {
        try
        {
            string generatedCode;
            switch (config.NamingType)
            {
                case NamingType.Time:
                    generatedCode = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
                    break;

                case NamingType.Random:
                    // Ensure the total length includes the prefix and hyphen
                    var prefixLength = config.Prefix.Length + 1; // Adding 1 for the hyphen
                    var minLength = Math.Max(config.MinimumNameLength - prefixLength, 1);
                    var maxLength = Math.Max(config.MaximumNameLength - prefixLength, minLength);

                    if (maxLength < minLength)
                    {
                        return $"{config.Prefix}{Guid.NewGuid()}";
                    }

                    var random = new Random();
                    var randomLength = random.Next(minLength, maxLength + 1); // Determine the random length of the number part
                    var randomNumber = new string(Enumerable.Range(0, randomLength)
                        .Select(_ => random.Next(0, 10).ToString()[0]) // Generate random digits
                        .ToArray());
                    generatedCode = randomNumber;
                    break;

                case NamingType.Series:
                    generatedCode = seriesCounter.ToString().PadLeft(config.MinimumNameLength, '0');
                    break;

                default:
                    return $"{config.Prefix}-{Guid.NewGuid()}";
            }

            // Ensure the generated code respects minimum and maximum length including prefix
            if (generatedCode.Length + config.Prefix.Length + 1 < config.MinimumNameLength)
            {
                generatedCode = generatedCode.PadLeft(config.MinimumNameLength - (config.Prefix.Length + 1), '0');
            }
            else if (generatedCode.Length + config.Prefix.Length + 1 > config.MaximumNameLength)
            {
                generatedCode = generatedCode[..(config.MaximumNameLength - (config.Prefix.Length + 1))];
            }

            return $"{config.Prefix}{generatedCode}";
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return $"{config.Prefix}-{Guid.NewGuid()}";
        }
    }
}
