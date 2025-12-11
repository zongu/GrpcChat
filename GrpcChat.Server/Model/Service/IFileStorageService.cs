
namespace GrpcChat.Server.Model.Service
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// 檔案儲存服務介面
    /// </summary>
    public interface IFileStorageService
    {
        /// <summary>
        /// 驗證檔案類型
        /// </summary>
        /// <param name="fileName">檔案名稱</param>
        /// <param name="mimeType">MIME類型</param>
        /// <returns>驗證結果</returns>
        (bool isValid, string? errorMessage) ValidateFileType(string fileName, string mimeType);

        /// <summary>
        /// 驗證檔案大小
        /// </summary>
        /// <param name="fileSize">檔案大小</param>
        /// <returns>驗證結果</returns>
        (bool isValid, string? errorMessage) ValidateFileSize(long fileSize);

        /// <summary>
        /// 取得檔案資訊
        /// </summary>
        /// <param name="fileId">檔案ID (包含副檔名)</param>
        /// <returns>檔案資訊</returns>
        (Exception? exception, FileInfo? fileInfo) GetFileInfo(string fileId);

        /// <summary>
        /// 建立檔案寫入串流
        /// </summary>
        /// <param name="fileId">檔案ID</param>
        /// <param name="fileName">檔案名稱</param>
        /// <returns>寫入串流</returns>
        (Exception? exception, FileStream? stream) CreateWriteStream(string fileId, string fileName);

        /// <summary>
        /// 建立檔案讀取串流
        /// </summary>
        /// <param name="fileId">檔案ID (包含副檔名)</param>
        /// <returns>讀取串流</returns>
        (Exception? exception, FileStream? stream) CreateReadStream(string fileId);

        /// <summary>
        /// 完成檔案寫入
        /// </summary>
        /// <param name="fileId">檔案ID (包含副檔名)</param>
        /// <returns>執行結果</returns>
        Exception? CompleteWrite(string fileId);

        /// <summary>
        /// 刪除暫存檔案
        /// </summary>
        /// <param name="fileId">檔案ID</param>
        /// <returns>執行結果</returns>
        Exception? DeleteTempFile(string fileId);

        /// <summary>
        /// 計算檔案校驗碼
        /// </summary>
        /// <param name="fileId">檔案ID (包含副檔名)</param>
        /// <returns>MD5校驗碼</returns>
        (Exception? exception, string? checksum) CalculateChecksum(string fileId);

        /// <summary>
        /// 取得暫存檔案大小 (用於續傳)
        /// </summary>
        /// <param name="fileId">檔案ID</param>
        /// <returns>已寫入的位元組數</returns>
        (Exception? exception, long bytesWritten) GetTempFileSize(string fileId);
    }
}
