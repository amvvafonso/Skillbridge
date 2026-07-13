using Amazon.S3;
using Amazon.S3.Model;

namespace Skillbridge.Services;

public abstract class IS3Api
{
    public abstract Task<string?> ObterFicheiroAsync(string bucket, string key);
    public abstract Task<bool> EditarFicheiroAsync(string bucket, string key, string editar);
    public abstract Task<bool> CriarBucketAsync(string bucket);
    public abstract Task<bool> EliminarFicheiroAsync(string bucket, string key);
    public abstract Task<bool> EliminarBucketAsync(string bucket, string key);
    public abstract Task<List<S3Bucket>?> ListBucketsAsync();
    public abstract Task<bool> UploadBinaryAsync(string bucket, string key, byte[] data, string contentType);
    public abstract Task<(byte[] Data, string ContentType)?> GetBinaryAsync(string bucket, string key);
    public abstract Task<List<S3Object>?> ListFilesAsync(string bucket);
    public abstract AmazonS3Client GetS3Client();
    
    public class S3Api : IS3Api
    {
        private ILogger<S3Api> _logger;
        private readonly AmazonS3Client _s3Client;

        public S3Api(IConfiguration configuration, ILogger<S3Api> logger)
        {
            _logger = logger;
            var config = new AmazonS3Config
            {
                ServiceURL = configuration["S3API:Url"],
                ForcePathStyle = true
            };

            _s3Client = new AmazonS3Client(configuration["S3API:keyID"], configuration["S3API:key"], config);
        }


        public override AmazonS3Client GetS3Client()
        {
            return _s3Client;
        }


        public override async Task<string?> ObterFicheiroAsync(string bucket, string key)
        {
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    var request = new GetObjectRequest
                    {
                        BucketName = bucket,
                        Key = key
                    };

                    using GetObjectResponse response = await _s3Client.GetObjectAsync(request);
                    using StreamReader reader = new StreamReader(response.ResponseStream);
                    
                    return await reader.ReadToEndAsync();
                }
                catch (AmazonS3Exception e)
                {
                    _logger.LogError(e, $"Error fetching file {key} from S3");
                }
            }
            return string.Empty;
        }

        public override async Task<bool> EditarFicheiroAsync(string bucket, string key, string editar)
        {
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    var request = await _s3Client.PutObjectAsync(new PutObjectRequest
                    {
                        BucketName = bucket,
                        Key = key,
                        ContentBody = editar
                    });

                    return request.HttpStatusCode == System.Net.HttpStatusCode.OK;
                }
                catch (AmazonS3Exception e)
                {
                    _logger.LogError(e, $"Error editing file {key} from S3");
                    
                }
            }

            return false;
        }

        public override async Task<bool> CriarBucketAsync(string bucket)
        {
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    var request = new PutBucketRequest
                    {
                        BucketName = bucket,
                        UseClientRegion = true,
                        ObjectLockEnabledForBucket = true,
                    };

                    var response = await _s3Client.PutBucketAsync(request);

                    return response.HttpStatusCode == System.Net.HttpStatusCode.OK;
                }
                catch (AmazonS3Exception e)
                {
                    _logger.LogError(e, $"Error creating bucket {bucket}");
                }
            }
            return false;
        }

        public override async Task<bool> EliminarFicheiroAsync(string bucket, string key)
        {
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    var request = new DeleteObjectRequest
                    {
                        BucketName = bucket,
                        Key = key
                    };
                    await _s3Client.DeleteObjectAsync(request);

                    return true;
                }
                catch (AmazonS3Exception e)
                {
                    _logger.LogError(e, $"Error deleting file {key} from S3");
                }
            }
            return false;
        }

        public override async Task<bool> EliminarBucketAsync(string bucket, string key)
        {
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    var request = await _s3Client.DeleteBucketAsync(bucket);
                    return request.HttpStatusCode == System.Net.HttpStatusCode.OK;
                }
                catch (AmazonS3Exception e)
                {
                    _logger.LogError(e, $"Error deleting bucket {bucket} from S3");
                }
            }

            return false;
        }

        public override async Task<List<S3Bucket>> ListBucketsAsync()
        {
            try
            {
                var request = await _s3Client.ListBucketsAsync(new ListBucketsRequest());
                return request.Buckets;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error listing buckets");
                return [];
            }
        }

        public override async Task<bool> UploadBinaryAsync(string bucket, string key, byte[] data, string contentType)
        {
            for (int i = 0; i < 3; i++)
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
                    var response = await _s3Client.PutObjectAsync(request);
                    return response.HttpStatusCode == System.Net.HttpStatusCode.OK;
                }
                catch (AmazonS3Exception e)
                {
                    _logger.LogError(e, $"Error uploading binary {key} to S3");
                }
            }

            return false;
        }

        public override async Task<(byte[] Data, string ContentType)?> GetBinaryAsync(string bucket, string key)
        {
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    var request = new GetObjectRequest
                    {
                        BucketName = bucket,
                        Key = key
                    };

                    using var response = await _s3Client.GetObjectAsync(request);
                    using var ms = new MemoryStream();
                    await response.ResponseStream.CopyToAsync(ms);

                    return (ms.ToArray(), response.Headers.ContentType);
                }
                catch (AmazonS3Exception e)
                {
                    _logger.LogError(e, $"Error getting binary {key} from S3");
                }
            }
            return null;
        }


        public override async Task<List<S3Object>> ListFilesAsync(string bucket)
        {
            try
            {
                string continuationToken = null;
                var All = new List<S3Object>();
                do
                {
                    var request = await _s3Client.ListObjectsV2Async(new ListObjectsV2Request
                    {
                        BucketName = bucket,
                        ContinuationToken = continuationToken
                    });

                    All.AddRange(request.S3Objects);

                    continuationToken = (bool)request.IsTruncated ? request.NextContinuationToken : null;

                } while (continuationToken != null);

                return All;
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error listing files from {bucket}");
                return [];
            }
        }
    }
}