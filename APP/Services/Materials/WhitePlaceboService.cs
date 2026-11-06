using APP.IRepository;
using APP.Services.Authorization;
using APP.Utils;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Materials.WhitePlacebo;
using DOMAIN.Entities.Requisitions;
using DOMAIN.Entities.Warehouses;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;
using SHARED.Services.Identity;

namespace APP.Services.Materials;

public sealed partial class WhitePlaceboService(ApplicationDbContext context,
    ICurrentUserService currentUser, IApprovalRepository approvals)
{
    private const string View = PermissionKeys.CanViewWhitePlacebo;

    private async Task<Result<Guid>> Department(params string[] permissions)
    {
        var actor = await context.Users.IgnoreQueryFilters().IgnoreAutoIncludes()
            .Where(item => item.Id == currentUser.UserId && item.DeletedAt == null && !item.IsDisabled)
            .Select(item => new { item.DepartmentId, Type = (DepartmentType?)item.Department.Type }).SingleOrDefaultAsync();
        if (actor?.DepartmentId is not { } department || actor.Type != DepartmentType.Production)
            return Error.Forbidden("WhitePlacebo.Department", "Select your production department to work with White Placebo.");
        var grants = await CurrentPermissionSet.Load(context, currentUser.UserId);
        if (!grants.Contains(View) || permissions.Any(permission => !grants.Contains(permission)))
            return Error.Forbidden("WhitePlacebo.Permission", "Your current permissions do not allow this White Placebo action.");
        return department;
    }

    private IQueryable<WhitePlaceboRecipe> Recipes(Guid department) => context.WhitePlaceboRecipes
        .Where(item => item.DepartmentId == department).Include(item => item.Material)
        .Include(item => item.UoM).Include(item => item.Ingredients).ThenInclude(item => item.Material)
        .Include(item => item.Ingredients).ThenInclude(item => item.UoM).AsSplitQuery();

    private IQueryable<ShelfMaterialBatch> Shelves(Guid department) => context.ShelfMaterialBatches
        .Include(item => item.MaterialBatch).ThenInclude(item => item.Material)
        .Include(item => item.MaterialBatch).ThenInclude(item => item.ReservedQuantities)
        .Include(item => item.WarehouseLocationShelf).ThenInclude(item => item.WarehouseLocationRack)
        .ThenInclude(item => item.WarehouseLocation).ThenInclude(item => item.Warehouse)
        .Where(item => item.WarehouseLocationShelf.WarehouseLocationRack.WarehouseLocation.Warehouse.DepartmentId == department
            && item.WarehouseLocationShelf.WarehouseLocationRack.WarehouseLocation.Warehouse.Type == WarehouseType.RawMaterialStorage
            && item.MaterialBatch.Material.Kind == MaterialKind.Raw && !item.MaterialBatch.Material.IsUnlimited)
        .AsSplitQuery();

    public async Task<Result<WhitePlaceboWorkspaceDto>> Workspace()
    {
        var scope = await Department();
        if (scope.IsFailure) return scope.Errors;
        var materials = await context.MaterialDepartments.Where(item => item.DepartmentId == scope.Value
            && item.Material.Kind == MaterialKind.Raw && !item.Material.IsUnlimited)
            .Select(item => new WhitePlaceboMaterialDto(item.MaterialId, item.Material.Name,
                item.Material.Code, item.UoMId, item.UoM.Symbol, item.Material.IsWhitePlacebo)).ToListAsync();
        var recipe = await Recipes(scope.Value).SingleOrDefaultAsync();
        var now = DateTime.UtcNow;
        var stocks = await Shelves(scope.Value).Where(item => item.Quantity > 0
            && item.MaterialBatch.Status == BatchStatus.Available
            && !item.MaterialBatch.Material.IsWhitePlacebo
            )
            .WhereMaterialDate(item => item.MaterialBatch, date => date == null || date == DateTime.MinValue || date > now)
            .OrderBy(item => item.MaterialBatch.ExpiryDate).ThenBy(item => item.Id).ToListAsync();
        var units = materials.ToDictionary(item => item.Id);
        var stock = stocks.Where(item => units.ContainsKey(item.MaterialBatch.MaterialId)
            && units[item.MaterialBatch.MaterialId].UoMId == item.MaterialBatch.UoMId)
            .Select(item => new WhitePlaceboStockDto(item.Id, item.MaterialBatch.MaterialId,
                item.MaterialBatch.Material.Name, item.MaterialBatch.BatchNumber,
                item.WarehouseLocationShelf.Name, units[item.MaterialBatch.MaterialId].UnitSymbol,
                Math.Max(0, Math.Min(item.Quantity, item.MaterialBatch.RemainingQuantity)), item.MaterialBatch.ExpiryDate,
                item.WarehouseLocationShelf.WarehouseLocationRack.Name))
            .Where(item => item.Quantity > 0).ToList();
        return new WhitePlaceboWorkspaceDto(recipe is null ? null : RecipeDto(recipe), materials, stock);
    }

    private static WhitePlaceboRecipeDto RecipeDto(WhitePlaceboRecipe recipe) => new(
        recipe.MaterialId, recipe.Material.Name, recipe.UoM.Symbol, recipe.Revision, recipe.Reference,
        recipe.Instructions, recipe.StorageConditions, recipe.HoldHours, recipe.ExpectedQuantity,
        recipe.Ingredients.OrderBy(item => item.CreatedAt).ThenBy(item => item.Id).Select(item => new WhitePlaceboIngredientDto(
            item.MaterialId, item.Material.Name, item.Material.Code, item.UoMId, item.UoM.Symbol, item.Quantity)).ToList());

    public async Task<Result> SaveRecipe(SaveWhitePlaceboRecipe body)
    {
        var scope = await Department(PermissionKeys.CanEditWhitePlaceboRecipe);
        if (scope.IsFailure) return scope.Errors;
        if (body == null || body.ExpectedQuantity <= 0 || body.ExpectedQuantity > 1_000_000_000_000m
            || string.IsNullOrWhiteSpace(body.Reference) || string.IsNullOrWhiteSpace(body.Instructions)
            || string.IsNullOrWhiteSpace(body.StorageConditions) || body.HoldHours is < 1 or > 8760
            || body.Ingredients == null || body.Ingredients.Count is < 1 or > 100
            || body.Ingredients.Any(item => item.MaterialId == body.MaterialId || item.Quantity <= 0 || item.Quantity > 1_000_000_000_000m)
            || body.Ingredients.Select(item => item.MaterialId).Distinct().Count() != body.Ingredients.Count)
            return Error.Validation("WhitePlacebo.Recipe", "Enter an approved recipe reference, instructions, storage, holding hours and positive, distinct ingredient quantities.");
        await using var transaction = context.Database.IsRelational()
            ? await context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable) : null;
        var recipe = await Recipes(scope.Value).SingleOrDefaultAsync();
        if ((recipe?.Revision ?? 0) != body.Revision)
            return Error.Conflict("WhitePlacebo.RecipeChanged", "The recipe changed. Reload before saving.");
        if (recipe != null && recipe.MaterialId != body.MaterialId)
            return Error.Validation("WhitePlacebo.MaterialFixed", "This department's White Placebo material is already configured and cannot be replaced.");
        var ids = body.Ingredients.Select(item => item.MaterialId).Append(body.MaterialId).ToArray();
        var linked = await context.MaterialDepartments.Include(item => item.Material).Include(item => item.UoM)
            .Where(item => item.DepartmentId == scope.Value && ids.Contains(item.MaterialId)).ToListAsync();
        if (linked.Count != ids.Length || linked.Any(item => item.Material.Kind != MaterialKind.Raw || item.Material.IsUnlimited
            || item.UoM.Category != DOMAIN.Entities.Base.UnitOfMeasureCategory.Weight)
            || linked.Any(item => item.MaterialId != body.MaterialId && item.Material.IsWhitePlacebo))
            return Error.Validation("WhitePlacebo.Materials", "Link each material to your department with a weight unit. Ingredients must be ordinary, finite raw materials.");
        var output = linked.Single(item => item.MaterialId == body.MaterialId);
        if (await context.RequisitionItems.AnyAsync(item => item.MaterialId == body.MaterialId
            && item.Requisition.RequisitionType == RequisitionType.Purchase && item.Requisition.Status != RequestStatus.Rejected))
            return Error.Conflict("WhitePlacebo.Purchases", "This material has purchase history or an active purchase request. Use a dedicated White Placebo material.");
        if (recipe == null)
        {
            if (await context.MaterialBatches.AnyAsync(item => item.MaterialId == body.MaterialId))
                return Error.Conflict("WhitePlacebo.ExistingStock", "Choose a dedicated material without existing batches so all White Placebo stock has preparation evidence.");
            recipe = new WhitePlaceboRecipe { Id = Guid.NewGuid(), DepartmentId = scope.Value, MaterialId = output.MaterialId };
            context.WhitePlaceboRecipes.Add(recipe);
        }
        else context.RemoveRange(recipe.Ingredients);
        recipe.UoMId = output.UoMId;
        recipe.Revision++;
        recipe.Reference = body.Reference.Trim();
        recipe.Instructions = body.Instructions.Trim();
        recipe.StorageConditions = body.StorageConditions.Trim();
        recipe.HoldHours = body.HoldHours;
        recipe.ExpectedQuantity = body.ExpectedQuantity;
        recipe.Ingredients = body.Ingredients.Select(item => new WhitePlaceboRecipeIngredient {
            Id = Guid.NewGuid(), WhitePlaceboRecipeId = recipe.Id, MaterialId = item.MaterialId, UoMId = linked.Single(link => link.MaterialId == item.MaterialId).UoMId,
            Quantity = item.Quantity, CreatedById = currentUser.UserId }).ToList();
        context.AddRange(recipe.Ingredients);
        output.Material.IsWhitePlacebo = true;
        recipe.LastUpdatedById = currentUser.UserId;
        var recheck = await Department(PermissionKeys.CanEditWhitePlaceboRecipe);
        if (recheck.IsFailure || recheck.Value != scope.Value)
            return Error.Forbidden("WhitePlacebo.Permission", "Your permissions changed before saving.");
        await context.SaveChangesAsync();
        if (transaction != null) await transaction.CommitAsync();
        return Result.Success();
    }
}
