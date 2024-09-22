using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;

namespace DOMAIN.Entities.Configurations;

public class Configuration : BaseEntity
{
    [StringLength(255)]
    public string ItemType { get; set; }
    [StringLength(255)]
    public string Prefix { get; set; }
    public NamingType NamingType { get; set; }
    public int MinimumNameLength { get; set; }
    public int MaximumNameLength { get; set; }
}

public enum NamingType
{
    Series,
    Random,
    Time
}