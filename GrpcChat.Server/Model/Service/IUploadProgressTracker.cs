
namespace GrpcChat.Server.Model.Service
{
    /// <summary>
    /// 上傳進度追蹤器介面
    /// </summary>
    public interface IUploadProgressTracker
    {
        /// <summary>
        /// 取得上傳進度
        /// </summary>
        /// <param name="fileId">檔案ID</param>
        /// <returns>上傳進度資訊</returns>
        (System.Exception? exception, UploadProgressInfo? progress) GetProgress(string fileId);

        /// <summary>
        /// 更新上傳進度
        /// </summary>
        /// <param name="fileId">檔案ID</param>
        /// <param name="fileName">檔案名稱</param>
        /// <param name="bytesReceived">已接收位元組數</param>
        /// <param name="totalSize">總大小</param>
        /// <returns>執行結果</returns>
        System.Exception? UpdateProgress(string fileId, string fileName, long bytesReceived, long totalSize);

        /// <summary>
        /// 完成上傳
        /// </summary>
        /// <param name="fileId">檔案ID</param>
        /// <param name="checksum">校驗碼</param>
        /// <returns>執行結果</returns>
        System.Exception? CompleteUpload(string fileId, string checksum);

        /// <summary>
        /// 標記上傳失敗
        /// </summary>
        /// <param name="fileId">檔案ID</param>
        /// <param name="errorMessage">錯誤訊息</param>
        /// <returns>執行結果</returns>
        System.Exception? FailUpload(string fileId, string errorMessage);

        /// <summary>
        /// 刪除進度記錄
        /// </summary>
        /// <param name="fileId">檔案ID</param>
        /// <returns>執行結果</returns>
        System.Exception? RemoveProgress(string fileId);
    }

    /// <summary>
    /// 上傳進度資訊
    /// </summary>
    public class UploadProgressInfo
    {
        public string FileId { get; set; } = string.Empty;

        public string FileName { get; set; } = string.Empty;

        public long BytesReceived { get; set; }

        public long TotalSize { get; set; }

        public string Status { get; set; } = string.Empty;

        public string Checksum { get; set; } = string.Empty;

        public string ErrorMessage { get; set; } = string.Empty;
    }
}
