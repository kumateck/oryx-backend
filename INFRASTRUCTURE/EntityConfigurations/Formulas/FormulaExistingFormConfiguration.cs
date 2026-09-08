using DOMAIN.Entities.Forms;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace INFRASTRUCTURE.EntityConfigurations.Formulas;

public class FormulaResponseLinkConfiguration : IEntityTypeConfiguration<Response>
{
    public void Configure(EntityTypeBuilder<Response> builder)
    {
        builder.HasOne(item => item.FormRevision).WithMany()
            .HasForeignKey(item => item.FormRevisionId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class FormulaFormResponseLinkConfiguration : IEntityTypeConfiguration<FormResponse>
{
    public void Configure(EntityTypeBuilder<FormResponse> builder)
    {
        builder.HasOne(item => item.FormFieldRevision).WithMany()
            .HasForeignKey(item => item.FormFieldRevisionId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class FormulaResponseApprovalLinkConfiguration : IEntityTypeConfiguration<ResponseApproval>
{
    public void Configure(EntityTypeBuilder<ResponseApproval> builder)
    {
        builder.HasOne(item => item.FormulaSubmissionSet).WithMany()
            .HasForeignKey(item => item.FormulaSubmissionSetId).OnDelete(DeleteBehavior.Restrict);
    }
}
