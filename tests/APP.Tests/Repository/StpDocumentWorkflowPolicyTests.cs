using APP.Repository;
using APP.Services.Storage;
using DOMAIN.Entities.StpDocuments;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SHARED;
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

    [Fact]
    public async Task Missing_stored_file_cannot_be_submitted_for_review()
    {
        await using var context = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
            new StpTestUser()
        );
        var versionId = Guid.NewGuid();
        var document = new StpDocument
        {
            Id = Guid.NewGuid(),
            OwnerType = "ProductStandardTestProcedure",
            OwnerId = Guid.NewGuid(),
            Status = StpDocumentStatus.Draft,
            CurrentDraftVersionId = versionId
        };
        context.Add(document);
        context.Add(new StpDocumentVersion
        {
            Id = versionId,
            StpDocumentId = document.Id,
            VersionNumber = 1,
            StorageKey = "missing/v1.docx",
            FileName = "missing.docx",
            Sha256 = new string('a', 64)
        });
        await context.SaveChangesAsync();

        var repository = new StpDocumentRepository(
            context,
            new MissingBlobStorage(),
            null!,
            null!,
            null!,
            null!,
            NullLogger<StpDocumentRepository>.Instance
        );
        var result = await repository.SubmitForReview(document.Id, Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal("StpDocument.StoredFileUnavailable", result.Error.Code);
        Assert.Equal(StpDocumentStatus.Draft, document.Status);
    }

    [Theory]
    [InlineData(StpDocumentStatus.Draft, true)]
    [InlineData(StpDocumentStatus.InReview, false)]
    [InlineData(StpDocumentStatus.Reviewed, false)]
    [InlineData(StpDocumentStatus.Approved, false)]
    public void Only_an_identical_current_draft_upload_is_idempotent(
        StpDocumentStatus status,
        bool expected)
    {
        var versionId = Guid.NewGuid();
        var sha256 = new string('a', 64);
        var document = new StpDocument
        {
            Status = status,
            CurrentDraftVersionId = versionId,
            Versions =
            [
                new StpDocumentVersion
                {
                    Id = versionId,
                    Sha256 = sha256
                }
            ]
        };

        Assert.Equal(expected, StpDocumentRepository.IsDuplicateDraftUpload(document, sha256));
        Assert.False(StpDocumentRepository.IsDuplicateDraftUpload(document, new string('b', 64)));
    }
}

file sealed class MissingBlobStorage : IBlobStorageService
{
    public Task<Result<bool>> BlobExistsAsync(string bucketName, string objectName) =>
        Task.FromResult(Result.Success(false));

    public Task<Result> UploadBlobAsync(string bucketName, IFormFile file, string objectName, string previousObjectName = null!) =>
        throw new NotSupportedException();
    public Result UploadBlob(string bucketName, IFormFile file, string objectName, string previousObjectName = null!) =>
        throw new NotSupportedException();
    public Task<Result<(Stream Stream, string ContentType, string Name)>> GetBlobAsync(string bucketName, string objectName) =>
        throw new NotSupportedException();
    public Task<Result<(Stream Stream, string ContentType, string Name)>> GetBlobAsync(string bucketName, string modelId, string reference) =>
        throw new NotSupportedException();
    public Task<Result> UploadChunkAsync(string bucketName, IFormFile chunk, string objectName, int chunkIndex) =>
        throw new NotSupportedException();
    public Task<Result> CombineChunksAsync(string bucketName, string objectName, int totalChunks) =>
        throw new NotSupportedException();
}

file sealed class StpTestUser : ICurrentUserService
{
    public Guid? UserId => null;
    public Guid? DepartmentId => null;
    public string DepartmentType => string.Empty;
}
