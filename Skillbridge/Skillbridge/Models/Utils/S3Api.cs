using File = Skillbridge.Models.Project.File;

namespace Skillbridge.Models.Utils;
using Amazon.S3;
using Amazon.S3.Model;

public class S3Api
{
    private readonly AmazonS3Client _s3client;
    private readonly IConfiguration _configuration;
    public S3Api(IConfiguration configuration)
    {
        _configuration = configuration;
        var config = new AmazonS3Config
        {
            ServiceURL = _configuration["S3API:Url"],
            ForcePathStyle = true
        };
        
        _s3client = new  AmazonS3Client(_configuration["S3API:keyID"], _configuration["S3API:key"], config);
    }
    

    public AmazonS3Client GetS3Client()
    {
        return _s3client;
    }
    
    
    public async Task<string> ObterFicheiroAsync(string bucket, string key)
    {
        try
        {
            var request = new GetObjectRequest
            {
                BucketName = bucket,
                Key = key
            };

            using GetObjectResponse response = await _s3client.GetObjectAsync(request);
            using StreamReader reader = new StreamReader(response.ResponseStream);

            return await reader.ReadToEndAsync();
        }
        catch (AmazonS3Exception e)
        {
            Console.WriteLine($"[ERROR][ObterFicheiroAsync] {e.Message}");
            return null;
        }
    }

    public async Task<bool> EditarFicheiroAsync(string bucket, string key, string editar)
    {
        try
        {
            var request = await _s3client.PutObjectAsync(new PutObjectRequest
            {
                BucketName = bucket,
                Key = key,
                ContentBody = editar
            });
            
            return request.HttpStatusCode == System.Net.HttpStatusCode.OK;
        }
        catch (AmazonS3Exception e)
        {
            Console.WriteLine($"[ERROR][EditarFicheiroAsync] {e.Message}");
            return false;
        }
    }

    public async Task<bool> CriarBucketAsync(string bucket) {
        try
        {
            var request = new PutBucketRequest
            {
                BucketName = bucket,
                UseClientRegion = true,
                ObjectLockEnabledForBucket =  true,
            };
            
            var response = await _s3client.PutBucketAsync(request);

            return response.HttpStatusCode == System.Net.HttpStatusCode.OK;
        }
        catch (AmazonS3Exception e)
        {
            Console.WriteLine($"[ERROR][CriarBucketAsync] {e.Message}");
            return false;
        }
    }

    public async Task<bool> EliminarFicheiroAsync(string bucket, string key)
    {
        try
        {
            var request = new DeleteObjectRequest
            {
                BucketName = bucket,
                Key = key
            };
            await _s3client.DeleteObjectAsync(request);

            return true;
        }
        catch (AmazonS3Exception e)
        {
            Console.WriteLine($"[ERROR][EliminarFicheiroAsync] {e.Message}");
            return false;
        }
    }
    
    public async Task<bool> EliminarBucketAsync(string bucket, string key)
    {
        try
        {
            var request = await _s3client.DeleteBucketAsync(bucket);
            return request.HttpStatusCode == System.Net.HttpStatusCode.OK;
        }
        catch (AmazonS3Exception e)
        {
            Console.WriteLine($"[ERROR][EliminarBucketAsync] {e.Message}");
            return false;
        }
    }

    public async Task<List<S3Bucket>> ListBucketsAsync()
    {
        try
        {
            var request = await _s3client.ListBucketsAsync(new ListBucketsRequest());
            return request.Buckets;
        }
        catch (Exception e)
        {
            Console.WriteLine($"[ERROR][ListBucketsAsync] {e.Message}");
            return null;
        }
    }

    public async Task<bool> UploadBinaryAsync(string bucket, string key, byte[] data, string contentType)
    {
        try
        {
            var request = new PutObjectRequest
            {
                BucketName = bucket,
                Key = key,
                InputStream = new System.IO.MemoryStream(data),
                ContentType = contentType
                
            };
            var response = await _s3client.PutObjectAsync(request);
            return response.HttpStatusCode == System.Net.HttpStatusCode.OK;
        }
        catch (AmazonS3Exception e)
        {
            Console.WriteLine($"[ERROR][UploadBinaryAsync] {e.Message}");
            return false;
        }
    }

    public async Task<(byte[] Data, string ContentType)?> GetBinaryAsync(string bucket, string key)
    {
        try
        {
            var request = new GetObjectRequest
            {
                BucketName = bucket,
                Key = key
            };

            using var response = await _s3client.GetObjectAsync(request);
            using var ms = new MemoryStream();
            await response.ResponseStream.CopyToAsync(ms);

            return (ms.ToArray(), response.Headers.ContentType);
        }
        catch (AmazonS3Exception e)
        {
            Console.WriteLine($"[ERROR][GetBinaryAsync] {e.Message}");
            return null;
        }
    }
    

    public async Task<List<S3Object>> ListFilesAsync(string bucket)
    {
        try
        {
            string continuationToken = null;
            var All = new  List<S3Object>();
            do
            {
                var request = await _s3client.ListObjectsV2Async(new ListObjectsV2Request
                {
                    BucketName = bucket,
                    ContinuationToken = continuationToken
                });

                All.AddRange(request.S3Objects);
                
                continuationToken = (bool) request.IsTruncated ? request.NextContinuationToken : null;

            } while (continuationToken != null);
            
            return All;
        }
        catch (Exception e)
        {
            Console.WriteLine($"[ERROR][ListFilesAsync] {e.Message}");
            return null;
        }
    }
    
    public async Task<string?> GetBucketRegionAsync(string bucketName)
    {
        try
        {
            var response = await _s3client.GetBucketLocationAsync(new GetBucketLocationRequest
            {
                BucketName = bucketName
            });

            return response.Location?.Value;
        }
        catch (AmazonS3Exception e)
        {
            Console.WriteLine($"[ERROR][GetBucketRegionAsync] {e.Message}");
            return null;
        }
    }
}