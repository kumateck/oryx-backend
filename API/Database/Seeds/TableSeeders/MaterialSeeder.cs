/*using APP.IRepository;
using DOMAIN.Entities.Materials;

namespace API.Database.Seeds.TableSeeders;

public class MaterialSeeder : ISeeder
{
    public void Handle(IServiceScope scope)
    {
        var materialRepository = scope.ServiceProvider.GetService<IMaterialRepository>();

        Task.Run(() => SeedMaterialAsync("", "", materialRepository)).GetAwaiter().GetResult();
    }

    private async Task SeedMaterialAsync(string rawMaterialPath, string packageMaterialPath, IMaterialRepository materialRepository)
    {
        await materialRepository.ImportMaterialsFromExcel(rawMaterialPath, MaterialKind.Raw);
        await materialRepository.ImportMaterialsFromExcel(packageMaterialPath, MaterialKind.Package);
    }
}*/