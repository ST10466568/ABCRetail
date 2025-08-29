namespace ABCRetail.Services;

public interface IAzureFileService
{
    Task<bool> WriteLogAsync(string logContent, string fileName);
    Task<string?> ReadLogAsync(string fileName);
    Task<List<string>> ListLogFilesAsync();
    Task<bool> DeleteLogAsync(string fileName);
    Task<byte[]?> DownloadLogAsync(string fileName);
}


