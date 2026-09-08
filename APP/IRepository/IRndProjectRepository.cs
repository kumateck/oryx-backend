using APP.Utils;
using DOMAIN.Entities.RndProjects;
using SHARED;

namespace APP.IRepository;

public interface IRndProjectRepository
{
    Task<Result<Guid>> CreateProject(CreateRndProjectRequest request, Guid userId);
    Task<Result> UpdateProject(Guid id, UpdateRndProjectRequest request, Guid userId);
    Task<Result> UpdateStatus(Guid id, UpdateRndProjectStatusRequest request, Guid userId);
    Task<Result> DeleteProject(Guid id, Guid userId);
    Task<Result<RndProjectDto>> GetProject(Guid id);
    Task<Result<Paginateable<IEnumerable<RndProjectDto>>>> GetProjects(
        int page,
        int pageSize,
        string searchQuery,
        RndProjectStatus? status
    );
}
