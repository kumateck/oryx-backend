using DOMAIN.Entities.QcWorksheets;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;

namespace APP.Services.QcWorksheets.WorksheetDocxImport;

public sealed record CatalogEquipment(Guid Id, string Code, string Name);

public sealed record CatalogReagent(Guid Id, string Name);

public sealed record CatalogTemplate(Guid Id, string Code, string Name, IReadOnlyCollection<string> FieldKeys = null);

/// <summary>
/// The reference data recognizers match against. Deliberately synchronous and in-memory: a
/// recognizer stays a pure function of (document, catalog), so it can be unit-tested with a
/// hand-built catalog and never touches the database.
/// </summary>
public interface IWorksheetImportCatalog
{
    /// <summary>Equipment by <c>QcEquipment.EquipmentId</c> code (e.g. QCD/EQT/BAL/006).</summary>
    CatalogEquipment FindEquipment(string code);

    /// <summary>Reagent by normalized name.</summary>
    CatalogReagent FindReagent(string name);

    /// <summary>
    /// A saved, non-superseded MediaQualification template for a medium: by medium code when
    /// one is given (imported media templates take the medium code as their code), otherwise
    /// by normalized medium name.
    /// </summary>
    CatalogTemplate FindMediaTemplate(string mediumName, string mediumCode = null);

    /// <summary>A saved, non-superseded shared template (e.g. the EM template) by its code, with its field keys.</summary>
    CatalogTemplate FindSharedTemplate(string code);
}

public sealed class InMemoryWorksheetImportCatalog(
    IEnumerable<CatalogEquipment> equipment,
    IEnumerable<CatalogReagent> reagents,
    IEnumerable<CatalogTemplate> mediaTemplates,
    IEnumerable<CatalogTemplate> sharedTemplates = null) : IWorksheetImportCatalog
{
    private readonly IReadOnlyList<CatalogTemplate> _sharedTemplates = (sharedTemplates ?? []).ToList();

    public CatalogTemplate FindSharedTemplate(string code) =>
        _sharedTemplates.FirstOrDefault(template => ImportText.Canonical(template.Code) == ImportText.Canonical(code));

    private readonly ILookup<string, CatalogEquipment> _equipment =
        equipment.ToLookup(item => ImportText.Canonical(item.Code));

    private readonly ILookup<string, CatalogReagent> _reagents =
        reagents.ToLookup(item => ImportText.Canonical(item.Name));

    private readonly IReadOnlyList<CatalogTemplate> _mediaTemplates = mediaTemplates.ToList();

    public static InMemoryWorksheetImportCatalog Empty { get; } = new([], [], []);

    public CatalogEquipment FindEquipment(string code) =>
        string.IsNullOrWhiteSpace(code) ? null : _equipment[ImportText.Canonical(code)].FirstOrDefault();

    public CatalogReagent FindReagent(string name) =>
        string.IsNullOrWhiteSpace(name) ? null : _reagents[ImportText.Canonical(name)].FirstOrDefault();

    public CatalogTemplate FindMediaTemplate(string mediumName, string mediumCode = null)
    {
        var code = ImportText.Canonical(mediumCode);
        if (code.Length > 0
            && _mediaTemplates.FirstOrDefault(template => ImportText.Canonical(template.Code) == code) is { } byCode)
            return byCode;

        // Imported names read "Culture Media Qualification – {medium}". Equality once that
        // wording is removed, never "contains"/"ends with": "Casein Digest Agar" must not land
        // on the Soyabean-Casein Digest Agar template.
        var wanted = ImportText.Canonical(mediumName);
        return wanted.Length == 0
            ? null
            : _mediaTemplates.FirstOrDefault(template => MediumPart(template.Name) == wanted);
    }

    private static string MediumPart(string templateName) =>
        ImportText.Canonical(templateName).Replace("culturemediaqualification", string.Empty).Replace("mediaqualification", string.Empty);
}

public interface IWorksheetImportCatalogLoader
{
    Task<IWorksheetImportCatalog> LoadAsync(CancellationToken cancellationToken = default);
}

/// <summary>Loads the catalog once per import request. Read-only.</summary>
public sealed class WorksheetImportCatalogLoader(ApplicationDbContext context) : IWorksheetImportCatalogLoader
{
    private static readonly string[] SharedTemplateCodes = [EnvironmentalMonitoringRecognizer.TemplateCode];

    public async Task<IWorksheetImportCatalog> LoadAsync(CancellationToken cancellationToken = default)
    {
        var equipment = await context.QcEquipments
            .AsNoTracking()
            .Where(item => item.EquipmentId != null)
            .Select(item => new CatalogEquipment(item.Id, item.EquipmentId, item.Name))
            .ToListAsync(cancellationToken);

        var reagents = await context.Reagents
            .AsNoTracking()
            .Where(item => item.Name != null)
            .Select(item => new CatalogReagent(item.Id, item.Name))
            .ToListAsync(cancellationToken);

        // Newest version first, so a match lands on the current template of a medium.
        var templates = await context.QcWorksheetTemplates
            .AsNoTracking()
            .Where(item => item.Category == WorksheetCategory.MediaQualification
                           && item.Status != QcDocumentStatus.Superseded)
            .OrderByDescending(item => item.Version)
            .Select(item => new CatalogTemplate(item.Id, item.Code, item.Name, null))
            .ToListAsync(cancellationToken);

        var shared = await context.QcWorksheetTemplates
            .AsNoTracking()
            .Where(item => SharedTemplateCodes.Contains(item.Code) && item.Status != QcDocumentStatus.Superseded)
            .OrderByDescending(item => item.Version)
            .Select(item => new
            {
                item.Id, item.Code, item.Name,
                Keys = item.Sections.SelectMany(section => section.Fields).Select(field => field.FieldKey).ToList()
            })
            .ToListAsync(cancellationToken);

        return new InMemoryWorksheetImportCatalog(equipment, reagents, templates,
            shared.Select(item => new CatalogTemplate(item.Id, item.Code, item.Name, item.Keys)));
    }
}
