
namespace GrpcChat.Server.Command
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Google.Protobuf;
    using Grpc.Core;
    using GrpcChat.Server.Applibs;
    using GrpcChat.Server.Model.Service;
    using GrpcChat.Service;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// 檔案傳輸 gRPC 服務
    /// </summary>
    public class FileTransferCommand : FileTransferService.FileTransferServiceBase
    {
        private ILogger<FileTransferCommand> logger;

        private IFileStorageService storageService;

        private IUploadProgressTracker progressTracker;

        public FileTransferCommand(
            ILogger<FileTransferCommand> logger,
            IFileStorageService storageService,
            IUploadProgressTracker progressTracker)
        {
            this.logger = logger;
            this.storageService = storageService;
            this.progressTracker = progressTracker;
        }

        /// <summary>
        /// 下載檔案 (Server Stream RPC)
        /// </summary>
        public override async Task DownloadFile(
            DownloadRequest request,
            IServerStreamWriter<FileChunk> responseStream,
            ServerCallContext context)
        {
            var fileId = request.FileId;
            this.logger.LogInformation($"開始下載檔案: {fileId}, Offset: {request.Offset}");

            try
            {
                // 取得檔案資訊
                var fileInfoResult = this.storageService.GetFileInfo(fileId);

                if (fileInfoResult.exception != null)
                {
                    throw new RpcException(new Status(StatusCode.NotFound, fileInfoResult.exception.Message));
                }

                var fileInfo = fileInfoResult.fileInfo;

                // 建立讀取串流
                var streamResult = this.storageService.CreateReadStream(fileId);

                if (streamResult.exception != null)
                {
                    throw new RpcException(new Status(StatusCode.Internal, streamResult.exception.Message));
                }

                using (var stream = streamResult.stream)
                {
                    // 處理斷點續傳
                    if (request.Offset > 0)
                    {
                        if (request.Offset > fileInfo.Length)
                        {
                            throw new RpcException(new Status(StatusCode.InvalidArgument, "Offset 超過檔案大小"));
                        }

                        // 如果 Offset 等於檔案大小，表示已下載完成，直接傳送完成訊息
                        if (request.Offset == fileInfo.Length)
                        {
                            var earlyChecksumResult = this.storageService.CalculateChecksum(fileId);
                            var earlyChecksum = earlyChecksumResult.exception == null ? earlyChecksumResult.checksum : string.Empty;

                            var completeChunk = new FileChunk
                            {
                                FileId = fileId,
                                FileName = fileInfo.Name,
                                ChunkData = ByteString.Empty,
                                Offset = fileInfo.Length,
                                TotalSize = fileInfo.Length,
                                IsCompleted = true,
                                Checksum = earlyChecksum
                            };

                            await responseStream.WriteAsync(completeChunk).ConfigureAwait(false);
                            this.logger.LogInformation($"檔案已下載完成 (續傳檢查): {fileId}");
                            return;
                        }

                        stream.Seek(request.Offset, System.IO.SeekOrigin.Begin);
                    }

                    var buffer = new byte[FileTransferConfig.ChunkSize];
                    var totalSize = fileInfo.Length;
                    var currentOffset = request.Offset;
                    int bytesRead;

                    while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, context.CancellationToken).ConfigureAwait(false)) > 0)
                    {
                        // 檢查取消請求
                        context.CancellationToken.ThrowIfCancellationRequested();

                        var chunk = new FileChunk
                        {
                            FileId = fileId,
                            FileName = fileInfo.Name,
                            ChunkData = ByteString.CopyFrom(buffer, 0, bytesRead),
                            Offset = currentOffset,
                            TotalSize = totalSize,
                            IsCompleted = false
                        };

                        await responseStream.WriteAsync(chunk).ConfigureAwait(false);
                        currentOffset += bytesRead;
                    }

                    // 計算校驗碼並發送最後一個區塊
                    var checksumResult = this.storageService.CalculateChecksum(fileId);
                    var checksum = checksumResult.exception == null ? checksumResult.checksum : string.Empty;

                    var finalChunk = new FileChunk
                    {
                        FileId = fileId,
                        FileName = fileInfo.Name,
                        ChunkData = ByteString.Empty,
                        Offset = currentOffset,
                        TotalSize = totalSize,
                        IsCompleted = true,
                        Checksum = checksum
                    };

                    await responseStream.WriteAsync(finalChunk).ConfigureAwait(false);
                }

                this.logger.LogInformation($"檔案下載完成: {fileId}");
            }
            catch (OperationCanceledException)
            {
                this.logger.LogWarning($"檔案下載已取消: {fileId}");
                throw new RpcException(new Status(StatusCode.Cancelled, "下載已取消"));
            }
            catch (RpcException)
            {
                throw;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, $"{this.GetType().Name} DownloadFile Exception, fileId: {fileId}");
                throw new RpcException(new Status(StatusCode.Internal, ex.Message));
            }
        }

        /// <summary>
        /// 上傳檔案 (Client Stream RPC)
        /// </summary>
        public override async Task<UploadStatus> UploadFile(
            IAsyncStreamReader<FileChunk> requestStream,
            ServerCallContext context)
        {
            string fileId = null;
            string fileName = null;
            long totalSize = 0;
            long bytesReceived = 0;

            try
            {
                System.IO.FileStream writeStream = null;

                await foreach (var chunk in requestStream.ReadAllAsync(context.CancellationToken).ConfigureAwait(false))
                {
                    // 檢查取消請求
                    context.CancellationToken.ThrowIfCancellationRequested();

                    // 第一個區塊: 初始化
                    if (fileId == null)
                    {
                        fileId = chunk.FileId;
                        fileName = chunk.FileName;
                        totalSize = chunk.TotalSize;

                        // 驗證檔案類型
                        var typeValidation = this.storageService.ValidateFileType(fileName, chunk.MimeType);

                        if (!typeValidation.isValid)
                        {
                            return new UploadStatus
                            {
                                FileId = fileId,
                                Success = false,
                                ErrorMessage = typeValidation.errorMessage
                            };
                        }

                        // 驗證檔案大小
                        var sizeValidation = this.storageService.ValidateFileSize(totalSize);

                        if (!sizeValidation.isValid)
                        {
                            return new UploadStatus
                            {
                                FileId = fileId,
                                Success = false,
                                ErrorMessage = sizeValidation.errorMessage
                            };
                        }

                        // 檢查是否有續傳需求
                        var tempSizeResult = this.storageService.GetTempFileSize(fileId);
                        bytesReceived = tempSizeResult.bytesWritten;

                        this.logger.LogInformation($"開始上傳檔案: {fileId}, {fileName}, 總大小: {totalSize}, 續傳位置: {bytesReceived}");

                        // 建立寫入串流
                        var streamResult = this.storageService.CreateWriteStream(fileId, fileName);

                        if (streamResult.exception != null)
                        {
                            return new UploadStatus
                            {
                                FileId = fileId,
                                Success = false,
                                ErrorMessage = streamResult.exception.Message
                            };
                        }

                        writeStream = streamResult.stream;
                    }

                    // 寫入資料
                    if (chunk.ChunkData != null && chunk.ChunkData.Length > 0)
                    {
                        // DoS 防護: 檢查是否超過預期大小
                        if (bytesReceived + chunk.ChunkData.Length > totalSize + FileTransferConfig.ChunkSize)
                        {
                            writeStream?.Dispose();
                            this.storageService.DeleteTempFile(fileId);
                            this.progressTracker.FailUpload(fileId, "資料大小異常，可能為惡意攻擊");

                            return new UploadStatus
                            {
                                FileId = fileId,
                                Success = false,
                                ErrorMessage = "資料大小異常"
                            };
                        }

                        var data = chunk.ChunkData.ToByteArray();
                        await writeStream.WriteAsync(data, 0, data.Length, context.CancellationToken).ConfigureAwait(false);
                        bytesReceived += data.Length;

                        // 更新進度
                        this.progressTracker.UpdateProgress(fileId, fileName, bytesReceived, totalSize);
                    }

                    // 最後一個區塊: 完成上傳
                    if (chunk.IsCompleted)
                    {
                        writeStream?.Dispose();
                        writeStream = null;

                        // 完成檔案寫入
                        var completeResult = this.storageService.CompleteWrite(fileId);

                        if (completeResult != null)
                        {
                            this.progressTracker.FailUpload(fileId, completeResult.Message);

                            return new UploadStatus
                            {
                                FileId = fileId,
                                Success = false,
                                ErrorMessage = completeResult.Message
                            };
                        }

                        // 計算校驗碼
                        var checksumResult = this.storageService.CalculateChecksum(fileId);

                        if (checksumResult.exception != null)
                        {
                            this.progressTracker.FailUpload(fileId, checksumResult.exception.Message);

                            return new UploadStatus
                            {
                                FileId = fileId,
                                Success = false,
                                ErrorMessage = checksumResult.exception.Message
                            };
                        }

                        // 驗證校驗碼 (如果客戶端提供)
                        if (!string.IsNullOrEmpty(chunk.Checksum) && chunk.Checksum != checksumResult.checksum)
                        {
                            this.storageService.DeleteTempFile(fileId);
                            this.progressTracker.FailUpload(fileId, "校驗碼不符");

                            return new UploadStatus
                            {
                                FileId = fileId,
                                Success = false,
                                ErrorMessage = "校驗碼不符，檔案可能損壞"
                            };
                        }

                        // 標記上傳完成
                        this.progressTracker.CompleteUpload(fileId, checksumResult.checksum);

                        this.logger.LogInformation($"檔案上傳完成: {fileId}, 校驗碼: {checksumResult.checksum}");

                        return new UploadStatus
                        {
                            FileId = fileId,
                            Success = true,
                            BytesReceived = bytesReceived,
                            Checksum = checksumResult.checksum
                        };
                    }
                }

                // 串流結束但未收到完成標記
                writeStream?.Dispose();

                return new UploadStatus
                {
                    FileId = fileId ?? string.Empty,
                    Success = false,
                    BytesReceived = bytesReceived,
                    ErrorMessage = "上傳未完成"
                };
            }
            catch (OperationCanceledException)
            {
                this.logger.LogWarning($"檔案上傳已取消: {fileId}");
                this.progressTracker.FailUpload(fileId, "上傳已取消");

                return new UploadStatus
                {
                    FileId = fileId ?? string.Empty,
                    Success = false,
                    BytesReceived = bytesReceived,
                    ErrorMessage = "上傳已取消"
                };
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, $"{this.GetType().Name} UploadFile Exception, fileId: {fileId}");
                this.progressTracker.FailUpload(fileId, ex.Message);

                return new UploadStatus
                {
                    FileId = fileId ?? string.Empty,
                    Success = false,
                    BytesReceived = bytesReceived,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// 取得上傳進度 (用於續傳)
        /// </summary>
        public override Task<UploadProgress> GetUploadProgress(
            ResumeUploadRequest request,
            ServerCallContext context)
        {
            try
            {
                var fileId = request.FileId;
                var progressResult = this.progressTracker.GetProgress(fileId);

                if (progressResult.exception != null)
                {
                    throw new RpcException(new Status(StatusCode.Internal, progressResult.exception.Message));
                }

                if (progressResult.progress == null)
                {
                    // 檢查暫存檔案
                    var tempSizeResult = this.storageService.GetTempFileSize(fileId);

                    return Task.FromResult(new UploadProgress
                    {
                        FileId = fileId,
                        BytesReceived = tempSizeResult.bytesWritten,
                        Status = tempSizeResult.bytesWritten > 0 ? "uploading" : "not_found"
                    });
                }

                var progress = progressResult.progress;

                return Task.FromResult(new UploadProgress
                {
                    FileId = progress.FileId,
                    FileName = progress.FileName ?? string.Empty,
                    BytesReceived = progress.BytesReceived,
                    TotalSize = progress.TotalSize,
                    Status = progress.Status ?? string.Empty,
                    Checksum = progress.Checksum ?? string.Empty
                });
            }
            catch (RpcException)
            {
                throw;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, $"{this.GetType().Name} GetUploadProgress Exception, fileId: {request.FileId}");
                throw new RpcException(new Status(StatusCode.Internal, ex.Message));
            }
        }
    }
}
