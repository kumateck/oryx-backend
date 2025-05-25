using APP.Utils;
using DOMAIN.Entities.Base;
using INFRASTRUCTURE.Context;

namespace API.Database.Seeds.TableSeeders;

public class OperationSeeder : ISeeder
{
    public void Handle(IServiceScope scope)
    {
        var dbContext = scope.ServiceProvider.GetService<ApplicationDbContext>();

        //if (dbContext.Operations.Any()) return;

        SeedOperations(dbContext);
    }

    private static void SeedOperations(ApplicationDbContext dbContext)
    {
        var allOps = OperationUtils.All().ToList();

        foreach (var op in allOps)
        {
            var existing = dbContext.Operations.FirstOrDefault(o => o.Name == op.Name);

            if (existing != null)
            {
                // Update existing operation with new Action (if needed)
                existing.Action = op.Action;
                existing.Description = op.Description; // Optional: update description if changed
                existing.Order = op.Order;             // Optional: update order if changed
                dbContext.Operations.Update(existing);
            }
            else
            {
                // Add new operation
                var newOperation = new Operation
                {
                    Name = op.Name,
                    Description = op.Description,
                    Order = op.Order,
                    Action = op.Action
                };

                dbContext.Operations.Add(newOperation);
            }
        }
        dbContext.SaveChanges();
    }
}
