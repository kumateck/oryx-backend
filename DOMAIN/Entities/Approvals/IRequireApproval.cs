namespace DOMAIN.Entities.Approvals;

public interface IRequireApproval
{
    public bool Approved { get; set; }
}