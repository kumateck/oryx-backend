using SHARED;

namespace DOMAIN.Entities.Configurations;

public class ConfigurationErrors
{
    public static Error ModelTypeNotUnique =>
        Error.Validation("Configurations.ModelType", $"The already exists a configuration for the ModelType provided.");
}