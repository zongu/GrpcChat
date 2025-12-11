
namespace GrpcChat.Client.Model.Command
{
    using System;
    using System.IO;
    using System.Threading;
    using GrpcChat.Client.GrpcClient;
    using NLog;

    /// <summary>
    /// 下載檔案指令
    /// </summary>
    public class DownloadFileCommand : ICommand
    {
        private ILogger logger;

        private IFileTransferClient fileTransferClient;

        public DownloadFileCommand(ILogger logger, IFileTransferClient fileTransferClient)
        {
            this.logger = logger;
            this.fileTransferClient = fileTransferClient;
        }

        public bool Execute()
        {
            try
            {
                Console.Write("請輸入檔案ID: ");
                var fileId = Console.ReadLine()?.Trim();

                if (string.IsNullOrEmpty(fileId))
                {
                    Console.WriteLine("檔案ID不可為空");
                    return true;
                }

                Console.Write("請輸入儲存路徑: ");
                var savePath = Console.ReadLine()?.Trim().Trim('"');

                if (string.IsNullOrEmpty(savePath))
                {
                    // 使用預設路徑
                    savePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileId);
                    Console.WriteLine($"使用預設路徑: {savePath}");
                }
                else
                {
                    // 檢查是否為目錄路徑（以 \ 或 / 結尾，或是磁碟機根目錄如 G: 或 G:\）
                    bool isDirectory = savePath.EndsWith(Path.DirectorySeparatorChar.ToString()) ||
                                       savePath.EndsWith(Path.AltDirectorySeparatorChar.ToString()) ||
                                       (savePath.Length == 2 && savePath[1] == ':') ||
                                       (savePath.Length == 3 && savePath[1] == ':' && (savePath[2] == '\\' || savePath[2] == '/'));

                    // 如果目錄已存在，也視為目錄路徑
                    if (!isDirectory && Directory.Exists(savePath))
                    {
                        isDirectory = true;
                    }

                    if (isDirectory)
                    {
                        // 使用 fileId 作為檔案名稱
                        savePath = Path.Combine(savePath, fileId);
                        Console.WriteLine($"使用完整路徑: {savePath}");
                    }
                }

                Console.WriteLine("下載中... (按 Ctrl+C 取消)");

                using (var cts = new CancellationTokenSource())
                {
                    Console.CancelKeyPress += (sender, e) =>
                    {
                        e.Cancel = true;
                        cts.Cancel();
                        Console.WriteLine("\n取消下載...");
                    };

                    var task = this.fileTransferClient.DownloadFileAsync(
                        fileId,
                        savePath,
                        (received, total) =>
                        {
                            var percent = total > 0 ? (double)received / total * 100 : 0;
                            Console.Write($"\r下載進度: {received:N0} / {total:N0} bytes ({percent:F1}%)   ");
                        },
                        cts.Token);

                    var result = task.GetAwaiter().GetResult();

                    Console.WriteLine();

                    if (result.exception != null)
                    {
                        Console.WriteLine($"下載失敗: {result.exception.Message}");
                    }
                    else if (result.result != null)
                    {
                        if (result.result.Success)
                        {
                            Console.WriteLine($"下載成功!");
                            Console.WriteLine($"  檔案名稱: {result.result.FileName}");
                            Console.WriteLine($"  大小: {result.result.BytesDownloaded:N0} bytes");
                            Console.WriteLine($"  校驗碼: {result.result.Checksum}");
                            Console.WriteLine($"  儲存路徑: {savePath}");
                        }
                        else
                        {
                            Console.WriteLine($"下載失敗: {result.result.ErrorMessage}");
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
