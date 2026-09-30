using DOMAIN.Entities.QualityRoutines;
using SHARED;

namespace APP.IRepository;

public interface IRoutineQcRepository
{
    Task<Result<Guid>> CreateArd(CreateRoutineArdRequest request, Guid actorId);
    Task<Result<Guid>> CreateDefinition(CreateRoutineDefinitionRequest request, Guid actorId);
    Task<Result<Guid>> CreateExecution(CreateRoutineExecutionRequest request, Guid actorId);
    Task<Result<Guid>> AddSample(Guid executionId, CreateRoutineSampleRequest request, Guid actorId);
    Task<Result<List<RoutineExecutionDto>>> ListExecutions();
    Task<Result<List<RoutineArdDto>>> ListArds();
    Task<Result<List<RoutineDefinitionDto>>> ListDefinitions();
    Task<Result<RoutineCertificateDto>> GetCertificate(Guid id);
    Task<Result<RoutineExecutionDto>> GetExecution(Guid id);
    Task<Result<RoutineTrackContextDto>> GetTrack(Guid id);
    Task<Result> SubmitTrackForApproval(Guid trackId, Guid actorId);
    Task<Result<Guid>> GenerateCertificate(Guid sampleId, Guid actorId);
}
