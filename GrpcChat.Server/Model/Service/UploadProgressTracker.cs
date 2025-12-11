
namespace GrpcChat.Server.Model.Service
{
    using System;
    using System.Text.Json;
    using GrpcChat.Server.Applibs;
    using Microsoft.Extensions.Logging;
    using StackExchange.Redis;

    /// <summary>
    /// 上傳進度追蹤器實作 (使用Redis)
    /// </summary>
    public class UploadProgressTracker : IUploadProgressTracker
    {
        private ILogger<UploadProgressTracker> logger;

        private IDatabase redisDb;

        private string keyPrefix;

        public UploadProgressTracker(ILogger<UploadProgressTracker> logger)
        {
            this.logger = logger;
            this.redisDb = NoSqlService.RedisConnections.GetDatabase(NoSqlService.RedisDataBase);
            this.keyPrefix = $"{NoSqlService.RedisAffixKey}:upload:";
        }

        public (Exception? exception, UploadProgressInfo? progress) GetProgress(string fileId)
        {
            try
            {
                var key = this.GetKey(fileId);
                var value = this.redisDb.StringGet(key);

                if (value.IsNullOrEmpty)
                {
                    return (null, null);
                }

                var progress = JsonSerializer.Deserialize<UploadProgressInfo>(value!);
                return (null, progress);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, $"{this.GetType().Name} GetProgress Exception, fileId: {fileId}");
                return (ex, null);
            }
        }

        public Exception? UpdateProgress(string fileId, string fileName, long bytesReceived, long totalSize)
        {
            try
            {
                var key = this.GetKey(fileId);
                var progress = new UploadProgressInfo
                {
                    FileId = fileId,
                    FileName = fileName,
                    BytesReceived = bytesReceived,
                    TotalSize = totalSize,
                    Status = "uploading"
                };

                var json = JsonSerializer.Serialize(progress);
                var expiry = TimeSpan.FromSeconds(FileTransferConfig.UploadProgressExpireSeconds);
                this.redisDb.StringSet(key, json, expiry);

                return null;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, $"{this.GetType().Name} UpdateProgress Exception, fileId: {fileId}");
                return ex;
            }
        }

        public Exception? CompleteUpload(string fileId, string checksum)
        {
            try
            {
                var key = this.GetKey(fileId);
                var value = this.redisDb.StringGet(key);

                if (value.IsNullOrEmpty)
                {
                    return new InvalidOperationException($"找不到上傳進度記錄: {fileId}");
                }

                var progress = JsonSerializer.Deserialize<UploadProgressInfo>(value!);
                if (progress != null)
                {
                    progress.Status = "completed";
                    progress.Checksum = checksum;
                }

                var json = JsonSerializer.Serialize(progress);
                var expiry = TimeSpan.FromSeconds(FileTransferConfig.UploadProgressExpireSeconds);
                this.redisDb.StringSet(key, json, expiry);

                return null;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, $"{this.GetType().Name} CompleteUpload Exception, fileId: {fileId}");
                return ex;
            }
        }

        public Exception? FailUpload(string fileId, string errorMessage)
        {
            try
            {
                var key = this.GetKey(fileId);
                var value = this.redisDb.StringGet(key);

                UploadProgressInfo progress;

                if (value.IsNullOrEmpty)
                {
                    progress = new UploadProgressInfo
                    {
                        FileId = fileId,
                        Status = "failed",
                        ErrorMessage = errorMessage
                    };
                }
                else
                {
                    progress = JsonSerializer.Deserialize<UploadProgressInfo>(value);
                    progress.Status = "failed";
                    progress.ErrorMessage = errorMessage;
                }

                var json = JsonSerializer.Serialize(progress);
                var expiry = TimeSpan.FromSeconds(FileTransferConfig.UploadProgressExpireSeconds);
                this.redisDb.StringSet(key, json, expiry);

                return null;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, $"{this.GetType().Name} FailUpload Exception, fileId: {fileId}");
                return ex;
            }
        }

        public Exception? RemoveProgress(string fileId)
        {
            try
            {
                var key = this.GetKey(fileId);
                this.redisDb.KeyDelete(key);
                return null;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, $"{this.GetType().Name} RemoveProgress Exception, fileId: {fileId}");
                return ex;
            }
        }

        private string GetKey(string fileId)
        {
            return $"{this.keyPrefix}{fileId}";
        }
    }
}
