using Azure;
using Azure.Data.Tables;
using Microsoft.Extensions.Configuration;
using ABCRetail.Models;
using ABCRetail.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ABCRetail.Services
{
    public class AzureTableService : IAzureTableService
    {
        private readonly TableServiceClient _tableServiceClient;
        private readonly WorkingDataFetcher _workingDataFetcher;

        public AzureTableService(IConfiguration configuration)
        {
            var connectionString = configuration["AzureStorage:ConnectionString"];
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("Azure Storage connection string is not configured");
            }
            Console.WriteLine($"AzureTableService: Using connection string: {connectionString.Substring(0, Math.Min(50, connectionString.Length))}...");
            _tableServiceClient = new TableServiceClient(connectionString);
            _workingDataFetcher = new WorkingDataFetcher(configuration);
        }

        public async Task<List<string>> ListTablesAsync()
        {
            try
            {
                var tables = new List<string>();
                await foreach (var table in _tableServiceClient.QueryAsync())
                {
                    tables.Add(table.Name);
                }
                return tables;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AzureTableService: ListTablesAsync failed: {ex.Message}");
                // Return known table names as fallback
                return new List<string> { "Customers", "Products", "Orders" };
            }
        }

        public async Task<IEnumerable<T>> GetAllEntitiesAsync<T>() where T : class, ITableEntity, new()
        {
            try
            {
                // Try Azure SDK first
                var tableName = GetTableName<T>();
                Console.WriteLine($"AzureTableService: Trying to get all {typeof(T).Name} entities from table '{tableName}' using Azure SDK");
                
                var tableClient = _tableServiceClient.GetTableClient(tableName);
                
                // Check if table exists first
                try
                {
                    await tableClient.CreateIfNotExistsAsync();
                    Console.WriteLine($"AzureTableService: Table '{tableName}' exists or was created");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"AzureTableService: Failed to create/verify table '{tableName}': {ex.Message}");
                }
                
                var entities = new List<T>();
                
                // Try different query approaches
                try
                {
                    // Method 1: Query with empty filter
                    var query = tableClient.QueryAsync<T>(filter: "");
                    await foreach (var entity in query)
                    {
                        entities.Add(entity);
                        Console.WriteLine($"AzureTableService: Found entity: {entity}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"AzureTableService: Method 1 failed: {ex.Message}");
                    
                    try
                    {
                        // Method 2: Query with specific partition key
                        if (typeof(T) == typeof(Customer))
                        {
                            var customerQuery = tableClient.QueryAsync<Customer>(filter: "PartitionKey eq 'Customer'");
                            await foreach (var entity in customerQuery)
                            {
                                entities.Add(entity as T);
                                Console.WriteLine($"AzureTableService: Found customer: {entity.FirstName} {entity.LastName}");
                            }
                        }
                        else if (typeof(T) == typeof(Product))
                        {
                            var productQuery = tableClient.QueryAsync<Product>(filter: "PartitionKey eq 'Product'");
                            await foreach (var entity in productQuery)
                            {
                                entities.Add(entity as T);
                                Console.WriteLine($"AzureTableService: Found product: {entity.Name}");
                            }
                        }
                    }
                    catch (Exception ex2)
                    {
                        Console.WriteLine($"AzureTableService: Method 2 also failed: {ex2.Message}");
                    }
                }
                
                if (entities.Any())
                {
                    Console.WriteLine($"AzureTableService: Successfully retrieved {entities.Count} {typeof(T).Name} entities via Azure SDK");
                    return entities;
                }
                else
                {
                    Console.WriteLine($"AzureTableService: No {typeof(T).Name} entities found via Azure SDK, falling back to WorkingDataFetcher");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AzureTableService: Azure SDK failed for {typeof(T).Name}: {ex.Message}, falling back to WorkingDataFetcher");
            }

            // Fallback to WorkingDataFetcher
            Console.WriteLine($"AzureTableService: Using WorkingDataFetcher fallback for {typeof(T).Name}");
            return await GetEntitiesViaWorkingFetcher<T>();
        }

        public async Task<T?> GetEntityAsync<T>(string partitionKey, string rowKey) where T : class, ITableEntity, new()
        {
            try
            {
                // Try Azure SDK first
                var tableName = GetTableName<T>();
                var tableClient = _tableServiceClient.GetTableClient(tableName);
                var entity = await tableClient.GetEntityAsync<T>(partitionKey, rowKey);
                if (entity.HasValue)
                {
                    Console.WriteLine($"AzureTableService: Successfully retrieved {typeof(T).Name} entity via Azure SDK");
                    return entity.Value;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AzureTableService: Azure SDK failed for {typeof(T).Name}: {ex.Message}, falling back to WorkingDataFetcher");
            }

            // Fallback to WorkingDataFetcher
            Console.WriteLine($"AzureTableService: Using WorkingDataFetcher fallback for {typeof(T).Name}");
            return await GetEntityViaWorkingFetcher<T>(partitionKey, rowKey);
        }

        public async Task<IEnumerable<T>> GetEntitiesAsync<T>(string partitionKey) where T : class, ITableEntity, new()
        {
            try
            {
                // Try Azure SDK first
                var tableName = GetTableName<T>();
                var tableClient = _tableServiceClient.GetTableClient(tableName);
                
                var entities = new List<T>();
                var query = tableClient.QueryAsync<T>(e => e.PartitionKey == partitionKey);
                await foreach (var entity in query)
                {
                    entities.Add(entity);
                }
                
                if (entities.Any())
                {
                    Console.WriteLine($"AzureTableService: Successfully retrieved {entities.Count} {typeof(T).Name} entities with PartitionKey '{partitionKey}' via Azure SDK");
                    return entities;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AzureTableService: Azure SDK failed for {typeof(T).Name}: {ex.Message}, falling back to WorkingDataFetcher");
            }

            // Fallback to WorkingDataFetcher
            Console.WriteLine($"AzureTableService: Using WorkingDataFetcher fallback for {typeof(T).Name}");
            var allEntities = await GetEntitiesViaWorkingFetcher<T>();
            return allEntities.Where(e => e.PartitionKey == partitionKey);
        }

        public async Task<bool> CreateEntityAsync<T>(T entity) where T : class, ITableEntity
        {
            try
            {
                var tableName = GetTableName<T>();
                var tableClient = _tableServiceClient.GetTableClient(tableName);
                await tableClient.AddEntityAsync(entity);
                Console.WriteLine($"AzureTableService: Successfully created {typeof(T).Name} entity via Azure SDK");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AzureTableService: CreateEntityAsync failed for {typeof(T).Name}: {ex.Message}");
                // Note: WorkingDataFetcher is read-only, cannot create entities
                return false;
            }
        }

        public async Task<bool> UpdateEntityAsync<T>(T entity) where T : class, ITableEntity
        {
            try
            {
                var tableName = GetTableName<T>();
                var tableClient = _tableServiceClient.GetTableClient(tableName);
                await tableClient.UpdateEntityAsync(entity, ETag.All, TableUpdateMode.Replace);
                Console.WriteLine($"AzureTableService: Successfully updated {typeof(T).Name} entity via Azure SDK");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AzureTableService: UpdateEntityAsync failed for {typeof(T).Name}: {ex.Message}");
                // Note: WorkingDataFetcher is read-only, cannot update entities
                return false;
            }
        }

        public async Task<bool> DeleteEntityAsync<T>(string partitionKey, string rowKey) where T : class, ITableEntity, new()
        {
            try
            {
                var tableName = GetTableName<T>();
                var tableClient = _tableServiceClient.GetTableClient(tableName);
                await tableClient.DeleteEntityAsync(partitionKey, rowKey);
                Console.WriteLine($"AzureTableService: Successfully deleted {typeof(T).Name} entity via Azure SDK");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AzureTableService: DeleteEntityAsync failed for {typeof(T).Name}: {ex.Message}");
                // Note: WorkingDataFetcher is read-only, cannot delete entities
                return false;
            }
        }

        public async Task<bool> EntityExistsAsync<T>(string partitionKey, string rowKey) where T : class, ITableEntity, new()
        {
            try
            {
                var tableName = GetTableName<T>();
                var tableClient = _tableServiceClient.GetTableClient(tableName);
                var entity = await tableClient.GetEntityAsync<T>(partitionKey, rowKey);
                return entity.HasValue;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AzureTableService: EntityExistsAsync failed for {typeof(T).Name}: {ex.Message}, falling back to WorkingDataFetcher");
            }

            // Fallback to WorkingDataFetcher
            Console.WriteLine($"AzureTableService: Using WorkingDataFetcher fallback for EntityExistsAsync");
            var fallbackEntity = await GetEntityViaWorkingFetcher<T>(partitionKey, rowKey);
            return fallbackEntity != null;
        }

        private string GetTableName<T>()
        {
            if (typeof(T) == typeof(Customer)) return "Customers";
            if (typeof(T) == typeof(Product)) return "Products";
            if (typeof(T) == typeof(Order)) return "Orders";
            throw new ArgumentException($"Unknown entity type: {typeof(T).Name}");
        }

        // WorkingDataFetcher fallback methods
        private async Task<IEnumerable<T>> GetEntitiesViaWorkingFetcher<T>() where T : class, ITableEntity, new()
        {
            try
            {
                if (typeof(T) == typeof(Customer))
                {
                    var customers = await _workingDataFetcher.GetCustomersAsync();
                    return customers.Cast<T>();
                }
                else if (typeof(T) == typeof(Product))
                {
                    var products = await _workingDataFetcher.GetProductsAsync();
                    return products.Cast<T>();
                }
                else
                {
                    Console.WriteLine($"WorkingDataFetcher doesn't support {typeof(T).Name} entities");
                    return Enumerable.Empty<T>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"WorkingDataFetcher fallback failed: {ex.Message}");
                return Enumerable.Empty<T>();
            }
        }

        private async Task<T?> GetEntityViaWorkingFetcher<T>(string partitionKey, string rowKey) where T : class, ITableEntity, new()
        {
            try
            {
                var entities = await GetEntitiesViaWorkingFetcher<T>();
                return entities.FirstOrDefault(e => e.PartitionKey == partitionKey && e.RowKey == rowKey);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"WorkingDataFetcher GetEntity fallback failed: {ex.Message}");
                return null;
            }
        }
    }
}
