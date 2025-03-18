using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;

namespace DOMAIN.Entities.Charges;

public class Charge : BaseEntity
{
    [StringLength(100)] public string Name { get; set; }
}