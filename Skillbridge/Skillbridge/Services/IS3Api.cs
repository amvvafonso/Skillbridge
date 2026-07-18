using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Skillbridge.Data;
using Skillbridge.Models.Project;

namespace Skillbridge.Services;

/// <summary>
/// Serviço de acesso ao armazenamento de objetos S3, todas as operações de leitura,
/// escrita e eliminação validam que o utilizador tem acesso ao bucket através do
/// projeto associado, exceto <see cref="UploadBinaryAsync"/> e <see cref="GetBinaryAsync"/>,
/// que devem ser protegidos pela camada chamadora (ex: controller)
/// </summary>
public interface IS3Api
{
    /// <summary>
    /// Obtém o conteúdo em texto de um ficheiro armazenado num bucket S3,
    /// desde que o utilizador tenha acesso ao bucket
    /// </summary>
    /// <param name="bucket">Nome do bucket onde o ficheiro está armazenado</param>
    /// <param name="key">Chave (nome) do ficheiro a obter</param>
    /// <param name="userId">Identificador do utilizador que solicita o acesso</param>
    /// <returns>O conteúdo do ficheiro em texto, ou uma string vazia se não houver permissão ou o ficheiro não existir</returns>
    Task<string?> ObterFicheiroAsync(string bucket, string key, string userId);
    
    /// <summary>
    /// Cria ou substitui o conteúdo de texto de um ficheiro num bucket S3,
    /// desde que o utilizador tenha acesso ao bucket
    /// </summary>
    /// <param name="bucket">Nome do bucket de destino</param>
    /// <param name="key">Chave (nome) do ficheiro a editar</param>
    /// <param name="editar">Novo conteúdo do ficheiro</param>
    /// <param name="userId">Identificador do utilizador que solicita a edição</param>
    /// <returns><c>true</c> se a operação for concluída com sucesso, caso contrário => <c>false</c>.</returns>
    Task<bool> EditarFicheiroAsync(string bucket, string key, string editar, string userId);
    
    /// <summary>
    /// Cria um bucket S3. Não valida permissões, uma vez que é chamado
    /// apenas durante a criação de um projeto
    /// </summary>
    /// <param name="bucket">Nome do bucket a criar</param>
    /// <returns><c>true</c> se o bucket for criado com sucesso, caso contrário => <c>false</c>.</returns>
    Task<bool> CriarBucketAsync(string bucket);
    
    /// <summary>
    /// Elimina um ficheiro de um bucket S3, desde que o utilizador tenha acesso ao bucket
    /// </summary>
    /// <param name="bucket">Nome do bucket onde o ficheiro está armazenado</param>
    /// <param name="key">Chave (nome) do ficheiro a eliminar</param>
    /// <param name="userId">Identificador do utilizador que solicita a eliminação</param>
    /// <returns><c>true</c> se o ficheiro for eliminado com sucesso, caso contrário => <c>false</c>.</returns>
    Task<bool> EliminarFicheiroAsync(string bucket, string key, string userId);
    
    /// <summary>
    /// Elimina um bucket S3 na sua totalidade, desde que o utilizador tenha acesso ao bucket
    /// </summary>
    /// <param name="bucket">Nome do bucket a eliminar</param>
    /// <param name="userId">Identificador do utilizador que solicita a eliminação</param>
    /// <returns><c>true</c> se o bucket for eliminado com sucesso, caso contrário => <c>false</c>.</returns>
    Task<bool> EliminarBucketAsync(string bucket, string userId);
    
    /// <summary>
    /// Lista os buckets S3 existentes aos quais o utilizador tem acesso através
    /// dos projetos em que participa
    /// </summary>
    /// <param name="userId">Identificador do user</param>
    /// <returns>Lista de <see cref="S3Bucket"/> acessíveis pelo utilizador, ou <c>null</c> em caso de erro.</returns>
    Task<List<S3Bucket>?> ListBucketsAsync(string userId);
    
    /// <summary>
    /// Faz upload de dados binários para um bucket S3, não valida permissões
    /// internamente, a camada chamadora deve garantir o controlo de acesso
    /// </summary>
    /// <param name="bucket">Nome do bucket de destino</param>
    /// <param name="key">Chave (nome) do ficheiro a criar</param>
    /// <param name="data">Conteúdo binário do ficheiro</param>
    /// <param name="contentType">Tipo de conteúdo (MIME type) do ficheiro</param>
    /// <returns><c>true</c> se o upload for concluído com sucesso, caso contrário => <c>false</c>.</returns>
    Task<bool> UploadBinaryAsync(string bucket, string key, byte[] data, string contentType);
    
    /// <summary>
    /// Obtém o conteúdo binário de um ficheiro armazenado num bucket S3 não valida
    /// permissões internamente, a camada chamadora deve garantir o controlo de acesso
    /// </summary>
    /// <param name="bucket">Nome do bucket onde o ficheiro está armazenado</param>
    /// <param name="key">Chave (nome) do ficheiro a obter</param>
    /// <returns>Os dados binários e o tipo de conteúdo, ou <c>null</c> se o ficheiro não existir.</returns>
    Task<(byte[] Data, string ContentType)?> GetBinaryAsync(string bucket, string key);
    
    /// <summary>
    /// Lista todos os ficheiros existentes num bucket S3, percorrendo a paginação
    /// automaticamente, desde que o utilizador tenha acesso ao bucket
    /// </summary>
    /// <param name="bucket">Nome do bucket a listar</param>
    /// <param name="userId">Identificador do utilizador que solicita a listagem</param>
    /// <returns>Lista de <see cref="S3Object"/> encontrados no bucket, ou <c>null</c> se não houver permissão.</returns>
    Task<List<S3Object>?> ListFilesAsync(string bucket, string userId);
    
    /// <summary>
    /// Expõe diretamente o cliente <see cref="AmazonS3Client"/> configurado.
    /// </summary>
    /// <returns>A instância do cliente S3 utilizada internamente pelo serviço.</returns>
    AmazonS3Client GetS3Client();


    /// <inheritdoc />
    public class S3Api : IS3Api
    {
        private readonly ILogger<S3Api> _logger;
        private readonly AmazonS3Client _s3Client;
        private readonly ApplicationDbContext _context;
        
        
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


        /// <inheritdoc />
        public AmazonS3Client GetS3Client()
        {
            return _s3Client;
        }


        /// <inheritdoc />
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

  
        /// <inheritdoc />
        public async Task<bool> EditarFicheiroAsync(string bucket, string key, string editar, string userId)
        {
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    var allowedBuckets = await BucketAllowed(userId);
                    if (!allowedBuckets.Contains(bucket)) return false;
                    
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
                    _logger.LogError(e, "Error editing file {Key} from S3", key);
                    
                }
            }

            return false;
        }


        /// <inheritdoc />
        public async Task<bool> CriarBucketAsync(string bucket)
        {
            for (var i = 0; i < 3; i++)
            {
                try
                {
                    var request = new PutBucketRequest
                    {
                        BucketName = bucket,
                        UseClientRegion = true,
                        ObjectLockEnabledForBucket = true
                    };

                    var response = await _s3Client.PutBucketAsync(request);

                    return response.HttpStatusCode == System.Net.HttpStatusCode.OK;
                }
                catch (AmazonS3Exception e)
                {
                    _logger.LogError(e, "Error creating bucket {Bucket}", bucket);
                }
            }
            return false;
        }

   
        /// <inheritdoc />
        public async Task<bool> EliminarFicheiroAsync(string bucket, string key, string userId)
        {
            for (var i = 0; i < 3; i++)
            {
                try
                {
                    var allowedBuckets = await BucketAllowed(userId);
                    if (!allowedBuckets.Contains(bucket)) return false;
                    
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

  
        /// <inheritdoc />
        public async Task<bool> EliminarBucketAsync(string bucket, string userId)
        {
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    var allowedBuckets = await BucketAllowed(userId);
                    if (!allowedBuckets.Contains(bucket)) return false;
                    
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

        /// <inheritdoc />
        public async Task<List<S3Bucket>?> ListBucketsAsync(string userId)
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

        /// <inheritdoc />
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
                        InputStream = new MemoryStream(data),
                        ContentType = contentType

                    };
                    var response = await _s3Client.PutObjectAsync(request);
                    return response.HttpStatusCode == System.Net.HttpStatusCode.OK;
                }
                catch (AmazonS3Exception e)
                {
                    _logger.LogError(e, "Error uploading binary {Key} to S3", key);
                }
            }

            return false;
        }

        /// <inheritdoc />
        public async Task<(byte[] Data, string ContentType)?> GetBinaryAsync(string bucket, string key)
        {
            for (var i = 0; i < 3; i++)
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


        /// <inheritdoc />
        public async Task<List<S3Object>?> ListFilesAsync(string bucket, string userId)
        {
            try
            {
                var allowedBuckets = await BucketAllowed(userId);
                if (!allowedBuckets.Contains(bucket)) return null;
                
                string? continuationToken = string.Empty;
                var all = new List<S3Object>();
                do
                {
                    var request = await _s3Client.ListObjectsV2Async(new ListObjectsV2Request
                    {
                        BucketName = bucket,
                        ContinuationToken = continuationToken
                    });

                    all.AddRange(request.S3Objects);

                    var check = request.IsTruncated ?? false;
                    
                    continuationToken = check ? request.NextContinuationToken : null;

                } while (continuationToken != null);

                return all;
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