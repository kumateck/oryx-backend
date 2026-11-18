using DOMAIN.Entities.Forms;
using DOMAIN.Entities.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace INFRASTRUCTURE.EntityConfigurations.Forms;
public sealed class TestCorrectionReturnConfiguration : IEntityTypeConfiguration<TestCorrectionReturn>
{
    public void Configure(EntityTypeBuilder<TestCorrectionReturn> b)
    {
        b.ToTable("TestCorrectionReturns"); b.HasKey(x => x.Id);
        b.HasIndex(x => x.SourceResponseId).IsUnique();
        b.HasIndex(x => x.CorrectionResponseId).IsUnique();
        b.Property(x => x.Reason).HasMaxLength(2000).IsRequired();
        b.Property(x => x.PreviousState).HasColumnType("jsonb").IsRequired();
        b.HasOne<Response>().WithMany().HasForeignKey(x => x.SourceResponseId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Response>().WithMany().HasForeignKey(x => x.CorrectionResponseId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.ReturnedById).OnDelete(DeleteBehavior.Restrict);
    }
}
