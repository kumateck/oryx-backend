using DOMAIN.Entities.QcWorksheets;

namespace APP.Services.QcWorksheets.WorksheetDocxImport;

/// <summary>
/// The one place a proposed template becomes the real create-template request.
/// <para>
/// Phase A (a parallel branch) adds <c>OptionsJson</c> to <see cref="WorksheetField"/> and the
/// create request. Until it merges, field-level <see cref="ProposedWorksheetField.Options"/>
/// have nowhere to go here and are dropped; table-column <c>options</c> / <c>group</c> already
/// travel inside ColumnDefinitions. When Phase A lands, set
/// <c>OptionsJson = JsonSerializer.Serialize(field.Options)</c> below — nothing else changes.
/// </para>
/// </summary>
public static class WorksheetImportTemplateMapper
{
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
                ReferencedResultSourceTemplateId = field.ReferencedResultSourceTemplateId,
                ReferencedResultSourceFieldKey = field.ReferencedResultSourceFieldKey,
                ReferencedResultResolutionFieldKey = field.ReferencedResultResolutionFieldKey
            }).ToList()
        }).ToList()
    };
}
