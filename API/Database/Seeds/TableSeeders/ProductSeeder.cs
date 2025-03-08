using DOMAIN.Entities.Products;
using INFRASTRUCTURE.Context;

namespace API.Database.Seeds.TableSeeders;

public class ProductSeeder : ISeeder
{
    public void Handle(IServiceScope scope)
    {
        var dbContext = scope.ServiceProvider.GetService<ApplicationDbContext>();
        
        if (dbContext.Products.Count() < 50) return;
        
        SeedProducts(dbContext);
    }

    private static void SeedProducts(ApplicationDbContext dbContext)
    {
        var category = dbContext.ProductCategories.FirstOrDefault();
        var baseUoM = dbContext.UnitOfMeasures.FirstOrDefault();
        var basePackingUoM = dbContext.UnitOfMeasures.Skip(1).FirstOrDefault() ?? baseUoM;
        var equipment = dbContext.Equipments.FirstOrDefault();
        var department = dbContext.Departments.FirstOrDefault(d => d.Name == "Tablet");
        
        var products = new List<Product>();
        
        for (int i = 1; i <= 50; i++)
        {
            products.Add(new Product
            {
                Code = $"PRD-{i:000}",
                Name = $"Product {i}",
                GenericName = $"Generic Product {i}",
                StorageCondition = "Cool and Dry Place",
                PackageStyle = "Box",
                FilledWeight = "500g",
                ShelfLife = "2 Years",
                ActionUse = "General Purpose",
                Description = $"This is a dummy description for Product {i}.",
                FdaRegistrationNumber = $"FDA-{i:00000}",
                MasterFormulaNumber = $"MFN-{i:00000}",
                PrimaryPackDescription = "Primary Packaging Description",
                SecondaryPackDescription = "Secondary Packaging Description",
                TertiaryPackDescription = "Tertiary Packaging Description",
                CategoryId = category.Id,
                BaseQuantity = 100,
                BasePackingQuantity = 10,
                BaseUomId = baseUoM?.Id,
                BasePackingUomId = basePackingUoM?.Id,
                EquipmentId = equipment?.Id,
                DepartmentId = department?.Id,
            });
        }
        
        dbContext.Products.AddRange(products);
        dbContext.SaveChanges();
    }
}
