using DOMAIN.Entities.Base;

namespace DOMAIN.Entities.StaffRequisitions;

public class StaffRequisition : BaseEntity
{
    public int StaffRequired { get; set; }
}