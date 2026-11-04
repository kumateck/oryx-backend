using DOMAIN.Entities.Base;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

internal sealed record ProcedureRepairInspection(ProductionProcedureRepairResult Summary, string Snapshot);

internal static partial class ProductionProcedureRepair
{
    internal static async Task<Result<ProcedureRepairInspection>> Inspect(ApplicationDbContext db,
        UserManager<User> users, Guid activityId)
    {
        var activity = await db.ProductionActivities.AsSplitQuery().AsNoTracking()
            .Include(item => item.ProductionScheduleProduct)
            .Include(item => item.Steps).ThenInclude(step => step.ResponsibleUsers)
            .Include(item => item.Steps).ThenInclude(step => step.WorkCenters)
            .Include(item => item.Steps).ThenInclude(step => step.Resources)
            .Include(item => item.Steps).ThenInclude(step => step.Operation).SingleOrDefaultAsync(item => item.Id == activityId);
        if (activity is null || activity.Status is ProductionStatus.Completed or ProductionStatus.Cancelled)
            return Error.Conflict("Repair.Procedure", "The production run must be open.");
        var product = await ProcedureAssignments.LoadProductAsync(db, activity.ProductionScheduleProduct.ProductId);
        if (product is null || product.Routes.Count == 0)
            return Error.Conflict("Repair.Procedure", "The product has no saved procedure.");
        var steps = activity.Steps.OrderBy(step => step.Order).ToList();
        if (steps.GroupBy(step => step.Order).Any(group => group.Count() != 1)
            || product.Routes.GroupBy(route => route.Order).Any(group => group.Count() != 1))
            return Error.Conflict("Repair.Procedure", "Duplicate step positions must be reconciled before repairing.");
        var completed = steps.Where(step => step.Status is ProductionStatus.Completed or ProductionStatus.Cancelled).ToList();
        var floor = completed.Count == 0 ? int.MinValue : completed.Max(step => step.Order) + 1;
        var open = steps.Where(step => step.Order >= floor && step.Status is not (ProductionStatus.Completed or ProductionStatus.Cancelled)).ToList();
        var routes = product.Routes.Where(route => route.Order >= floor).OrderBy(route => route.Order).ToList();
        if (open.Count == 0 && routes.Count == 0)
            return Error.Conflict("Repair.Procedure", "Every step is completed; there is nothing to repair.");
        var assignments = await ProcedureAssignments.BuildAsync(db, users, product);
        var summary = new ProductionProcedureRepairResult
        {
            StepsUpdated = routes.Count(route => open.Any(step => step.Order == route.Order)),
            StepsAdded = routes.Count(route => steps.All(step => step.Order != route.Order)),
            StepsRemoved = open.Count(step => step.Status == ProductionStatus.New && routes.All(route => route.Order != step.Order)),
            StepsKept = open.Where(step => step.Status != ProductionStatus.New && routes.All(route => route.Order != step.Order))
                .Select(step => $"{step.Order}. {step.Operation?.Name ?? "Step"}").ToList(),
        };
        var snapshot = ProductionMaterialSources.Json(new
        {
            activity.Id, activity.Status, activity.UpdatedAt,
            Steps = steps.Select(step => new { step.Id, step.Order, step.Status, step.OperationId, step.WorkflowId, step.IsCritical,
                step.StartedAt, step.CompletedAt, step.UpdatedAt,
                Resources = step.Resources.Select(item => item.ResourceId).Order(),
                WorkCenters = step.WorkCenters.Select(item => item.WorkCenterId).Order(),
                People = step.ResponsibleUsers.OrderBy(item => item.UserId).Select(item => new { item.UserId, item.Action, item.ProductAnalyticalRawDataId }) }),
            Routes = product.Routes.OrderBy(route => route.Order).Select(route => new { route.Id, route.Order, route.OperationId, route.WorkflowId,
                route.IsCritical, Resources = route.Resources.Select(item => item.ResourceId).Order(),
                WorkCenters = route.WorkCenters.Select(item => item.WorkCenterId).Order() }),
            People = assignments.OrderBy(item => item.Order).ThenBy(item => item.UserId).Select(item => new { item.UserId, item.Order, item.ArdId, item.Action }),
        });
        return new ProcedureRepairInspection(summary, snapshot);
    }
}
