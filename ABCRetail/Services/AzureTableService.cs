using Azure;
using Azure.Data.Tables;
using ABCRetail.Models;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace ABCRetail.Services
{
    public class AzureTableService : IAzureTableService
    {
        private readonly TableServiceClient _tableServiceClient;
        private readonly WorkingDataFetcher _workingDataFetcher;
        private readonly IConfiguration _configuration;

        public AzureTableService(IConfiguration configuration)
        {
            _configuration = configuration;
            var connectionString = _configuration["AzureStorage:ConnectionString"];
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
                Console.WriteLine($"❌ Azure SDK ListTablesAsync failed: {ex.Message}");
                Console.WriteLine("🔄 Falling back to WorkingDataFetcher...");
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
                var tableClient = _tableServiceClient.GetTableClient(tableName);
                
                var entities = new List<T>();
                await foreach (var entity in tableClient.QueryAsync<T>())
                {
                    entities.Add(entity);
                }
                
                if (entities.Any())
                {
                    Console.WriteLine($"✅ Azure SDK successfully retrieved {entities.Count} {typeof(T).Name} entities");
                    return entities;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Azure SDK GetAllEntitiesAsync failed: {ex.Message}");
            }

            // Fallback to WorkingDataFetcher
            Console.WriteLine("🔄 Falling back to WorkingDataFetcher for GetAllEntitiesAsync...");
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
                    Console.WriteLine($"✅ Azure SDK successfully retrieved {typeof(T).Name} entity");
                    return entity.Value;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Azure SDK GetEntityAsync failed: {ex.Message}");
            }

            // Fallback to WorkingDataFetcher
            Console.WriteLine("🔄 Falling back to WorkingDataFetcher for GetEntityAsync...");
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
                    Console.WriteLine($"✅ Azure SDK successfully retrieved {entities.Count} {typeof(T).Name} entities with PartitionKey '{partitionKey}'");
                    return entities;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Azure SDK GetEntitiesAsync failed: {ex.Message}");
            }

            // Fallback to WorkingDataFetcher
            Console.WriteLine("🔄 Falling back to WorkingDataFetcher for GetEntitiesAsync...");
            var allEntities = await GetEntitiesViaWorkingFetcher<T>();
            return allEntities.Where(e => e.PartitionKey == partitionKey);
        }

        public async Task<bool> CreateEntityAsync<T>(T entity) where T : class, ITableEntity
        {
            try
            {
                // Try Azure SDK first
                var tableName = GetTableName<T>();
                var tableClient = _tableServiceClient.GetTableClient(tableName);
                await tableClient.AddEntityAsync(entity);
                Console.WriteLine($"✅ Azure SDK successfully created {typeof(T).Name} entity");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Azure SDK CreateEntityAsync failed: {ex.Message}");
                Console.WriteLine("🔄 Note: WorkingDataFetcher is read-only, cannot create entities");
                return false;
            }
        }

        public async Task<bool> UpdateEntityAsync<T>(T entity) where T : class, ITableEntity
        {
            try
            {
                // Try Azure SDK first
                var tableName = GetTableName<T>();
                var tableClient = _tableServiceClient.GetTableClient(tableName);
                await tableClient.UpdateEntityAsync(entity, ETag.All, TableUpdateMode.Replace);
                Console.WriteLine($"✅ Azure SDK successfully updated {typeof(T).Name} entity");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Azure SDK UpdateEntityAsync failed: {ex.Message}");
                Console.WriteLine("🔄 Note: WorkingDataFetcher is read-only, cannot update entities");
                return false;
            }
        }

        public async Task<bool> DeleteEntityAsync<T>(string partitionKey, string rowKey) where T : class, ITableEntity, new()
        {
            try
            {
                // Try Azure SDK first
                var tableName = GetTableName<T>();
                var tableClient = _tableServiceClient.GetTableClient(tableName);
                await tableClient.DeleteEntityAsync(partitionKey, rowKey);
                Console.WriteLine($"✅ Azure SDK successfully deleted {typeof(T).Name} entity");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Azure SDK DeleteEntityAsync failed: {ex.Message}");
                Console.WriteLine("🔄 Note: WorkingDataFetcher is read-only, cannot delete entities");
                return false;
            }
        }

        public async Task<bool> EntityExistsAsync<T>(string partitionKey, string rowKey) where T : class, ITableEntity, new()
        {
            try
            {
                // Try Azure SDK first
                var tableName = GetTableName<T>();
                var tableClient = _tableServiceClient.GetTableClient(tableName);
                var sdkEntity = await tableClient.GetEntityAsync<T>(partitionKey, rowKey);
                return sdkEntity.HasValue;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Azure SDK EntityExistsAsync failed: {ex.Message}");
            }

            // Fallback to WorkingDataFetcher
            Console.WriteLine("🔄 Falling back to WorkingDataFetcher for EntityExistsAsync...");
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
                    Console.WriteLine($"⚠️ WorkingDataFetcher doesn't support {typeof(T).Name} entities");
                    return Enumerable.Empty<T>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ WorkingDataFetcher fallback failed: {ex.Message}");
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
                Console.WriteLine($"❌ WorkingDataFetcher GetEntity fallback failed: {ex.Message}");
                return null;
            }
        }
    }
}
