namespace DOMAIN.Entities.QualityAudits;

public class AuditChecklistTemplateDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public List<AuditChecklistTemplateItemDto> Items { get; set; } = [];
}

public class AuditChecklistTemplateItemDto
{
    public Guid Id { get; set; }
    public string SectionName { get; set; }
    public string QuestionText { get; set; }
    public bool IsRequired { get; set; }
    public int Order { get; set; }
}
