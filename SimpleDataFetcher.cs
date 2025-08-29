using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic; // Added for List
using System.Linq; // Added for Take

namespace SimpleDataFetcher
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("🔍 Simple Azure Table Storage Data Fetcher");
            Console.WriteLine("==========================================");
            
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Accept", "application/json;odata=minimalmetadata");
            httpClient.DefaultRequestHeaders.Add("User-Agent", "SimpleFetcher/1.0");
            
            // Test Customers
            Console.WriteLine("\n📊 Fetching Customers...");
            var customersUrl = "https://abcretailstoragevuyo.table.core.windows.net/Customers?sv=2024-11-04&ss=bfqt&srt=so&sp=rwdlacupiytfx&se=2025-08-29T04:04:35Z&st=2025-08-28T19:49:35Z&spr=https&sig=H1kGzZT9hliQpPFsA6Sz0meKDtQNynBTx7M2e5DyEZw%3D";
            
            try
            {
                var customersResponse = await httpClient.GetStringAsync(customersUrl);
                var customersData = JsonSerializer.Deserialize<AzureTableResponse<Customer>>(customersResponse);
                
                if (customersData?.Value != null)
                {
                    Console.WriteLine($"✅ Found {customersData.Value.Count} customers!");
                    foreach (var customer in customersData.Value.Take(5))
                    {
                        Console.WriteLine($"   📋 {customer.FullName} - {customer.Email}");
                    }
                }
                else
                {
                    Console.WriteLine("❌ No customers data found");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error fetching customers: {ex.Message}");
            }
            
            // Test Products
            Console.WriteLine("\n📦 Fetching Products...");
            var productsUrl = "https://abcretailstoragevuyo.table.core.windows.net/Products?sv=2024-11-04&ss=bfqt&srt=so&sp=rwdlacupiytfx&se=2025-08-29T04:04:35Z&st=2025-08-28T19:49:35Z&spr=https&sig=H1kGzZT9hliQpPFsA6Sz0meKDtQNynBTx7M2e5DyEZw%3D";
            
            try
            {
                var productsResponse = await httpClient.GetStringAsync(productsUrl);
                var productsData = JsonSerializer.Deserialize<AzureTableResponse<Product>>(productsResponse);
                
                if (productsData?.Value != null)
                {
                    Console.WriteLine($"✅ Found {productsData.Value.Count} products!");
                    foreach (var product in productsData.Value.Take(5))
                    {
                        Console.WriteLine($"   📦 {product.Name} - ${product.Price} ({product.Category})");
                    }
                }
                else
                {
                    Console.WriteLine("❌ No products data found");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error fetching products: {ex.Message}");
            }
            
            Console.WriteLine("\n🏁 Data fetching completed!");
        }
    }
    
    // Simple models that match the Azure Table response
    public class Customer
    {
        public string PartitionKey { get; set; } = "";
        public string RowKey { get; set; } = "";
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Address { get; set; } = "";
        public string City { get; set; } = "";
        public string State { get; set; } = "";
        public string ZipCode { get; set; } = "";
        public string FullName { get; set; } = "";
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
    }
    
    public class Product
    {
        public string PartitionKey { get; set; } = "";
        public string RowKey { get; set; } = "";
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public string Category { get; set; } = "";
        public string Brand { get; set; } = "";
        public string ImageUrl { get; set; } = "";
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
    }
    
    public class AzureTableResponse<T>
    {
        public string? OdataMetadata { get; set; }
        public List<T> Value { get; set; } = new List<T>();
    }
}


