using DOMAIN.Entities.Forms;
using Microsoft.EntityFrameworkCore;

namespace INFRASTRUCTURE.Context;

public partial class ApplicationDbContext
{
    public DbSet<TemplateRepair> TemplateRepairs => Set<TemplateRepair>();
    public DbSet<TemplateRepairAssignment> TemplateRepairAssignments => Set<TemplateRepairAssignment>();

    private void ValidateTemplateRepairHistory()
    {
        if (ChangeTracker.Entries().Any(e => e.Entity is TemplateRepair or TemplateRepairAssignment &&
            e.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Template repair history is append-only.");
    }
}
