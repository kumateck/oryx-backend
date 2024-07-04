using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
using SHARED;
using SHARED.Responses;

namespace APP.Services.Storage;

public interface IBlobStorageService
{
    Task<Result> UploadBlobAsync(string containerName, IFormFile file, string reference, string previousRef = null);
    Result UploadBlob(string containerName, IFormFile file, string reference, string previousRef = null);
    Task<Result<BlobDownloadInfo>> GetBlobAsync(string containerName, string name);
    Task<Result<FileDownloadResponse>> GetBlobAsync(string containerName, Guid modelId, Guid reference);
    Task<Result<List<FileDownloadResponse>>> DownloadBlobsAsync(string containerName, string reference);
}