
namespace GrpcChat.Server.Applibs
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// 檔案傳輸設定
    /// </summary>
    public static class FileTransferConfig
    {
        /// <summary>
        /// 允許的檔案類型
        /// </summary>
        public static readonly HashSet<string> AllowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".gif",
            ".bmp",
            ".webp"
        };

        /// <summary>
        /// 允許的 MIME 類型
        /// </summary>
        public static readonly HashSet<string> AllowedMimeTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg",
            "image/png",
            "image/gif",
            "image/bmp",
            "image/webp"
        };

        /// <summary>
        /// 最大檔案大小 (10MB)
        /// </summary>
        public static readonly long MaxFileSize = 10 * 1024 * 1024;

        /// <summary>
        /// 區塊大小 (64KB)
        /// </summary>
        public static readonly int ChunkSize = 64 * 1024;

        /// <summary>
        /// 檔案儲存路徑
        /// </summary>
        public static readonly string StoragePath = ConfigHelper.Config["FileStoragePath"] ?? "uploads";

        /// <summary>
        /// 上傳進度過期時間 (秒)
        /// </summary>
        public static readonly int UploadProgressExpireSeconds = 24 * 60 * 60;

        /// <summary>
        /// 預設逾時時間 (秒)
        /// </summary>
        public static readonly int DefaultTimeoutSeconds = 300;
    }
}
