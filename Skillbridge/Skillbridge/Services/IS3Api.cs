using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Skillbridge.Data;
using Skillbridge.Models.Project;

namespace Skillbridge.Services;

public interface IS3Api
{
    Task<string?> ObterFicheiroAsync(string bucket, string key, string userId);
    Task<bool> EditarFicheiroAsync(string bucket, string key, string editar);
    Task<bool> CriarBucketAsync(string bucket);
    Task<bool> EliminarFicheiroAsync(string bucket, string key);
    Task<bool> EliminarBucketAsync(string bucket, string key);
    Task<List<S3Bucket>?> ListBucketsAsync(string userId);
    Task<bool> UploadBinaryAsync(string bucket, string key, byte[] data, string contentType);
    Task<(byte[] Data, string ContentType)?> GetBinaryAsync(string bucket, string key);
    Task<List<S3Object>?> ListFilesAsync(string bucket);
    AmazonS3Client GetS3Client();
    
    public class S3Api : IS3Api
    {
        private ILogger<S3Api> _logger;
        private readonly AmazonS3Client _s3Client;
        private ApplicationDbContext _context;
        public S3Api(IConfiguration configuration, ILogger<S3Api> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
            var config = new AmazonS3Config
            {
                ServiceURL = configuration["S3API:Url"],
                ForcePathStyle = true
            };

            _s3Client = new AmazonS3Client(configuration["S3API:keyID"], configuration["S3API:key"], config);
        }


        public AmazonS3Client GetS3Client()
        {
            return _s3Client;
        }


        public async Task<string?> ObterFicheiroAsync(string bucket, string key, string userId)
        {
            var buckets =  await BucketAllowed(userId);
            if (!buckets.Contains(bucket)) return string.Empty; 
            
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

        public async Task<bool> EditarFicheiroAsync(string bucket, string key, string editar)
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

        public async Task<bool> CriarBucketAsync(string bucket)
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

        public async Task<bool> EliminarFicheiroAsync(string bucket, string key)
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

        public async Task<bool> EliminarBucketAsync(string bucket, string key)
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

        public async Task<List<S3Bucket>> ListBucketsAsync(string userId)
        {
            try
            {
                var allowedBuckets = await BucketAllowed(userId);
                var existingBuckets = await _s3Client.ListBucketsAsync();

                return existingBuckets.Buckets
                    .Where(b => allowedBuckets.Contains(b.BucketName))
                    .ToList();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error listing buckets");
                return [];
            }
        }

        public async Task<bool> UploadBinaryAsync(string bucket, string key, byte[] data, string contentType)
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

        public async Task<(byte[] Data, string ContentType)?> GetBinaryAsync(string bucket, string key)
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


        public async Task<List<S3Object>> ListFilesAsync(string bucket)
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

        private async Task<List<string?>> BucketAllowed(string userId)
        {
            var userProjects = await _context.UserProjectAccesses
                .Join(_context.Project,
                    pj => pj.ProjectId,
                    usp => usp.ProjectId,
                    (usp, pj) => new { pj, usp })
                .Join(_context.Organizations,
                    prev => prev.pj.OrganizationId,
                    org => org.OrganizationId,
                    (prev, org) => new { prev.pj, prev.usp, org })
                .Where(upa => upa.usp.UserId == userId)
                .Select(upa => new Project(upa.pj, upa.org))
                .ToListAsync();
            
            return userProjects.Select(pj => pj.ProjectDirectory).ToList();
        }
    }
}