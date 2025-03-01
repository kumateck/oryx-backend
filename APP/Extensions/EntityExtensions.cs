using DOMAIN.Entities.Base;
using DOMAIN.Entities.Users;
using DOMAIN.Entities.Warehouses;

namespace APP.Extensions;

public static class EntityExtensions
{
    public static bool IsDeleted(this IBaseEntity entity)
    {
        return entity.DeletedAt.HasValue;
    }

    public static Warehouse GetUserProductionWarehouse(this User user)
    {
        var warehouses = user.Department?.Warehouses ?? [];
        return warehouses.FirstOrDefault(w => w.Type == WarehouseType.Production);
    }
}