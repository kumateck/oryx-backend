using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;

namespace DOMAIN.Entities.UniformityOfWeights;

public class UniformityOfWeight : BaseEntity
{
    [StringLength(1000)] public string Name { get; set; }
    [StringLength(1000)] public string BalanceNumber { get; set; }
    public int NumberOfItems { get; set; }
    public decimal NominalWeight { get; set; }
}

