using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Requisitions;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;

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
            ItemType = RequisitionType.Stock.ToString(),
            ApprovalStages =
            [
                new ApprovalStage
                {
                    UserId = dbContext.Users.IgnoreQueryFilters().FirstOrDefault(u => u.Email == "dkadusei@kumateck.com")?.Id,
                    Required = false
                },
                new ApprovalStage
                {
                    UserId = dbContext.Users.IgnoreQueryFilters().FirstOrDefault(u => u.Email == "douglassboakye22@gmail.com")?.Id,
                    Required = true
                },
                new ApprovalStage
                {
                    UserId = dbContext.Users.IgnoreQueryFilters().FirstOrDefault(u => u.Email == "anthonygyan@gmail.com")?.Id,
                    Required = false
                }
            ]
        };

        dbContext.Approvals.Add(approval);
        dbContext.SaveChanges();
    }
}