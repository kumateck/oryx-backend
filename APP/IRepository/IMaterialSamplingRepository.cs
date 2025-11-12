using DOMAIN.Entities.Checklists;
using DOMAIN.Entities.MaterialSampling;
using SHARED;

namespace APP.IRepository;

public interface IMaterialSamplingRepository
{
    Task<Result<Guid>> CreateMaterialSampling(CreateMaterialSamplingRequest materialSamplingRequest);
    Task<Result> AddIssueNumberToMaterialSample(Guid materialSampleId, string issueNumber, Guid userId);
    Task<Result<MaterialSamplingDto>> GetMaterialSamplingByGrnAndBatch(Guid grnId, Guid batchId);
    Task<Result<Guid>> CreatePreSampleChecklist(CreatePreSampleChecklistRequest request);
    Task<Result<PreSampleChecklistDto>> GetPreSampleChecklistByGrnAndBatch(Guid grnId, Guid batchId);
}