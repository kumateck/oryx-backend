using APP.Utils;
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
                Name = RoleUtils.ProductionManger,
                NormalizedName = RoleUtils.ProductionManger.Normalize(),
                DisplayName = RoleUtils.ProductionManger,
                Type = DepartmentType.Production,
                IsManager = true
            },
            new()
            {
                Name = RoleUtils.WarehouseManger,
                NormalizedName = RoleUtils.WarehouseManger.Normalize(),
                DisplayName = RoleUtils.WarehouseManger,
                Type = DepartmentType.Production,
                IsManager = true
            },
            new()
            {
                Name = RoleUtils.QaManager,
                NormalizedName = RoleUtils.QaManager.Normalize(),
                DisplayName = RoleUtils.QaManager,
                Type = DepartmentType.NonProduction,
                IsManager = true
            },
            new()
            {
                Name = RoleUtils.QcManager,
                NormalizedName = RoleUtils.QcManager.Normalize(),
                DisplayName = RoleUtils.QcManager,
                Type = DepartmentType.NonProduction,
                IsManager = true
            },
            new()
            {
                Name = RoleUtils.HrManager,
                NormalizedName = RoleUtils.HrManager.Normalize(),
                DisplayName = RoleUtils.HrManager,
                Type = DepartmentType.NonProduction,
                IsManager = true
            },
            new()
            {
                Name = RoleUtils.LogisticsManager,
                NormalizedName = RoleUtils.LogisticsManager.Normalize(),
                DisplayName = RoleUtils.LogisticsManager,
                Type = DepartmentType.NonProduction,
                IsManager = true
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
                Code = "DEP009",
                Description = "Handles production operations for syrup products",
                Type = DepartmentType.Production,
                CreatedAt = DateTime.UtcNow,
                Warehouses = new List<Warehouse>()
            },
            new()
            {
                Name = "OINTMENT Department",
                Code = "DEP010",
                Description = "Handles production operations for ointment products",
                Type = DepartmentType.Production,
                CreatedAt = DateTime.UtcNow,
                Warehouses = new List<Warehouse>()
            },
            new()
            {
                Name = "TABLET Department",
                Code = "DEP011",
                Description = "Handles production operations tablet operations",
                Type = DepartmentType.Production,
                CreatedAt = DateTime.UtcNow,
                Warehouses = new List<Warehouse>()
            },
            new()
            {
                Name = "BETA Department",
                Code = "DEP012",
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
