using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Roles;
using DOMAIN.Entities.Warehouses;
using INFRASTRUCTURE.Context;

namespace API.Database.Seeds.TableSeeders;

public class DepartmentSeeder : ISeeder
{
    public void Handle(IServiceScope scope)
    {
        var dbContext = scope.ServiceProvider.GetService<ApplicationDbContext>();

        if (dbContext != null)
        {
            SeedDepartments(dbContext);
        }
    }

    private static void SeedDepartments(ApplicationDbContext dbContext)
    {
        var requiredRoles = new List<Role>
        {
            new()
            {
                Name = "Production Manager",
                NormalizedName = "PRODUCTION_MANAGER",
                DisplayName = "Production Manager",
                Type = DepartmentType.Production
            },
            new()
            {
                Name = "Warehouse Manager",
                NormalizedName = "WAREHOUSE_MANAGER",
                DisplayName = "Warehouse Manager",
                Type = DepartmentType.Production
            }
        };

        foreach (var role in requiredRoles.Where(role => !dbContext.Roles.Any(r => r.Name == role.Name)))
        {
            dbContext.Roles.Add(role);
            dbContext.SaveChanges();
        }
        
        if (dbContext.Departments.Any()) return;
        
        var departments = new List<Department>
        {
            new()
            {
                Name = "SYRUP Department",
                Description = "Handles production operations for syrup products",
                Type = DepartmentType.Production,
                CreatedAt = DateTime.UtcNow,
                Warehouses = new List<Warehouse>()
            },
            new()
            {
                Name = "OINTMENT Department",
                Description = "Handles production operations for ointment products",
                Type = DepartmentType.Production,
                CreatedAt = DateTime.UtcNow,
                Warehouses = new List<Warehouse>()
            },
            new()
            {
                Name = "TABLET Department",
                Description = "Handles production operations tablet operations",
                Type = DepartmentType.Production,
                CreatedAt = DateTime.UtcNow,
                Warehouses = new List<Warehouse>()
            },
            new()
            {
                Name = "BETA Department",
                Description = "Handles production operations beta operations",
                Type = DepartmentType.Production,
                CreatedAt = DateTime.UtcNow,
                Warehouses = new List<Warehouse>()
            },
            // Add more departments here if needed
        };

        foreach (var department in departments)
        {
            if (department.Type == DepartmentType.Production)
            {
                var departmentName = department.Name;

                department.Warehouses.Add(new Warehouse
                {
                    Id = Guid.NewGuid(),
                    Name = $"{departmentName} Production Floor",
                    Description = $"The {departmentName} production materials",
                    DepartmentId = department.Id,
                    CreatedAt = DateTime.UtcNow,
                    Type = WarehouseType.Production
                });

                department.Warehouses.Add(new Warehouse
                {
                    Id = Guid.NewGuid(),
                    Name = $"{departmentName} Package Warehouse",
                    Description = $"The {departmentName} packaged materials storage warehouse",
                    DepartmentId = department.Id,
                    CreatedAt = DateTime.UtcNow,
                    Type = WarehouseType.PackagedStorage
                });

                department.Warehouses.Add(new Warehouse
                {
                    Id = Guid.NewGuid(),
                    Name = $"{departmentName} Raw Warehouse",
                    Description = $"The {departmentName} raw materials storage warehouse",
                    DepartmentId = department.Id,
                    CreatedAt = DateTime.UtcNow,
                    Type = WarehouseType.RawMaterialStorage
                });

                department.Warehouses.Add(new Warehouse
                {
                    Id = Guid.NewGuid(),
                    Name = $"{departmentName} Finished Goods Warehouse",
                    Description = $"The {departmentName} finished goods warehouse",
                    DepartmentId = department.Id,
                    CreatedAt = DateTime.UtcNow,
                    Type = WarehouseType.FinishedGoodsStorage
                });
            }

            dbContext.Departments.Add(department);
            dbContext.RoleDepartments.Add(new RoleDepartment
            {
                RoleId = dbContext.Roles.First(r => r.Name == "Production Manager").Id,
                DepartmentId = department.Id
            });
            dbContext.RoleDepartments.Add(new RoleDepartment
            {
                RoleId = dbContext.Roles.First(r => r.Name == "Warehouse Manager").Id,
                DepartmentId = department.Id
            });
        }

        dbContext.SaveChanges();
    }
}
