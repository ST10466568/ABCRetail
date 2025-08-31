using Azure.Storage.Files.Shares;
using Microsoft.Extensions.Configuration;

namespace ABCRetail.Services;

public class AzureFileService : IAzureFileService
{
    private readonly ShareServiceClient? _shareServiceClient;
    private readonly string _shareName;
    private readonly bool _isAzureConnected;

    public AzureFileService(IConfiguration configuration)
    {
        var connectionString = configuration["AzureStorage:ConnectionString"];
        var fileSasUrl = configuration["AzureStorage:FileSasUrl"];
        _shareName = "applogs";
        
        // Check if we have a valid SAS URL or connection string
        if (!string.IsNullOrEmpty(fileSasUrl))
        {
            try
            {
                // Use the full SAS URL for file storage
                _shareServiceClient = new ShareServiceClient(new Uri(fileSasUrl));
                _isAzureConnected = true;

            }
            catch (Exception ex)
            {

                _isAzureConnected = false;
            }
        }
        else if (!string.IsNullOrEmpty(connectionString) && 
            !connectionString.Contains("YOUR_ACCOUNT_NAME") && 
            !connectionString.Contains("YOUR_ACCOUNT_KEY"))
        {
            try
            {
                _shareServiceClient = new ShareServiceClient(connectionString);
                _isAzureConnected = true;

            }
            catch (Exception ex)
            {

                _isAzureConnected = false;
            }
        }
        else
        {

            _isAzureConnected = false;
        }
    }

    public async Task<bool> WriteLogAsync(string logContent, string fileName)
    {
        if (!_isAzureConnected || _shareServiceClient == null)
            return false;

        try
        {
            var shareClient = _shareServiceClient.GetShareClient(_shareName);
            await shareClient.CreateIfNotExistsAsync();

            var directoryClient = shareClient.GetRootDirectoryClient();
            var fileClient = directoryClient.GetFileClient(fileName);
            
            using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(logContent));
            await fileClient.CreateAsync(stream.Length);
            await fileClient.UploadRangeAsync(new Azure.HttpRange(0, stream.Length), stream);
            
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<string?> ReadLogAsync(string fileName)
    {
        if (!_isAzureConnected || _shareServiceClient == null)
            return null;

        try
        {
            var shareClient = _shareServiceClient.GetShareClient(_shareName);
            var directoryClient = shareClient.GetRootDirectoryClient();
            var fileClient = directoryClient.GetFileClient(fileName);

            if (await fileClient.ExistsAsync())
            {
                var response = await fileClient.DownloadAsync();
                using var streamReader = new StreamReader(response.Value.Content);
                return await streamReader.ReadToEndAsync();
            }
        }
        catch
        {
            // Log error if needed
        }

        return null;
    }

    public async Task<List<string>> ListLogFilesAsync()
    {
        var logFiles = new List<string>();
        
        if (!_isAzureConnected || _shareServiceClient == null)
            return logFiles;

        try
        {
            var shareClient = _shareServiceClient.GetShareClient(_shareName);
            var directoryClient = shareClient.GetRootDirectoryClient();
            
            await foreach (var fileItem in directoryClient.GetFilesAndDirectoriesAsync())
            {
                if (fileItem.IsDirectory == false)
                {
                    logFiles.Add(fileItem.Name);
                }
            }
        }
        catch
        {
            // Log error if needed
        }

        return logFiles;
    }

    public async Task<bool> DeleteLogAsync(string fileName)
    {
        if (!_isAzureConnected || _shareServiceClient == null)
            return false;

        try
        {
            var shareClient = _shareServiceClient.GetShareClient(_shareName);
            var directoryClient = shareClient.GetRootDirectoryClient();
            var fileClient = directoryClient.GetFileClient(fileName);
            
            await fileClient.DeleteIfExistsAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<byte[]?> DownloadLogAsync(string fileName)
    {
        if (!_isAzureConnected || _shareServiceClient == null)
            return null;

        try
        {
            var shareClient = _shareServiceClient.GetShareClient(_shareName);
            var directoryClient = shareClient.GetRootDirectoryClient();
            var fileClient = directoryClient.GetFileClient(fileName);

            if (await fileClient.ExistsAsync())
            {
                var response = await fileClient.DownloadAsync();
                using var memoryStream = new MemoryStream();
                await response.Value.Content.CopyToAsync(memoryStream);
                return memoryStream.ToArray();
            }
        }
        catch
        {
            // Log error if needed
        }

        return null;
    }
}
