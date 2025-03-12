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
        var existingOperations = dbContext.Operations.ToDictionary(o => o.Name, o => o);
        var operationList = OperationUtils.All();

        var operationsToUpdate = new List<Operation>();
        var newOperations = new List<Operation>();

        foreach (var operation in operationList)
        {
            if (existingOperations.TryGetValue(operation.Name, out var existingOperation))
            {
                // Update existing operation (even if order is the same)
                existingOperation.Order = operation.Order;
                existingOperation.Description = operation.Description;
                operationsToUpdate.Add(existingOperation);
            }
            else
            {
                // Add new operation
                newOperations.Add(new Operation
                {
                    Name = operation.Name,
                    Description = operation.Description,
                    Order = operation.Order
                });
            }
        }

        // Apply updates and insert new operations
        if (operationsToUpdate.Count != 0)
            dbContext.Operations.UpdateRange(operationsToUpdate);

        if (newOperations.Count != 0)
            dbContext.Operations.AddRange(newOperations);

        if (operationsToUpdate.Count != 0 || newOperations.Count != 0)
            dbContext.SaveChanges();
    }
}