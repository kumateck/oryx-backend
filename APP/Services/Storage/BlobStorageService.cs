using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
using SHARED;
using SHARED.Responses;

namespace APP.Services.Storage;

public class BlobStorageService : IBlobStorageService
{
     private readonly string _accessKey = Environment.GetEnvironmentVariable("accessKey");
     
    public async Task<Result> UploadBlobAsync(string containerName, IFormFile file, string reference, string previousRef = null)
    {
        try
        {
            var container = new BlobContainerClient(_accessKey, containerName);

            await container.CreateIfNotExistsAsync();

            var blob = container.GetBlobClient(reference);
            if (previousRef != null)
            {
                var prev = container.GetBlobClient(previousRef);
                await prev.DeleteIfExistsAsync();
            }
            await blob.DeleteIfExistsAsync();
            await blob.UploadAsync(file.OpenReadStream());
            return Result.Success();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return StorageErrors.SaveFileFailure(e.Message);
        }
    }
    
    public Result UploadBlob(string containerName, IFormFile file, string reference, string previousRef = null)
    {
        try
        {
            var container = new BlobContainerClient(_accessKey, containerName);

            container.CreateIfNotExists();

            var blob = container.GetBlobClient(reference);
            if (previousRef != null)
            {
                var prev = container.GetBlobClient(previousRef);
                prev.DeleteIfExists();
            }
            blob.DeleteIfExists();

            blob.Upload(file.OpenReadStream());
            return Result.Success();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return StorageErrors.SaveFileFailure(e.Message);
        }
    }

    public async Task<Result<BlobDownloadInfo>> GetBlobAsync(string containerName, string name)
    {
        var container =
            new BlobServiceClient(_accessKey).GetBlobContainerClient(
                containerName);
        var blobClient = container.GetBlobClient(name);
        var response = await blobClient.DownloadAsync();
        return response.Value;
    }
    
    public async Task<Result<FileDownloadResponse>> GetBlobAsync(string containerName, Guid modelId, Guid reference)
    {
        var container =
            new BlobServiceClient(_accessKey).GetBlobContainerClient(
                containerName);
        var blobClient = container.GetBlobClient($"{modelId}/{reference.ToString()}");
        return new FileDownloadResponse
        {
            Name = $"{DateTime.Now}",
            FileInfo = await blobClient.DownloadAsync()
        };
    }
    
    public async Task<Result<List<FileDownloadResponse>>> DownloadBlobsAsync(string containerName, string reference)
    {
        var container = new BlobContainerClient(_accessKey, containerName);
        var blobs = new List<FileDownloadResponse>();
        
        await foreach (var blob in container.GetBlobsAsync(prefix: reference))
        {
            var blobClient = container.GetBlobClient(blob.Name);
            var fileName = blob.Name.Split("/").Length > 1 ? blob.Name.Split("/")[1] : blob.Name;
            var blobDownloadInfo = await blobClient.DownloadAsync();
            blobs.Add(new FileDownloadResponse
            {
                FileInfo = blobDownloadInfo,
                Name = fileName
            });
        }

        return blobs;
    }
}