
namespace GrpcChat.Client.GrpcClient
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// 檔案傳輸客戶端介面
    /// </summary>
    public interface IFileTransferClient
    {
        /// <summary>
        /// 上傳檔案
        /// </summary>
        /// <param name="filePath">本機檔案路徑</param>
        /// <param name="fileId">檔案ID (可選，用於續傳)</param>
        /// <param name="progress">進度回調</param>
        /// <param name="cancellationToken">取消權杖</param>
        /// <returns>上傳結果</returns>
        Task<(Exception exception, UploadResult result)> UploadFileAsync(
            string filePath,
            string fileId,
            Action<long, long> progress,
            CancellationToken cancellationToken);

        /// <summary>
        /// 下載檔案
        /// </summary>
        /// <param name="fileId">檔案ID</param>
        /// <param name="savePath">儲存路徑</param>
        /// <param name="progress">進度回調</param>
        /// <param name="cancellationToken">取消權杖</param>
        /// <returns>下載結果</returns>
        Task<(Exception exception, DownloadResult result)> DownloadFileAsync(
            string fileId,
            string savePath,
            Action<long, long> progress,
            CancellationToken cancellationToken);

        /// <summary>
        /// 取得上傳進度 (用於續傳)
        /// </summary>
        /// <param name="fileId">檔案ID</param>
        /// <returns>上傳進度</returns>
        (Exception exception, UploadProgressResult progress) GetUploadProgress(string fileId);
    }

    /// <summary>
    /// 上傳結果
    /// </summary>
    public class UploadResult
    {
        public string FileId { get; set; } = string.Empty;

        public bool Success { get; set; }

        public long BytesUploaded { get; set; }

        public string Checksum { get; set; } = string.Empty;

        public string ErrorMessage { get; set; } = string.Empty;
    }

    /// <summary>
    /// 下載結果
    /// </summary>
    public class DownloadResult
    {
        public string FileId { get; set; } = string.Empty;

        public string FileName { get; set; } = string.Empty;

        public bool Success { get; set; }

        public long BytesDownloaded { get; set; }

        public string Checksum { get; set; } = string.Empty;

        public string ErrorMessage { get; set; } = string.Empty;
    }

    /// <summary>
    /// 上傳進度結果
    /// </summary>
    public class UploadProgressResult
    {
        public string FileId { get; set; } = string.Empty;

        public string FileName { get; set; } = string.Empty;

        public long BytesReceived { get; set; }

        public long TotalSize { get; set; }

        public string Status { get; set; } = string.Empty;

        public string Checksum { get; set; } = string.Empty;
    }
}
