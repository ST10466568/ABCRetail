using Azure.Data.Tables;
using Azure;
using ABCRetail.Models;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace ABCRetail.Services
{
    public class ConnectionStringService : IAzureTableServiceV2
    {
        private readonly ILogger<ConnectionStringService> _logger;
        private readonly string _connectionString;
        private readonly TableServiceClient _tableServiceClient;
        private readonly TableClient _productsTableClient;
        private readonly TableClient _customersTableClient;

        public ConnectionStringService(IConfiguration configuration, ILogger<ConnectionStringService> logger)
        {
            _logger = logger;
            
            // Get connection string from both possible locations
            _connectionString = configuration.GetConnectionString("AzureStorage") 
                ?? configuration["AzureStorage:ConnectionString"];
            
            if (string.IsNullOrEmpty(_connectionString))
            {
                throw new InvalidOperationException("Azure Storage connection string not found in configuration");
            }
            
            _logger.LogInformation("🔗 ConnectionStringService initialized with connection string");
            
            try
            {
                // Initialize Azure SDK clients
                _tableServiceClient = new TableServiceClient(_connectionString);
                _productsTableClient = _tableServiceClient.GetTableClient("Products");
                _customersTableClient = _tableServiceClient.GetTableClient("Customers");
                
                _logger.LogInformation("✅ Azure SDK clients initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to initialize Azure SDK clients");
                throw;
            }
        }

        public async Task<List<Product>> GetAllProductsAsync()
        {
            try
            {
                _logger.LogInformation("📦 Retrieving all products using Azure SDK with connection string");
                
                var products = new List<Product>();
                await foreach (var entity in _productsTableClient.QueryAsync<TableEntity>())
                {
                    try
                    {
                        var product = MapTableEntityToProduct(entity);
                        products.Add(product);
                    }
                    catch (Exception mapEx)
                    {
                        _logger.LogWarning(mapEx, "⚠️ Failed to map product entity: {PartitionKey}/{RowKey}", 
                            entity.PartitionKey, entity.RowKey);
                    }
                }
                
                _logger.LogInformation("✅ Retrieved {Count} products successfully via Azure SDK", products.Count);
                
                // Log first few products for debugging
                foreach (var product in products.Take(3))
                {
                    _logger.LogInformation("📦 Product: {Name} (ID: {Id}, PartitionKey: {PartitionKey})", 
                        product.Name, product.RowKey, product.PartitionKey);
                }
                
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
                _logger.LogInformation("👥 Retrieving all customers using Azure SDK with connection string");
                
                var customers = new List<Customer>();
                await foreach (var entity in _customersTableClient.QueryAsync<TableEntity>())
                {
                    try
                    {
                        var customer = MapTableEntityToCustomer(entity);
                        customers.Add(customer);
                    }
                    catch (Exception mapEx)
                    {
                        _logger.LogWarning(mapEx, "⚠️ Failed to map customer entity: {PartitionKey}/{RowKey}", 
                            entity.PartitionKey, entity.RowKey);
                    }
                }
                
                _logger.LogInformation("✅ Retrieved {Count} customers successfully via Azure SDK", customers.Count);
                
                // Log first few customers for debugging
                foreach (var customer in customers.Take(3))
                {
                    _logger.LogInformation("👥 Customer: {FullName} (ID: {Id}, PartitionKey: {PartitionKey})", 
                        customer.FullName, customer.RowKey, customer.PartitionKey);
                }
                
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
                _logger.LogInformation("🔍 Retrieving product with ID: {ProductId} using Azure SDK", id);
                
                // Strategy 1: Try with hardcoded PartitionKey first
                try
                {
                    var response = await _productsTableClient.GetEntityAsync<TableEntity>("Product", id);
                    if (response.HasValue)
                    {
                        var product = MapTableEntityToProduct(response.Value);
                        _logger.LogInformation("✅ Product '{Name}' found with PartitionKey 'Product'", product.Name);
                        return product;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug("⚠️ Product not found with PartitionKey 'Product': {Message}", ex.Message);
                }
                
                // Strategy 2: Try with different partition keys
                var partitionKeys = new[] { "Products", "product", "products" };
                foreach (var partitionKey in partitionKeys)
                {
                    try
                    {
                        var response = await _productsTableClient.GetEntityAsync<TableEntity>(partitionKey, id);
                        if (response.HasValue)
                        {
                            var product = MapTableEntityToProduct(response.Value);
                            _logger.LogInformation("✅ Product '{Name}' found with PartitionKey '{PartitionKey}'", 
                                product.Name, partitionKey);
                            return product;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug("⚠️ Product not found with PartitionKey '{PartitionKey}': {Message}", 
                            partitionKey, ex.Message);
                    }
                }
                
                // Strategy 3: Query by RowKey only
                try
                {
                    await foreach (var entity in _productsTableClient.QueryAsync<TableEntity>($"RowKey eq '{id}'"))
                    {
                        var product = MapTableEntityToProduct(entity);
                        _logger.LogInformation("✅ Product '{Name}' found via RowKey query (PartitionKey: {PartitionKey})", 
                            product.Name, entity.PartitionKey);
                        return product;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug("⚠️ RowKey query failed: {Message}", ex.Message);
                }
                
                _logger.LogWarning("⚠️ Product with ID {ProductId} not found via any strategy", id);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error retrieving product with ID: {ProductId}", id);
                throw;
            }
        }

        public async Task<Customer?> GetCustomerAsync(string id)
        {
            try
            {
                _logger.LogInformation("🔍 Retrieving customer with ID: {CustomerId} using Azure SDK", id);
                
                // Strategy 1: Try with hardcoded PartitionKey first
                try
                {
                    var response = await _customersTableClient.GetEntityAsync<TableEntity>("Customer", id);
                    if (response.HasValue)
                    {
                        var customer = MapTableEntityToCustomer(response.Value);
                        _logger.LogInformation("✅ Customer '{FullName}' found with PartitionKey 'Customer'", customer.FullName);
                        return customer;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug("⚠️ Customer not found with PartitionKey 'Customer': {Message}", ex.Message);
                }
                
                // Strategy 2: Try with different partition keys
                var partitionKeys = new[] { "Customers", "customer", "customers" };
                foreach (var partitionKey in partitionKeys)
                {
                    try
                    {
                        var response = await _customersTableClient.GetEntityAsync<TableEntity>(partitionKey, id);
                        if (response.HasValue)
                        {
                            var customer = MapTableEntityToCustomer(response.Value);
                            _logger.LogInformation("✅ Customer '{FullName}' found with PartitionKey '{PartitionKey}'", 
                                customer.FullName, partitionKey);
                            return customer;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug("⚠️ Customer not found with PartitionKey '{PartitionKey}': {Message}", 
                            partitionKey, ex.Message);
                    }
                }
                
                // Strategy 3: Query by RowKey only
                try
                {
                    await foreach (var entity in _customersTableClient.QueryAsync<TableEntity>($"RowKey eq '{id}'"))
                    {
                        var customer = MapTableEntityToCustomer(entity);
                        _logger.LogInformation("✅ Customer '{FullName}' found via RowKey query (PartitionKey: {PartitionKey})", 
                            customer.FullName, entity.PartitionKey);
                        return customer;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug("⚠️ RowKey query failed: {Message}", ex.Message);
                }
                
                _logger.LogWarning("⚠️ Customer with ID {CustomerId} not found via any strategy", id);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error retrieving customer with ID: {CustomerId}", id);
                throw;
            }
        }

        public async Task<bool> UpdateProductAsync(Product product)
        {
            try
            {
                _logger.LogInformation("🔧 Updating product: {Name} (ID: {Id}) using Azure SDK", product.Name, product.RowKey);
                
                // Strategy 1: Try update with ETag if available
                if (product.ETag != default(ETag))
                {
                    try
                    {
                        _logger.LogInformation("🔧 Strategy 1: Updating with ETag: {ETag}", product.ETag);
                        
                        // Update the last modified date
                        product.LastModifiedDate = DateTime.UtcNow;
                        
                        // Convert to TableEntity
                        var tableEntity = MapProductToTableEntity(product);
                        
                        // Use MergeEntityAsync for updates with the product's ETag
                        await _productsTableClient.UpdateEntityAsync(tableEntity, product.ETag, TableUpdateMode.Merge);
                        
                        _logger.LogInformation("✅ Product '{Name}' updated successfully via ETag strategy", product.Name);
                        return true;
                    }
                    catch (Exception etagEx)
                    {
                        _logger.LogWarning(etagEx, "⚠️ ETag update failed, trying alternative strategies: {Message}", etagEx.Message);
                    }
                }
                
                // Strategy 2: Try to retrieve the product first to get fresh ETag
                try
                {
                    _logger.LogInformation("🔧 Strategy 2: Retrieving fresh product to get ETag");
                    
                    var freshProduct = await GetProductAsync(product.RowKey);
                    if (freshProduct != null && freshProduct.ETag != default(ETag))
                    {
                        _logger.LogInformation("🔧 Strategy 2: Found fresh product with ETag: {ETag}", freshProduct.ETag);
                        
                        // Update the fresh product with new values
                        freshProduct.Name = product.Name;
                        freshProduct.Description = product.Description;
                        freshProduct.Price = product.Price;
                        freshProduct.StockQuantity = product.StockQuantity;
                        freshProduct.Category = product.Category;
                        freshProduct.ImageUrl = product.ImageUrl;
                        freshProduct.Brand = product.Brand;
                        freshProduct.IsActive = product.IsActive;
                        freshProduct.LastModifiedDate = DateTime.UtcNow;
                        
                        // Convert to TableEntity
                        var tableEntity = MapProductToTableEntity(freshProduct);
                        
                        // Use MergeEntityAsync for updates with the fresh ETag
                        await _productsTableClient.UpdateEntityAsync(tableEntity, freshProduct.ETag, TableUpdateMode.Merge);
                        
                        _logger.LogInformation("✅ Product '{Name}' updated successfully via fresh ETag strategy", product.Name);
                        return true;
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ Strategy 2: Could not retrieve fresh product or ETag is missing");
                    }
                }
                catch (Exception retrieveEx)
                {
                    _logger.LogWarning(retrieveEx, "⚠️ Strategy 2 failed: {Message}", retrieveEx.Message);
                }
                
                // Strategy 3: Try delete and recreate if all else fails
                try
                {
                    _logger.LogInformation("🔧 Strategy 3: Attempting delete and recreate");
                    
                    // Delete the existing product
                    await _productsTableClient.DeleteEntityAsync("Product", product.RowKey);
                    _logger.LogInformation("✅ Deleted existing product for recreation");
                    
                    // Create new product with same ID
                    product.CreatedDate = DateTime.UtcNow;
                    product.LastModifiedDate = DateTime.UtcNow;
                    
                    var tableEntity = MapProductToTableEntity(product);
                    await _productsTableClient.AddEntityAsync(tableEntity);
                    
                    _logger.LogInformation("✅ Product '{Name}' recreated successfully via delete/recreate strategy", product.Name);
                    return true;
                }
                catch (Exception recreateEx)
                {
                    _logger.LogError(recreateEx, "❌ Strategy 3 (delete/recreate) failed: {Message}", recreateEx.Message);
                }
                
                // If all strategies failed
                var errorMessage = $"❌ All update strategies failed for product: {product.Name} (ID: {product.RowKey})";
                _logger.LogError(errorMessage);
                throw new InvalidOperationException(errorMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to update product: {Name}", product.Name);
                throw;
            }
        }

        public async Task<bool> UpdateCustomerAsync(Customer customer)
        {
            try
            {
                _logger.LogInformation("🔧 Updating customer: {FullName} (ID: {Id}) using Azure SDK", customer.FullName, customer.RowKey);
                
                // Strategy 1: Try update with ETag if available
                if (customer.ETag != default(ETag))
                {
                    try
                    {
                        _logger.LogInformation("🔧 Strategy 1: Updating with ETag: {ETag}", customer.ETag);
                        
                        // Convert to TableEntity
                        var tableEntity = MapCustomerToTableEntity(customer);
                        
                        // Use MergeEntityAsync for updates with the customer's ETag
                        await _customersTableClient.UpdateEntityAsync(tableEntity, customer.ETag, TableUpdateMode.Merge);
                        
                        _logger.LogInformation("✅ Customer '{FullName}' updated successfully via ETag strategy", customer.FullName);
                        return true;
                    }
                    catch (Exception etagEx)
                    {
                        _logger.LogWarning(etagEx, "⚠️ ETag update failed, trying alternative strategies: {Message}", etagEx.Message);
                    }
                }
                
                // Strategy 2: Try to retrieve the customer first to get fresh ETag
                try
                {
                    _logger.LogInformation("🔧 Strategy 2: Retrieving fresh customer to get ETag");
                    
                    var freshCustomer = await GetCustomerAsync(customer.RowKey);
                    if (freshCustomer != null && freshCustomer.ETag != default(ETag))
                    {
                        _logger.LogInformation("🔧 Strategy 2: Found fresh customer with ETag: {ETag}", freshCustomer.ETag);
                        
                        // Update the fresh customer with new values
                        freshCustomer.FirstName = customer.FirstName;
                        freshCustomer.LastName = customer.LastName;
                        freshCustomer.Email = customer.Email;
                        freshCustomer.Phone = customer.Phone;
                        freshCustomer.Address = customer.Address;
                        freshCustomer.City = customer.City;
                        freshCustomer.State = customer.State;
                        freshCustomer.ZipCode = customer.ZipCode;
                        freshCustomer.IsActive = customer.IsActive;
                        
                        // Convert to TableEntity
                        var tableEntity = MapCustomerToTableEntity(freshCustomer);
                        
                        // Use MergeEntityAsync for updates with the fresh ETag
                        await _customersTableClient.UpdateEntityAsync(tableEntity, freshCustomer.ETag, TableUpdateMode.Merge);
                        
                        _logger.LogInformation("✅ Customer '{FullName}' updated successfully via fresh ETag strategy", customer.FullName);
                        return true;
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ Strategy 2: Could not retrieve fresh customer or ETag is missing");
                    }
                }
                catch (Exception retrieveEx)
                {
                    _logger.LogWarning(retrieveEx, "⚠️ Strategy 2 failed: {Message}", retrieveEx.Message);
                }
                
                // Strategy 3: Try delete and recreate if all else fails
                try
                {
                    _logger.LogInformation("🔧 Strategy 3: Attempting delete and recreate");
                    
                    // Delete the existing customer
                    await _customersTableClient.DeleteEntityAsync("Customer", customer.RowKey);
                    _logger.LogInformation("✅ Deleted existing customer for recreation");
                    
                    // Create new customer with same ID
                    customer.CreatedDate = DateTime.UtcNow;
                    
                    var tableEntity = MapCustomerToTableEntity(customer);
                    await _customersTableClient.AddEntityAsync(tableEntity);
                    
                    _logger.LogInformation("✅ Customer '{FullName}' recreated successfully via delete/recreate strategy", customer.FullName);
                    return true;
                }
                catch (Exception recreateEx)
                {
                    _logger.LogError(recreateEx, "❌ Strategy 3 (delete/recreate) failed: {Message}", recreateEx.Message);
                }
                
                // If all strategies failed
                var errorMessage = $"❌ All update strategies failed for customer: {customer.FullName} (ID: {customer.RowKey})";
                _logger.LogError(errorMessage);
                throw new InvalidOperationException(errorMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to update customer: {FullName}", customer.FullName);
                throw;
            }
        }

        public async Task<bool> CreateProductAsync(Product product)
        {
            try
            {
                _logger.LogInformation("➕ Creating new product: {Name} using Azure SDK", product.Name);
                
                // Set creation date
                product.CreatedDate = DateTime.UtcNow;
                
                // Convert to TableEntity
                var tableEntity = MapProductToTableEntity(product);
                
                // Use AddEntityAsync for creation
                await _productsTableClient.AddEntityAsync(tableEntity);
                
                _logger.LogInformation("✅ Product '{Name}' created successfully via Azure SDK", product.Name);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to create product: {Name}", product.Name);
                throw;
            }
        }

        public async Task<bool> CreateCustomerAsync(Customer customer)
        {
            try
            {
                _logger.LogInformation("➕ Creating new customer: {FullName} using Azure SDK", customer.FullName);
                
                // Set creation date
                customer.CreatedDate = DateTime.UtcNow;
                
                // Convert to TableEntity
                var tableEntity = MapCustomerToTableEntity(customer);
                
                // Use AddEntityAsync for creation
                await _customersTableClient.AddEntityAsync(tableEntity);
                
                _logger.LogInformation("✅ Customer '{FullName}' created successfully via Azure SDK", customer.FullName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to create customer: {FullName}", customer.FullName);
                throw;
            }
        }

        public async Task<bool> DeleteProductAsync(string id)
        {
            try
            {
                _logger.LogInformation("🗑️ Deleting product with ID: {Id} using Azure SDK", id);
                
                await _productsTableClient.DeleteEntityAsync("Product", id);
                
                _logger.LogInformation("✅ Product with ID {Id} deleted successfully via Azure SDK", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to delete product with ID: {Id}", id);
                throw;
            }
        }

        public async Task<bool> DeleteCustomerAsync(string id)
        {
            try
            {
                _logger.LogInformation("🗑️ Deleting customer with ID: {Id} using Azure SDK", id);
                
                await _customersTableClient.DeleteEntityAsync("Customer", id);
                
                _logger.LogInformation("✅ Customer with ID {Id} deleted successfully via Azure SDK", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to delete customer with ID: {Id}", id);
                throw;
            }
        }

        public async Task<List<string>> ListTablesAsync()
        {
            try
            {
                _logger.LogInformation("📋 Listing available tables using Azure SDK");
                
                var tableNames = new List<string>();
                await foreach (var table in _tableServiceClient.QueryAsync())
                {
                    tableNames.Add(table.Name);
                }
                
                _logger.LogInformation("✅ Found {Count} tables: {Tables}", tableNames.Count, string.Join(", ", tableNames));
                return tableNames;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to list tables");
                throw;
            }
        }

        // Recovery method for missing products
        public async Task<Product?> RecoverMissingProductAsync(string productId)
        {
            try
            {
                _logger.LogInformation("🔍 Attempting to recover missing product: {ProductId}", productId);
                
                // Try multiple partition keys
                var partitionKeys = new[] { "Product", "Products", "product", "products" };
                
                foreach (var partitionKey in partitionKeys)
                {
                    try
                    {
                        _logger.LogInformation("🔍 Trying partition key: {PartitionKey}", partitionKey);
                        
                        var response = await _productsTableClient.GetEntityAsync<TableEntity>(partitionKey, productId);
                        if (response.HasValue)
                        {
                            var product = MapTableEntityToProduct(response.Value);
                            _logger.LogInformation("✅ Product '{Name}' recovered with partition key: {PartitionKey}", 
                                product.Name, partitionKey);
                            return product;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug("⚠️ Partition key {PartitionKey} failed: {Message}", partitionKey, ex.Message);
                    }
                }
                
                // Try querying by RowKey only
                try
                {
                    _logger.LogInformation("🔍 Trying RowKey-only query");
                    await foreach (var entity in _productsTableClient.QueryAsync<TableEntity>($"RowKey eq '{productId}'"))
                    {
                        var product = MapTableEntityToProduct(entity);
                        _logger.LogInformation("✅ Product '{Name}' recovered via RowKey query (PartitionKey: {PartitionKey})", 
                            product.Name, entity.PartitionKey);
                        return product;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug("⚠️ RowKey query failed: {Message}", ex.Message);
                }
                
                _logger.LogWarning("⚠️ Could not recover product with ID: {ProductId}", productId);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error during product recovery: {ProductId}", productId);
                return null;
            }
        }

        // Recovery method for missing customers
        public async Task<Customer?> RecoverMissingCustomerAsync(string customerId)
        {
            try
            {
                _logger.LogInformation("🔍 Attempting to recover missing customer: {CustomerId}", customerId);
                
                // Try multiple partition keys
                var partitionKeys = new[] { "Customer", "Customers", "customer", "customers" };
                
                foreach (var partitionKey in partitionKeys)
                {
                    try
                    {
                        _logger.LogInformation("🔍 Trying partition key: {PartitionKey}", partitionKey);
                        
                        var response = await _customersTableClient.GetEntityAsync<TableEntity>(partitionKey, customerId);
                        if (response.HasValue)
                        {
                            var customer = MapTableEntityToCustomer(response.Value);
                            _logger.LogInformation("✅ Customer '{FullName}' recovered with partition key: {PartitionKey}", 
                                customer.FullName, partitionKey);
                            return customer;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug("⚠️ Partition key {PartitionKey} failed: {Message}", partitionKey, ex.Message);
                    }
                }
                
                // Try querying by RowKey only
                try
                {
                    _logger.LogInformation("🔍 Trying RowKey-only query");
                    await foreach (var entity in _customersTableClient.QueryAsync<TableEntity>($"RowKey eq '{customerId}'"))
                    {
                        var customer = MapTableEntityToCustomer(entity);
                        _logger.LogInformation("✅ Customer '{FullName}' recovered via RowKey query (PartitionKey: {PartitionKey})", 
                            customer.FullName, entity.PartitionKey);
                        return customer;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug("⚠️ RowKey query failed: {Message}", ex.Message);
                }
                
                _logger.LogWarning("⚠️ Could not recover customer with ID: {CustomerId}", customerId);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error during customer recovery: {CustomerId}", customerId);
                return null;
            }
        }

        // Diagnostic method to check product status
        public async Task<string> DiagnoseProductStatusAsync(string productId)
        {
            try
            {
                _logger.LogInformation("🔍 Diagnosing product status: {ProductId}", productId);
                
                var diagnostics = new List<string>();
                
                // Check if product exists with different partition keys
                var partitionKeys = new[] { "Product", "Products", "product", "products" };
                foreach (var partitionKey in partitionKeys)
                {
                    try
                    {
                        var response = await _productsTableClient.GetEntityAsync<TableEntity>(partitionKey, productId);
                        if (response.HasValue)
                        {
                            diagnostics.Add($"✅ Found with PartitionKey '{partitionKey}': {response.Value.GetString("Name")}");
                        }
                        else
                        {
                            diagnostics.Add($"⚠️ Not found with PartitionKey '{partitionKey}'");
                        }
                    }
                    catch (Exception ex)
                    {
                        diagnostics.Add($"❌ Error with PartitionKey '{partitionKey}': {ex.Message}");
                    }
                }
                
                // Check RowKey query
                try
                {
                    var found = false;
                    await foreach (var entity in _productsTableClient.QueryAsync<TableEntity>($"RowKey eq '{productId}'"))
                    {
                        diagnostics.Add($"✅ Found via RowKey query: {entity.GetString("Name")} (PartitionKey: {entity.PartitionKey})");
                        found = true;
                    }
                    if (!found)
                    {
                        diagnostics.Add("⚠️ Not found via RowKey query");
                    }
                }
                catch (Exception ex)
                {
                    diagnostics.Add($"❌ RowKey query error: {ex.Message}");
                }
                
                var result = string.Join("\n", diagnostics);
                _logger.LogInformation("🔍 Product diagnosis complete: {ProductId}\n{Diagnosis}", productId, result);
                return result;
            }
            catch (Exception ex)
            {
                var error = $"❌ Diagnosis failed: {ex.Message}";
                _logger.LogError(ex, "❌ Error during product diagnosis: {ProductId}", productId);
                return error;
            }
        }

        // Diagnostic method to check customer status
        public async Task<string> DiagnoseCustomerStatusAsync(string customerId)
        {
            try
            {
                _logger.LogInformation("🔍 Diagnosing customer status: {CustomerId}", customerId);
                
                var diagnostics = new List<string>();
                
                // Check if customer exists with different partition keys
                var partitionKeys = new[] { "Customer", "Customers", "customer", "customers" };
                foreach (var partitionKey in partitionKeys)
                {
                    try
                    {
                        var response = await _customersTableClient.GetEntityAsync<TableEntity>(partitionKey, customerId);
                        if (response.HasValue)
                        {
                            var firstName = response.Value.GetString("FirstName") ?? "";
                            var lastName = response.Value.GetString("LastName") ?? "";
                            diagnostics.Add($"✅ Found with PartitionKey '{partitionKey}': {firstName} {lastName}");
                        }
                        else
                        {
                            diagnostics.Add($"⚠️ Not found with PartitionKey '{partitionKey}'");
                        }
                    }
                    catch (Exception ex)
                    {
                        diagnostics.Add($"❌ Error with PartitionKey '{partitionKey}': {ex.Message}");
                    }
                }
                
                // Check RowKey query
                try
                {
                    var found = false;
                    await foreach (var entity in _customersTableClient.QueryAsync<TableEntity>($"RowKey eq '{customerId}'"))
                    {
                        var firstName = entity.GetString("FirstName") ?? "";
                        var lastName = entity.GetString("LastName") ?? "";
                        diagnostics.Add($"✅ Found via RowKey query: {firstName} {lastName} (PartitionKey: {entity.PartitionKey})");
                        found = true;
                    }
                    if (!found)
                    {
                        diagnostics.Add("⚠️ Not found via RowKey query");
                    }
                }
                catch (Exception ex)
                {
                    diagnostics.Add($"❌ RowKey query error: {ex.Message}");
                }
                
                var result = string.Join("\n", diagnostics);
                _logger.LogInformation("🔍 Customer diagnosis complete: {CustomerId}\n{Diagnosis}", customerId, result);
                return result;
            }
            catch (Exception ex)
            {
                var error = $"❌ Diagnosis failed: {ex.Message}";
                _logger.LogError(ex, "❌ Error during customer diagnosis: {CustomerId}", customerId);
                return error;
            }
        }

        // Helper methods for mapping between models and TableEntity
        private Product MapTableEntityToProduct(TableEntity entity)
        {
            var product = new Product
            {
                RowKey = entity.RowKey,
                PartitionKey = entity.PartitionKey,
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
            
            // Set the ETag from the TableEntity
            product.ETag = entity.ETag;
            _logger.LogDebug("✅ ETag set for product {ProductName}: {ETag}", product.Name, product.ETag);
            
            return product;
        }

        private Customer MapTableEntityToCustomer(TableEntity entity)
        {
            var customer = new Customer
            {
                RowKey = entity.RowKey,
                PartitionKey = entity.PartitionKey,
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
            
            // Set the ETag from the TableEntity
            customer.ETag = entity.ETag;
            _logger.LogDebug("✅ ETag set for customer {FullName}: {ETag}", customer.FullName, customer.ETag);
            
            return customer;
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
            
            // Preserve the ETag from the original product
            if (product.ETag != default(ETag))
            {
                entity.ETag = product.ETag;
                _logger.LogDebug("✅ ETag preserved in TableEntity: {ETag}", entity.ETag);
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
            
            // Preserve the ETag from the original customer
            if (customer.ETag != default(ETag))
            {
                entity.ETag = customer.ETag;
                _logger.LogDebug("✅ ETag preserved in TableEntity: {ETag}", entity.ETag);
            }
            
            return entity;
        }
    }
}
