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
        var departments = dbContext.Departments
            .IgnoreQueryFilters()
            .ToDictionary(d => d.Name,
                d => d);

        var allOpsByDepartment = OperationUtils.All();

        foreach (var departmentEntry in allOpsByDepartment)
        {
            var departmentName = departmentEntry.Key;
            var operations = departmentEntry.Value;

            var departmentValid = departments.TryGetValue(departmentName, out var department);

            if (!departmentValid) continue;

            if (department == null)
            {
                continue;
            }

            var newOperations = new List<Operation>();

            foreach (var op in operations)
            {
                var existing = dbContext.Operations
                    .FirstOrDefault(o => o.Name == op.Name && o.Department == department);

                if (existing == null)
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