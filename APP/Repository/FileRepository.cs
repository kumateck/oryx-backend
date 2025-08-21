using APP.IRepository;
using APP.Services.Storage;
using DOMAIN.Entities.Attachments;
using DOMAIN.Entities.PurchaseOrders;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class FileRepository(ApplicationDbContext context, IBlobStorageService blobStorageService) : IFileRepository
{
    public async Task<Result> SaveBlobItem(string modelType, Guid modelId, string reference, IFormFile file,
        Guid? userId)
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

            switch (modelType)
            {
                case nameof(PurchaseOrder):
                    var purchaseOrder = await context.PurchaseOrders.FirstOrDefaultAsync(item => item.Id == modelId && item.Status != PurchaseOrderStatus.Completed);
                    if (purchaseOrder is not null)
                    {
                        purchaseOrder.Status = PurchaseOrderStatus.Attached;
                        context.PurchaseOrders.Update(purchaseOrder);
                        await context.SaveChangesAsync();
                    }
                    break;
            }
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
        }

        return Result.Success();
    }

    public async Task<Result> SaveBlobItem(string modelType, Guid modelId, List<IFormFile> files, Guid?  userId)
    {
        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var references = new List<string>();

            foreach (var file in files)
            {
                var reference = Guid.NewGuid();
                var attachment = new Attachment
                {
                    ModelId = modelId,
                    ModelType = modelType,
                    Reference = reference.ToString(),
                    Name = Path.GetFileName(file.FileName),
                    CreatedById = userId
                };

                context.Attachments.Add(attachment);
                references.Add(reference.ToString()); 
            }

            await context.SaveChangesAsync();

            foreach (var file in files)
            {
                var reference = references[files.IndexOf(file)];
                var result = await blobStorageService.UploadBlobAsync(modelType.ToLower(), file, $"{modelId}/{reference}");
            
                if (result.IsFailure)
                {
                    await transaction.RollbackAsync();
                    return result.Error;
                }
            }

            // Update status after all files are uploaded
            if (modelType == nameof(PurchaseOrder))
            {
                var purchaseOrder = await context.PurchaseOrders.FirstOrDefaultAsync(item => item.Id == modelId && item.Status != PurchaseOrderStatus.Completed);
                if (purchaseOrder is not null)
                {
                    purchaseOrder.Status = PurchaseOrderStatus.Attached;
                    context.PurchaseOrders.Update(purchaseOrder);
                }
            }

            // if (modelType == nameof(Invoice))
            // {
            //     var invoice = await context.Invoices.FirstOrDefaultAsync(item => item.Id == modelId && item.Status != InvoiceStatus.Completed);
            //     if (invoice is not null)
            //     {
            //         invoice.Status = InvoiceStatus.Completed;
            //         context.Invoices.Update(invoice);
            //     }
            // }

            await context.SaveChangesAsync();
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