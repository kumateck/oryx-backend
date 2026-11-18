using DOMAIN.Entities.Materials;

namespace DOMAIN.Entities.Products.Production;

/// <summary>A page of batches whose production has started but whose BMR is not yet issued.</summary>
public sealed record UpcomingBatchIssuePage(List<UpcomingBatchIssue> Data, int TotalRecordCount, int PageIndex, int PageCount);

/// <summary>A started batch the warehouse will issue materials for once its BMR is issued, with what was reserved for it.</summary>
public sealed record UpcomingBatchIssue(Guid BmrId, Guid ProductionScheduleProductId, string ProductCode, string ProductName,
    string ScheduleCode, DateTime StartedAt, string BatchNumber, decimal BatchQuantity, string BatchUnit, List<UpcomingBatchMaterial> Materials);

/// <summary>One reserved lot on one shelf: what the warehouse picks for the batch.</summary>
public sealed record UpcomingBatchMaterial(Guid MaterialId, string MaterialCode, string MaterialName, MaterialKind Kind,
    string LotNumber, string Warehouse, string Location, decimal Quantity, string Unit);
