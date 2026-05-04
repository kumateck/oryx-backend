using AutoMapper;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.Materials.Batch;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;

namespace APP.Mapper.Resolvers.MaterialBatch;

public class MaterialBatchAnalysedByResolver(ApplicationDbContext dbContext)
    : IValueResolver<DOMAIN.Entities.Materials.Batch.MaterialBatch, object, string>
{
    public string Resolve(
        DOMAIN.Entities.Materials.Batch.MaterialBatch source,
        object destination,
        string destMember,
        ResolutionContext context
    )
    {
        FormAssignee formAssignee;

        if (
            context.Items.TryGetValue("FormAssignees", out var formAssigneesObj)
            && formAssigneesObj is List<FormAssignee> formAssignees
        )
        {
            formAssignee = formAssignees.FirstOrDefault(f => f.MaterialBatchId == source.Id);
        }
        else
        {
            formAssignee = dbContext
                .FormAssignees.Include(f => f.FieldAssignees)
                    .ThenInclude(fa => fa.Assignee)
                .FirstOrDefault(f => f.MaterialBatchId == source.Id);
        }

        if (formAssignee == null)
            return null;

        return string.Join(
            ",",
            formAssignee
                .FieldAssignees.Select(f => $"{f.Assignee.FirstName} {f.Assignee.LastName}")
                .Distinct()
                .ToList()
        );
    }
}
