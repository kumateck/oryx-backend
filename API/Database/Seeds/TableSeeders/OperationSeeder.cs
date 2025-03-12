using APP.Utils;
using DOMAIN.Entities.Base;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;

namespace API.Database.Seeds.TableSeeders;

public class OperationSeeder : ISeeder
{
    public void Handle(IServiceScope scope)
    {
        var dbContext = scope.ServiceProvider.GetService<ApplicationDbContext>();

        SeedOperations(dbContext);
    }

    private static void SeedOperations(ApplicationDbContext dbContext)
    {
        var existingOperations = dbContext.Operations.AsNoTracking().ToList();

        var newOperations = OperationUtils.All()
            .Where(operation => !existingOperations.Any(e => e.Name == operation.Name))
            .Select(operation => new Operation
            {
                Name = operation.Name,
                Description = operation.Description,
                Order = operation.Order
            })
            .ToList();

        if (newOperations.Any())
        {
            dbContext.Operations.AddRange(newOperations);
            dbContext.SaveChanges();
        }
    }
}