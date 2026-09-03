using DOMAIN.Entities.QualityAudits;
using INFRASTRUCTURE.Context;

namespace API.Database.Seeds.TableSeeders;

public class AuditChecklistTemplateSeeder : ISeeder
{
    public void Handle(IServiceScope scope)
    {
        var dbContext = scope.ServiceProvider.GetService<ApplicationDbContext>();
        if (dbContext != null) SeedTemplates(dbContext);
    }

    private static void SeedTemplates(ApplicationDbContext dbContext)
    {
        if (dbContext.AuditChecklistTemplates.Any()) return;

        dbContext.AuditChecklistTemplates.AddRange(
            new AuditChecklistTemplate
            {
                Name = "WHO GMP Self-Inspection - Manufacturing Area",
                Description = "Covers the manufacturing-area scope of a WHO GMP self-inspection.",
                IsActive = true,
                Items =
                [
                    new AuditChecklistTemplateItem { SectionName = "Facilities & Buildings", QuestionText = "Are premises maintained in good repair and suitable for the operations carried out?", IsRequired = true, Order = 1 },
                    new AuditChecklistTemplateItem { SectionName = "Calibration", QuestionText = "Are all production and measuring instruments within their calibration due date?", IsRequired = true, Order = 2 },
                    new AuditChecklistTemplateItem { SectionName = "Raw Materials", QuestionText = "Are raw materials sampled, tested, and released before use per approved specifications?", IsRequired = true, Order = 3 },
                    new AuditChecklistTemplateItem { SectionName = "In-Process Controls", QuestionText = "Are in-process controls and line clearances documented for each batch?", IsRequired = true, Order = 4 },
                    new AuditChecklistTemplateItem { SectionName = "Documentation", QuestionText = "Are batch manufacturing/packaging records complete, contemporaneous, and reconciled?", IsRequired = true, Order = 5 },
                    new AuditChecklistTemplateItem { SectionName = "Data Integrity", QuestionText = "Are electronic and paper records attributable, legible, and tamper-evident?", IsRequired = true, Order = 6 },
                    new AuditChecklistTemplateItem { SectionName = "Deviation & CAPA", QuestionText = "Are deviations investigated with root cause analysis and corrective/preventive actions tracked to closure?", IsRequired = true, Order = 7 },
                    new AuditChecklistTemplateItem { SectionName = "Cleaning & Hygiene", QuestionText = "Are cleaning validation/verification records current for the equipment used?", IsRequired = true, Order = 8 },
                ],
            },
            new AuditChecklistTemplate
            {
                Name = "Supplier Quality Audit",
                Description = "Second-party audit checklist for evaluating a material or API supplier.",
                IsActive = true,
                Items =
                [
                    new AuditChecklistTemplateItem { SectionName = "Quality System", QuestionText = "Does the supplier operate a documented quality management system?", IsRequired = true, Order = 1 },
                    new AuditChecklistTemplateItem { SectionName = "Change Control", QuestionText = "Does the supplier notify customers of changes affecting material quality before implementation?", IsRequired = true, Order = 2 },
                    new AuditChecklistTemplateItem { SectionName = "Certificates", QuestionText = "Are Certificates of Analysis issued per batch and retained per the required retention period?", IsRequired = true, Order = 3 },
                    new AuditChecklistTemplateItem { SectionName = "Traceability", QuestionText = "Can the supplier trace a delivered batch back to its raw materials and process records?", IsRequired = true, Order = 4 },
                    new AuditChecklistTemplateItem { SectionName = "Complaints", QuestionText = "Does the supplier have a documented complaint-handling and recall process?", IsRequired = true, Order = 5 },
                ],
            }
        );

        dbContext.SaveChanges();
    }
}
