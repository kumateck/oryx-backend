using DOMAIN.Entities.Formulas;
using Microsoft.EntityFrameworkCore;

namespace INFRASTRUCTURE.Context;

public partial class ApplicationDbContext
{
    public DbSet<QuestionFormulaRepair> QuestionFormulaRepairs => Set<QuestionFormulaRepair>();
    public DbSet<LegacyFormulaConfigurationPin> LegacyFormulaConfigurationPins => Set<LegacyFormulaConfigurationPin>();
    public DbSet<LegacyFormulaRepairEvaluation> LegacyFormulaRepairEvaluations => Set<LegacyFormulaRepairEvaluation>();

    private void ValidateFormulaRepairHistory()
    {
        if (ChangeTracker.Entries().Any(e => e.Entity is QuestionFormulaRepair or
            LegacyFormulaConfigurationPin or LegacyFormulaRepairEvaluation &&
            e.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Formula repair history is append-only.");
    }
}
