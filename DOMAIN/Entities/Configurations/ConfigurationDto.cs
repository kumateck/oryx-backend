namespace DOMAIN.Entities.Configurations;

public class ConfigurationDto
{
    public Guid Id { get; set; }
    public string ItemType { get; set; }
    public string Prefix { get; set; }
    public NamingType NamingType { get; set; }
    public int MinimumNameLength { get; set; }
    public int MaximumNameLength { get; set; }
}