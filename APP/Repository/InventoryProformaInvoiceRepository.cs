// using APP.IRepository;
// using APP.Utils;
// using DOMAIN.Entities.Invoices;
// using DOMAIN.Entities.ProformaInvoices;
// using SHARED;
//
// namespace APP.Repository;
//
// public class InventoryProformaInvoiceRepository : IInventoryProformaInvoiceRepository
// {
//     public async Task<Result<Guid>> CreateProformaInvoice(CreateProformaInvoice request)
//     {
//         throw new NotImplementedException();
//     }
//
//     public async Task<Result> SendProformaInvoiceToCustomer(Guid proformaInvoiceId, Guid userId)
//     {
//         throw new NotImplementedException();
//     }
//
//     public async Task<Result<Paginateable<IEnumerable<ProformaInvoiceDto>>>> GetProformaInvoices(int page, int pageSize, string searchQuery, ProformaInvoiceStatus? status = null,
//         bool? approved = null)
//     {
//         throw new NotImplementedException();
//     }
//
//     public async Task<Result<ProformaInvoiceDto>> GetProformaInvoice(Guid id)
//     {
//         throw new NotImplementedException();
//     }
//
//     public async Task<Result> UpdateProformaInvoice(Guid id, CreateProformaInvoice request)
//     {
//         throw new NotImplementedException();
//     }
//
//     public async Task<Result> DeleteProformaInvoice(Guid id, Guid userId)
//     {
//         throw new NotImplementedException();
//     }
//
//     public async Task<Result<Guid>> CreateInvoice(CreateInvoice request)
//     {
//         throw new NotImplementedException();
//     }
//
//     public async Task<Result<Paginateable<IEnumerable<InvoiceDto>>>> GetInvoices(int page, int pageSize, string searchQuery)
//     {
//         throw new NotImplementedException();
//     }
//
//     public async Task<Result<InvoiceDto>> GetInvoice(Guid id)
//     {
//         throw new NotImplementedException();
//     }
//
//     public async Task<Result> UpdateInvoice(Guid id, CreateInvoice request)
//     {
//         throw new NotImplementedException();
//     }
//
//     public async Task<Result> DeleteInvoice(Guid id, Guid userId)
//     {
//         throw new NotImplementedException();
//     }
// }