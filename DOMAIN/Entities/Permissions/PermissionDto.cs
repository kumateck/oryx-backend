namespace DOMAIN.Entities.Permissions;

public class PermissionDto(string module, string submodule, string key, string name, string description, /*bool hasOptions,*/ List<string> types = null)
{
    public string Module { get; set; } = module;
    public string SubModule { get; set; } = submodule;
    public string Key { get; set; } = key;
    public string Name { get; set; } = name;
    public string Description { get; set; } = description;
    public bool HasOptions { get; set; } 
    public List<string> Types { get; set; } = types ?? ["Access"];
}

public class PermissionModuleDto
{
    public string Module { get; set; }
    public bool IsActive { get; set; }
    public List<PermissionDetailDto> Children { get; set; } = [];
}

public class PermissionDetailDto
{
    public string Key { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string SubModule { get; set; }
    public bool HasOptions { get; set; }
    public List<string> Types { get; set; } = [];
}
