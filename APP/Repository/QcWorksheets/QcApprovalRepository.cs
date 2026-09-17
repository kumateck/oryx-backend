using APP.IRepository;
using AutoMapper;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.QcWorksheets;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository.QcWorksheets;

/// <summary>
/// Reads over the single shared <see cref="QcApproval"/> table. Keeping every QC approval
/// point in one table is what makes a single cross-entity queue possible; this list grows
/// as later milestones add Specification, WorksheetInstance and OosCase without needing a
/// new query per entity type.
/// </summary>
public class QcApprovalRepository(ApplicationDbContext context, IMapper mapper) : IQcApprovalRepository
{
    public async Task<Result<List<QcPendingApprovalDto>>> GetPendingApprovals(
        Guid userId, List<Guid> roleIds)
    {
        roleIds ??= [];

        var candidates = await context.QcApprovals
            .Include(item => item.User)
            .Where(item => item.Status == ApprovalStatus.Pending
                           && item.ActivatedAt != null
                           && (item.UserId == userId
                               || (item.RoleId.HasValue && roleIds.Contains(item.RoleId.Value))))
            .ToListAsync();

        if (candidates.Count == 0)
            return Result.Success(new List<QcPendingApprovalDto>());

        // Only the current round of each document is actionable.
        var latestRounds = await context.QcApprovals
            .GroupBy(item => new { item.EntityType, item.EntityId })
            .Select(group => new
            {
                group.Key.EntityType,
                group.Key.EntityId,
                Round = group.Max(item => item.ApprovalRound)
            })
            .ToListAsync();

        var latest = latestRounds.ToDictionary(
            item => (item.EntityType, item.EntityId), item => item.Round);

        var actionable = candidates
            .Where(item => latest.TryGetValue((item.EntityType, item.EntityId), out var round)
                           && item.ApprovalRound == round)
            .ToList();

        var stpIds = actionable
            .Where(item => item.EntityType == QcApprovalEntityTypes.StandardTestProcedure)
            .Select(item => item.EntityId)
            .Distinct()
            .ToList();

        var templateIds = actionable
            .Where(item => item.EntityType == QcApprovalEntityTypes.WorksheetTemplate)
            .Select(item => item.EntityId)
            .Distinct()
            .ToList();

        var specificationIds = actionable
            .Where(item => item.EntityType == QcApprovalEntityTypes.Specification)
            .Select(item => item.EntityId)
            .Distinct()
            .ToList();

        var stps = await context.QcStandardTestProcedures
            .Include(item => item.CreatedBy)
            .Where(item => stpIds.Contains(item.Id))
            .ToListAsync();

        var templates = await context.QcWorksheetTemplates
            .Include(item => item.CreatedBy)
            .Where(item => templateIds.Contains(item.Id))
            .ToListAsync();

        var specifications = await context.QcSpecifications
            .Include(item => item.CreatedBy)
            .Where(item => specificationIds.Contains(item.Id))
            .ToListAsync();

        var results = new List<QcPendingApprovalDto>();

        foreach (var stage in actionable)
        {
            switch (stage.EntityType)
            {
                case QcApprovalEntityTypes.StandardTestProcedure:
                    var stp = stps.FirstOrDefault(item => item.Id == stage.EntityId);
                    if (stp is null) continue;
                    results.Add(new QcPendingApprovalDto
                    {
                        EntityType = stage.EntityType,
                        EntityId = stp.Id,
                        Code = stp.Code,
                        Name = stp.Name,
                        Version = stp.Version,
                        Status = stp.Status,
                        CreatedAt = stp.CreatedAt,
                        CreatedBy = mapper.Map<UserDto>(stp.CreatedBy),
                        Order = stage.Order,
                        ApprovalRound = stage.ApprovalRound,
                        ResourcePath = $"qc/worksheets/stps/{stp.Id}"
                    });
                    break;

                case QcApprovalEntityTypes.WorksheetTemplate:
                    var template = templates.FirstOrDefault(item => item.Id == stage.EntityId);
                    if (template is null) continue;
                    results.Add(new QcPendingApprovalDto
                    {
                        EntityType = stage.EntityType,
                        EntityId = template.Id,
                        Code = template.Code,
                        Name = template.Name,
                        Version = template.Version,
                        Status = template.Status,
                        CreatedAt = template.CreatedAt,
                        CreatedBy = mapper.Map<UserDto>(template.CreatedBy),
                        Order = stage.Order,
                        ApprovalRound = stage.ApprovalRound,
                        ResourcePath = $"qc/worksheets/templates/{template.Id}"
                    });
                    break;

                case QcApprovalEntityTypes.Specification:
                    var specification = specifications.FirstOrDefault(item => item.Id == stage.EntityId);
                    if (specification is null) continue;
                    results.Add(new QcPendingApprovalDto
                    {
                        EntityType = stage.EntityType,
                        EntityId = specification.Id,
                        Code = specification.Code,
                        Name = specification.Name,
                        Version = specification.Version,
                        Status = specification.Status,
                        CreatedAt = specification.CreatedAt,
                        CreatedBy = mapper.Map<UserDto>(specification.CreatedBy),
                        Order = stage.Order,
                        ApprovalRound = stage.ApprovalRound,
                        ResourcePath = $"qc/worksheets/specifications/{specification.Id}"
                    });
                    break;
            }
        }

        return Result.Success(results.OrderBy(item => item.CreatedAt).ToList());
    }

    public async Task<Result<List<QcApprovalDto>>> GetApprovalsForEntity(string entityType, Guid entityId)
    {
        var rows = await context.QcApprovals
            .Include(item => item.ApprovedBy)
            .Include(item => item.User)
            .Where(item => item.EntityType == entityType && item.EntityId == entityId)
            .OrderBy(item => item.ApprovalRound)
            .ThenBy(item => item.Order)
            .ToListAsync();

        return Result.Success(rows.Select(item => new QcApprovalDto
        {
            Id = item.Id,
            EntityType = item.EntityType,
            EntityId = item.EntityId,
            ApprovalId = item.ApprovalId,
            ApprovalRound = item.ApprovalRound,
            Order = item.Order,
            Required = item.Required,
            Status = item.Status,
            ApprovalTime = item.ApprovalTime,
            ReauthConfirmedAt = item.ReauthConfirmedAt,
            ActivatedAt = item.ActivatedAt,
            Comments = item.Comments,
            ApprovedBy = mapper.Map<UserDto>(item.ApprovedBy),
            User = mapper.Map<UserDto>(item.User),
            RoleId = item.RoleId
        }).ToList());
    }
}
