using System;
using System.Collections.Generic;
using System.Text;

namespace WindCalcEngine.Contracts.Interfaces
{
    /// <summary>
    /// 对象存储(MinIO/OSS)访问契约
    /// 用于解耦底层的文件存储实现，供 ML.NET 模型的存储与热加载使用
    /// </summary>
    public interface IObjectStorageService
    {
        /// <summary>
        /// 上传文件流并返回存储路径
        /// </summary>
        /// <param name="bucketName">存储桶名称</param>
        /// <param name="objectName">文件对象名称</param>
        /// <param name="dataStream">文件数据流</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>存储的相对路径</returns>
        Task<string> UploadFileAsync(
            string bucketName,
            string objectName,
            Stream dataStream,
            CancellationToken ct = default);

        /// <summary>
        /// 下载文件为流 (用于 C# 引擎运行时从 MinIO 热加载 ML.NET 模型)
        /// </summary>
        /// <param name="bucketName">存储桶名称</param>
        /// <param name="objectName">文件对象名称</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>内存文件流</returns>
        Task<Stream> DownloadFileAsync(
            string bucketName,
            string objectName,
            CancellationToken ct = default);
    }
}
