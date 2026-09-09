using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;
using SHARED;

namespace APP.Services.Storage;

public partial class BlobStorageService
{
    public async Task<Result<bool>> BlobExistsAsync(string bucketName, string objectName)
    {
        if (!int.TryParse(_port, out var port))
            return Result.Failure<bool>(StorageErrors.PortNotFound(nameof(port)));

        var client = new MinioClient()
            .WithEndpoint(_endpoint, port)
            .WithCredentials(_accessKey, _secretKey)
            .Build();

        try
        {
            await client.StatObjectAsync(
                new StatObjectArgs().WithBucket(bucketName).WithObject(objectName)
            );
            return Result.Success(true);
        }
        catch (ObjectNotFoundException)
        {
            return Result.Success(false);
        }
        catch (BucketNotFoundException)
        {
            return Result.Success(false);
        }
        catch (Exception e)
        {
            return Result.Failure<bool>(StorageErrors.SaveFileFailure(e.Message));
        }
    }
}
