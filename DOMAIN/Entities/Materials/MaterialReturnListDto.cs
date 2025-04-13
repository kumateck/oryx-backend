namespace DOMAIN.Entities.Materials;

public class MaterialReturnListDto
{
    public string ProductName { get; set; }
    public string BatchNumber { get; set; }
    public DateTime ReturnDate { get; set; }
    public string ReturnDepartment { get; set; }
    public string ProductionSchedule { get; set; }
    public string Status { get; set; }
}