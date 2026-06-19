namespace Skillbridge.Models.Utils;
using Amazon.S3;
using Amazon.S3.Model;

public class S3Api
{
    private readonly AmazonS3Client _s3client;
    private readonly string S3Url = "http://s3.jaranero.duckdns.org";
    public S3Api()
    {
        var config = new AmazonS3Config
        {
            ServiceURL = S3Url,
            ForcePathStyle = true
        };
        
        _s3client = new  AmazonS3Client("skillbridge", "skillbridge", config);
    }
    public async Task<string> ObterFicheiroAsync(string bucket, string key)
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

    public async Task EditarFicheiroAsync(string bucket, string key, string editar)
    {
        await _s3client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = bucket,
            Key = key,
            ContentBody = editar
        });
    }
}