using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using ABCRetail.Models;
using ABCRetail.Services;

namespace ABCRetail.Services
{
    public class WorkingDataFetcher
    {
        private readonly HttpClient _httpClient;
        private readonly string _customersUrl;
        private readonly string _productsUrl;

        public WorkingDataFetcher(IConfiguration configuration)
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json;odata=minimalmetadata");
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "WorkingFetcher/1.0");
            
            // Use URLs from configuration
            _customersUrl = configuration["AzureStorage:CustomersTableSasUrl"];
            _productsUrl = configuration["AzureStorage:ProductsTableSasUrl"];
            
            Console.WriteLine($"🔧 WorkingDataFetcher: Customers URL configured: {_customersUrl}");
            Console.WriteLine($"🔧 WorkingDataFetcher: Products URL configured: {_productsUrl}");
        }

        public async Task<List<Customer>> GetCustomersAsync()
        {
            try
            {
                Console.WriteLine("🔍 WorkingDataFetcher: Fetching customers directly from Azure...");
                var response = await _httpClient.GetStringAsync(_customersUrl);
                Console.WriteLine($"✅ WorkingDataFetcher: Got response of {response.Length} characters");
                
                // Save raw response to file for inspection
                await File.WriteAllTextAsync("customers_raw.json", response);
                Console.WriteLine("💾 Raw customer JSON saved to customers_raw.json");
                
                // Debug: Show the first 500 characters of the response
                Console.WriteLine($"🔍 First 500 chars of response: {response.Substring(0, Math.Min(500, response.Length))}");
                
                // Debug: Try to parse as JsonDocument first to see the structure
                try
                {
                    var jsonDoc = JsonDocument.Parse(response);
                    Console.WriteLine($"🔍 JSON root properties: {string.Join(", ", jsonDoc.RootElement.EnumerateObject().Select(p => p.Name))}");
                    
                    if (jsonDoc.RootElement.TryGetProperty("value", out var valueProp))
                    {
                        Console.WriteLine($"🔍 'value' property type: {valueProp.ValueKind}");
                        if (valueProp.ValueKind == JsonValueKind.Array)
                        {
                            Console.WriteLine($"🔍 'value' array count: {valueProp.GetArrayLength()}");
                        }
                    }
                }
                catch (Exception jsonEx)
                {
                    Console.WriteLine($"🔍 JSON parsing failed: {jsonEx.Message}");
                }
                
                var azureResponse = JsonSerializer.Deserialize<AzureTableResponse<Customer>>(response);
                
                if (azureResponse?.Value != null)
                {
                    Console.WriteLine($"🎉 WorkingDataFetcher: Successfully retrieved {azureResponse.Value.Count} customers!");
                    return azureResponse.Value;
                }
                else
                {
                    Console.WriteLine("⚠️ WorkingDataFetcher: No customers found in response");
                    Console.WriteLine($"🔍 azureResponse is null: {azureResponse == null}");
                    if (azureResponse != null)
                    {
                        Console.WriteLine($"🔍 azureResponse.Value is null: {azureResponse.Value == null}");
                    }
                    return new List<Customer>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ WorkingDataFetcher: Error fetching customers: {ex.Message}");
                Console.WriteLine($"🔍 Stack trace: {ex.StackTrace}");
                return new List<Customer>();
            }
        }

        public async Task<List<Product>> GetProductsAsync()
        {
            try
            {
                Console.WriteLine("🔍 WorkingDataFetcher: Fetching products directly from Azure...");
                var response = await _httpClient.GetStringAsync(_productsUrl);
                Console.WriteLine($"✅ WorkingDataFetcher: Got response of {response.Length} characters");
                
                // Save raw response to file for inspection
                await File.WriteAllTextAsync("products_raw.json", response);
                Console.WriteLine("💾 Raw product JSON saved to products_raw.json");
                
                // Debug: Show the first 500 characters of the response
                Console.WriteLine($"🔍 First 500 chars of response: {response.Substring(0, Math.Min(500, response.Length))}");
                
                // Debug: Try to parse as JsonDocument first to see the structure
                try
                {
                    var jsonDoc = JsonDocument.Parse(response);
                    Console.WriteLine($"🔍 JSON root properties: {string.Join(", ", jsonDoc.RootElement.EnumerateObject().Select(p => p.Name))}");
                    
                    if (jsonDoc.RootElement.TryGetProperty("value", out var valueProp))
                    {
                        Console.WriteLine($"🔍 'value' property type: {valueProp.ValueKind}");
                        if (valueProp.ValueKind == JsonValueKind.Array)
                        {
                            Console.WriteLine($"🔍 'value' array count: {valueProp.GetArrayLength()}");
                        }
                    }
                }
                catch (Exception jsonEx)
                {
                    Console.WriteLine($"🔍 JSON parsing failed: {jsonEx.Message}");
                }
                
                var azureResponse = JsonSerializer.Deserialize<AzureTableResponse<Product>>(response);
                
                if (azureResponse?.Value != null)
                {
                    Console.WriteLine($"🎉 WorkingDataFetcher: Successfully retrieved {azureResponse.Value.Count} products!");
                    return azureResponse.Value;
                }
                else
                {
                    Console.WriteLine("⚠️ WorkingDataFetcher: No products found in response");
                    Console.WriteLine($"🔍 azureResponse is null: {azureResponse == null}");
                    if (azureResponse != null)
                    {
                        Console.WriteLine($"🔍 azureResponse.Value is null: {azureResponse.Value == null}");
                    }
                    return new List<Product>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ WorkingDataFetcher: Error fetching products: {ex.Message}");
                Console.WriteLine($"🔍 Stack trace: {ex.StackTrace}");
                return new List<Product>();
            }
        }

        public async Task<Product?> GetProductAsync(string rowKey)
        {
            try
            {
                Console.WriteLine($"🔍 WorkingDataFetcher: Fetching product with RowKey: {rowKey}");
                
                // Get all products and find the one with matching RowKey
                var products = await GetProductsAsync();
                var product = products.FirstOrDefault(p => p.RowKey == rowKey);
                
                if (product != null)
                {
                    Console.WriteLine($"✅ WorkingDataFetcher: Found product: {product.Name}");
                    return product;
                }
                else
                {
                    Console.WriteLine($"⚠️ WorkingDataFetcher: Product with RowKey {rowKey} not found");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ WorkingDataFetcher: Error fetching product {rowKey}: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> UpdateProductAsync(Product product)
        {
            try
            {
                Console.WriteLine($"🔧 WorkingDataFetcher: Updating product {product.Name} (RowKey: {product.RowKey})");
                
                // Update the LastModifiedDate
                product.LastModifiedDate = DateTime.UtcNow;
                
                // Azure Table Storage expects OData format, not plain JSON
                // Based on PowerShell testing, we need to use PUT with proper headers
                var odataJson = JsonSerializer.Serialize(new
                {
                    // Only include the fields that need to be updated
                    Name = product.Name,
                    Description = product.Description,
                    Price = product.Price,
                    StockQuantity = product.StockQuantity,
                    Category = product.Category,
                    ImageUrl = product.ImageUrl,
                    LastModifiedDate = product.LastModifiedDate,
                    IsActive = product.IsActive
                }, new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                });
                
                Console.WriteLine($"📝 WorkingDataFetcher: OData update payload: {odataJson}");
                
                // Create the content for the HTTP request
                var content = new StringContent(odataJson, System.Text.Encoding.UTF8, "application/json");
                
                // Add required Azure Table Storage headers (based on PowerShell testing)
                _httpClient.DefaultRequestHeaders.Remove("x-ms-version");
                _httpClient.DefaultRequestHeaders.Add("x-ms-version", "2020-04-08");
                _httpClient.DefaultRequestHeaders.Remove("DataServiceVersion");
                _httpClient.DefaultRequestHeaders.Add("DataServiceVersion", "3.0");
                
                // Note: SAS token is already in the URL, no need for Authorization header
                // PowerShell test confirmed this approach works
                Console.WriteLine($"🔐 WorkingDataFetcher: Using URL-based SAS authentication (PowerShell confirmed)");
                
                // Use PUT method as confirmed working in PowerShell
                // URL format: Products(PartitionKey='Product',RowKey='{id}')
                var updateUrl = $"{_productsUrl}(PartitionKey='Product',RowKey='{product.RowKey}')";
                Console.WriteLine($"🔗 WorkingDataFetcher: Update URL: {updateUrl}");
                
                var response = await _httpClient.PutAsync(updateUrl, content);
                
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"✅ WorkingDataFetcher: Successfully updated product {product.Name}");
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    var errorMessage = $"❌ WorkingDataFetcher: Failed to update product. Status: {response.StatusCode}, Error: {errorContent}";
                    Console.WriteLine(errorMessage);
                    throw new Exception(errorMessage);
                }
            }
            catch (Exception ex)
            {
                var detailedError = $"❌ WorkingDataFetcher: Error updating product {product.Name}: {ex.Message}";
                if (ex.InnerException != null)
                {
                    detailedError += $"\nInner Exception: {ex.InnerException.Message}";
                }
                detailedError += $"\nStack Trace: {ex.StackTrace}";
                Console.WriteLine(detailedError);
                throw new Exception(detailedError, ex);
            }
        }
        
        public async Task<Customer?> GetCustomerAsync(string rowKey)
        {
            try
            {
                Console.WriteLine($"🔍 WorkingDataFetcher: Fetching customer with RowKey: {rowKey}");
                
                // Get all customers and find the one with matching RowKey
                var customers = await GetCustomersAsync();
                var customer = customers.FirstOrDefault(c => c.RowKey == rowKey);
                
                if (customer != null)
                {
                    Console.WriteLine($"✅ WorkingDataFetcher: Found customer: {customer.FullName}");
                    return customer;
                }
                else
                {
                    Console.WriteLine($"⚠️ WorkingDataFetcher: Customer with RowKey {rowKey} not found");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ WorkingDataFetcher: Error fetching customer {rowKey}: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> UpdateCustomerAsync(Customer customer)
        {
            try
            {
                Console.WriteLine($"🔧 WorkingDataFetcher: Updating customer {customer.FullName} (RowKey: {customer.RowKey})");
                
                // Update the LastModifiedDate if it exists
                if (customer.GetType().GetProperty("LastModifiedDate") != null)
                {
                    customer.GetType().GetProperty("LastModifiedDate")?.SetValue(customer, DateTime.UtcNow);
                }
                
                // Azure Table Storage expects OData format, not plain JSON
                // We need to use MERGE operation for updates
                var odataJson = JsonSerializer.Serialize(new
                {
                    // Only include the fields that need to be updated
                    FirstName = customer.FirstName,
                    LastName = customer.LastName,
                    Email = customer.Email,
                    Phone = customer.Phone,
                    Address = customer.Address,
                    City = customer.City,
                    State = customer.State,
                    ZipCode = customer.ZipCode,
                    IsActive = customer.IsActive,
                    FullName = customer.FullName
                }, new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                });
                
                Console.WriteLine($"📝 WorkingDataFetcher: OData update payload: {odataJson}");
                
                // Create the content for the HTTP request
                var content = new StringContent(odataJson, System.Text.Encoding.UTF8, "application/json");
                
                // Add required Azure Table Storage headers
                _httpClient.DefaultRequestHeaders.Remove("x-ms-version");
                _httpClient.DefaultRequestHeaders.Add("x-ms-version", "2020-04-08");
                _httpClient.DefaultRequestHeaders.Remove("DataServiceVersion");
                _httpClient.DefaultRequestHeaders.Add("DataServiceVersion", "3.0");
                
                // Note: SAS token is already in the URL, no need for Authorization header
                // PowerShell test confirmed this approach works
                Console.WriteLine($"🔐 WorkingDataFetcher: Using URL-based SAS authentication (PowerShell confirmed)");
                
                // Use PUT method as confirmed working in PowerShell
                // URL format: Customers(PartitionKey='Customer',RowKey='{id}')
                var updateUrl = $"{_customersUrl}(PartitionKey='Customer',RowKey='{customer.RowKey}')";
                Console.WriteLine($"🔗 WorkingDataFetcher: Update URL: {updateUrl}");
                
                var response = await _httpClient.PutAsync(updateUrl, content);
                
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"✅ WorkingDataFetcher: Successfully updated customer {customer.FullName}");
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    var errorMessage = $"❌ WorkingDataFetcher: Failed to update customer. Status: {response.StatusCode}, Error: {errorContent}";
                    Console.WriteLine(errorMessage);
                    throw new Exception(errorMessage);
                }
            }
            catch (Exception ex)
            {
                var detailedError = $"❌ WorkingDataFetcher: Error updating customer {customer.FullName}: {ex.Message}";
                if (ex.InnerException != null)
                {
                    detailedError += $"\nInner Exception: {ex.InnerException.Message}";
                }
                detailedError += $"\nStack Trace: {ex.StackTrace}";
                Console.WriteLine(detailedError);
                throw new Exception(detailedError, ex);
            }
        }
        
        public async Task<string> TestConnectionAsync()
        {
            try
            {
                Console.WriteLine("🔍 WorkingDataFetcher: Testing Azure connection...");
                
                var customersResponse = await _httpClient.GetStringAsync(_customersUrl);
                var productsResponse = await _httpClient.GetStringAsync(_productsUrl);
                
                return $"✅ Connection successful! Customers: {customersResponse.Length} chars, Products: {productsResponse.Length} chars";
            }
            catch (Exception ex)
            {
                return $"❌ Connection failed: {ex.Message}";
            }
        }
        
    }
}
