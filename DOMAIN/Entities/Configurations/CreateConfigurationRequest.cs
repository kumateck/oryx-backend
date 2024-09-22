namespace DOMAIN.Entities.Configurations;

public class CreateConfigurationRequest
{
    public string ModelType { get; set; }
    public string Prefix { get; set; }
    public NamingType NamingType { get; set; }
    public int MinimumNameLength { get; set; }
    public int MaximumNameLength { get; set; }
}