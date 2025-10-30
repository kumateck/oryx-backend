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

        if (!dbContext.Operations.Any())
        {
            SeedOperations(dbContext);
        }
    }

    private static void SeedOperations(ApplicationDbContext dbContext)
    {
        var departments = dbContext.Departments.IgnoreQueryFilters().ToList();

        var allOpsByDepartment = OperationUtils.All();

        foreach (var departmentEntry in allOpsByDepartment)
        {
            var departmentName = departmentEntry.Key;
            var operations = departmentEntry.Value;

            var department = departments.FirstOrDefault(d => d.Name.StartsWith(departmentName));

            if (department == null)
            {
                continue;
            }
            
            var newOperations = new List<Operation>();

            foreach (var op in operations)
            {
                var existing = dbContext.Operations.FirstOrDefault(o => o.Name == op.Name);

                if (existing != null)
                {
                    // Update existing operation
                    existing.Description = op.Description;
                    existing.Order = op.Order;
                    existing.DepartmentId = department.Id;
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
                        DepartmentId = department.Id
                    };
                    newOperations.Add(newOperation);
                }
            }
            dbContext.Operations.AddRangeAsync(newOperations);
        }
        dbContext.SaveChanges();
    }
}