using DOMAIN.Entities.Forms;
using DOMAIN.Entities.QualityRoutines;
using SHARED;

namespace APP.Repository;

internal static class CommercialCoaConfiguration
{
    internal static Result<List<CommercialCoaItem>> Build(
        Form form,
        AnalysisType analysisType,
        IReadOnlyCollection<CommercialCoaItemRequest> requested,
        Guid? materialArdId = null,
        Guid? productArdId = null)
    {
        var available = form.Sections
            .Where(section => section.AnalysisType == null || section.AnalysisType == analysisType)
            .SelectMany(section => section.Fields
                .Select(field => new { Section = section, Field = field })).ToList();
        var selected = requested?.ToList() ?? [];
        if (analysisType == AnalysisType.Microbial && selected.Count == 0)
            return Error.Validation("Ard.CoaItems",
                "A Microbial ARD must explicitly select at least one COA item.");
        if (selected.Count == 0)
        {
            selected = available.Select((item, index) => new CommercialCoaItemRequest
            {
                FormFieldId = item.Field.Id,
                DisplayLabel = item.Field.Question?.Label ?? item.Section.Name,
                GroupName = item.Section.GroupName,
                SpecificationText = item.Section.Description,
                Reference = item.Field.Question?.Reference,
                DisplayOrder = index
            }).ToList();
        }
        if (selected.Select(item => item.FormFieldId).Distinct().Count() != selected.Count)
            return Error.Validation("Ard.CoaItems", "COA field selections must be unique.");
        var availableIds = available.Select(item => item.Field.Id).ToHashSet();
        if (selected.Any(item => !availableIds.Contains(item.FormFieldId)))
            return Error.Validation("Ard.CoaItems",
                "Every COA item must belong to the ARD worksheet form.");
        if (selected.Any(item => string.IsNullOrWhiteSpace(item.DisplayLabel) ||
            string.IsNullOrWhiteSpace(item.SpecificationText)))
            return Error.Validation("Ard.CoaItems",
                "Every COA item requires a display label and specification.");
        return selected.OrderBy(item => item.DisplayOrder).Select(item => new CommercialCoaItem
        {
            Id = Guid.NewGuid(),
            MaterialArdId = materialArdId,
            ProductArdId = productArdId,
            FormFieldId = item.FormFieldId,
            DisplayLabel = item.DisplayLabel.Trim(),
            GroupName = item.GroupName?.Trim(),
            SpecificationText = item.SpecificationText.Trim(),
            Unit = item.Unit?.Trim(),
            Reference = item.Reference?.Trim(),
            DisplayOrder = item.DisplayOrder
        }).ToList();
    }

    internal static List<CommercialCoaItemRequest> ToDto(
        IEnumerable<CommercialCoaItem> items) => items
        .OrderBy(item => item.DisplayOrder)
        .Select(item => new CommercialCoaItemRequest
        {
            FormFieldId = item.FormFieldId,
            DisplayLabel = item.DisplayLabel,
            GroupName = item.GroupName,
            SpecificationText = item.SpecificationText,
            Unit = item.Unit,
            Reference = item.Reference,
            DisplayOrder = item.DisplayOrder
        }).ToList();
}
