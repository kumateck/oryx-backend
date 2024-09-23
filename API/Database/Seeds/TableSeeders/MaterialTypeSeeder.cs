using DOMAIN.Entities.Materials;
using INFRASTRUCTURE.Context;

namespace API.Database.Seeds.TableSeeders;

public class MaterialTypeSeeder : ISeeder
{
    public void Handle(IServiceScope scope)
    {
        var dbContext = scope.ServiceProvider.GetService<ApplicationDbContext>();
        if (dbContext == null) return;
        SeedMaterials(dbContext);
    }

    private void SeedMaterials(ApplicationDbContext dbContext)
    {
        if (dbContext.MaterialTypes.Any()) return;
        foreach (var type in new List<string>{"Active", "Inactive", "Excepient"})
        {
            dbContext.MaterialTypes.Add(new MaterialType
            {
                Name = type
            });
            dbContext.SaveChanges();
        }
    }
}