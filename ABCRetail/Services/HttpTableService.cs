using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using ABCRetail.Models;

namespace ABCRetail.Services;

public class HttpTableService
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;

    public HttpTableService(IConfiguration configuration)
    {
        _configuration = configuration;
        _httpClient = new HttpClient();
        
        // Set headers to match Postman behavior
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json;odata=minimalmetadata");
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "PostmanRuntime/7.32.3");
        _httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
        _httpClient.DefaultRequestHeaders.Add("Connection", "keep-alive");
    }

    public async Task<List<Customer>> GetCustomersAsync()
    {
        try
        {
            var customersUrl = _configuration["AzureStorage:CustomersTableSasUrl"];
            if (string.IsNullOrEmpty(customersUrl))
            {
                Console.WriteLine("❌ No Customers table SAS URL found in configuration");
                return new List<Customer>();
            }

            Console.WriteLine($"🔍 HTTP GetCustomersAsync: Using URL: {customersUrl}");
            Console.WriteLine($"🔍 HTTP Headers: Accept={_httpClient.DefaultRequestHeaders.Accept}, User-Agent={_httpClient.DefaultRequestHeaders.UserAgent}");
            
            var response = await _httpClient.GetAsync(customersUrl);
            Console.WriteLine($"🔍 HTTP Response Status: {response.StatusCode}");
            Console.WriteLine($"🔍 HTTP Response Headers: {string.Join(", ", response.Headers.Select(h => $"{h.Key}={string.Join(";", h.Value)}"))}");
            
            if (response.IsSuccessStatusCode)
            {
                var jsonContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"✅ HTTP response successful: {response.StatusCode}");
                Console.WriteLine($"📄 Response content length: {jsonContent.Length} characters");
                Console.WriteLine($"🔍 First 500 chars of response: {jsonContent.Substring(0, Math.Min(500, jsonContent.Length))}");
                
                // Parse the OData response
                var tableResponse = JsonSerializer.Deserialize<AzureTableResponse<Customer>>(jsonContent);
                if (tableResponse?.Value != null)
                {
                    Console.WriteLine($"🎉 Successfully retrieved {tableResponse.Value.Count} customers via HTTP!");
                    foreach (var customer in tableResponse.Value.Take(3)) // Log first 3
                    {
                        Console.WriteLine($"   📋 Customer: {customer.FullName} (PK: {customer.PartitionKey}, RK: {customer.RowKey})");
                    }
                    return tableResponse.Value;
                }
                else
                {
                    Console.WriteLine("⚠️ Response deserialized but no Value property found");
                    Console.WriteLine($"🔍 Raw JSON: {jsonContent.Substring(0, Math.Min(500, jsonContent.Length))}...");
                    
                    // Try to parse as raw JSON to see what we're getting
                    try
                    {
                        var rawJson = JsonDocument.Parse(jsonContent);
                        Console.WriteLine($"🔍 JSON Root Properties: {string.Join(", ", rawJson.RootElement.EnumerateObject().Select(p => p.Name))}");
                    }
                    catch (Exception jsonEx)
                    {
                        Console.WriteLine($"❌ JSON parsing error: {jsonEx.Message}");
                    }
                }
            }
            else
            {
                Console.WriteLine($"❌ HTTP request failed: {response.StatusCode} - {response.ReasonPhrase}");
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"🔍 Error content: {errorContent}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ HTTP GetCustomersAsync error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }

        return new List<Customer>();
    }

    public async Task<List<Product>> GetProductsAsync()
    {
        try
        {
            var productsUrl = _configuration["AzureStorage:ProductsTableSasUrl"];
            if (string.IsNullOrEmpty(productsUrl))
            {
                Console.WriteLine("❌ No Products table SAS URL found in configuration");
                return new List<Product>();
            }

            Console.WriteLine($"🔍 HTTP GetProductsAsync: Using URL: {productsUrl}");
            Console.WriteLine($"🔍 HTTP Headers: Accept={_httpClient.DefaultRequestHeaders.Accept}, User-Agent={_httpClient.DefaultRequestHeaders.UserAgent}");
            
            var response = await _httpClient.GetAsync(productsUrl);
            Console.WriteLine($"🔍 HTTP Response Status: {response.StatusCode}");
            Console.WriteLine($"🔍 HTTP Response Headers: {string.Join(", ", response.Headers.Select(h => $"{h.Key}={string.Join(";", h.Value)}"))}");
            
            if (response.IsSuccessStatusCode)
            {
                var jsonContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"✅ HTTP response successful: {response.StatusCode}");
                Console.WriteLine($"📄 Response content length: {jsonContent.Length} characters");
                Console.WriteLine($"🔍 First 500 chars of response: {jsonContent.Substring(0, Math.Min(500, jsonContent.Length))}");
                
                // Parse the OData response
                var tableResponse = JsonSerializer.Deserialize<AzureTableResponse<Product>>(jsonContent);
                if (tableResponse?.Value != null)
                {
                    Console.WriteLine($"🎉 Successfully retrieved {tableResponse.Value.Count} products via HTTP!");
                    foreach (var product in tableResponse.Value.Take(3)) // Log first 3
                    {
                        Console.WriteLine($"   📦 Product: {product.Name} (PK: {product.PartitionKey}, RK: {product.RowKey})");
                    }
                    return tableResponse.Value;
                }
                else
                {
                    Console.WriteLine("⚠️ Response deserialized but no Value property found");
                    Console.WriteLine($"🔍 Raw JSON: {jsonContent.Substring(0, Math.Min(500, jsonContent.Length))}...");
                    
                    // Try to parse as raw JSON to see what we're getting
                    try
                    {
                        var rawJson = JsonDocument.Parse(jsonContent);
                        Console.WriteLine($"🔍 JSON Root Properties: {string.Join(", ", rawJson.RootElement.EnumerateObject().Select(p => p.Name))}");
                    }
                    catch (Exception jsonEx)
                    {
                        Console.WriteLine($"❌ JSON parsing error: {jsonEx.Message}");
                    }
                }
            }
            else
            {
                Console.WriteLine($"❌ HTTP request failed: {response.StatusCode} - {response.ReasonPhrase}");
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"🔍 Error content: {errorContent}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ HTTP GetProductsAsync error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }

        return new List<Product>();
    }

    public async Task<int> GetCustomerCountAsync()
    {
        var customers = await GetCustomersAsync();
        return customers.Count;
    }

    public async Task<int> GetProductCountAsync()
    {
        var products = await GetProductsAsync();
        return products.Count;
    }
    
    public string GetConfigurationInfo()
    {
        var customersUrl = _configuration["AzureStorage:CustomersTableSasUrl"];
        var productsUrl = _configuration["AzureStorage:ProductsTableSasUrl"];
        
        var info = $"Configuration Info:\n";
        info += $"Customers URL: {customersUrl}\n";
        info += $"Products URL: {productsUrl}\n";
        info += $"Customers URL Length: {customersUrl?.Length ?? 0}\n";
        info += $"Products URL Length: {productsUrl?.Length ?? 0}\n";
        info += $"Customers URL Contains 'Customers': {customersUrl?.Contains("Customers") ?? false}\n";
        info += $"Products URL Contains 'Products': {productsUrl?.Contains("Products") ?? false}\n";
        info += $"Customers URL Contains 'sv=': {customersUrl?.Contains("sv=") ?? false}\n";
        info += $"Products URL Contains 'sv=': {productsUrl?.Contains("sv=") ?? false}\n";
        
        return info;
    }
    
    public async Task<string> TestUrlDirectlyAsync(string url)
    {
        try
        {
            Console.WriteLine($"🔍 Testing URL directly: {url}");
            Console.WriteLine($"🔍 URL Length: {url.Length}");
            Console.WriteLine($"🔍 URL Contains 'Products': {url.Contains("Products")}");
            Console.WriteLine($"🔍 URL Contains 'Customers': {url.Contains("Customers")}");
            Console.WriteLine($"🔍 URL Contains 'sv=': {url.Contains("sv=")}");
            Console.WriteLine($"🔍 URL Contains 'sig=': {url.Contains("sig=")}");
            
            // Create a new HttpClient for this test
            using var testClient = new HttpClient();
            testClient.DefaultRequestHeaders.Add("Accept", "application/json;odata=minimalmetadata");
            testClient.DefaultRequestHeaders.Add("User-Agent", "PostmanRuntime/7.32.3");
            
            Console.WriteLine($"🔍 Sending request with headers:");
            Console.WriteLine($"   Accept: {testClient.DefaultRequestHeaders.Accept}");
            Console.WriteLine($"   User-Agent: {testClient.DefaultRequestHeaders.UserAgent}");
            
            var response = await testClient.GetAsync(url);
            Console.WriteLine($"🔍 Direct test response: {response.StatusCode}");
            Console.WriteLine($"🔍 Response headers: {string.Join(", ", response.Headers.Select(h => $"{h.Key}={string.Join(";", h.Value)}"))}");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"🔍 Direct test content length: {content.Length}");
                Console.WriteLine($"🔍 Direct test first 200 chars: {content.Substring(0, Math.Min(200, content.Length))}");
                
                // Try to parse as JSON to see the structure
                try
                {
                    var jsonDoc = JsonDocument.Parse(content);
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
                
                return content;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"🔍 Direct test error: {response.StatusCode} - {errorContent}");
                return $"ERROR: {response.StatusCode} - {errorContent}";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Direct URL test error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return $"EXCEPTION: {ex.Message}";
        }
    }
}

// OData response structure for Azure Tables
public class AzureTableResponse<T>
{
    [JsonPropertyName("odata.metadata")]
    public string? OdataMetadata { get; set; }
    
    [JsonPropertyName("value")]
    public List<T> Value { get; set; } = new List<T>();
}
