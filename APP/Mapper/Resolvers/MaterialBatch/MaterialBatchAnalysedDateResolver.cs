using AutoMapper;
using DOMAIN.Entities.Forms;
using INFRASTRUCTURE.Context;

namespace APP.Mapper.Resolvers.MaterialBatch;

public class MaterialBatchAnalysedDateResolver(ApplicationDbContext dbContext)
    : IValueResolver<DOMAIN.Entities.Materials.Batch.MaterialBatch, object, DateTime?>
{
    public DateTime? Resolve(
        DOMAIN.Entities.Materials.Batch.MaterialBatch source,
        object destination,
        DateTime? destMember,
        ResolutionContext context
    )
    {
        if (
            context.TryGetItems(out var items)
            && items.TryGetValue("FormAssignees", out var formAssigneesObj)
            && formAssigneesObj is List<FormAssignee> formAssignees
        )
        {
            return formAssignees.FirstOrDefault(f => f.MaterialBatchId == source.Id)?.CreatedAt;
        }

        return dbContext
            .FormAssignees.Where(f => f.MaterialBatchId == source.Id)
            .Select(f => (DateTime?)f.CreatedAt)
            .FirstOrDefault();
    }
}
