// using APP.Utils;
// using DOMAIN.Entities.Invoices;
// using DOMAIN.Entities.ProformaInvoices;
// using SHARED;
//
// namespace APP.IRepository;
//
// public interface IInventoryProformaInvoiceRepository
// {
//     // Proforma Invoices
//     Task<Result<Guid>> CreateProformaInvoice(CreateProformaInvoice request);
//     Task<Result> SendProformaInvoiceToCustomer(Guid proformaInvoiceId, Guid userId);
//     Task<Result<Paginateable<IEnumerable<ProformaInvoiceDto>>>> GetProformaInvoices(int page,
//         int pageSize,
//         string searchQuery,
//         ProformaInvoiceStatus? status = null,
//         bool? approved = null);
//     Task<Result<ProformaInvoiceDto>> GetProformaInvoice(Guid id);
//     Task<Result> UpdateProformaInvoice(Guid id, CreateProformaInvoice request);
//     Task<Result> DeleteProformaInvoice(Guid id, Guid userId);
//
//     // In IProductionOrderRepository
//     Task<Result<Guid>> CreateInvoice(CreateInvoice request);
//     Task<Result<Paginateable<IEnumerable<InvoiceDto>>>> GetInvoices(int page, int pageSize, string searchQuery);
//     Task<Result<InvoiceDto>> GetInvoice(Guid id);
//     Task<Result> UpdateInvoice(Guid id, CreateInvoice request);
//     Task<Result> DeleteInvoice(Guid id, Guid userId);
// }