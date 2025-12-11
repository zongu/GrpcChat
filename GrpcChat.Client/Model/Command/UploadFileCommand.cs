
namespace GrpcChat.Client.Model.Command
{
    using System;
    using System.IO;
    using System.Threading;
    using GrpcChat.Client.GrpcClient;
    using NLog;

    /// <summary>
    /// 上傳檔案指令
    /// </summary>
    public class UploadFileCommand : ICommand
    {
        private ILogger logger;

        private IFileTransferClient fileTransferClient;

        public UploadFileCommand(ILogger logger, IFileTransferClient fileTransferClient)
        {
            this.logger = logger;
            this.fileTransferClient = fileTransferClient;
        }

        public bool Execute()
        {
            try
            {
                Console.Write("請輸入檔案路徑: ");
                var filePath = Console.ReadLine()?.Trim().Trim('"');

                if (string.IsNullOrEmpty(filePath))
                {
                    Console.WriteLine("檔案路徑不可為空");
                    return true;
                }

                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"檔案不存在: {filePath}");
                    return true;
                }

                Console.Write("是否續傳? 輸入檔案ID (留空則新上傳): ");
                var fileId = Console.ReadLine()?.Trim();

                if (string.IsNullOrEmpty(fileId))
                {
                    fileId = null;
                }

                Console.WriteLine("上傳中... (按 Ctrl+C 取消)");

                using (var cts = new CancellationTokenSource())
                {
                    Console.CancelKeyPress += (sender, e) =>
                    {
                        e.Cancel = true;
                        cts.Cancel();
                        Console.WriteLine("\n取消上傳...");
                    };

                    var task = this.fileTransferClient.UploadFileAsync(
                        filePath,
                        fileId,
                        (received, total) =>
                        {
                            var percent = (double)received / total * 100;
                            Console.Write($"\r上傳進度: {received:N0} / {total:N0} bytes ({percent:F1}%)   ");
                        },
                        cts.Token);

                    var result = task.GetAwaiter().GetResult();

                    Console.WriteLine();

                    if (result.exception != null)
                    {
                        Console.WriteLine($"上傳失敗: {result.exception.Message}");
                    }
                    else if (result.result != null)
                    {
                        if (result.result.Success)
                        {
                            Console.WriteLine($"上傳成功!");
                            Console.WriteLine($"  檔案ID: {result.result.FileId}");
                            Console.WriteLine($"  大小: {result.result.BytesUploaded:N0} bytes");
                            Console.WriteLine($"  校驗碼: {result.result.Checksum}");
                        }
                        else
                        {
                            Console.WriteLine($"上傳失敗: {result.result.ErrorMessage}");
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, $"{this.GetType().Name} Execute Exception");
                Console.WriteLine($"發生錯誤: {ex.Message}");
                return true;
            }
        }
    }
}
