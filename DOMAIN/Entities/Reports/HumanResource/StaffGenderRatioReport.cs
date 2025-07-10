namespace DOMAIN.Entities.Reports.HumanResource;

public class StaffGenderRatioReport
{
        public int NumberOfPermanentMale { get; set; }
        public int NumberOfPermanentFemale { get; set; }
        public int NumberOfCasualMale { get; set; }
        public int NumberOfCasualFemale { get; set; }
        public int NumberOfNssMale { get; set; }
        public int NumberOfNssFemale { get; set; }
        public int NumberOfAnnexMale { get; set; }
        public int NumberOfAnnexFemale { get; set; }
    
        public int TotalPermanentMale { get; set; }
        public int TotalPermanentFemale { get; set; }
        public int TotalCasualMale { get; set; }
        public int TotalCasualFemale { get; set; }
        public int TotalNssMale { get; set; }
        public int TotalNssFemale { get; set; }
        public int TotalAnnexMale { get; set; }
        public int TotalAnnexFemale { get; set; }
    
        // totals per department row
        public int TotalMalePerDepartment => NumberOfCasualMale + NumberOfPermanentMale + NumberOfNssMale + NumberOfAnnexMale;
        public int TotalFemalePerDepartment => NumberOfCasualFemale + NumberOfPermanentFemale + NumberOfNssFemale + NumberOfAnnexFemale;
    
        // grand total
        public int TotalMale => TotalPermanentMale + TotalCasualMale + TotalNssMale + TotalAnnexMale;
        public int TotalFemale => TotalPermanentFemale + TotalCasualFemale + TotalNssFemale + TotalAnnexFemale;
    
        public int TotalEmployees => TotalMale + TotalFemale;
        
}