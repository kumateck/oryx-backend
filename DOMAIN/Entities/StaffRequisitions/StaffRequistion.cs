using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Designations;

namespace DOMAIN.Entities.StaffRequisitions;

public class StaffRequisition : BaseEntity, IRequireApproval
{
    public int StaffRequired { get; set; }
    
    public BudgetStatus BudgetStatus { get; set; }
    
    public AppointmentType AppointmentType { get; set; }

    public StaffRequisitionStatus StaffRequisitionStatus { get; set; } = StaffRequisitionStatus.Pending;
    
    public Guid DepartmentId { get; set; }
    
    public Department Department { get; set; }
    
    public DateTime RequestUrgency { get; set; }
    
    public string Justification { get; set; }
    
    public string Qualification { get; set; }
    public string EducationalQualification { get; set; }
    
    public string AdditionalRequirements { get; set; }
    
    public Guid DesignationId { get; set; }
    
    public Designation Designation { get; set; }

    public List<StaffRequisitionApproval> Approvals { get; set; } = [];

    public bool Approved { get; set; }
}

public class StaffRequisitionApproval : ResponsibleApprovalStage
{
    public Guid Id { get; set; }
    
    public Guid StaffRequisitionId { get; set; }
    
    public StaffRequisition StaffRequisition { get; set; }
    
    public Guid ApprovalId { get; set; }
    
    public Approval Approval { get; set; }
}
public enum BudgetStatus
{
    Budgeted,
    NotBudgeted
}

public enum AppointmentType
{
    NewAppointment,
    Replacement
}

public enum StaffRequisitionStatus
{
    New,
    Pending,
    Approved,
    Rejected,
    Expired
}