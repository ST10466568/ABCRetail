using ABCRetail.Models;

namespace ABCRetail.Services
{
    public interface IAzureTableServiceV2
    {
        // Product operations
        Task<List<Product>> GetAllProductsAsync();
        Task<Product?> GetProductAsync(string id);
        Task<bool> UpdateProductAsync(Product product);
        Task<bool> CreateProductAsync(Product product);
        Task<bool> DeleteProductAsync(string id);

        // Customer operations
        Task<List<Customer>> GetAllCustomersAsync();
        Task<Customer?> GetCustomerAsync(string id);
        Task<bool> UpdateCustomerAsync(Customer customer);
        Task<bool> CreateCustomerAsync(Customer customer);
        Task<bool> DeleteCustomerAsync(string id);

        // Table operations
        Task<List<string>> ListTablesAsync();
        
        // Recovery and diagnostic operations
        Task<Product?> RecoverMissingProductAsync(string productId);
        Task<string> DiagnoseProductStatusAsync(string productId);
        Task<Customer?> RecoverMissingCustomerAsync(string customerId);
        Task<string> DiagnoseCustomerStatusAsync(string customerId);
    }
}
