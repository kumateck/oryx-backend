using DOMAIN.Entities.Procurement.Suppliers;
using SHARED;

namespace APP.IRepository;

public interface ISupplierRelationshipRepository
{
    Task<Result<List<SupplierCertificationDto>>> GetCertifications(Guid supplierId);
    Task<Result<Guid>> CreateCertification(Guid supplierId, SupplierCertificationRequest request, Guid userId);
    Task<Result> UpdateCertification(Guid supplierId, Guid id, SupplierCertificationRequest request, Guid userId);
    Task<Result> DeleteCertification(Guid supplierId, Guid id, Guid userId);
    Task<Result<List<SupplierCertificationDto>>> GetExpiringCertifications(int withinDays, DateTime? asOf = null);

    Task<Result<List<SupplierContactDto>>> GetContacts(Guid supplierId);
    Task<Result<Guid>> CreateContact(Guid supplierId, SupplierContactRequest request, Guid userId);
    Task<Result> UpdateContact(Guid supplierId, Guid id, SupplierContactRequest request, Guid userId);
    Task<Result> DeleteContact(Guid supplierId, Guid id, Guid userId);

    Task<Result<List<SupplierBankDetailDto>>> GetBankDetails(Guid supplierId);
    Task<Result<Guid>> CreateBankDetail(Guid supplierId, SupplierBankDetailRequest request, Guid userId);
    Task<Result> UpdateBankDetail(Guid supplierId, Guid id, SupplierBankDetailRequest request, Guid userId);
    Task<Result> DeleteBankDetail(Guid supplierId, Guid id, Guid userId);

    Task<Result<List<SupplierPricingAgreementDto>>> GetPricingAgreements(Guid supplierId);
    Task<Result<SupplierPricingAgreementDto>> GetPricingAgreementProposal(Guid supplierId, Guid id,
        Guid userId, List<Guid> roleIds);
    Task<Result<Guid>> CreatePricingAgreement(Guid supplierId, SupplierPricingAgreementRequest request, Guid userId);
    Task<Result> UpdatePricingAgreement(Guid supplierId, Guid id, SupplierPricingAgreementRequest request, Guid userId);
    Task<Result> DeletePricingAgreement(Guid supplierId, Guid id, Guid userId);
    Task<Result<SupplierPricingAgreementDto>> GetActivePricingAgreement(
        Guid supplierId, Guid materialId, Guid uomId, DateTime asOf);

    Task<Result<List<SupplierComplianceDueDto>>> GetRequalificationDue(int withinDays, DateTime? asOf = null);
    Task<Result<SupplierPerformanceDto>> ComputePerformance(
        Guid supplierId, ComputeSupplierPerformanceRequest request);
    Task<Result<Guid>> PersistPerformance(Guid supplierId, ComputeSupplierPerformanceRequest request, Guid userId);
    Task<Result<List<SupplierPerformanceDto>>> GetPerformanceRecords(Guid supplierId);
    Task<Result<SupplierSpendSummaryDto>> GetSpendSummary(Guid supplierId, DateTime from, DateTime to);
}
