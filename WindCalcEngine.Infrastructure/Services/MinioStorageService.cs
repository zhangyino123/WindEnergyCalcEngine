using Minio;
using Minio.DataModel.Args;
using System;
using System.Collections.Generic;
using System.Text;
using WindCalcEngine.Contracts.Interfaces;


namespace WindCalcEngine.Infrastructure.Services
{
    /// <summary>
    /// MinIO 对象存储实现，用于 ML.NET 模型的存储与加载
    /// </summary>
    public class MinioStorageService : IObjectStorageService
    {
        private readonly IMinioClient _minioClient;

        public MinioStorageService(IMinioClient minioClient)
        {
            _minioClient = minioClient;
        }

        public async Task<string> UploadFileAsync(string bucketName, string objectName, Stream dataStream, CancellationToken ct = default)
        {
            // 确保存储桶存在
            bool found = await _minioClient.BucketExistsAsync(new BucketExistsArgs().WithBucket(bucketName), ct);
            if (!found)
            {
                await _minioClient.MakeBucketAsync(new MakeBucketArgs().WithBucket(bucketName), ct);
            }

            await _minioClient.PutObjectAsync(new PutObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithStreamData(dataStream)
                .WithObjectSize(dataStream.Length)
                .WithContentType("application/zip"), ct);

            return $"{bucketName}/{objectName}";
        }

        public async Task<Stream> DownloadFileAsync(string bucketName, string objectName, CancellationToken ct = default)
        {
            var memoryStream = new MemoryStream();
            await _minioClient.GetObjectAsync(new GetObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithCallbackStream(stream => stream.CopyTo(memoryStream)), ct);

            memoryStream.Position = 0;
            return memoryStream;
        }
    }
}
