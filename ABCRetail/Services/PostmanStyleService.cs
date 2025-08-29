using ABCRetail.Models;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Net.Http;

namespace ABCRetail.Services
{
    public class PostmanStyleService : IAzureTableServiceV2
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<PostmanStyleService> _logger;
        private readonly string _productsSasUrl;
        private readonly string _customersSasUrl;

        public PostmanStyleService(IConfiguration configuration, ILogger<PostmanStyleService> logger)
        {
            _logger = logger;
            _httpClient = new HttpClient();
            
            // Use EXACTLY the same headers as Postman (which works)
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json;odata=minimalmetadata");
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "PostmanRuntime/7.32.3");
            _httpClient.DefaultRequestHeaders.Add("x-ms-version", "2020-04-08");
            _httpClient.DefaultRequestHeaders.Add("DataServiceVersion", "3.0");
            
            // Get the SAS URLs
            _productsSasUrl = configuration["AzureStorage:ProductsTableSasUrl"];
            _customersSasUrl = configuration["AzureStorage:CustomersTableSasUrl"];
            
            _logger.LogInformation("📮 PostmanStyleService initialized - mimicking Postman exactly");
            _logger.LogInformation("📦 Products URL: {ProductsUrl}", _productsSasUrl);
            _logger.LogInformation("👥 Customers URL: {CustomersUrl}", _customersSasUrl);
        }

        public async Task<List<Product>> GetAllProductsAsync()
        {
            try
            {
                _logger.LogInformation("📦 Retrieving all products using Postman-style approach");
                
                var response = await _httpClient.GetAsync(_productsSasUrl);
                _logger.LogInformation("📊 Products response status: {StatusCode}", response.StatusCode);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation("📄 Products response content length: {Length}", content.Length);
                    
                    var products = ParseProductsFromJson(content);
                    _logger.LogInformation("✅ Retrieved {Count} products successfully via Postman-style approach", products.Count);
                    return products;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    var errorMessage = $"Failed to retrieve products. Status: {response.StatusCode}, Error: {errorContent}";
                    _logger.LogError(errorMessage);
                    throw new Exception(errorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to retrieve products");
                throw;
            }
        }

        public async Task<List<Customer>> GetAllCustomersAsync()
        {
            try
            {
                _logger.LogInformation("👥 Retrieving all customers using Postman-style approach");
                
                var response = await _httpClient.GetAsync(_customersSasUrl);
                _logger.LogInformation("📊 Customers response status: {StatusCode}", response.StatusCode);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation("📄 Customers response content length: {Length}", content.Length);
                    
                    var customers = ParseCustomersFromJson(content);
                    _logger.LogInformation("✅ Retrieved {Count} customers successfully via Postman-style approach", customers.Count);
                    return customers;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    var errorMessage = $"Failed to retrieve customers. Status: {response.StatusCode}, Error: {errorContent}";
                    _logger.LogError(errorMessage);
                    throw new Exception(errorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to retrieve customers");
                throw;
            }
        }

        public async Task<Product?> GetProductAsync(string id)
        {
            try
            {
                _logger.LogInformation("🔍 Retrieving product with ID: {Id} using Postman-style approach", id);
                
                // Use OData filter to get specific product
                var filterUrl = $"{_productsSasUrl}?$filter=RowKey eq '{id}'";
                _logger.LogInformation("🔗 Filter URL: {FilterUrl}", filterUrl);
                
                var response = await _httpClient.GetAsync(filterUrl);
                _logger.LogInformation("📊 Product response status: {StatusCode}", response.StatusCode);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation("📄 Product response content length: {Length}", content.Length);
                    
                    var products = ParseProductsFromJson(content);
                    var product = products.FirstOrDefault();
                    
                    if (product != null)
                    {
                        _logger.LogInformation("✅ Product '{Name}' retrieved successfully via Postman-style approach", product.Name);
                        return product;
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ Product with ID {Id} not found in response", id);
                        return null;
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    var errorMessage = $"Failed to retrieve product. Status: {response.StatusCode}, Error: {errorContent}";
                    _logger.LogError(errorMessage);
                    throw new Exception(errorMessage);
                }
            }
            catch (Exception ex)
            {
                var detailedError = $"❌ Failed to retrieve product with ID: {id}\nException: {ex.Message}";
                if (ex.InnerException != null)
                {
                    detailedError += $"\nInner Exception: {ex.InnerException.Message}";
                }
                detailedError += $"\nStack Trace: {ex.StackTrace}";
                
                _logger.LogError(detailedError);
                throw new Exception(detailedError, ex);
            }
        }

        public async Task<Customer?> GetCustomerAsync(string id)
        {
            try
            {
                _logger.LogInformation("🔍 Retrieving customer with ID: {Id} using Postman-style approach", id);
                
                // Use OData filter to get specific customer
                var filterUrl = $"{_customersSasUrl}?$filter=RowKey eq '{id}'";
                _logger.LogInformation("🔗 Filter URL: {FilterUrl}", filterUrl);
                
                var response = await _httpClient.GetAsync(filterUrl);
                _logger.LogInformation("📊 Customer response status: {StatusCode}", response.StatusCode);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation("📄 Customer response content length: {Length}", content.Length);
                    
                    var customers = ParseCustomersFromJson(content);
                    var customer = customers.FirstOrDefault();
                    
                    if (customer != null)
                    {
                        _logger.LogInformation("✅ Customer '{FullName}' retrieved successfully via Postman-style approach", customer.FullName);
                        return customer;
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ Customer with ID {Id} not found in response", id);
                        return null;
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    var errorMessage = $"Failed to retrieve customer. Status: {response.StatusCode}, Error: {errorContent}";
                    _logger.LogError(errorMessage);
                    throw new Exception(errorMessage);
                }
            }
            catch (Exception ex)
            {
                var detailedError = $"❌ Failed to retrieve customer with ID: {id}\nException: {ex.Message}";
                if (ex.InnerException != null)
                {
                    detailedError += $"\nInner Exception: {ex.InnerException.Message}";
                }
                detailedError += $"\nStack Trace: {ex.StackTrace}";
                
                _logger.LogError(detailedError);
                throw new Exception(detailedError, ex);
            }
        }

        // For now, implement these as no-ops since we're focusing on reading
        public async Task<bool> UpdateProductAsync(Product product)
        {
            _logger.LogWarning("⚠️ UpdateProductAsync not implemented in PostmanStyleService - focusing on reading first");
            return false;
        }

        public async Task<bool> UpdateCustomerAsync(Customer customer)
        {
            _logger.LogWarning("⚠️ UpdateCustomerAsync not implemented in PostmanStyleService - focusing on reading first");
            return false;
        }

        public async Task<bool> CreateProductAsync(Product product)
        {
            _logger.LogWarning("⚠️ CreateProductAsync not implemented in PostmanStyleService - focusing on reading first");
            return false;
        }

        public async Task<bool> CreateCustomerAsync(Customer customer)
        {
            _logger.LogWarning("⚠️ CreateCustomerAsync not implemented in PostmanStyleService - focusing on reading first");
            return false;
        }

        public async Task<bool> DeleteProductAsync(string id)
        {
            _logger.LogWarning("⚠️ DeleteProductAsync not implemented in PostmanStyleService - focusing on reading first");
            return false;
        }

        public async Task<bool> DeleteCustomerAsync(string id)
        {
            _logger.LogWarning("⚠️ DeleteCustomerAsync not implemented in PostmanStyleService - focusing on reading first");
            return false;
        }

        public async Task<List<string>> ListTablesAsync()
        {
            _logger.LogWarning("⚠️ ListTablesAsync not implemented in PostmanStyleService - focusing on reading first");
            return new List<string>();
        }
        
        public async Task<Product?> RecoverMissingProductAsync(string productId)
        {
            _logger.LogWarning("⚠️ RecoverMissingProductAsync not implemented in PostmanStyleService - focusing on reading first");
            return null;
        }
        
        public async Task<string> DiagnoseProductStatusAsync(string productId)
        {
            _logger.LogWarning("⚠️ DiagnoseProductStatusAsync not implemented in PostmanStyleService - focusing on reading first");
            return "Diagnosis not implemented in this service";
        }

        public async Task<Customer?> RecoverMissingCustomerAsync(string customerId)
        {
            _logger.LogWarning("⚠️ RecoverMissingCustomerAsync not implemented in PostmanStyleService - focusing on reading first");
            return null;
        }

        public async Task<string> DiagnoseCustomerStatusAsync(string customerId)
        {
            _logger.LogWarning("⚠️ DiagnoseCustomerStatusAsync not implemented in PostmanStyleService - focusing on reading first");
            return "Diagnosis not implemented in this service";
        }

        // Helper methods for parsing JSON responses
        private List<Product> ParseProductsFromJson(string jsonContent)
        {
            try
            {
                _logger.LogInformation("🔍 Parsing products JSON content...");
                
                var response = JsonSerializer.Deserialize<AzureTableResponse<Product>>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                
                var products = response?.Value ?? new List<Product>();
                _logger.LogInformation("✅ Successfully parsed {Count} products from JSON", products.Count);
                
                // Log first few products for debugging
                foreach (var product in products.Take(3))
                {
                    _logger.LogInformation("📦 Product: {Name} (ID: {Id})", product.Name, product.RowKey);
                }
                
                return products;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to parse products from JSON");
                _logger.LogError("JSON content: {JsonContent}", jsonContent);
                return new List<Product>();
            }
        }

        private List<Customer> ParseCustomersFromJson(string jsonContent)
        {
            try
            {
                _logger.LogInformation("🔍 Parsing customers JSON content...");
                
                var response = JsonSerializer.Deserialize<AzureTableResponse<Customer>>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                
                var customers = response?.Value ?? new List<Customer>();
                _logger.LogInformation("✅ Successfully parsed {Count} customers from JSON", customers.Count);
                
                // Log first few customers for debugging
                foreach (var customer in customers.Take(3))
                {
                    _logger.LogInformation("👥 Customer: {FullName} (ID: {Id})", customer.FullName, customer.RowKey);
                }
                
                return customers;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to parse customers from JSON");
                _logger.LogError("JSON content: {JsonContent}", jsonContent);
                return new List<Customer>();
            }
        }
    }
}
