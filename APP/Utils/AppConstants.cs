namespace APP.Utils;

public static class AppConstants
{
    public const string MyAllowedSpecificOrigins = "_myAllowedSpecificOrigins";
    public const string LocalhostFrontEnd = "http://localhost:4200";
    public const string HttpMethodPost = "POST";
    public const string HttpMethodPut = "PUT";

    public const int ErrorNumberForMysqlUniqueConstraintViolation = 1062;
    public const int ErrorNumberForForeignKeyConstraint = 1452;
    
    public const string HorizontalLogo = "horizontal-logo";
    public const string VerticalLogo = "vertical-logo";

    public const string AppName = "App-Name";
    
    //Tenant Ids
    public const string DefaultTenantId = "Entrance";
    public const string And = "AND";
    public const string Or = "Or";
    
    //Domains
    public const string DomainType = "A";
    public const string SShPort = "22";
    
    //Permission
    public const string Permission = "permission";
    
    //Mapper Context
    public const string Status = "Status";
    public const string FindingActions = "FindingActions";
    public const string ModelType = "ModelType";
    public const string IsAdmin = "IsAdmin";
    public const string UserId = "UserId";
}