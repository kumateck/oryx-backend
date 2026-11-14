using System.Globalization;
using DOMAIN.Entities.BillOfMaterials.Request;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.Routes;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;

namespace APP.Services.Authorization;

/// <summary>
/// What a bulk section save changes, worked out by comparing the submitted rows with the stored ones. A row is matched by
/// what identifies it (the material, the operation, the packing style); a new row needs Add, a missing one Delete and a
/// different one Edit. A save that changes nothing that is compared still counts as an edit, so no write needs no key.
/// Stored rows are read past the global query filters (only deleted rows are left out), so a row whose material is
/// hidden by a filter is still compared rather than mistaken for a new one.
/// </summary>
public static class ProductSectionChanges
{
    private static string Number(decimal value) => value.ToString("0.############", CultureInfo.InvariantCulture);
    private static string Ids(IEnumerable<Guid> ids) => string.Join(",", ids.Order());

    public static HashSet<ProductVerb> Compare(IEnumerable<(string Key, string Fingerprint)> stored, IEnumerable<(string Key, string Fingerprint)> submitted)
    {
        static Dictionary<string, string> Rows(IEnumerable<(string Key, string Fingerprint)> rows) => rows.GroupBy(row => row.Key)
            .ToDictionary(group => group.Key, group => string.Join("|", group.Select(row => row.Fingerprint).Order(StringComparer.Ordinal)));
        var before = Rows(stored);
        var after = Rows(submitted);
        var verbs = new HashSet<ProductVerb>();
        if (after.Keys.Any(key => !before.ContainsKey(key))) verbs.Add(ProductVerb.Add);
        if (before.Keys.Any(key => !after.ContainsKey(key))) verbs.Add(ProductVerb.Delete);
        if (after.Any(row => before.TryGetValue(row.Key, out var old) && old != row.Value)) verbs.Add(ProductVerb.Edit);
        if (verbs.Count == 0) verbs.Add(ProductVerb.Edit);
        return verbs;
    }

    public static async Task<HashSet<ProductVerb>> Bom(ApplicationDbContext db, CreateBillOfMaterialRequest request)
    {
        var stored = await db.ProductBillOfMaterials.IgnoreQueryFilters().AsNoTracking()
            .Where(link => link.ProductId == request.ProductId && link.IsActive && link.DeletedAt == null)
            .SelectMany(link => link.BillOfMaterial.Items.Where(item => item.DeletedAt == null))
            .Select(item => new { item.MaterialId, item.BaseQuantity, item.Grade, item.Order, item.IsSubstitutable,
                Substitutes = item.Substitutes.Where(substitute => substitute.DeletedAt == null)
                    .Select(substitute => new { substitute.SubstituteMaterialId, substitute.BaseQuantity }).ToList() })
            .ToListAsync();
        // A substitute's own quantity is part of the line: changing it is an edit.
        static string Substitutes(IEnumerable<(Guid Id, decimal? Quantity)> rows) =>
            string.Join(",", rows.OrderBy(row => row.Id).Select(row => $"{row.Id}={(row.Quantity.HasValue ? Number(row.Quantity.Value) : "")}"));
        return Compare(
            stored.Select(item => (item.MaterialId.ToString(), $"{Number(item.BaseQuantity)};{item.Grade};{item.Order};{item.IsSubstitutable};"
                + Substitutes(item.Substitutes.Select(substitute => (substitute.SubstituteMaterialId, substitute.BaseQuantity))))),
            (request.Items ?? []).Select(item => (item.MaterialId.ToString(),
                $"{Number(item.BaseQuantity)};{item.Grade};{item.Order};{item.IsSubstitutable};"
                + Substitutes(item.Substitutes is { Count: > 0 }
                    ? item.Substitutes.Select(substitute => (substitute.MaterialId, (decimal?)substitute.BaseQuantity))
                    : (item.SubstituteMaterialIds ?? []).Select(id => (id, (decimal?)null))))));
    }

    public static async Task<HashSet<ProductVerb>> PackingMaterials(ApplicationDbContext db, Guid productId, IReadOnlyCollection<CreateProductPackageRequest> rows)
    {
        var stored = await db.ProductPackages.IgnoreQueryFilters().AsNoTracking().Where(package => package.ProductId == productId && package.DeletedAt == null)
            .Select(package => new { package.MaterialId, package.ProductPackingId, package.BaseQuantity, package.UnitCapacity, package.DirectLinkMaterialId,
                package.PackingExcessMargin, package.Loose, package.MaterialThickness, package.OtherStandards,
                Substitutes = package.Substitutes.Select(substitute => substitute.SubstituteMaterialId).ToList() })
            .ToListAsync();
        return Compare(
            stored.Select(row => ($"{row.MaterialId}:{row.ProductPackingId}",
                $"{Number(row.BaseQuantity)};{Number(row.UnitCapacity)};{row.DirectLinkMaterialId};{Number(row.PackingExcessMargin)};{Number(row.Loose)};{row.MaterialThickness};{row.OtherStandards};{Ids(row.Substitutes)}")),
            (rows ?? []).Select(row => ($"{row.MaterialId}:{row.ProductPackingId}",
                $"{Number(row.BaseQuantity)};{Number(row.UnitCapacity)};{row.DirectLinkMaterialId};{Number(row.PackingExcessMargin)};{Number(row.Loose)};{row.MaterialThickness};{row.OtherStandards};{Ids(row.SubstituteMaterialIds ?? [])}")));
    }

    public static async Task<HashSet<ProductVerb>> Procedure(ApplicationDbContext db, Guid productId, IReadOnlyCollection<CreateRouteRequest> rows)
    {
        var stored = await db.Routes.IgnoreQueryFilters().AsNoTracking().Where(route => route.ProductId == productId && route.DeletedAt == null)
            .Select(route => new { route.OperationId, route.Order, route.EstimatedTime, route.IsCritical,
                Resources = route.Resources.Select(resource => resource.ResourceId).ToList(),
                Users = route.ResponsibleUsers.Select(user => user.UserId).ToList() })
            .ToListAsync();
        return Compare(
            stored.Select(route => (route.OperationId.ToString(), $"{route.Order};{route.EstimatedTime};{route.IsCritical};{Ids(route.Resources)};{Ids(route.Users)}")),
            (rows ?? []).Select(route => (route.OperationId.ToString(),
                $"{route.Order};{route.EstimatedTime};{route.IsCritical};{Ids((route.Resources ?? []).Select(resource => resource.ResourceId))};{Ids((route.ResponsibleUsers ?? []).Select(user => user.UserId))}")));
    }

    public static async Task<HashSet<ProductVerb>> PackingStyles(ApplicationDbContext db, Guid productId, IReadOnlyCollection<CreateProductPacking> rows)
    {
        var stored = await db.ProductPackings.IgnoreQueryFilters().AsNoTracking().Where(style => style.ProductId == productId && style.DeletedAt == null)
            .Select(style => new { style.Id, style.Name, style.Description, style.PackPerShipper, style.BasePackingQuantity, style.ExpectedYield, style.IsDefault,
                Lines = style.PackingLists.Count })
            .ToListAsync();
        // A style without a stored id is new.
        return Compare(
            stored.Select(style => (style.Id.ToString(), $"{style.Name};{style.Description};{style.PackPerShipper};{Number(style.BasePackingQuantity)};{Number(style.ExpectedYield)};{style.IsDefault};{style.Lines}")),
            (rows ?? []).Select(style => (style.Id is { } id && id != Guid.Empty ? id.ToString() : $"new:{Guid.NewGuid()}",
                $"{style.Name};{style.Description};{style.PackPerShipper};{Number(style.BasePackingQuantity)};{Number(style.ExpectedYield)};{style.IsDefault};{(style.PackingLists ?? []).Count}")));
    }
}
