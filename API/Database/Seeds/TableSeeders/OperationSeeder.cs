using APP.Utils;
using DOMAIN.Entities.Base;
using INFRASTRUCTURE.Context;

namespace API.Database.Seeds.TableSeeders;

public class OperationSeeder : ISeeder
{
    public void Handle(IServiceScope scope)
    {
        var dbContext = scope.ServiceProvider.GetService<ApplicationDbContext>();

        if (dbContext.Operations.Any()) return;

        SeedOperations(dbContext);
    }

    private static void SeedOperations(ApplicationDbContext dbContext)
    {
        var operations = OperationUtils.All().Select(operation => new Operation { Name = operation.Name, Description = operation.Description }).ToList();

        dbContext.Operations.AddRange(operations);
        dbContext.SaveChanges();
    }
}
