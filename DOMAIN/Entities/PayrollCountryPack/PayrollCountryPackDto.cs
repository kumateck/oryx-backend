using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Countries;

namespace DOMAIN.Entities.PayrollCountryPack;

public class PayrollCountryPackDto : BaseDto
{
    public Guid CountryId { get; set; }
    public CountryDto Country { get; set; }

    [StringLength(50)] public string Code { get; set; }
    [StringLength(255)] public string Name { get; set; }
    [StringLength(50)] public string Version { get; set; }
    [StringLength(int.MaxValue)] public string PayloadJson { get; set; }
    public bool IsActive { get; set; } = true;
}