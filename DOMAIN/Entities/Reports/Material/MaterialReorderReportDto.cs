namespace DOMAIN.Entities.Reports.Material;

public class MaterialReorderReportDto
{
    public string MaterialName { get; set; } 
    public string MaterialCode { get; set; } 
    public decimal CurrentQuantity { get; set; }
    public decimal ReOrderLevel { get; set; }
    public string UomSymbol { get; set; }
}