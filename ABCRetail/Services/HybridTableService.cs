using Azure.Data.Tables;
using ABCRetail.Models;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Net.Http;
using System.Text;

namespace ABCRetail.Services
{
    public class HybridTableService : IAzureTableServiceV2
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<HybridTableService> _logger;
        private readonly string _productsSasUrl;
        private readonly string _customersSasUrl;
        private readonly string _connectionString;

        public HybridTableService(IConfiguration configuration, ILogger<HybridTableService> logger)
        {
            _logger = logger;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json;odata=minimalmetadata");
            _httpClient.DefaultRequestHeaders.Add("x-ms-version", "2020-04-08");
            _httpClient.DefaultRequestHeaders.Add("DataServiceVersion", "3.0");
            
            // Use SAS URLs for reading (these work)
            _productsSasUrl = configuration["AzureStorage:ProductsTableSasUrl"];
            _customersSasUrl = configuration["AzureStorage:CustomersTableSasUrl"];
            
            // Use connection string for writing (Azure SDK)
            _connectionString = configuration.GetConnectionString("AzureStorage") 
                ?? configuration["AzureStorage:ConnectionString"];
            
            _logger.LogInformation("🔗 HybridTableService initialized with SAS URLs for reading and connection string for writing");
        }

        public async Task<List<Product>> GetAllProductsAsync()
        {
            try
            {
                _logger.LogInformation("📦 Retrieving all products using SAS URL (working method)");
                
                var response = await _httpClient.GetAsync(_productsSasUrl);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var products = ParseProductsFromJson(content);
                    _logger.LogInformation($"✅ Retrieved {products.Count} products successfully via SAS URL");
                    return products;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Failed to retrieve products. Status: {response.StatusCode}, Error: {errorContent}");
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
                _logger.LogInformation("👥 Retrieving all customers using SAS URL (working method)");
                
                var response = await _httpClient.GetAsync(_customersSasUrl);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var customers = ParseCustomersFromJson(content);
                    _logger.LogInformation($"✅ Retrieved {customers.Count} customers successfully via SAS URL");
                    return customers;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Failed to retrieve customers. Status: {response.StatusCode}, Error: {errorContent}");
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
                _logger.LogInformation($"🔍 Retrieving product with ID: {id} using SAS URL");
                
                // Use the working SAS URL approach
                var response = await _httpClient.GetAsync($"{_productsSasUrl}(PartitionKey='Product',RowKey='{id}')");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var product = ParseSingleProductFromJson(content);
                    if (product != null)
                    {
                        _logger.LogInformation($"✅ Product '{product.Name}' retrieved successfully via SAS URL");
                        return product;
                    }
                }
                
                _logger.LogWarning($"⚠️ Product with ID {id} not found");
                return null;
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
                _logger.LogInformation($"🔍 Retrieving customer with ID: {id} using SAS URL");
                
                // Use the working SAS URL approach
                var response = await _httpClient.GetAsync($"{_customersSasUrl}(PartitionKey='Customer',RowKey='{id}')");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var customer = ParseSingleCustomerFromJson(content);
                    if (customer != null)
                    {
                        _logger.LogInformation($"✅ Customer '{customer.FullName}' retrieved successfully via SAS URL");
                        return customer;
                    }
                }
                
                _logger.LogWarning($"⚠️ Customer with ID {id} not found");
                return null;
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

        public async Task<bool> UpdateProductAsync(Product product)
        {
            try
            {
                _logger.LogInformation($"🔧 Updating product: {product.Name} (ID: {product.RowKey}) using Azure SDK");
                
                // Use Azure SDK with connection string for updates
                var tableServiceClient = new TableServiceClient(_connectionString);
                var productsTableClient = tableServiceClient.GetTableClient("Products");
                
                // Update the last modified date
                product.LastModifiedDate = DateTime.UtcNow;
                
                // Convert to TableEntity
                var tableEntity = MapProductToTableEntity(product);
                
                // Use MergeEntityAsync for updates
                await productsTableClient.UpdateEntityAsync(tableEntity, tableEntity.ETag, Azure.Data.Tables.TableUpdateMode.Merge);
                
                _logger.LogInformation($"✅ Product '{product.Name}' updated successfully via Azure SDK");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Failed to update product: {product.Name}");
                throw;
            }
        }

        public async Task<bool> UpdateCustomerAsync(Customer customer)
        {
            try
            {
                _logger.LogInformation($"🔧 Updating customer: {customer.FullName} (ID: {customer.RowKey}) using Azure SDK");
                
                // Use Azure SDK with connection string for updates
                var tableServiceClient = new TableServiceClient(_connectionString);
                var customersTableClient = tableServiceClient.GetTableClient("Customers");
                
                // Convert to TableEntity
                var tableEntity = MapCustomerToTableEntity(customer);
                
                // Use MergeEntityAsync for updates
                await customersTableClient.UpdateEntityAsync(tableEntity, tableEntity.ETag, Azure.Data.Tables.TableUpdateMode.Merge);
                
                _logger.LogInformation($"✅ Customer '{customer.FullName}' updated successfully via Azure SDK");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Failed to update customer: {customer.FullName}");
                throw;
            }
        }

        public async Task<bool> CreateProductAsync(Product product)
        {
            try
            {
                _logger.LogInformation($"➕ Creating new product: {product.Name} using Azure SDK");
                
                // Use Azure SDK with connection string for creation
                var tableServiceClient = new TableServiceClient(_connectionString);
                var productsTableClient = tableServiceClient.GetTableClient("Products");
                
                // Set creation date
                product.CreatedDate = DateTime.UtcNow;
                
                // Convert to TableEntity
                var tableEntity = MapProductToTableEntity(product);
                
                // Use AddEntityAsync for creation
                await productsTableClient.AddEntityAsync(tableEntity);
                
                _logger.LogInformation($"✅ Product '{product.Name}' created successfully via Azure SDK");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Failed to create product: {product.Name}");
                throw;
            }
        }

        public async Task<bool> CreateCustomerAsync(Customer customer)
        {
            try
            {
                _logger.LogInformation($"➕ Creating new customer: {customer.FullName} using Azure SDK");
                
                // Use Azure SDK with connection string for creation
                var tableServiceClient = new TableServiceClient(_connectionString);
                var customersTableClient = tableServiceClient.GetTableClient("Customers");
                
                // Set creation date
                customer.CreatedDate = DateTime.UtcNow;
                
                // Convert to TableEntity
                var tableEntity = MapCustomerToTableEntity(customer);
                
                // Use AddEntityAsync for creation
                await customersTableClient.AddEntityAsync(tableEntity);
                
                _logger.LogInformation($"✅ Customer '{customer.FullName}' created successfully via Azure SDK");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Failed to create customer: {customer.FullName}");
                throw;
            }
        }

        public async Task<bool> DeleteProductAsync(string id)
        {
            try
            {
                _logger.LogInformation($"🗑️ Deleting product with ID: {id} using Azure SDK");
                
                // Use Azure SDK with connection string for deletion
                var tableServiceClient = new TableServiceClient(_connectionString);
                var productsTableClient = tableServiceClient.GetTableClient("Products");
                
                await productsTableClient.DeleteEntityAsync("Product", id);
                
                _logger.LogInformation($"✅ Product with ID {id} deleted successfully via Azure SDK");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Failed to delete product with ID: {id}");
                throw;
            }
        }

        public async Task<bool> DeleteCustomerAsync(string id)
        {
            try
            {
                _logger.LogInformation($"🗑️ Deleting customer with ID: {id} using Azure SDK");
                
                // Use Azure SDK with connection string for deletion
                var tableServiceClient = new TableServiceClient(_connectionString);
                var customersTableClient = tableServiceClient.GetTableClient("Customers");
                
                await customersTableClient.DeleteEntityAsync("Customer", id);
                
                _logger.LogInformation($"✅ Customer with ID {id} deleted successfully via Azure SDK");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Failed to delete customer with ID: {id}");
                throw;
            }
        }

        public async Task<List<string>> ListTablesAsync()
        {
            try
            {
                _logger.LogInformation("📋 Listing available tables using Azure SDK");
                
                var tableServiceClient = new TableServiceClient(_connectionString);
                var tableNames = new List<string>();
                await foreach (var table in tableServiceClient.QueryAsync())
                {
                    tableNames.Add(table.Name);
                }
                
                _logger.LogInformation($"✅ Found {tableNames.Count} tables: {string.Join(", ", tableNames)}");
                return tableNames;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to list tables");
                throw;
            }
        }
        
        public async Task<Product?> RecoverMissingProductAsync(string productId)
        {
            // Implementation for recovering missing products
            return null;
        }
        
        public async Task<string> DiagnoseProductStatusAsync(string productId)
        {
            // Implementation for diagnosing product status
            return "Diagnosis not implemented in this service";
        }

        public async Task<Customer?> RecoverMissingCustomerAsync(string customerId)
        {
            // Implementation for recovering missing customers
            return null;
        }

        public async Task<string> DiagnoseCustomerStatusAsync(string customerId)
        {
            // Implementation for diagnosing customer status
            return "Diagnosis not implemented in this service";
        }

        // Helper methods for parsing JSON responses from SAS URLs
        private List<Product> ParseProductsFromJson(string jsonContent)
        {
            try
            {
                var response = JsonSerializer.Deserialize<AzureTableResponse<Product>>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                
                return response?.Value ?? new List<Product>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse products from JSON");
                return new List<Product>();
            }
        }

        private List<Customer> ParseCustomersFromJson(string jsonContent)
        {
            try
            {
                var response = JsonSerializer.Deserialize<AzureTableResponse<Customer>>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                
                return response?.Value ?? new List<Customer>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse customers from JSON");
                return new List<Customer>();
            }
        }

        private Product? ParseSingleProductFromJson(string jsonContent)
        {
            try
            {
                var response = JsonSerializer.Deserialize<AzureTableResponse<Product>>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                
                return response?.Value?.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse single product from JSON");
                return null;
            }
        }

        private Customer? ParseSingleCustomerFromJson(string jsonContent)
        {
            try
            {
                var response = JsonSerializer.Deserialize<AzureTableResponse<Customer>>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                
                return response?.Value?.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse single customer from JSON");
                return null;
            }
        }

        // Helper methods for mapping between models and TableEntity
        private Product MapTableEntityToProduct(TableEntity entity)
        {
            return new Product
            {
                RowKey = entity.RowKey,
                Name = entity.GetString("Name") ?? "",
                Description = entity.GetString("Description") ?? "",
                Price = (decimal)(entity.GetDouble("Price") ?? 0.0),
                StockQuantity = entity.GetInt32("StockQuantity") ?? 0,
                Category = entity.GetString("Category") ?? "",
                ImageUrl = entity.GetString("ImageUrl") ?? "",
                Brand = entity.GetString("Brand") ?? "",
                IsActive = entity.GetBoolean("IsActive") ?? true,
                CreatedDate = entity.GetDateTime("CreatedDate") ?? DateTime.UtcNow,
                LastModifiedDate = entity.GetDateTime("LastModifiedDate")
            };
        }

        private Customer MapTableEntityToCustomer(TableEntity entity)
        {
            return new Customer
            {
                RowKey = entity.RowKey,
                FirstName = entity.GetString("FirstName") ?? "",
                LastName = entity.GetString("LastName") ?? "",
                Email = entity.GetString("Email") ?? "",
                Phone = entity.GetString("Phone") ?? "",
                Address = entity.GetString("Address") ?? "",
                City = entity.GetString("City") ?? "",
                State = entity.GetString("State") ?? "",
                ZipCode = entity.GetString("ZipCode") ?? "",
                IsActive = entity.GetBoolean("IsActive") ?? true,
                CreatedDate = entity.GetDateTime("CreatedDate") ?? DateTime.UtcNow
            };
        }

        private TableEntity MapProductToTableEntity(Product product)
        {
            var entity = new TableEntity("Product", product.RowKey)
            {
                ["Name"] = product.Name,
                ["Description"] = product.Description,
                ["Price"] = product.Price,
                ["StockQuantity"] = product.StockQuantity,
                ["Category"] = product.Category,
                ["ImageUrl"] = product.ImageUrl,
                ["Brand"] = product.Brand,
                ["IsActive"] = product.IsActive,
                ["CreatedDate"] = product.CreatedDate
            };
            
            if (product.LastModifiedDate.HasValue)
            {
                entity["LastModifiedDate"] = product.LastModifiedDate.Value;
            }
            
            return entity;
        }

        private TableEntity MapCustomerToTableEntity(Customer customer)
        {
            var entity = new TableEntity("Customer", customer.RowKey)
            {
                ["FirstName"] = customer.FirstName,
                ["LastName"] = customer.LastName,
                ["Email"] = customer.Email,
                ["Phone"] = customer.Phone,
                ["Address"] = customer.Address,
                ["City"] = customer.City,
                ["State"] = customer.State,
                ["ZipCode"] = customer.ZipCode,
                ["IsActive"] = customer.IsActive,
                ["CreatedDate"] = customer.CreatedDate
            };
            
            return entity;
        }
    }
}
