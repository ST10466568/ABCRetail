using Azure.Data.Tables;
using ABCRetail.Models;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace ABCRetail.Services
{
    public class AzureTableServiceV2 : IAzureTableServiceV2
    {
        private readonly TableServiceClient _tableServiceClient;
        private readonly TableClient _productsTableClient;
        private readonly TableClient _customersTableClient;
        private readonly ILogger<AzureTableServiceV2> _logger;

        public AzureTableServiceV2(IConfiguration configuration, ILogger<AzureTableServiceV2> logger)
        {
            _logger = logger;
            
            try
            {
                // Use connection string instead of SAS tokens
                var connectionString = configuration.GetConnectionString("AzureStorage") 
                    ?? configuration["AzureStorage:ConnectionString"];
                
                if (string.IsNullOrEmpty(connectionString))
                {
                    throw new InvalidOperationException("Azure Storage connection string not found in configuration");
                }

                _logger.LogInformation("🔗 Initializing Azure Table Service V2 with connection string");
                
                _tableServiceClient = new TableServiceClient(connectionString);
                _productsTableClient = _tableServiceClient.GetTableClient("Products");
                _customersTableClient = _tableServiceClient.GetTableClient("Customers");
                
                _logger.LogInformation("✅ Azure Table Service V2 initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to initialize Azure Table Service V2");
                throw;
            }
        }

        public async Task<List<Product>> GetAllProductsAsync()
        {
            try
            {
                _logger.LogInformation("📦 Retrieving all products using Azure SDK");
                
                var products = new List<Product>();
                await foreach (var entity in _productsTableClient.QueryAsync<TableEntity>())
                {
                    var product = MapTableEntityToProduct(entity);
                    products.Add(product);
                    _logger.LogInformation($"📦 Found product: {product.Name} (PartitionKey: {product.PartitionKey}, RowKey: {product.RowKey})");
                }
                
                _logger.LogInformation($"✅ Retrieved {products.Count} products successfully");
                return products;
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
                _logger.LogInformation("👥 Retrieving all customers using Azure SDK");
                
                var customers = new List<Customer>();
                await foreach (var entity in _customersTableClient.QueryAsync<TableEntity>())
                {
                    var customer = MapTableEntityToCustomer(entity);
                    customers.Add(customer);
                    _logger.LogInformation($"👥 Found customer: {customer.FullName} (PartitionKey: {customer.PartitionKey}, RowKey: {customer.RowKey})");
                }
                
                _logger.LogInformation($"✅ Retrieved {customers.Count} customers successfully");
                return customers;
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
                _logger.LogInformation($"🔍 Retrieving product with ID: {id}");
                
                // First try to get by RowKey only (search all partition keys)
                var products = new List<Product>();
                await foreach (var entity in _productsTableClient.QueryAsync<TableEntity>($"RowKey eq '{id}'"))
                {
                    var product = MapTableEntityToProduct(entity);
                    products.Add(product);
                }
                
                if (products.Count > 0)
                {
                    var product = products.First();
                    _logger.LogInformation($"✅ Product '{product.Name}' retrieved successfully (PartitionKey: {product.PartitionKey})");
                    return product;
                }
                
                // If not found, try the hardcoded approach as fallback
                try
                {
                    var response = await _productsTableClient.GetEntityAsync<TableEntity>("Product", id);
                    if (response.HasValue)
                    {
                        var product = MapTableEntityToProduct(response.Value);
                        _logger.LogInformation($"✅ Product '{product.Name}' retrieved successfully (fallback method)");
                        return product;
                    }
                }
                catch (Exception fallbackEx)
                {
                    _logger.LogWarning($"⚠️ Fallback method failed: {fallbackEx.Message}");
                }
                
                _logger.LogWarning($"⚠️ Product with ID {id} not found in any partition");
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
                _logger.LogInformation($"🔍 Retrieving customer with ID: {id}");
                
                // First try to get by RowKey only (search all partition keys)
                var customers = new List<Customer>();
                await foreach (var entity in _customersTableClient.QueryAsync<TableEntity>($"RowKey eq '{id}'"))
                {
                    var customer = MapTableEntityToCustomer(entity);
                    customers.Add(customer);
                }
                
                if (customers.Count > 0)
                {
                    var customer = customers.First();
                    _logger.LogInformation($"✅ Customer '{customer.FullName}' retrieved successfully (PartitionKey: {customer.PartitionKey})");
                    return customer;
                }
                
                // If not found, try the hardcoded approach as fallback
                try
                {
                    var response = await _customersTableClient.GetEntityAsync<TableEntity>("Customer", id);
                    if (response.Value != null)
                    {
                        var customer = MapTableEntityToCustomer(response.Value);
                        _logger.LogInformation($"✅ Customer '{customer.FullName}' retrieved successfully (fallback method)");
                        return customer;
                    }
                }
                catch (Exception fallbackEx)
                {
                    _logger.LogWarning($"⚠️ Fallback method failed: {fallbackEx.Message}");
                }
                
                _logger.LogWarning($"⚠️ Customer with ID {id} not found in any partition");
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
                _logger.LogInformation($"🔧 Updating product: {product.Name} (ID: {product.RowKey})");
                
                // Update the last modified date
                product.LastModifiedDate = DateTime.UtcNow;
                
                // Convert to TableEntity
                var tableEntity = MapProductToTableEntity(product);
                
                // Use MergeEntityAsync for updates
                await _productsTableClient.UpdateEntityAsync(tableEntity, tableEntity.ETag, Azure.Data.Tables.TableUpdateMode.Merge);
                
                _logger.LogInformation($"✅ Product '{product.Name}' updated successfully");
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
                _logger.LogInformation($"🔧 Updating customer: {customer.FullName} (ID: {customer.RowKey})");
                
                // Convert to TableEntity
                var tableEntity = MapCustomerToTableEntity(customer);
                
                // Use MergeEntityAsync for updates
                await _customersTableClient.UpdateEntityAsync(tableEntity, tableEntity.ETag, Azure.Data.Tables.TableUpdateMode.Merge);
                
                _logger.LogInformation($"✅ Customer '{customer.FullName}' updated successfully");
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
                _logger.LogInformation($"➕ Creating new product: {product.Name}");
                
                // Set creation date
                product.CreatedDate = DateTime.UtcNow;
                
                // Convert to TableEntity
                var tableEntity = MapProductToTableEntity(product);
                
                // Use AddEntityAsync for creation
                await _productsTableClient.AddEntityAsync(tableEntity);
                
                _logger.LogInformation($"✅ Product '{product.Name}' created successfully");
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
                _logger.LogInformation($"➕ Creating new customer: {customer.FullName}");
                
                // Set creation date
                customer.CreatedDate = DateTime.UtcNow;
                
                // Convert to TableEntity
                var tableEntity = MapCustomerToTableEntity(customer);
                
                // Use AddEntityAsync for creation
                await _customersTableClient.AddEntityAsync(tableEntity);
                
                _logger.LogInformation($"✅ Customer '{customer.FullName}' created successfully");
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
                _logger.LogInformation($"🗑️ Deleting product with ID: {id}");
                
                await _productsTableClient.DeleteEntityAsync("Product", id);
                
                _logger.LogInformation($"✅ Product with ID {id} deleted successfully");
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
                _logger.LogInformation($"🗑️ Deleting customer with ID: {id}");
                
                await _customersTableClient.DeleteEntityAsync("Customer", id);
                
                _logger.LogInformation($"✅ Customer with ID {id} deleted successfully");
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
                _logger.LogInformation("📋 Listing available tables");
                
                var tableNames = new List<string>();
                await foreach (var table in _tableServiceClient.QueryAsync())
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
            _logger.LogWarning("⚠️ RecoverMissingProductAsync not implemented in AzureTableServiceV2 - focusing on reading first");
            return null;
        }
        
        public async Task<string> DiagnoseProductStatusAsync(string productId)
        {
            _logger.LogWarning("⚠️ DiagnoseProductStatusAsync not implemented in AzureTableServiceV2 - focusing on reading first");
            return "Diagnosis not implemented in this service";
        }

        public async Task<Customer?> RecoverMissingCustomerAsync(string customerId)
        {
            _logger.LogWarning("⚠️ RecoverMissingCustomerAsync not implemented in AzureTableServiceV2 - focusing on reading first");
            return null;
        }

        public async Task<string> DiagnoseCustomerStatusAsync(string customerId)
        {
            _logger.LogWarning("⚠️ DiagnoseCustomerStatusAsync not implemented in AzureTableServiceV2 - focusing on reading first");
            return "Diagnosis not implemented in this service";
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
