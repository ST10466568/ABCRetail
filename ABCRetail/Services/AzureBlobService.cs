using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;

namespace ABCRetail.Services;

public class AzureBlobService : IAzureBlobService
{
    private readonly BlobServiceClient? _blobServiceClient;
    private readonly string _containerName;
    private readonly bool _isAzureConnected;

    public AzureBlobService(IConfiguration configuration)
    {
        var connectionString = configuration["AzureStorage:BlobConnectionString"];
        var blobSasUrl = configuration["AzureStorage:BlobSasUrl"];
        var sasToken = configuration["AzureStorage:BlobSasToken"];
        _containerName = configuration["AzureStorage:BlobContainerName"] ?? "product-images";
        
        // Check if we have a valid SAS URL or connection string and SAS token
        if (!string.IsNullOrEmpty(blobSasUrl))
        {
            try
            {
                // Use the full SAS URL for blob storage
                _blobServiceClient = new BlobServiceClient(new Uri(blobSasUrl));
                _isAzureConnected = true;
                Console.WriteLine($"✅ Azure Blob Service connected successfully using SAS URL to container: {_containerName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Azure Blob Service SAS URL connection failed: {ex.Message}");
                _isAzureConnected = false;
            }
        }
        else if (!string.IsNullOrEmpty(connectionString) && 
            !string.IsNullOrEmpty(sasToken))
        {
            try
            {
                _blobServiceClient = new BlobServiceClient(connectionString);
                _isAzureConnected = true;
                Console.WriteLine($"✅ Azure Blob Service connected successfully using connection string to container: {_containerName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Azure Blob Service connection string failed: {ex.Message}");
                _isAzureConnected = false;
            }
        }
        else
        {
            Console.WriteLine("⚠️ Azure Blob Service: Missing SAS URL or valid connection string and SAS token");
            _isAzureConnected = false;
        }
    }

    public async Task<string> UploadImageAsync(IFormFile file, string fileName)
    {
        if (!_isAzureConnected || _blobServiceClient == null)
            return "/images/placeholder.jpg";

        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            await containerClient.CreateIfNotExistsAsync();

            var blobClient = containerClient.GetBlobClient(fileName);
            using var stream = file.OpenReadStream();
            await blobClient.UploadAsync(stream, overwrite: true);

            return blobClient.Uri.ToString();
        }
        catch
        {
            return "/images/placeholder.jpg";
        }
    }

    public async Task<byte[]?> DownloadImageAsync(string fileName)
    {
        if (!_isAzureConnected || _blobServiceClient == null)
            return null;

        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobClient = containerClient.GetBlobClient(fileName);

            if (await blobClient.ExistsAsync())
            {
                var response = await blobClient.DownloadAsync();
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

    public async Task<bool> DeleteImageAsync(string fileName)
    {
        if (!_isAzureConnected || _blobServiceClient == null)
            return false;

        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobClient = containerClient.GetBlobClient(fileName);
            await blobClient.DeleteIfExistsAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<string>> ListImagesAsync()
    {
        var imageNames = new List<string>();
        
        if (!_isAzureConnected || _blobServiceClient == null)
            return imageNames;

        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            await foreach (var blobItem in containerClient.GetBlobsAsync())
            {
                imageNames.Add(blobItem.Name);
            }
        }
        catch
        {
            // Log error if needed
        }

        return imageNames;
    }

    public async Task<string> GetImageUrlAsync(string fileName)
    {
        if (!_isAzureConnected || _blobServiceClient == null)
            return "/images/placeholder.jpg";

        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobClient = containerClient.GetBlobClient(fileName);
            return blobClient.Uri.ToString();
        }
        catch
        {
            return "/images/placeholder.jpg";
        }
    }

    public string GetContainerUrl()
    {
        if (!_isAzureConnected || _blobServiceClient == null)
            return string.Empty;

        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            return containerClient.Uri.ToString();
        }
        catch
        {
            return string.Empty;
        }
    }

    public bool IsConnected()
    {
        return _isAzureConnected;
    }
}
