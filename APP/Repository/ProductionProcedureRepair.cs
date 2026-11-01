using DOMAIN.Entities.Base;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

/// <summary>
/// Re-applies a product's current procedure to one running batch. A batch copies its steps from the
/// procedure when it starts, so a procedure corrected later does not reach it. This brings the active
/// step and the steps still to do in line with the procedure as it is now: operation, workflow,
/// criticality, work centres, resources, and the people responsible with their instruction and
/// analytical raw data. Completed steps, and anything before them, are never changed.
/// </summary>
internal static class ProductionProcedureRepair
{
    internal static async Task<Result<ProductionProcedureRepairResult>> RepairAsync(
        ApplicationDbContext context, UserManager<User> userManager, Guid activityId, Guid userId)
    {
        var activity = await context.ProductionActivities.AsSplitQuery()
            .Include(item => item.ProductionScheduleProduct)
            .Include(item => item.Steps).ThenInclude(step => step.ResponsibleUsers)
            .Include(item => item.Steps).ThenInclude(step => step.WorkCenters)
            .Include(item => item.Steps).ThenInclude(step => step.Resources)
            .Include(item => item.Steps).ThenInclude(step => step.Operation)
            .FirstOrDefaultAsync(item => item.Id == activityId);
        if (activity is null)
            return Error.NotFound("ProductionActivity.NotFound", "Production activity not found.");
        if (activity.Status is ProductionStatus.Completed or ProductionStatus.Cancelled)
            return Error.Validation("ProductionActivity.Finished", "This production run has finished, so its procedure cannot be repaired.");

        var product = await ProcedureAssignments.LoadProductAsync(context, activity.ProductionScheduleProduct.ProductId);
        if (product is null) return Error.NotFound("Product.NotFound", "The product of this batch was not found.");
        if (product.Routes.Count == 0)
            return Error.Validation("Product.Validation", "The product has no procedure to repair from.");
        var assignments = await ProcedureAssignments.BuildAsync(context, userManager, product);

        // Only the active step and those after it are repaired; a completed step is never touched,
        // and nothing is placed before the last completed step.
        var steps = activity.Steps.OrderBy(step => step.Order).ToList();
        var done = steps.Where(step => step.Status is ProductionStatus.Completed or ProductionStatus.Cancelled).ToList();
        var floor = done.Count == 0 ? int.MinValue : done.Max(step => step.Order) + 1;
        var open = steps.Where(step => step.Status is not (ProductionStatus.Completed or ProductionStatus.Cancelled) && step.Order >= floor)
            .ToDictionary(step => step.Order);
        var routes = product.Routes.Where(route => route.Order >= floor).OrderBy(route => route.Order).ToList();
        if (open.Count == 0 && routes.Count == 0)
            return Error.Validation("ProductionActivity.NothingToRepair", "Every step of this batch is completed, so there is nothing to repair.");

        var result = new ProductionProcedureRepairResult();
        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            foreach (var route in routes)
            {
                var people = assignments.Where(assignment => assignment.Order == route.Order).ToList();
                if (open.Remove(route.Order, out var step))
                {
                    step.OperationId = route.OperationId;
                    step.WorkflowId = route.WorkflowId;
                    step.IsCritical = route.IsCritical;
                    await ReplaceLinksAsync(context, step, route);
                    ReplacePeople(context, step, people);
                    result.StepsUpdated++;
                }
                else if (steps.All(existing => existing.Order != route.Order))
                {
                    activity.Steps.Add(new ProductionActivityStep
                    {
                        ProductionActivityId = activity.Id,
                        OperationId = route.OperationId,
                        WorkflowId = route.WorkflowId,
                        Order = route.Order,
                        IsCritical = route.IsCritical,
                        Status = ProductionStatus.New,
                        Resources = route.Resources.Select(item => new ProductionActivityStepResource { ResourceId = item.ResourceId }).ToList(),
                        WorkCenters = route.WorkCenters.Select(item => new ProductionActivityStepWorkCenter { WorkCenterId = item.WorkCenterId }).ToList(),
                        ResponsibleUsers = people.Select(person => new ProductionActivityStepUser
                        {
                            UserId = person.UserId, ProductAnalyticalRawDataId = person.ArdId, Action = person.Action,
                        }).ToList(),
                    });
                    result.StepsAdded++;
                }
            }

            // Steps the procedure no longer has: one not started yet goes; one already started stays.
            foreach (var leftover in open.Values)
            {
                if (leftover.Status == ProductionStatus.New)
                {
                    // Removed in the database directly; the tracked rows are let go so saving does not touch them again.
                    foreach (var child in leftover.ResponsibleUsers.Cast<object>().Concat(leftover.WorkCenters).Concat(leftover.Resources).ToList())
                        context.Entry(child).State = EntityState.Detached;
                    context.Entry(leftover).State = EntityState.Detached;
                    await context.ProductionActivityStepUsers.IgnoreQueryFilters().Where(item => item.ProductionActivityStepId == leftover.Id).ExecuteDeleteAsync();
                    await context.ProductionActivityStepWorkCenters.IgnoreQueryFilters().Where(item => item.ProductionActivityStepId == leftover.Id).ExecuteDeleteAsync();
                    await context.ProductionActivityStepResources.IgnoreQueryFilters().Where(item => item.ProductionActivityStepId == leftover.Id).ExecuteDeleteAsync();
                    await context.ProductionActivitySteps.IgnoreQueryFilters().Where(item => item.Id == leftover.Id).ExecuteDeleteAsync();
                    result.StepsRemoved++;
                }
                else
                {
                    result.StepsKept.Add($"{leftover.Order}. {leftover.Operation?.Name ?? "Step"}");
                }
            }

            context.ProductionActivityLogs.Add(new ProductionActivityLog
            {
                ProductionActivityId = activity.Id,
                UserId = userId,
                Message = $"Procedure repaired from the product's current procedure: {result.StepsUpdated} step(s) updated, "
                    + $"{result.StepsAdded} added, {result.StepsRemoved} removed. Completed steps were not changed.",
                Timestamp = DateTime.UtcNow,
            });
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
            return result;
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync();
            return Error.Conflict("ProductionActivity.RepairFailed",
                "A step the procedure no longer has is already used by a record, so the procedure could not be repaired. Nothing was changed.");
        }
        catch (Npgsql.PostgresException)
        {
            await transaction.RollbackAsync();
            return Error.Conflict("ProductionActivity.RepairFailed",
                "A step the procedure no longer has is already used by a record, so the procedure could not be repaired. Nothing was changed.");
        }
    }

    /// <summary>Work centres and resources are plain links, so they are replaced outright.</summary>
    private static async Task ReplaceLinksAsync(ApplicationDbContext context, ProductionActivityStep step, DOMAIN.Entities.Routes.Route route)
    {
        var workCenters = route.WorkCenters.Select(item => item.WorkCenterId).ToHashSet();
        var resources = route.Resources.Select(item => item.ResourceId).ToHashSet();
        await context.ProductionActivityStepWorkCenters.IgnoreQueryFilters()
            .Where(item => item.ProductionActivityStepId == step.Id && !workCenters.Contains(item.WorkCenterId)).ExecuteDeleteAsync();
        await context.ProductionActivityStepResources.IgnoreQueryFilters()
            .Where(item => item.ProductionActivityStepId == step.Id && !resources.Contains(item.ResourceId)).ExecuteDeleteAsync();
        var haveWorkCenters = step.WorkCenters.Select(item => item.WorkCenterId).ToHashSet();
        var haveResources = step.Resources.Select(item => item.ResourceId).ToHashSet();
        foreach (var stale in step.WorkCenters.Where(item => !workCenters.Contains(item.WorkCenterId)).ToList())
        { step.WorkCenters.Remove(stale); context.Entry(stale).State = EntityState.Detached; }
        foreach (var stale in step.Resources.Where(item => !resources.Contains(item.ResourceId)).ToList())
        { step.Resources.Remove(stale); context.Entry(stale).State = EntityState.Detached; }
        foreach (var id in workCenters.Where(id => !haveWorkCenters.Contains(id)))
            step.WorkCenters.Add(new ProductionActivityStepWorkCenter { ProductionActivityStepId = step.Id, WorkCenterId = id });
        foreach (var id in resources.Where(id => !haveResources.Contains(id)))
            step.Resources.Add(new ProductionActivityStepResource { ProductionActivityStepId = step.Id, ResourceId = id });
    }

    /// <summary>
    /// The people on the step follow the procedure: those who stay keep their row with the procedure's
    /// instruction and analytical raw data, those no longer on it are removed (kept as history), and new
    /// ones are added.
    /// </summary>
    private static void ReplacePeople(ApplicationDbContext context, ProductionActivityStep step, List<ProcedureAssignments.Assignment> people)
    {
        var wanted = people.ToDictionary(person => person.UserId);
        foreach (var member in step.ResponsibleUsers.ToList())
        {
            if (wanted.Remove(member.UserId, out var person))
            {
                member.Action = person.Action;
                member.ProductAnalyticalRawDataId = person.ArdId;
            }
            else
            {
                context.ProductionActivityStepUsers.Remove(member);
            }
        }
        foreach (var person in wanted.Values)
            step.ResponsibleUsers.Add(new ProductionActivityStepUser
            {
                ProductionActivityStepId = step.Id, UserId = person.UserId, ProductAnalyticalRawDataId = person.ArdId, Action = person.Action,
            });
    }
}
