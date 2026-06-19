using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Countries;
using DOMAIN.Entities.Currencies;
using DOMAIN.Entities.Items;

namespace DOMAIN.Entities.Vendors;

public class Vendor : BaseEntity
{
    [StringLength(1000000)]
    public string Name { get; set; }

    [StringLength(1000000)]
    public string Address { get; set; }

    [StringLength(100)]
    public string Phone { get; set; }

    [StringLength(100)]
    public string Email { get; set; }
    public Guid CountryId { get; set; }
    public Country Country { get; set; }

    [StringLength(1000000)]
    public string ContactPerson { get; set; }

    public Guid CurrencyId { get; set; }
    public Currency Currency { get; set; }
    public List<VendorItem> Items { get; set; } = [];
}

public class VendorItem : BaseEntity
{
    public Guid VendorId { get; set; }
    public Vendor Vendor { get; set; }
    public Guid ItemId { get; set; }
    public Item Item { get; set; }
}
