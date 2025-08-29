using Azure.Data.Tables;

namespace ABCRetail.Services;

public interface IAzureTableService
{
    Task<T?> GetEntityAsync<T>(string partitionKey, string rowKey) where T : class, ITableEntity, new();
    Task<IEnumerable<T>> GetEntitiesAsync<T>(string partitionKey) where T : class, ITableEntity, new();
    Task<IEnumerable<T>> GetAllEntitiesAsync<T>() where T : class, ITableEntity, new();
    Task<bool> CreateEntityAsync<T>(T entity) where T : class, ITableEntity;
    Task<bool> UpdateEntityAsync<T>(T entity) where T : class, ITableEntity;
    Task<bool> DeleteEntityAsync<T>(string partitionKey, string rowKey) where T : class, ITableEntity, new();
    Task<bool> EntityExistsAsync<T>(string partitionKey, string rowKey) where T : class, ITableEntity, new();
    Task<List<string>> ListTablesAsync();
}
