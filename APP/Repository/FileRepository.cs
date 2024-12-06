using System.Data.Entity;
using APP.IRepository;
using APP.Services.Storage;
using DOMAIN.Entities.Attachments;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Http;
using SHARED;

namespace APP.Repository;

public class FileRepository(ApplicationDbContext context, IBlobStorageService blobStorageService) : IFileRepository
{
    public async Task<Result> SaveBlobItem(string modelType, Guid modelId, string reference, IFormFile file, Guid userId)
    {
        await using var transaction = await context.Database.BeginTransactionAsync();
        
        var attachment = new Attachment
        {
            ModelId = modelId,
            ModelType = modelType,
            Reference = reference,
            Name = Path.GetFileName(file.FileName),
            CreatedById = userId
        };
        context.Attachments.Add(attachment);
        await context.SaveChangesAsync();

        try
        {
            var result = await blobStorageService.UploadBlobAsync(modelType.ToLower(), file, $"{modelId}/{reference}");
            if (result.IsFailure)
            {
                await transaction.RollbackAsync();
                return result.Error;
            }
            await transaction.CommitAsync();
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
        }

        return Result.Success();
    }
    
    public async Task<Result> DeleteAttachment(Guid modelId, Guid userId)
    {
        var attachments = await context.Attachments
            .Where(item => item.ModelId == modelId)
            .ToListAsync();

        attachments.ForEach(item =>
        {
            item.DeletedAt = DateTime.Now;
            item.LastDeletedById = userId;
        });
        context.Attachments.UpdateRange(attachments);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> DeleteAttachment(Guid id, string reference, Guid userId)
    {
        var attachment = await context.Attachments
            .FirstOrDefaultAsync(item => item.ModelId == id && item.Reference == reference);

        if (attachment != null)
        {
            attachment.DeletedAt = DateTime.Now;
            attachment.LastDeletedById = userId;
            context.Attachments.Update(attachment);
            await context.SaveChangesAsync();
        }
        return Result.Success();
    }
}