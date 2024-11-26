using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;

namespace DOMAIN.Entities.Countries;

public class Country : BaseEntity
{
    [StringLength(100)] public string Name { get; set; }
    [StringLength(100)] public string Nationality { get; set; }
    [StringLength(100)] public string Code { get; set; }
}