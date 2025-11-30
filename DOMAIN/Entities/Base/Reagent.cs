using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.Base;

public class CreateReagent
{
    public string Name { get; set; }
    public string Description { get; set; }
}
public class Reagent : BaseEntity
{
    [StringLength(1000)] public string Name { get; set; }
    [StringLength(100000000)] public string Description { get; set; }
}

public class ReagentDto : BaseDto
{
    public string Name { get; set; }
    public string Description { get; set; }
}