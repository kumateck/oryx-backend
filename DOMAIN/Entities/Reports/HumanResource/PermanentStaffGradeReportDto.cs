namespace DOMAIN.Entities.Reports.HumanResource;

public class PermanentStaffGradeReportDto
{
    public List<PermanentStaffGradeCountDto> Departments { get; set; } = [];
    public PermanentStaffGradeTotalDto Totals { get; set; }
}

public class PermanentStaffGradeTotalDto
{
    public int SeniorMgtMale { get; set; }
    public int SeniorMgtFemale { get; set; }
    public int SeniorStaffMale { get; set; }
    public int SeniorStaffFemale { get; set; }
    public int JuniorStaffMale { get; set; }
    public int JuniorStaffFemale { get; set; }

    public int TotalMale => SeniorMgtMale + SeniorStaffMale + JuniorStaffMale;
    public int TotalFemale => SeniorMgtFemale + SeniorStaffFemale + JuniorStaffFemale;
    public int Total => TotalMale + TotalFemale;
}

public class PermanentStaffGradeCountDto : PermanentStaffGradeTotalDto
{
    public string Department { get; set; }
}
