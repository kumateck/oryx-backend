using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Procurement.Manufacturers;
using DOMAIN.Entities.Procurement.Suppliers;

namespace DOMAIN.Entities.Reports.Procurement
{
    public class SupplierMaterialReportDto
    {
        public ManufacturerListDto Manufacturers { get; set; }
        public List<ManufacturerMaterialDto> Materials { get; set; } 
        public UnitOfMeasureDto Uom { get; set; }

        public SupplierListDto Supplier { get; set; }
    }


    public class SupplierMaterialFilters
{
    public string MaterialName { get; set; }
    public MaterialKind? MaterialType { get; set; }
    public string SupplierName { get; set; }
    public string ManufacturerName { get; set; }
    public DateTime? ValidityDateFrom { get; set; }
    public DateTime? ValidityDateTo { get; set; }
    public Guid? SupplierId { get; set; }
    public Guid? ManufacturerId { get; set; }
}

}