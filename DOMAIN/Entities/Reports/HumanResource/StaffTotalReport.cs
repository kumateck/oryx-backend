namespace DOMAIN.Entities.Reports.HumanResource;

public class StaffTotalReport
{
    public List<StaffTotalSummary> Departments { get; set; } = [];
    public StaffGrandTotal Totals { get; set; }
}


public class StaffGrandTotal
{
    public int TotalPermanentStaff { get; set; }
    public int TotalCasualStaff { get; set; }
    public int TotalStaff => TotalPermanentStaff + TotalCasualStaff;
}

public class StaffTotalSummary : StaffGrandTotal
{
    public string Department { get; set; }
}
