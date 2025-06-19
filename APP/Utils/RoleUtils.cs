namespace APP.Utils;

public class RoleUtils
{
    public const string AppRoleSuper = "super";
    public const string AppRoleAdmin = "admin";
    public const string ProductionManger = "Production Manager";
    public const string WarehouseManger = "Warehouse Manager";
    public const string HrManager = "HR Manager";
    public const string QaManager = "QA Manager";
    public const string QcManager = "QC Manager";

    
    public static IEnumerable<string> AppRoles()
    {
        return
        [
            AppRoleSuper,
            AppRoleAdmin
        ];
    }

    public static IEnumerable<string> DefaultRoles()
    {
        return
        [
            ProductionManger,
            WarehouseManger
        ];
    }
}