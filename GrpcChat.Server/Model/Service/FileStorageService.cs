
namespace GrpcChat.Server.Model.Service
{
    using System;
    using System.IO;
    using System.Security.Cryptography;
    using GrpcChat.Server.Applibs;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// 檔案儲存服務實作
    /// </summary>
    public class FileStorageService : IFileStorageService
    {
        private ILogger<FileStorageService> logger;

        private string storagePath;

        private string tempPath;

        public FileStorageService(ILogger<FileStorageService> logger)
        {
            this.logger = logger;
            this.storagePath = Path.GetFullPath(FileTransferConfig.StoragePath);
            this.tempPath = Path.Combine(this.storagePath, "temp");

            this.EnsureDirectoriesExist();
        }

        public (bool isValid, string? errorMessage) ValidateFileType(string fileName, string mimeType)
        {
            try
            {
                var extension = Path.GetExtension(fileName);

                if (string.IsNullOrEmpty(extension))
                {
                    return (false, "檔案缺少副檔名");
                }

                if (!FileTransferConfig.AllowedExtensions.Contains(extension))
                {
                    return (false, $"不支援的檔案類型: {extension}，僅允許圖片檔案");
                }

                if (!string.IsNullOrEmpty(mimeType) && !FileTransferConfig.AllowedMimeTypes.Contains(mimeType))
                {
                    return (false, $"不支援的MIME類型: {mimeType}");
                }

                return (true, null);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, $"{this.GetType().Name} ValidateFileType Exception");
                return (false, ex.Message);
            }
        }

        public (bool isValid, string? errorMessage) ValidateFileSize(long fileSize)
        {
            if (fileSize <= 0)
            {
                return (false, "檔案大小必須大於0");
            }

            if (fileSize > FileTransferConfig.MaxFileSize)
            {
                return (false, $"檔案大小超過限制，最大允許 {FileTransferConfig.MaxFileSize / 1024 / 1024}MB");
            }

            return (true, null);
        }

        public (Exception? exception, FileInfo? fileInfo) GetFileInfo(string fileId)
        {
            try
            {
                var filePath = this.GetFilePath(fileId);

                if (!File.Exists(filePath))
                {
                    return (new FileNotFoundException($"檔案不存在: {fileId}"), null);
                }

                var fileInfo = new FileInfo(filePath);
                return (null, fileInfo);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, $"{this.GetType().Name} GetFileInfo Exception, fileId: {fileId}");
                return (ex, null);
            }
        }

        public (Exception? exception, FileStream? stream) CreateWriteStream(string fileId, string fileName)
        {
            try
            {
                var tempFilePath = this.GetTempFilePath(fileId);
                var stream = new FileStream(tempFilePath, FileMode.Append, FileAccess.Write, FileShare.None);
                return (null, stream);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, $"{this.GetType().Name} CreateWriteStream Exception, fileId: {fileId}");
                return (ex, null);
            }
        }

        public (Exception? exception, FileStream? stream) CreateReadStream(string fileId)
        {
            try
            {
                var filePath = this.GetFilePath(fileId);

                if (!File.Exists(filePath))
                {
                    return (new FileNotFoundException($"檔案不存在: {fileId}"), null);
                }

                var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                return (null, stream);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, $"{this.GetType().Name} CreateReadStream Exception, fileId: {fileId}");
                return (ex, null);
            }
        }

        public Exception? CompleteWrite(string fileId)
        {
            try
            {
                var tempFilePath = this.GetTempFilePath(fileId);
                var filePath = this.GetFilePath(fileId);

                if (!File.Exists(tempFilePath))
                {
                    return new FileNotFoundException($"暫存檔案不存在: {fileId}");
                }

                File.Move(tempFilePath, filePath, true);
                return null;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, $"{this.GetType().Name} CompleteWrite Exception, fileId: {fileId}");
                return ex;
            }
        }

        public Exception? DeleteTempFile(string fileId)
        {
            try
            {
                var tempFilePath = this.GetTempFilePath(fileId);

                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                }

                return null;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, $"{this.GetType().Name} DeleteTempFile Exception, fileId: {fileId}");
                return ex;
            }
        }

        public (Exception? exception, string? checksum) CalculateChecksum(string fileId)
        {
            try
            {
                var filePath = this.GetFilePath(fileId);

                if (!File.Exists(filePath))
                {
                    return (new FileNotFoundException($"檔案不存在: {fileId}"), null);
                }

                using (var md5 = MD5.Create())
                using (var stream = File.OpenRead(filePath))
                {
                    var hash = md5.ComputeHash(stream);
                    var checksum = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                    return (null, checksum);
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, $"{this.GetType().Name} CalculateChecksum Exception, fileId: {fileId}");
                return (ex, null);
            }
        }

        public (Exception? exception, long bytesWritten) GetTempFileSize(string fileId)
        {
            try
            {
                var tempFilePath = this.GetTempFilePath(fileId);

                if (!File.Exists(tempFilePath))
                {
                    return (null, 0);
                }

                var fileInfo = new FileInfo(tempFilePath);
                return (null, fileInfo.Length);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, $"{this.GetType().Name} GetTempFileSize Exception, fileId: {fileId}");
                return (ex, 0);
            }
        }

        private void EnsureDirectoriesExist()
        {
            try
            {
                if (!Directory.Exists(this.storagePath))
                {
                    Directory.CreateDirectory(this.storagePath);
                }

                if (!Directory.Exists(this.tempPath))
                {
                    Directory.CreateDirectory(this.tempPath);
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, $"{this.GetType().Name} EnsureDirectoriesExist Exception");
            }
        }

        private string GetFilePath(string fileId)
        {
            return Path.Combine(this.storagePath, fileId);
        }

        private string GetTempFilePath(string fileId)
        {
            return Path.Combine(this.tempPath, $"{fileId}.uploading");
        }
    }
}
