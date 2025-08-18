using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Designations;
using DOMAIN.Entities.Warehouses;
using SHARED;

namespace DOMAIN.Entities.Departments;

public class Department 
{
    public Guid Id { get; set; }
    [StringLength(100)] public string Code { get; set; }
    [StringLength(255)] public string Name { get; set; }
    [StringLength(1000)] public string Description { get; set; }
    public DepartmentType Type { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? CreatedById { get; set; }
    public Guid? LastUpdatedById { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? LastDeletedById { get; set; }
    public Guid? ParentDepartmentId { get; set; }
    public Department ParentDepartment { get; set; }
    public List<Warehouse> Warehouses { get; set; } = [];
    
    public ICollection<Designation> Designations { get; set; } = new List<Designation>();
}

