using DOMAIN.Entities.Base;

namespace DOMAIN.Entities.QualityAudits;

public class AuditChecklistTemplate : BaseEntity
{
    public string Name { get; set; }
    public string Description { get; set; }
    public bool IsActive { get; set; } = true;
    public List<AuditChecklistTemplateItem> Items { get; set; } = [];
}

public class AuditChecklistTemplateItem : BaseEntity
{
    public Guid AuditChecklistTemplateId { get; set; }
    public AuditChecklistTemplate AuditChecklistTemplate { get; set; }
    public string SectionName { get; set; }
    public string QuestionText { get; set; }
    public bool IsRequired { get; set; }
    public int Order { get; set; }
}
