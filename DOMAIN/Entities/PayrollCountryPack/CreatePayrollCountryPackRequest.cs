namespace DOMAIN.Entities.PayrollCountryPack;

public class CreatePayrollCountryPackRequest
{
    public Guid CountryId { get; set; }
    public string Code { get; set; }
    public string Name { get; set; }
    public string Version { get; set; }
    public string PayloadJson { get; set; }
}