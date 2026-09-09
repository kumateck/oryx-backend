using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Warehouses;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace API.Database.Seeds.TableSeeders;

public class ResearchAndDevelopmentDepartmentSeeder : ISeeder
{
    public void Handle(IServiceScope scope)
    {
        var dbContext = scope.ServiceProvider.GetService<ApplicationDbContext>();
        if (dbContext != null) SeedDepartment(dbContext);
    }

    private static void SeedDepartment(ApplicationDbContext dbContext)
    {
        var department = dbContext
            .Departments.IgnoreQueryFilters()
            .FirstOrDefault(d => d.Name == "Research & Development");

        if (department is null)
        {
            department = new Department
            {
                Code = "RND",
                Name = "Research & Development",
                Description = "Research and development / new product development function.",
                Type = DepartmentType.RnD,
            };
            dbContext.Departments.Add(department);
        }
        else if (department.Type != DepartmentType.RnD)
        {
            // Backfill: this department was seeded before DepartmentType.RnD existed.
            department.Type = DepartmentType.RnD;
            dbContext.Departments.Update(department);
        }

        if (!dbContext.Warehouses.IgnoreQueryFilters().Any(w => w.Name == "R&D Lab Warehouse"))
        {
            dbContext.Warehouses.Add(new Warehouse
            {
                Name = "R&D Lab Warehouse",
                Description = "Small-quantity material storage for R&D trial batches.",
                DepartmentId = department.Id,
                Type = WarehouseType.RawMaterialStorage,
            });
        }

        dbContext.SaveChanges();
    }
}
