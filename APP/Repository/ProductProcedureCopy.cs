using DOMAIN.Entities.Routes;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

/// <summary>Copies instructions using each destination product's own ARD at the same stage and analysis type.</summary>
internal static class ProductProcedureCopy
{
    internal static async Task<Result<List<CreateRouteRequest>>> Resolve(ApplicationDbContext db, Guid productId, List<CreateRouteRequest> rows)
    {
        var atrIds = rows.SelectMany(row => row.ResponsibleUsers.Where(p => p.Action == OperationAction.Atr).Select(p => p.ProductAnalyticalRawDataId)
            .Concat(row.ResponsibleRoles.Where(p => p.Action == OperationAction.Atr).Select(p => p.ProductAnalyticalRawDataId))).ToList();
        if (atrIds.Any(id => !id.HasValue || id == Guid.Empty))
            return Error.Validation("Route.AtrStage", "Select analytical raw data and its ATR stage for every ATR instruction before copying or saving the procedure.");
        var ids = atrIds.Select(id => id.Value).Distinct().ToList();
        var documents = await db.ProductAnalyticalRawData.IgnoreAutoIncludes().AsNoTracking()
            .Where(ard => ard.DeletedAt == null && ard.ProductStandardTestProcedure.DeletedAt == null
                && (ids.Contains(ard.Id) || ard.ProductStandardTestProcedure.ProductId == productId))
            .Select(ard => new { ard.Id, ard.Stage, ard.AnalysisType, ard.ProductStandardTestProcedure.ProductId }).ToListAsync();
        var mapped = new Dictionary<Guid, Guid>();
        foreach (var sourceId in ids)
        {
            var source = documents.SingleOrDefault(ard => ard.Id == sourceId);
            if (source is null) return Error.Validation("Route.AtrDocument", "An ATR quality document is unavailable. Select its current analytical raw data before copying or saving.");
            if (source.ProductId == productId) { mapped[sourceId] = sourceId; continue; }
            var matches = documents.Where(ard => ard.ProductId == productId && ard.Stage == source.Stage && ard.AnalysisType == source.AnalysisType).ToList();
            if (matches.Count != 1)
                return Error.Validation("Route.AtrStage", matches.Count == 0
                    ? $"The target product has no {source.Stage} analytical raw data for {source.AnalysisType}. Configure that ATR stage before copying; its existing procedure was kept."
                    : $"The target product has multiple {source.Stage} analytical raw data documents for {source.AnalysisType}. Resolve the duplicate stage configuration before copying; its existing procedure was kept.");
            mapped[sourceId] = matches[0].Id;
        }
        Guid? Ard(OperationAction action, Guid? id) => action == OperationAction.Atr && id.HasValue ? mapped[id.Value] : null;
        return rows.Select(row => new CreateRouteRequest {
            OperationId = row.OperationId, EstimatedTime = row.EstimatedTime, Order = row.Order, IsCritical = row.IsCritical,
            Resources = row.Resources.Select(r => new CreateRouteResource { ResourceId = r.ResourceId }).ToList(),
            WorkCenters = row.WorkCenters.Select(w => new CreateRouteWorkCenter { WorkCenterId = w.WorkCenterId }).ToList(),
            ResponsibleUsers = row.ResponsibleUsers.Select(p => new CreateRouteResponsibleUser { UserId = p.UserId, Action = p.Action, ProductAnalyticalRawDataId = Ard(p.Action, p.ProductAnalyticalRawDataId) }).ToList(),
            ResponsibleRoles = row.ResponsibleRoles.Select(p => new CreateRouteResponsibleRole { RoleId = p.RoleId, Action = p.Action, ProductAnalyticalRawDataId = Ard(p.Action, p.ProductAnalyticalRawDataId) }).ToList(),
        }).ToList();
    }
}
