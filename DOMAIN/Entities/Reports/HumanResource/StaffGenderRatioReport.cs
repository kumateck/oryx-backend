namespace DOMAIN.Entities.Reports.HumanResource;

public class StaffGenderRatioReport
{
    public List<StaffGenderRatioCountDto> Departments { get; set; }
    public StaffGenderRatioTotalDto Totals { get; set; }
        
}

public class StaffGenderRatioTotalDto
{
    public int NumberOfPermanentMale { get; set; }
    public int NumberOfPermanentFemale { get; set; }
    public int NumberOfCasualMale { get; set; }
    public int NumberOfCasualFemale { get; set; }
    public int TotalMales => NumberOfCasualMale + NumberOfPermanentMale;
    public int TotalFemales => NumberOfCasualFemale + NumberOfPermanentFemale;
    
}

public class StaffGenderRatioCountDto : StaffGenderRatioTotalDto
{
    public string Department { get; set; }
}