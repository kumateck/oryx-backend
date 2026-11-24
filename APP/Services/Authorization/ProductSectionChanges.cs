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
    private static string Number(decimal value) => value.ToString("G29", CultureInfo.InvariantCulture);
    private static string Ids(IEnumerable<Guid> ids) => string.Join(",", ids.Order());

    // A substitute's own quantity is part of the line: changing it is an edit.
    private static string Substitutes(IEnumerable<(Guid Id, decimal? Quantity)> rows) =>
        string.Join(",", rows.OrderBy(row => row.Id).Select(row => $"{row.Id}={(row.Quantity.HasValue ? Number(row.Quantity.Value) : "")}"));

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
        var units = await db.UnitOfMeasures.IgnoreQueryFilters().AsNoTracking().ToDictionaryAsync(unit => unit.Id);
        // Equivalent display scales are the same recipe. A change of physical amount or dimension still needs Edit.
        string Amount(decimal? quantity, Guid? unitId)
        {
            if (!quantity.HasValue) return "";
            if (!unitId.HasValue || !units.TryGetValue(unitId.Value, out var unit)) return $"{unitId}:{Number(quantity.Value)}";
            var symbol = unit.Symbol?.Trim().ToLowerInvariant();
            var canonical = symbol is "mg" or "g" or "kg" ? "mg" : symbol is "ml" or "l" ? "ml" : null;
            if (canonical is null) return $"{unitId}:{Number(quantity.Value)}";
            try { return $"{unit.Type}:{canonical}:{Number(SHARED.UomConverter.Convert(quantity.Value, symbol, canonical)!.Value)}"; }
            catch (OverflowException) { return $"{unitId}:{Number(quantity.Value)}"; }
        }
        string Options(IEnumerable<(Guid Id, decimal? Quantity, Guid? Unit, decimal? Full)> rows) =>
            string.Join(",", rows.OrderBy(row => row.Id).Select(row => $"{row.Id}={Amount(row.Quantity, row.Unit)}:{Amount(row.Full, row.Unit)}"));
        var stored = await db.ProductBillOfMaterials.IgnoreQueryFilters().AsNoTracking()
            .Where(link => link.ProductId == request.ProductId && link.IsActive && link.DeletedAt == null)
            .SelectMany(link => link.BillOfMaterial.Items.Where(item => item.DeletedAt == null))
            .Select(item => new { item.MaterialId, item.BaseQuantity, item.BaseUoMId, item.PrescribedQuantity, item.UseDensityConversion, item.Grade, item.Order, item.IsSubstitutable,
                Substitutes = item.Substitutes.Where(substitute => substitute.DeletedAt == null)
                    .Select(substitute => new { substitute.SubstituteMaterialId, substitute.BaseQuantity, substitute.BaseUoMId, substitute.PrescribedQuantity }).ToList() })
            .ToListAsync();
        return Compare(
            stored.Select(item => (item.MaterialId.ToString(), $"{Amount(item.BaseQuantity, item.BaseUoMId)};{Amount(item.PrescribedQuantity, item.BaseUoMId)};{item.UseDensityConversion};{item.Grade};{item.Order};{item.IsSubstitutable};"
                + Options(item.Substitutes.Select(substitute => (substitute.SubstituteMaterialId, substitute.BaseQuantity, substitute.BaseUoMId, substitute.PrescribedQuantity))))),
            (request.Items ?? []).Select(item => (item.MaterialId.ToString(),
                $"{Amount(item.BaseQuantity, item.BaseUoMId)};{Amount(item.PrescribedQuantity, item.BaseUoMId)};{item.UseDensityConversion};{item.Grade};{item.Order};{item.IsSubstitutable};"
                + Options(item.Substitutes is { Count: > 0 }
                    ? item.Substitutes.Select(substitute => (substitute.MaterialId, (decimal?)substitute.BaseQuantity, substitute.BaseUoMId, (decimal?)substitute.PrescribedQuantity))
                    : (item.SubstituteMaterialIds ?? []).Select(id => (id, (decimal?)null, (Guid?)null, (decimal?)null))))));
    }

    public static async Task<HashSet<ProductVerb>> PackingMaterials(ApplicationDbContext db, Guid productId, IReadOnlyCollection<CreateProductPackageRequest> rows)
    {
        var stored = await db.ProductPackages.IgnoreQueryFilters().AsNoTracking().Where(package => package.ProductId == productId && package.DeletedAt == null)
            .Select(package => new { package.MaterialId, package.ProductPackingId, package.BaseQuantity, package.UnitCapacity, package.DirectLinkMaterialId,
                package.PackingExcessMargin, package.PrescribedQuantity, package.Loose, package.MaterialThickness, package.OtherStandards,
                Substitutes = package.Substitutes.Select(substitute => new { substitute.SubstituteMaterialId, substitute.PrescribedQuantity }).ToList() })
            .ToListAsync();
        return Compare(
            stored.Select(row => ($"{row.MaterialId}:{row.ProductPackingId}",
                $"{Number(row.BaseQuantity)};{Number(row.UnitCapacity)};{row.DirectLinkMaterialId};{Number(row.PackingExcessMargin)};{Number(row.PrescribedQuantity)};{Number(row.Loose)};{row.MaterialThickness};{row.OtherStandards};{Substitutes(row.Substitutes.Select(s => (s.SubstituteMaterialId, s.PrescribedQuantity)))}")),
            (rows ?? []).Select(row => ($"{row.MaterialId}:{row.ProductPackingId}",
                $"{Number(row.BaseQuantity)};{Number(row.UnitCapacity)};{row.DirectLinkMaterialId};{Number(row.PackingExcessMargin)};{Number(row.PrescribedQuantity)};{Number(row.Loose)};{row.MaterialThickness};{row.OtherStandards};{Substitutes(row.Substitutes is { Count: > 0 } ? row.Substitutes.Select(s => (s.MaterialId, (decimal?)s.PrescribedQuantity)) : (row.SubstituteMaterialIds ?? []).Select(id => (id, (decimal?)null)))}")));
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
