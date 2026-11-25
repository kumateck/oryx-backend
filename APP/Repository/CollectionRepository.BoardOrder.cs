using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using APP.Services.Authorization;
using APP.Services.Transactions;
using APP.Utils;
using DOMAIN.Entities.Base;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public partial class CollectionRepository
{
    public async Task<Result<ProductionBoardOrderDto>> GetProductionBoardOrder(Guid departmentId, Guid actor)
    {
        if (!(await CurrentPermissionSet.Load(context, actor)).Contains(PermissionKeys.CanViewDepartmentProductionBoardOrder)) return BoardOrderDenied();
        var department = await context.Departments.AsNoTracking().SingleOrDefaultAsync(row => row.Id == departmentId && row.Type == DepartmentType.Production);
        if (department is null) return Error.NotFound("BoardOrder.Department", "Choose an active production department.");
        var operations = await context.Operations.AsNoTracking().Where(row => row.DepartmentId == departmentId)
            .OrderBy(row => row.Order).ThenBy(row => row.Name).ThenBy(row => row.Id).ToListAsync();
        return new ProductionBoardOrderDto(department.Id, department.Name, BoardOrderVersion(departmentId, operations),
            operations.Select(row => new ProductionBoardOperationDto(row.Id, row.Name, row.Order)).ToList());
    }

    public Task<Result> SaveProductionBoardOrder(Guid departmentId, SaveProductionBoardOrder request, Guid actor) =>
        ReferenceMaintenanceTransaction.Run(context, async () =>
        {
            async Task<bool> Allowed() => (await CurrentPermissionSet.Load(context, actor)).Contains(PermissionKeys.CanReorderDepartmentProductionBoard);
            if (request is null || request.OperationIds is null || request.Version?.Length != 64 || request.OperationIds.Distinct().Count() != request.OperationIds.Count)
                return Error.Validation("BoardOrder.Request", "Send each operation once, with the version of the reviewed department order.");
            // Lock before the first SELECT establishes the serializable snapshot. A link
            // committed while waiting for the lock must be included in the reviewed set.
            if (context.Database.IsNpgsql())
                await context.Database.ExecuteSqlRawAsync("LOCK TABLE \"Operations\", \"Departments\" IN SHARE ROW EXCLUSIVE MODE");
            if (!await Allowed()) return BoardOrderDenied();
            if (!await context.Departments.AnyAsync(row => row.Id == departmentId && row.Type == DepartmentType.Production))
                return Error.NotFound("BoardOrder.Department", "Choose an active production department.");
            var operations = await context.Operations.Where(row => row.DepartmentId == departmentId).ToListAsync();
            if (request.Version != BoardOrderVersion(departmentId, operations))
                return Error.Conflict("BoardOrder.Changed", "The department's operations or order changed. Reload and review the current list.");
            if (operations.Count != request.OperationIds.Count || !operations.Select(row => row.Id).ToHashSet().SetEquals(request.OperationIds))
                return Error.Validation("BoardOrder.Operations", "Include every currently linked operation in this department, and no operations from another department.");
            if (!await Allowed()) return BoardOrderDenied();
            var positions = request.OperationIds.Select((id, index) => (id, order: index + 1)).ToDictionary(row => row.id, row => row.order);
            foreach (var operation in operations) { operation.Order = positions[operation.Id]; operation.LastUpdatedById = actor; }
            // Only board-column order changes. Product routes and recorded activity-step order are untouched.
            await context.SaveChangesAsync();
            return Result.Success();
        });

    private static string BoardOrderVersion(Guid departmentId, IEnumerable<Operation> operations) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
        {
            departmentId,
            Operations = operations.OrderBy(row => row.Id).Select(row => new { row.Id, row.Name, row.Order, row.UpdatedAt, row.DepartmentId })
        }))));
    private static Error BoardOrderDenied() => Error.Forbidden("BoardOrder.Permission", "Your current permissions do not allow this board-order task.");
}
