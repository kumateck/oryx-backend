using System.Text.Encodings.Web;
using System.Text.Json;
using DOMAIN.Entities.QcWorksheets;

namespace APP.Services.QcWorksheets.WorksheetDocxImport;

/// <summary>
/// The one place a proposed template becomes the real create-template request. Field
/// <see cref="ProposedWorksheetField.Options"/> become <c>OptionsJson</c> (a JSON string array,
/// null when there are none); table columns already carry the real column contract
/// (<c>fixedValues</c>, <c>rowHeader</c>, <c>options</c>, <c>group</c>) inside ColumnDefinitions,
/// so they pass through unchanged. Template save then applies the real option and fixed-row
/// validation.
/// </summary>
public static class WorksheetImportTemplateMapper
{
    private static readonly JsonSerializerOptions Compact = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static CreateWorksheetTemplateRequest ToCreateRequest(ProposedWorksheetTemplate template) => new()
    {
        Code = template.Code,
        Name = template.Name,
        Department = template.Department,
        Category = template.Category,
        Sections = template.Sections.Select(section => new CreateWorksheetSectionRequest
        {
            Order = section.Order,
            Name = section.Name,
            InstrumentId = section.InstrumentId,
            Fields = section.Fields.Select(field => new CreateWorksheetFieldRequest
            {
                Order = field.Order,
                FieldKey = field.FieldKey,
                Label = field.Label,
                Type = field.Type,
                Mode = field.Mode,
                Unit = field.Unit,
                Analyte = field.Analyte,
                ConstantValue = field.ConstantValue,
                FormulaExpression = field.FormulaExpression,
                ColumnDefinitions = field.ColumnDefinitions,
                OptionsJson = OptionsJson(field.Options),
                ReferencedResultSourceTemplateId = field.ReferencedResultSourceTemplateId,
                ReferencedResultSourceFieldKey = field.ReferencedResultSourceFieldKey,
                ReferencedResultResolutionFieldKey = field.ReferencedResultResolutionFieldKey
            }).ToList()
        }).ToList()
    };

    public static string OptionsJson(IReadOnlyCollection<string> options) =>
        options is { Count: > 0 } ? JsonSerializer.Serialize(options, Compact) : null;
}
