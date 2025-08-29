using Microsoft.AspNetCore.Http;

namespace ABCRetail.Services;

public interface IAzureBlobService
{
    Task<string> UploadImageAsync(IFormFile file, string fileName);
    Task<byte[]?> DownloadImageAsync(string fileName);
    Task<bool> DeleteImageAsync(string fileName);
    Task<List<string>> ListImagesAsync();
    Task<string> GetImageUrlAsync(string fileName);
    string GetContainerUrl();
    bool IsConnected();
}


