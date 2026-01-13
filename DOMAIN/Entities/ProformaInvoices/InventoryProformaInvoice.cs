// using System.ComponentModel.DataAnnotations;
// using DOMAIN.Entities.Approvals;
// using DOMAIN.Entities.Base;
// using DOMAIN.Entities.Vendors;
//
// namespace DOMAIN.Entities.ProformaInvoices;
//
// public class CreateInventoryProformaInvoice
// {
//     public string Code { get; set; }
//     public Guid VendorQuotationId { get; set; }
//     
//     [MinLength(1, ErrorMessage = "At least one item must be included in the proforma invoice")]
//     public List<CreateProformaInvoiceItem> Items { get; set; } = [];
// }
//
// public class CreateProformaInvoiceItem
// {
//     public Guid VendorItemId { get; set; }
//     public decimal Quantity { get; set; }
// }
// public class InventoryProformaInvoice : BaseEntity, IRequireApproval
// {
//     public string Code { get; set; }
//     public Guid VendorQuotationId { get; set; }
//     public Vendor VendorQuotation { get; set; }
//
//     public List<InventoryProformaInvoiceItem> Items { get; set; } = [];
//     public List<InventoryProformaInvoiceApproval> Approvals { get; set; } = [];
//     public ProformaInvoiceStatus InventoryStatus { get; set; }
//
//     public bool Approved { get; set; }
// }
//
// public class InventoryProformaInvoiceApproval : ResponsibleApprovalStage
// {
//     public Guid Id { get; set; }
//     public Guid InventoryProformaInvoiceId { get; set; }
//     public InventoryProformaInvoice InventoryProformaInvoice { get; set; }
//     
//     public Guid ApprovalId { get; set; }
//     public Approval Approval { get; set; }
// }
//
// public class InventoryProformaInvoiceItem : BaseEntity
// {
//     public Guid InventoryProformaInvoiceId { get; set; }
//     public InventoryProformaInvoice InventoryProformaInvoice { get; set; }
//     
//     public Guid VendorItemId { get; set; }
//     public VendorItem VendorItem { get; set; }
//     
//     public decimal Quantity { get; set; }
// }
//
// public class InventoryProformaInvoiceDto : BaseDto
// {
//     public string Code { get; set; }
//     public ProformaInvoiceStatus Status { get; set; }
//     public List<InventoryProformaInvoiceItemDto> Items { get; set; } = [];
// }
//
// public class InventoryProformaInvoiceItemDto : BaseDto
// {
//     public VendorItemDto VendorItem { get; set; }
//     public decimal Quantity { get; set; }
// }