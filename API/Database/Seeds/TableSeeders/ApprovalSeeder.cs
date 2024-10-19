using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Requisitions;
using INFRASTRUCTURE.Context;

namespace API.Database.Seeds.TableSeeders;

public class ApprovalSeeder : ISeeder
{
    public void Handle(IServiceScope scope)
    {
        var dbContext = scope.ServiceProvider.GetService<ApplicationDbContext>();
        
        if (dbContext.Approvals.Any()) return;

        SeedApprovals(dbContext);
    }

    private void SeedApprovals(ApplicationDbContext dbContext)
    {
        var approval = new Approval
        {
            ItemType = nameof(StockRequisition),
            ApprovalStages =
            [
                new ApprovalStage
                {
                    UserId = dbContext.Users.FirstOrDefault()?.Id,
                }
            ]
        };

        dbContext.Approvals.Add(approval);
        dbContext.SaveChanges();
    }
}