using DOMAIN.Entities.FullProcedures;
using DOMAIN.Entities.Roles;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;

namespace APP.Services.FullProcedures;

public static class TemplateAreaCatalogProvider
{
    private sealed record Purpose(
        string Id, string Name, TemplateDefinitionKind[] Kinds,
        string[] Subjects, string[] Capabilities);

    private static readonly Purpose[] PurposeDefinitions =
    [
        new("hr", "Human Resources", [TemplateDefinitionKind.Question, TemplateDefinitionKind.Section,
            TemplateDefinitionKind.Form, TemplateDefinitionKind.Workflow],
            ["employee", "candidate", "department"], ["capture-response", "approval", "confidential-response"]),
        new("production-procedure", "Production Procedures", Enum.GetValues<TemplateDefinitionKind>(),
            ["product", "batch", "equipment"], ["capture-response", "approval", "electronic-signature", "material-action", "quality-gate"]),
        new("quality-control", "Quality Control", Enum.GetValues<TemplateDefinitionKind>(),
            ["material", "product", "batch", "sample"], ["capture-response", "approval", "electronic-signature", "quality-gate", "calculation"]),
        new("microbiology", "Microbiology", Enum.GetValues<TemplateDefinitionKind>(),
            ["material", "product", "batch", "sample", "environment"], ["capture-response", "approval", "electronic-signature", "quality-gate", "calculation"]),
        new("research-development", "Research and Development", Enum.GetValues<TemplateDefinitionKind>(),
            ["product", "project", "batch", "formulation"], ["capture-response", "approval", "electronic-signature", "material-action", "abort", "redevelopment"]),
        new("warehouse", "Warehouse", [TemplateDefinitionKind.Question, TemplateDefinitionKind.Section,
            TemplateDefinitionKind.Form, TemplateDefinitionKind.Activity, TemplateDefinitionKind.Workflow],
            ["material", "product", "batch", "stock-requisition"], ["capture-response", "approval", "electronic-signature", "material-action"]),
        new("maintenance", "Maintenance", [TemplateDefinitionKind.Question, TemplateDefinitionKind.Section,
            TemplateDefinitionKind.Form, TemplateDefinitionKind.Activity, TemplateDefinitionKind.Workflow],
            ["equipment", "work-order"], ["capture-response", "approval", "electronic-signature"]),
    ];

    private static readonly TemplateCatalogItemDto[] SubjectTypes = Items(
        ("batch", "Batch"), ("candidate", "Candidate"), ("department", "Department"),
        ("employee", "Employee"), ("environment", "Environment"), ("equipment", "Equipment"),
        ("formulation", "Formulation"), ("material", "Material"), ("product", "Product"),
        ("project", "Project"), ("sample", "Sample"), ("stock-requisition", "Stock requisition"),
        ("work-order", "Work order"));

    private static readonly TemplateCatalogItemDto[] Capabilities = Items(
        ("abort", "Abort with remarks"), ("approval", "Approval"), ("calculation", "Controlled calculation"),
        ("capture-response", "Capture response"), ("confidential-response", "Confidential response"),
        ("electronic-signature", "Electronic signature"), ("material-action", "Material or stock action"),
        ("quality-gate", "Quality gate"), ("redevelopment", "R&D redevelopment"));

    private static readonly TemplateCatalogItemDto[] ReviewPolicies = Items(
        ("standard-review", "Author and reviewer"),
        ("regulated-three-person", "Separate author, reviewer, and approver"),
        ("confidential-review", "Restricted author and reviewer"));

    public static async Task<(TemplateAreaCatalog Policy, TemplateAreaCatalogDto Dto)> BuildAsync(
        ApplicationDbContext context, CancellationToken cancellationToken = default)
    {
        var roles = await context.Set<Role>().AsNoTracking().Where(item => item.DeletedAt == null)
            .OrderBy(item => item.DisplayName).ThenBy(item => item.Name).ToListAsync(cancellationToken);
        var purposeDtos = PurposeDefinitions.Select(item => new TemplatePurposeDto(
            item.Id, item.Name, item.Kinds.Select(kind => (int)kind).ToArray(),
            item.Subjects, item.Capabilities)).ToArray();
        var policy = new TemplateAreaCatalog(
            PurposeDefinitions.ToDictionary(item => item.Id, item => new TemplatePurposeDescriptor(
                item.Id, item.Kinds.ToHashSet(), item.Subjects.ToHashSet(), item.Capabilities.ToHashSet())),
            SubjectTypes.Select(item => item.Id).ToHashSet(),
            Capabilities.Select(item => item.Id).ToHashSet(),
            ReviewPolicies.Select(item => item.Id).ToHashSet(), roles.Select(item => item.Id).ToHashSet());
        var dto = new TemplateAreaCatalogDto(purposeDtos, SubjectTypes, Capabilities, ReviewPolicies,
            roles.Select(item => new TemplateOwnerGroupDto(item.Id,
                item.DisplayName ?? item.Name ?? item.Id.ToString())).ToArray());
        return (policy, dto);
    }

    private static TemplateCatalogItemDto[] Items(params (string Id, string Name)[] items) =>
        items.Select(item => new TemplateCatalogItemDto(item.Id, item.Name)).ToArray();
}
