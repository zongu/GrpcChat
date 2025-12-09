
namespace GrpcChat.Client.GrpcClient
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Security.Cryptography;
    using System.Threading;
    using System.Threading.Tasks;
    using Google.Protobuf;
    using Grpc.Core;
    using GrpcChat.Service;
    using NLog;

    /// <summary>
    /// 檔案傳輸客戶端實作
    /// </summary>
    public class FileTransferClient : IFileTransferClient
    {
        private ILogger logger;

        private FileTransferService.FileTransferServiceClient grpcClient;

        private int chunkSize;

        private int timeoutSeconds;

        /// <summary>
        /// 允許的檔案類型
        /// </summary>
        private HashSet<string> allowedExtensions;

        /// <summary>
        /// 最大檔案大小 (10MB)
        /// </summary>
        private long maxFileSize;

        public FileTransferClient(
            ILogger logger,
            FileTransferService.FileTransferServiceClient grpcClient)
        {
            this.logger = logger;
            this.grpcClient = grpcClient;
            this.chunkSize = 64 * 1024;
            this.timeoutSeconds = 300;
            this.maxFileSize = 10 * 1024 * 1024;
            this.allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".jpg",
                ".jpeg",
                ".png",
                ".gif",
                ".bmp",
                ".webp"
            };
        }

        public async Task<(Exception exception, UploadResult result)> UploadFileAsync(
            string filePath,
            string fileId,
            Action<long, long> progress,
            CancellationToken cancellationToken)
        {
            try
            {
                // 驗證檔案存在
                if (!File.Exists(filePath))
                {
                    return (new FileNotFoundException($"檔案不存在: {filePath}"), null);
                }

                var fileInfo = new FileInfo(filePath);

                // 驗證檔案類型
                var extension = Path.GetExtension(filePath);

                if (!this.allowedExtensions.Contains(extension))
                {
                    return (new InvalidOperationException($"不支援的檔案類型: {extension}，僅允許圖片檔案"), null);
                }

                // 驗證檔案大小
                if (fileInfo.Length > this.maxFileSize)
                {
                    return (new InvalidOperationException($"檔案大小超過限制，最大允許 {this.maxFileSize / 1024 / 1024}MB"), null);
                }

                // 產生或補全檔案ID (確保包含副檔名)
                if (string.IsNullOrEmpty(fileId))
                {
                    fileId = Guid.NewGuid().ToString("N") + extension.ToLowerInvariant();
                }
                else if (!Path.HasExtension(fileId))
                {
                    // 自定義名稱沒有副檔名時，自動補上
                    fileId = fileId + extension.ToLowerInvariant();
                }

                var fileName = Path.GetFileName(filePath);
                var totalSize = fileInfo.Length;

                // 取得續傳進度
                long startOffset = 0;
                var progressResult = this.GetUploadProgress(fileId);

                if (progressResult.exception == null && progressResult.progress != null)
                {
                    if (progressResult.progress.Status == "uploading")
                    {
                        startOffset = progressResult.progress.BytesReceived;
                        this.logger.Info($"續傳檔案: {fileId}, 從位置 {startOffset} 開始");
                    }
                }

                // 建立逾時取消權杖
                using (var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(this.timeoutSeconds)))
                using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token))
                {
                    var callOptions = new CallOptions(cancellationToken: linkedCts.Token);

                    using (var call = this.grpcClient.UploadFile(callOptions))
                    {
                        using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            // 跳到續傳位置
                            if (startOffset > 0)
                            {
                                stream.Seek(startOffset, SeekOrigin.Begin);
                            }

                            var buffer = new byte[this.chunkSize];
                            var currentOffset = startOffset;
                            int bytesRead;
                            var isFirst = true;

                            while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, linkedCts.Token).ConfigureAwait(false)) > 0)
                            {
                                linkedCts.Token.ThrowIfCancellationRequested();

                                var chunk = new FileChunk
                                {
                                    FileId = fileId,
                                    FileName = fileName,
                                    ChunkData = ByteString.CopyFrom(buffer, 0, bytesRead),
                                    Offset = currentOffset,
                                    TotalSize = totalSize,
                                    IsCompleted = false
                                };

                                // 第一個區塊加入 MIME 類型
                                if (isFirst)
                                {
                                    chunk.MimeType = this.GetMimeType(extension);
                                    isFirst = false;
                                }

                                await call.RequestStream.WriteAsync(chunk).ConfigureAwait(false);
                                currentOffset += bytesRead;

                                // 回報進度
                                progress?.Invoke(currentOffset, totalSize);
                            }

                            // 計算校驗碼
                            stream.Seek(0, SeekOrigin.Begin);
                            string checksum;

                            using (var md5 = MD5.Create())
                            {
                                var hash = md5.ComputeHash(stream);
                                checksum = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                            }

                            // 發送完成標記
                            var finalChunk = new FileChunk
                            {
                                FileId = fileId,
                                FileName = fileName,
                                ChunkData = ByteString.Empty,
                                Offset = currentOffset,
                                TotalSize = totalSize,
                                IsCompleted = true,
                                Checksum = checksum
                            };

                            await call.RequestStream.WriteAsync(finalChunk).ConfigureAwait(false);
                            await call.RequestStream.CompleteAsync().ConfigureAwait(false);

                            // 等待回應
                            var response = await call.ResponseAsync.ConfigureAwait(false);

                            return (null, new UploadResult
                            {
                                FileId = response.FileId,
                                Success = response.Success,
                                BytesUploaded = response.BytesReceived,
                                Checksum = response.Checksum,
                                ErrorMessage = response.ErrorMessage
                            });
                        }
                    }
                }
            }
            catch (OperationCanceledException ex)
            {
                this.logger.Warn($"檔案上傳已取消: {filePath}");
                return (ex, new UploadResult
                {
                    FileId = fileId,
                    Success = false,
                    ErrorMessage = "上傳已取消"
                });
            }
            catch (RpcException ex)
            {
                this.logger.Error(ex, $"{this.GetType().Name} UploadFileAsync RpcException");
                return (ex, new UploadResult
                {
                    FileId = fileId,
                    Success = false,
                    ErrorMessage = ex.Status.Detail
                });
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, $"{this.GetType().Name} UploadFileAsync Exception");
                return (ex, null);
            }
        }

        public async Task<(Exception exception, DownloadResult result)> DownloadFileAsync(
            string fileId,
            string savePath,
            Action<long, long> progress,
            CancellationToken cancellationToken)
        {
            string tempPath = null;

            try
            {
                // 確保目錄存在
                var directory = Path.GetDirectoryName(savePath);

                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // 暫存檔案路徑 (用於續傳)
                tempPath = savePath + ".downloading";
                long startOffset = 0;

                // 檢查是否有暫存檔案 (續傳)
                if (File.Exists(tempPath))
                {
                    var tempInfo = new FileInfo(tempPath);
                    startOffset = tempInfo.Length;
                    this.logger.Info($"續傳下載: {fileId}, 從位置 {startOffset} 開始");
                }

                // 建立逾時取消權杖
                using (var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(this.timeoutSeconds)))
                using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token))
                {
                    var request = new DownloadRequest
                    {
                        FileId = fileId,
                        Offset = startOffset
                    };

                    var callOptions = new CallOptions(cancellationToken: linkedCts.Token);

                    using (var call = this.grpcClient.DownloadFile(request, callOptions))
                    {
                        using (var stream = new FileStream(tempPath, FileMode.Append, FileAccess.Write, FileShare.None))
                        {
                            string fileName = null;
                            long totalSize = 0;
                            long bytesReceived = startOffset;
                            string checksum = null;

                            await foreach (var chunk in call.ResponseStream.ReadAllAsync(linkedCts.Token).ConfigureAwait(false))
                            {
                                linkedCts.Token.ThrowIfCancellationRequested();

                                if (fileName == null)
                                {
                                    fileName = chunk.FileName;
                                    totalSize = chunk.TotalSize;
                                }

                                // 寫入資料
                                if (chunk.ChunkData != null && chunk.ChunkData.Length > 0)
                                {
                                    var data = chunk.ChunkData.ToByteArray();
                                    await stream.WriteAsync(data, 0, data.Length, linkedCts.Token).ConfigureAwait(false);
                                    bytesReceived += data.Length;

                                    // 回報進度
                                    progress?.Invoke(bytesReceived, totalSize);
                                }

                                // 完成標記
                                if (chunk.IsCompleted)
                                {
                                    checksum = chunk.Checksum;
                                }
                            }

                            // 關閉串流
                            stream.Close();

                            // 驗證校驗碼
                            if (!string.IsNullOrEmpty(checksum))
                            {
                                using (var md5 = MD5.Create())
                                using (var fileStream = File.OpenRead(tempPath))
                                {
                                    var hash = md5.ComputeHash(fileStream);
                                    var localChecksum = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();

                                    if (localChecksum != checksum)
                                    {
                                        File.Delete(tempPath);
                                        return (new InvalidOperationException("校驗碼不符，檔案可能損壞"), null);
                                    }
                                }
                            }

                            // 移動到最終位置
                            if (File.Exists(savePath))
                            {
                                File.Delete(savePath);
                            }

                            File.Move(tempPath, savePath);

                            return (null, new DownloadResult
                            {
                                FileId = fileId,
                                FileName = fileName,
                                Success = true,
                                BytesDownloaded = bytesReceived,
                                Checksum = checksum
                            });
                        }
                    }
                }
            }
            catch (OperationCanceledException ex)
            {
                this.logger.Warn($"檔案下載已取消: {fileId}");
                return (ex, new DownloadResult
                {
                    FileId = fileId,
                    Success = false,
                    ErrorMessage = "下載已取消"
                });
            }
            catch (RpcException ex)
            {
                this.logger.Error(ex, $"{this.GetType().Name} DownloadFileAsync RpcException");
                return (ex, new DownloadResult
                {
                    FileId = fileId,
                    Success = false,
                    ErrorMessage = ex.Status.Detail
                });
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, $"{this.GetType().Name} DownloadFileAsync Exception");
                return (ex, null);
            }
        }

        public (Exception exception, UploadProgressResult progress) GetUploadProgress(string fileId)
        {
            try
            {
                var request = new ResumeUploadRequest
                {
                    FileId = fileId
                };

                var response = this.grpcClient.GetUploadProgress(request);

                return (null, new UploadProgressResult
                {
                    FileId = response.FileId,
                    FileName = response.FileName,
                    BytesReceived = response.BytesReceived,
                    TotalSize = response.TotalSize,
                    Status = response.Status,
                    Checksum = response.Checksum
                });
            }
            catch (RpcException ex)
            {
                this.logger.Error(ex, $"{this.GetType().Name} GetUploadProgress RpcException");
                return (ex, null);
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, $"{this.GetType().Name} GetUploadProgress Exception");
                return (ex, null);
            }
        }

        private string GetMimeType(string extension)
        {
            return extension.ToLowerInvariant() switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                ".webp" => "image/webp",
                _ => "application/octet-stream"
            };
        }
    }
}
