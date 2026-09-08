using DOMAIN.Entities.StpDocuments;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

public sealed class StpDocumentWorkflowPolicyTests
{
    [Theory]
    [InlineData(StpDocumentStatus.Draft, StpDocumentStatus.InReview)]
    [InlineData(StpDocumentStatus.InReview, StpDocumentStatus.Reviewed)]
    [InlineData(StpDocumentStatus.Reviewed, StpDocumentStatus.Approved)]
    [InlineData(StpDocumentStatus.InReview, StpDocumentStatus.Draft)]
    [InlineData(StpDocumentStatus.Reviewed, StpDocumentStatus.Draft)]
    public void ControlledTransitions_are_allowed(StpDocumentStatus from, StpDocumentStatus to) =>
        Assert.True(StpDocumentWorkflowPolicy.CanTransition(from, to));

    [Theory]
    [InlineData(StpDocumentStatus.Draft, StpDocumentStatus.Approved)]
    [InlineData(StpDocumentStatus.InReview, StpDocumentStatus.Approved)]
    [InlineData(StpDocumentStatus.Approved, StpDocumentStatus.InReview)]
    [InlineData(StpDocumentStatus.Approved, StpDocumentStatus.Draft)]
    public void Approval_bypasses_are_rejected(StpDocumentStatus from, StpDocumentStatus to) =>
        Assert.False(StpDocumentWorkflowPolicy.CanTransition(from, to));

    [Fact]
    public void Review_and_approval_require_three_distinct_users()
    {
        var author = Guid.NewGuid();
        var reviewer = Guid.NewGuid();
        var approver = Guid.NewGuid();

        Assert.False(StpDocumentWorkflowPolicy.HasIndependentReviewer(author, author));
        Assert.True(StpDocumentWorkflowPolicy.HasIndependentReviewer(author, reviewer));
        Assert.False(StpDocumentWorkflowPolicy.HasIndependentApprover(author, reviewer, reviewer));
        Assert.False(StpDocumentWorkflowPolicy.HasIndependentApprover(author, reviewer, author));
        Assert.True(StpDocumentWorkflowPolicy.HasIndependentApprover(author, reviewer, approver));
    }

    [Fact]
    public async Task Stored_version_and_signature_evidence_is_append_only()
    {
        await using var context = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
            new StpTestUser()
        );
        var document = new StpDocument
        {
            Id = Guid.NewGuid(),
            OwnerType = "ProductStandardTestProcedure",
            OwnerId = Guid.NewGuid()
        };
        var version = new StpDocumentVersion
        {
            Id = Guid.NewGuid(),
            StpDocumentId = document.Id,
            VersionNumber = 1,
            StorageKey = "document/v1.docx",
            FileName = "v1.docx",
            Sha256 = new string('a', 64)
        };
        context.AddRange(document, version);
        await context.SaveChangesAsync();

        version.FileName = "tampered.docx";

        await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
    }
}

file sealed class StpTestUser : ICurrentUserService
{
    public Guid? UserId => null;
    public Guid? DepartmentId => null;
    public string DepartmentType => string.Empty;
}
