using System.Text;
using System.Text.Json;
using ABCRetail.Models;

namespace ABCRetail.Services
{
    public interface IAzureFunctionsService
    {
        Task<List<Customer>> GetCustomersAsync();
        Task<Customer?> GetCustomerAsync(string id);
        Task<bool> CreateCustomerAsync(Customer customer);
        Task<bool> UpdateCustomerAsync(Customer customer);
        Task<bool> DeleteCustomerAsync(string id);
        
        Task<List<Product>> GetProductsAsync();
        Task<Product?> GetProductAsync(string id);
        Task<bool> CreateProductAsync(Product product);
        Task<bool> UpdateProductAsync(Product product);
        Task<bool> DeleteProductAsync(string id);
        
        Task<List<InventoryQueueMessage>> GetQueueMessagesAsync();
        Task<bool> SendQueueMessageAsync(InventoryQueueMessage message);
        Task<bool> ClearQueueAsync();
        
        Task<List<string>> GetBlobFilesAsync();
        Task<string> UploadBlobFileAsync(string fileName, byte[] content);
        Task<byte[]?> DownloadBlobFileAsync(string fileName);
        Task<bool> DeleteBlobFileAsync(string fileName);
        
        Task<List<string>> GetFileListAsync();
        Task<string> WriteFileAsync(string fileName, string content);
        Task<string?> ReadFileAsync(string fileName);
        Task<bool> DeleteFileAsync(string fileName);
    }

    public class AzureFunctionsService : IAzureFunctionsService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AzureFunctionsService> _logger;
        private readonly string _functionAppUrl;
        private readonly string _functionKey;

        public AzureFunctionsService(HttpClient httpClient, IConfiguration configuration, ILogger<AzureFunctionsService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            _functionAppUrl = _configuration["AzureFunctions:BaseUrl"] ?? "https://abcretail-functions-3195.azurewebsites.net";
            _functionKey = _configuration["AzureFunctions:FunctionKey"] ?? "DsCwx-G16RtXqJu-VrOodO4Hc6-twvBGRX_8gNA_ftlwAzFuq7z2rg==";
            
            _httpClient.DefaultRequestHeaders.Add("x-functions-key", _functionKey);
            _httpClient.DefaultRequestHeaders.Add("Content-Type", "application/json");
        }

        // Customer operations
        public async Task<List<Customer>> GetCustomersAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_functionAppUrl}/api/table/list");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var customers = JsonSerializer.Deserialize<List<Customer>>(content, new JsonSerializerOptions 
                    { 
                        PropertyNameCaseInsensitive = true 
                    });
                    return customers ?? new List<Customer>();
                }
                _logger.LogWarning("Failed to get customers: {StatusCode}", response.StatusCode);
                return new List<Customer>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customers from Azure Functions");
                return new List<Customer>();
            }
        }

        public async Task<Customer?> GetCustomerAsync(string id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_functionAppUrl}/api/table/read?partitionKey=CUSTOMER&rowKey={id}");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<Customer>(content, new JsonSerializerOptions 
                    { 
                        PropertyNameCaseInsensitive = true 
                    });
                }
                _logger.LogWarning("Failed to get customer {Id}: {StatusCode}", id, response.StatusCode);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customer {Id} from Azure Functions", id);
                return null;
            }
        }

        public async Task<bool> CreateCustomerAsync(Customer customer)
        {
            try
            {
                var json = JsonSerializer.Serialize(customer, new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
                });
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync($"{_functionAppUrl}/api/table/create", content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating customer via Azure Functions");
                return false;
            }
        }

        public async Task<bool> UpdateCustomerAsync(Customer customer)
        {
            try
            {
                var json = JsonSerializer.Serialize(customer, new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
                });
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PutAsync($"{_functionAppUrl}/api/table/update", content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating customer via Azure Functions");
                return false;
            }
        }

        public async Task<bool> DeleteCustomerAsync(string id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"{_functionAppUrl}/api/table/delete?partitionKey=CUSTOMER&rowKey={id}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting customer {Id} via Azure Functions", id);
                return false;
            }
        }

        // Product operations (using table operations for now)
        public async Task<List<Product>> GetProductsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_functionAppUrl}/api/table/list");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var products = JsonSerializer.Deserialize<List<Product>>(content, new JsonSerializerOptions 
                    { 
                        PropertyNameCaseInsensitive = true 
                    });
                    return products ?? new List<Product>();
                }
                _logger.LogWarning("Failed to get products: {StatusCode}", response.StatusCode);
                return new List<Product>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting products from Azure Functions");
                return new List<Product>();
            }
        }

        public async Task<Product?> GetProductAsync(string id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_functionAppUrl}/api/table/read?partitionKey=PRODUCT&rowKey={id}");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<Product>(content, new JsonSerializerOptions 
                    { 
                        PropertyNameCaseInsensitive = true 
                    });
                }
                _logger.LogWarning("Failed to get product {Id}: {StatusCode}", id, response.StatusCode);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting product {Id} from Azure Functions", id);
                return null;
            }
        }

        public async Task<bool> CreateProductAsync(Product product)
        {
            try
            {
                var json = JsonSerializer.Serialize(product, new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
                });
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync($"{_functionAppUrl}/api/table/create", content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product via Azure Functions");
                return false;
            }
        }

        public async Task<bool> UpdateProductAsync(Product product)
        {
            try
            {
                var json = JsonSerializer.Serialize(product, new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
                });
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PutAsync($"{_functionAppUrl}/api/table/update", content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product via Azure Functions");
                return false;
            }
        }

        public async Task<bool> DeleteProductAsync(string id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"{_functionAppUrl}/api/table/delete?partitionKey=PRODUCT&rowKey={id}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product {Id} via Azure Functions", id);
                return false;
            }
        }

        // Queue operations
        public async Task<List<InventoryQueueMessage>> GetQueueMessagesAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_functionAppUrl}/api/queue/receive?maxMessages=10");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var messages = JsonSerializer.Deserialize<List<InventoryQueueMessage>>(content, new JsonSerializerOptions 
                    { 
                        PropertyNameCaseInsensitive = true 
                    });
                    return messages ?? new List<InventoryQueueMessage>();
                }
                _logger.LogWarning("Failed to get queue messages: {StatusCode}", response.StatusCode);
                return new List<InventoryQueueMessage>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting queue messages from Azure Functions");
                return new List<InventoryQueueMessage>();
            }
        }

        public async Task<bool> SendQueueMessageAsync(InventoryQueueMessage message)
        {
            try
            {
                var json = JsonSerializer.Serialize(message, new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
                });
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync($"{_functionAppUrl}/api/queue/send", content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending queue message via Azure Functions");
                return false;
            }
        }

        public async Task<bool> ClearQueueAsync()
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"{_functionAppUrl}/api/queue/clear");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing queue via Azure Functions");
                return false;
            }
        }

        // Blob operations
        public async Task<List<string>> GetBlobFilesAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_functionAppUrl}/api/blob/list");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var files = JsonSerializer.Deserialize<List<object>>(content, new JsonSerializerOptions 
                    { 
                        PropertyNameCaseInsensitive = true 
                    });
                    return files?.Select(f => f.GetType().GetProperty("name")?.GetValue(f)?.ToString() ?? "").ToList() ?? new List<string>();
                }
                _logger.LogWarning("Failed to get blob files: {StatusCode}", response.StatusCode);
                return new List<string>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting blob files from Azure Functions");
                return new List<string>();
            }
        }

        public async Task<string> UploadBlobFileAsync(string fileName, byte[] content)
        {
            try
            {
                var base64Content = Convert.ToBase64String(content);
                var json = JsonSerializer.Serialize(new { content = base64Content }, new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
                });
                var stringContent = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync($"{_functionAppUrl}/api/blob/upload?fileName={fileName}", stringContent);
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    return result.GetProperty("url").GetString() ?? "";
                }
                return "";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading blob file via Azure Functions");
                return "";
            }
        }

        public async Task<byte[]?> DownloadBlobFileAsync(string fileName)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_functionAppUrl}/api/blob/download?fileName={fileName}");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<JsonElement>(content);
                    var base64Content = result.GetProperty("content").GetString();
                    return base64Content != null ? Convert.FromBase64String(base64Content) : null;
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading blob file via Azure Functions");
                return null;
            }
        }

        public async Task<bool> DeleteBlobFileAsync(string fileName)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"{_functionAppUrl}/api/blob/delete?fileName={fileName}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting blob file via Azure Functions");
                return false;
            }
        }

        // File operations
        public async Task<List<string>> GetFileListAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_functionAppUrl}/api/file/list");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var files = JsonSerializer.Deserialize<List<object>>(content, new JsonSerializerOptions 
                    { 
                        PropertyNameCaseInsensitive = true 
                    });
                    return files?.Select(f => f.GetType().GetProperty("name")?.GetValue(f)?.ToString() ?? "").ToList() ?? new List<string>();
                }
                _logger.LogWarning("Failed to get file list: {StatusCode}", response.StatusCode);
                return new List<string>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting file list from Azure Functions");
                return new List<string>();
            }
        }

        public async Task<string> WriteFileAsync(string fileName, string content)
        {
            try
            {
                var json = JsonSerializer.Serialize(new { content = content, isBase64 = false }, new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
                });
                var stringContent = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync($"{_functionAppUrl}/api/file/write?fileName={fileName}", stringContent);
                return response.IsSuccessStatusCode ? "Success" : "Failed";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error writing file via Azure Functions");
                return "Error";
            }
        }

        public async Task<string?> ReadFileAsync(string fileName)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_functionAppUrl}/api/file/read?fileName={fileName}");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<JsonElement>(content);
                    return result.GetProperty("content").GetString();
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading file via Azure Functions");
                return null;
            }
        }

        public async Task<bool> DeleteFileAsync(string fileName)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"{_functionAppUrl}/api/file/delete?fileName={fileName}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file via Azure Functions");
                return false;
            }
        }
    }
}
