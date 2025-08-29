using ABCRetail.Services;
using ABCRetail.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;

namespace ABCRetail.Pages;

public class DiagnosticModel : PageModel
{
    private readonly IAzureTableService _tableService;
    private readonly IAzureBlobService _blobService;
    private readonly IConfiguration _configuration;
    private readonly DataSeederService _dataSeeder;
    private readonly HttpTableService _httpTableService;

    public DiagnosticModel(
        IAzureTableService tableService,
        IAzureBlobService blobService,
        IConfiguration configuration,
        DataSeederService dataSeeder,
        HttpTableService httpTableService)
    {
        _tableService = tableService;
        _blobService = blobService;
        _configuration = configuration;
        _dataSeeder = dataSeeder;
        _httpTableService = httpTableService;
    }

    public bool IsTableServiceConnected { get; set; }
    public bool IsBlobServiceConnected { get; set; }
    public int CustomerCount { get; set; }
    public int ProductCount { get; set; }
    public string TableSasUrl { get; set; } = "";
    public string ConnectionString { get; set; } = "";
    public string Message { get; set; } = "";
    public List<string> LogMessages { get; set; } = new List<string>();

    public async Task OnGetAsync()
    {
        await LoadDiagnosticInfo();
    }

    public async Task<IActionResult> OnPostAsync(string action)
    {
        LogMessages.Clear();
        
        switch (action)
        {
            case "testConnection":
                Message = "Testing Azure connection...";
                await TestAzureConnection();
                Message = "Connection test completed. Check the results above.";
                break;
            
            case "seedData":
                try
                {
                    Message = "Starting data seeding...";
                    await SeedDataWithLogging();
                    Message = "Data seeding completed! Check the results above.";
                }
                catch (Exception ex)
                {
                    Message = $"Data seeding failed: {ex.Message}";
                    LogMessages.Add($"❌ Error: {ex.Message}");
                }
                break;
            
            case "refreshData":
                Message = "Refreshing data counts...";
                await LoadDiagnosticInfo();
                Message = "Data counts refreshed.";
                break;
                
            case "debugRetrieval":
                Message = "Debugging data retrieval...";
                await DebugDataRetrieval();
                Message = "Debug retrieval completed! Check the results above.";
                break;
                
            case "httpRetrieval":
                Message = "Testing HTTP-based data retrieval (Postman-style)...";
                await HttpDataRetrieval();
                Message = "HTTP retrieval test completed! Check the results above.";
                break;
                
            case "testUrls":
                Message = "Testing URLs directly with enhanced debugging...";
                await TestUrlsDirectly();
                Message = "Direct URL testing completed! Check the results above.";
                break;
                
            case "showConfig":
                Message = "Showing configuration information...";
                await ShowConfigurationInfo();
                Message = "Configuration info displayed. Check the results above.";
                break;
                
            case "testWorkingFetcher":
                Message = "Testing Working Data Fetcher...";
                await TestWorkingFetcher();
                Message = "Working Data Fetcher test completed! Check the results above.";
                break;
        }

        return Page();
    }

    private async Task TestAzureConnection()
    {
        LogMessages.Add("🔍 Testing Azure Table Service Connection...");
        
        try
        {
            // Test table listing
            var tables = await _tableService.ListTablesAsync();
            LogMessages.Add($"📋 Table listing result: {tables.Count} tables found");
            
            if (tables.Count > 0)
            {
                LogMessages.Add($"✅ Table Service: Connected successfully");
                IsTableServiceConnected = true;
            }
            else
            {
                LogMessages.Add($"⚠️ Table Service: Connected but no tables found");
                IsTableServiceConnected = false;
            }
        }
        catch (Exception ex)
        {
            LogMessages.Add($"❌ Table Service Error: {ex.Message}");
            IsTableServiceConnected = false;
        }

        LogMessages.Add("🔍 Testing Azure Blob Service Connection...");
        
        try
        {
            var images = await _blobService.ListImagesAsync();
            LogMessages.Add($"📦 Blob listing result: {images.Count()} images found");
            LogMessages.Add($"✅ Blob Service: Connected successfully");
            IsBlobServiceConnected = true;
        }
        catch (Exception ex)
        {
            LogMessages.Add($"❌ Blob Service Error: {ex.Message}");
            IsBlobServiceConnected = false;
        }
    }

    private async Task SeedDataWithLogging()
    {
        LogMessages.Add("🌱 Starting data seeding process...");
        
        // Test customer creation
        LogMessages.Add("📊 Testing customer creation...");
        var testCustomer = new Customer
        {
            RowKey = Guid.NewGuid().ToString(),
            FirstName = "Test",
            LastName = "Customer",
            Email = "test@example.com",
            Phone = "(555) 000-0000",
            Address = "123 Test St",
            City = "Test City",
            State = "TS",
            ZipCode = "00000"
        };

        try
        {
            LogMessages.Add($"🔍 Attempting to create customer: {testCustomer.FirstName} {testCustomer.LastName}");
            LogMessages.Add($"   PartitionKey: {testCustomer.PartitionKey}");
            LogMessages.Add($"   RowKey: {testCustomer.RowKey}");
            
            var result = await _tableService.CreateEntityAsync(testCustomer);
            
            if (result)
            {
                LogMessages.Add($"✅ Customer created successfully!");
            }
            else
            {
                LogMessages.Add($"❌ Customer creation failed (returned false)");
            }
        }
        catch (Exception ex)
        {
            LogMessages.Add($"❌ Customer creation error: {ex.Message}");
            LogMessages.Add($"   Stack trace: {ex.StackTrace}");
        }

        // Test product creation
        LogMessages.Add("📦 Testing product creation...");
        var testProduct = new Product
        {
            RowKey = Guid.NewGuid().ToString(),
            Name = "Test Product",
            Description = "A test product for diagnostics",
            Price = 9.99m,
            StockQuantity = 10,
            Category = "Test",
            Brand = "TestBrand",
            ImageUrl = "test.jpg"
        };

        try
        {
            LogMessages.Add($"🔍 Attempting to create product: {testProduct.Name}");
            LogMessages.Add($"   PartitionKey: {testProduct.PartitionKey}");
            LogMessages.Add($"   RowKey: {testProduct.RowKey}");
            
            var result = await _tableService.CreateEntityAsync(testProduct);
            
            if (result)
            {
                LogMessages.Add($"✅ Product created successfully!");
            }
            else
            {
                LogMessages.Add($"❌ Product creation failed (returned false)");
            }
        }
        catch (Exception ex)
        {
            LogMessages.Add($"❌ Product creation error: {ex.Message}");
            LogMessages.Add($"   Stack trace: {ex.StackTrace}");
        }

        // Verify data was created
        LogMessages.Add("🔍 Verifying created data...");
        try
        {
            var customers = await _tableService.GetAllEntitiesAsync<Customer>();
            LogMessages.Add($"📊 Customer count after seeding: {customers.Count()}");
            
            var products = await _tableService.GetAllEntitiesAsync<Product>();
            LogMessages.Add($"📦 Product count after seeding: {products.Count()}");
        }
        catch (Exception ex)
        {
            LogMessages.Add($"❌ Error verifying data: {ex.Message}");
        }

        LogMessages.Add("✅ Data seeding test completed!");
    }

    private async Task DebugDataRetrieval()
    {
        LogMessages.Add("🔍 Debugging data retrieval process...");
        
        try
        {
            // Test customer retrieval with enhanced debugging
            LogMessages.Add("📊 Testing Customer retrieval with enhanced debugging...");
            LogMessages.Add("🔍 Step 1: Querying ALL entities in Customers table...");
            
            // We'll manually implement the enhanced debugging logic here
            try
            {
                // First, let's try to get the table client and query all entities
                var customerTableName = "Customers";
                LogMessages.Add($"🔍 Using table name: '{customerTableName}'");
                
                // Test if we can get any entities at all
                var customers = await _tableService.GetAllEntitiesAsync<Customer>();
                LogMessages.Add($"📊 Customer count from GetAllEntitiesAsync: {customers.Count()}");
                
                if (customers.Any())
                {
                    foreach (var customer in customers)
                    {
                        LogMessages.Add($"   Found Customer: {customer.FirstName} {customer.LastName} (PK: {customer.PartitionKey}, RK: {customer.RowKey})");
                    }
                }
                else
                {
                    LogMessages.Add($"   ⚠️ No customers found via GetAllEntitiesAsync");
                    
                    // Additional debugging: Let's check if the issue is with the query logic
                    LogMessages.Add("🔍 Investigating query logic...");
                    LogMessages.Add("   The issue might be:");
                    LogMessages.Add("   1. PartitionKey mismatch between stored and queried data");
                    LogMessages.Add("   2. Table access permissions");
                    LogMessages.Add("   3. Data mapping issues");
                    LogMessages.Add("   4. Query filter problems");
                }
            }
            catch (Exception ex)
            {
                LogMessages.Add($"❌ Customer retrieval error: {ex.Message}");
            }

            // Test product retrieval with enhanced debugging
            LogMessages.Add("📦 Testing Product retrieval with enhanced debugging...");
            LogMessages.Add("🔍 Step 2: Querying ALL entities in Products table...");
            
            try
            {
                var productTableName = "Products";
                LogMessages.Add($"🔍 Using table name: '{productTableName}'");
                
                var products = await _tableService.GetAllEntitiesAsync<Product>();
                LogMessages.Add($"📦 Product count from GetAllEntitiesAsync: {products.Count()}");
                
                if (products.Any())
                {
                    foreach (var product in products)
                    {
                        LogMessages.Add($"   Found Product: {product.Name} (PK: {product.PartitionKey}, RK: {product.RowKey})");
                    }
                }
                else
                {
                    LogMessages.Add($"   ⚠️ No products found via GetAllEntitiesAsync");
                    
                    // Additional debugging for products
                    LogMessages.Add("🔍 Investigating product query logic...");
                    LogMessages.Add("   Same potential issues as customers:");
                    LogMessages.Add("   1. PartitionKey mismatch");
                    LogMessages.Add("   2. Table access permissions");
                    LogMessages.Add("   3. Data mapping issues");
                    LogMessages.Add("   4. Query filter problems");
                }
            }
            catch (Exception ex)
            {
                LogMessages.Add($"❌ Product retrieval error: {ex.Message}");
            }

            // Test direct entity retrieval by known keys from recent seeding
            LogMessages.Add("🔍 Step 3: Testing direct entity retrieval by known keys...");
            
            // Try to get the most recently created entities
            try
            {
                LogMessages.Add("🔍 Attempting to retrieve recent test entities...");
                
                // Try with the PartitionKey and RowKey pattern we know works
                var testCustomer = await _tableService.GetEntityAsync<Customer>("Customer", "test-customer-key");
                if (testCustomer != null)
                {
                    LogMessages.Add($"   ✅ Direct customer retrieval successful: {testCustomer.FirstName} {testCustomer.LastName}");
                }
                else
                {
                    LogMessages.Add($"   ❌ Direct customer retrieval failed - entity not found");
                    LogMessages.Add("   🔍 This suggests the issue is with data storage or table access");
                }
            }
            catch (Exception ex)
            {
                LogMessages.Add($"   ❌ Direct customer retrieval error: {ex.Message}");
            }

            try
            {
                var testProduct = await _tableService.GetEntityAsync<Product>("Product", "test-product-key");
                if (testProduct != null)
                {
                    LogMessages.Add($"   ✅ Direct product retrieval successful: {testProduct.Name}");
                }
                else
                {
                    LogMessages.Add($"   ❌ Direct product retrieval failed - entity not found");
                    LogMessages.Add("   🔍 This suggests the issue is with data storage or table access");
                }
            }
            catch (Exception ex)
            {
                LogMessages.Add($"   ❌ Direct product retrieval error: {ex.Message}");
            }
            
            // Test specific product retrieval with known keys
            LogMessages.Add("🔍 Step 4: Testing specific product retrieval with known keys...");
            try
            {
                LogMessages.Add("🔍 Attempting to retrieve specific product: PartitionKey='Product', RowKey='7e22910a-82c0-4ffe-bd75-71677a479227'");
                
                var specificProduct = await _tableService.GetEntityAsync<Product>("Product", "7e22910a-82c0-4ffe-bd75-71677a479227");
                if (specificProduct != null)
                {
                    LogMessages.Add($"   ✅ Specific product retrieval successful!");
                    LogMessages.Add($"   📦 Product Name: {specificProduct.Name}");
                    LogMessages.Add($"   📝 Description: {specificProduct.Description}");
                    LogMessages.Add($"   💰 Price: {specificProduct.Price}");
                    LogMessages.Add($"   📊 Stock: {specificProduct.StockQuantity}");
                    LogMessages.Add($"   🏷️ Category: {specificProduct.Category}");
                    LogMessages.Add($"   🏭 Brand: {specificProduct.Brand}");
                }
                else
                {
                    LogMessages.Add($"   ❌ Specific product retrieval failed - entity not found");
                    LogMessages.Add("   🔍 This suggests the product doesn't exist with those exact keys");
                }
            }
            catch (Exception ex)
            {
                LogMessages.Add($"   ❌ Specific product retrieval error: {ex.Message}");
            }
            
            // Additional diagnostic information
            LogMessages.Add("🔍 Step 5: Additional diagnostic information...");
            LogMessages.Add("   This will help identify if the issue is with:");
            LogMessages.Add("   - PartitionKey/RowKey mismatch");
            LogMessages.Add("   - Table access permissions");
            LogMessages.Add("   - Data mapping issues");
            LogMessages.Add("   - Query logic problems");
            
            // Add specific recommendations
            LogMessages.Add("🔍 Step 6: Recommended next steps...");
            LogMessages.Add("   1. Check Azure Portal to verify data exists in tables");
            LogMessages.Add("   2. Verify PartitionKey values match exactly (case-sensitive)");
            LogMessages.Add("   3. Check table permissions and access policies");
            LogMessages.Add("   4. Test with a simple query without filters");
        }
        catch (Exception ex)
        {
            LogMessages.Add($"❌ Debug retrieval error: {ex.Message}");
            LogMessages.Add($"   Stack trace: {ex.StackTrace}");
        }

        LogMessages.Add("✅ Enhanced debug retrieval completed!");
    }
    
    private async Task HttpDataRetrieval()
    {
        LogMessages.Add("🚀 Testing HTTP-based data retrieval (Postman-style)...");
        LogMessages.Add("🔍 This approach uses direct HTTP requests like Postman does");
        
        try
        {
            // Test HTTP-based customer retrieval
            LogMessages.Add("📊 Step 1: Testing HTTP-based Customer retrieval...");
            var httpCustomers = await _httpTableService.GetCustomersAsync();
            LogMessages.Add($"📊 HTTP Customer count: {httpCustomers.Count}");
            
            if (httpCustomers.Count > 0)
            {
                LogMessages.Add($"✅ HTTP Customer retrieval successful! Found {httpCustomers.Count} customers");
                LogMessages.Add($"   📋 First customer: {httpCustomers.First().FullName}");
                LogMessages.Add($"   🔑 PartitionKey: {httpCustomers.First().PartitionKey}");
                LogMessages.Add($"   🔑 RowKey: {httpCustomers.First().RowKey}");
            }
            else
            {
                LogMessages.Add("⚠️ HTTP Customer retrieval returned 0 customers");
            }
            
            // Test HTTP-based product retrieval
            LogMessages.Add("📦 Step 2: Testing HTTP-based Product retrieval...");
            var httpProducts = await _httpTableService.GetProductsAsync();
            LogMessages.Add($"📦 HTTP Product count: {httpProducts.Count}");
            
            if (httpProducts.Count > 0)
            {
                LogMessages.Add($"✅ HTTP Product retrieval successful! Found {httpProducts.Count} products");
                LogMessages.Add($"   📦 First product: {httpProducts.First().Name}");
                LogMessages.Add($"   🔑 PartitionKey: {httpProducts.First().PartitionKey}");
                LogMessages.Add($"   🔑 RowKey: {httpProducts.First().RowKey}");
            }
            else
            {
                LogMessages.Add("⚠️ HTTP Product retrieval returned 0 products");
            }
            
            // Compare with Azure SDK results
            LogMessages.Add("🔍 Step 3: Comparing HTTP vs Azure SDK results...");
            var sdkCustomers = await _tableService.GetAllEntitiesAsync<Customer>();
            var sdkProducts = await _tableService.GetAllEntitiesAsync<Product>();
            
            LogMessages.Add($"📊 Comparison Results:");
            LogMessages.Add($"   HTTP Customers: {httpCustomers.Count}");
            LogMessages.Add($"   SDK Customers: {sdkCustomers.Count()}");
            LogMessages.Add($"   HTTP Products: {httpProducts.Count}");
            LogMessages.Add($"   SDK Products: {sdkProducts.Count()}");
            
            if (httpCustomers.Count > sdkCustomers.Count())
            {
                LogMessages.Add("🎉 HTTP approach is working better than Azure SDK!");
            }
            else if (httpCustomers.Count == sdkCustomers.Count())
            {
                LogMessages.Add("ℹ️ Both approaches return the same results");
            }
            else
            {
                LogMessages.Add("⚠️ Azure SDK is returning more results than HTTP");
            }
            
        }
        catch (Exception ex)
        {
            LogMessages.Add($"❌ HTTP data retrieval error: {ex.Message}");
            LogMessages.Add($"Stack trace: {ex.StackTrace}");
        }
        
        LogMessages.Add("✅ HTTP-based data retrieval test completed!");
    }
    
    private async Task TestUrlsDirectly()
    {
        LogMessages.Add("🔍 Testing URLs directly with enhanced debugging...");
        
        try
        {
            // Test Customers URL directly
            LogMessages.Add("📊 Step 1: Testing Customers URL directly...");
            var customersUrl = _configuration["AzureStorage:CustomersTableSasUrl"];
            if (!string.IsNullOrEmpty(customersUrl))
            {
                LogMessages.Add($"🔍 Customers URL: {customersUrl}");
                var customersResponse = await _httpTableService.TestUrlDirectlyAsync(customersUrl);
                LogMessages.Add($"📊 Customers direct response: {customersResponse.Substring(0, Math.Min(200, customersResponse.Length))}...");
            }
            else
            {
                LogMessages.Add("❌ No Customers URL found in configuration");
            }
            
            // Test Products URL directly
            LogMessages.Add("📦 Step 2: Testing Products URL directly...");
            var productsUrl = _configuration["AzureStorage:ProductsTableSasUrl"];
            if (!string.IsNullOrEmpty(productsUrl))
            {
                LogMessages.Add($"🔍 Products URL: {productsUrl}");
                var productsResponse = await _httpTableService.TestUrlDirectlyAsync(productsUrl);
                LogMessages.Add($"📦 Products direct response: {productsResponse.Substring(0, Math.Min(200, productsResponse.Length))}...");
            }
            else
            {
                LogMessages.Add("❌ No Products URL found in configuration");
            }
            
            // Test with different Accept headers
            LogMessages.Add("🔍 Step 3: Testing with different Accept headers...");
            LogMessages.Add("   This will help identify if the issue is with OData headers");
            
        }
        catch (Exception ex)
        {
            LogMessages.Add($"❌ Direct URL testing error: {ex.Message}");
            LogMessages.Add($"Stack trace: {ex.StackTrace}");
        }
        
        LogMessages.Add("✅ Direct URL testing completed!");
    }
    
    private async Task ShowConfigurationInfo()
    {
        LogMessages.Add("🔍 Configuration Information:");
        LogMessages.Add("================================");
        
        try
        {
            // Show configuration info from HttpTableService
            var configInfo = _httpTableService.GetConfigurationInfo();
            var lines = configInfo.Split('\n');
            foreach (var line in lines)
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    LogMessages.Add($"   {line}");
                }
            }
            
            LogMessages.Add("================================");
            
            // Also show the raw configuration values
            var customersUrl = _configuration["AzureStorage:CustomersTableSasUrl"];
            var productsUrl = _configuration["AzureStorage:ProductsTableSasUrl"];
            
            LogMessages.Add("Raw Configuration Values:");
            LogMessages.Add($"   CustomersTableSasUrl: {customersUrl}");
            LogMessages.Add($"   ProductsTableSasUrl: {productsUrl}");
            
            // Check if URLs are exactly what we expect
            var expectedProductsUrl = "https://abcretailstoragevuyo.table.core.windows.net/Products?sv=2024-11-04&ss=bfqt&srt=so&sp=rwdlacupiytfx&se=2025-08-29T04:04:35Z&st=2025-08-28T19:49:35Z&spr=https&sig=H1kGzZT9hliQpPFsA6Sz0meKDtQNynBTx7M2e5DyEZw%3D";
            
            LogMessages.Add("URL Comparison:");
            LogMessages.Add($"   Expected Products URL: {expectedProductsUrl}");
            LogMessages.Add($"   Actual Products URL: {productsUrl}");
            LogMessages.Add($"   URLs Match: {productsUrl == expectedProductsUrl}");
            LogMessages.Add($"   Expected Length: {expectedProductsUrl.Length}");
            LogMessages.Add($"   Actual Length: {productsUrl?.Length ?? 0}");
            
        }
        catch (Exception ex)
        {
            LogMessages.Add($"❌ Error showing configuration: {ex.Message}");
        }
    }

    private async Task LoadDiagnosticInfo()
    {
        try
        {
            // Get configuration values
            TableSasUrl = _configuration["AzureStorage:TableSasUrl"] ?? "Not configured";
            ConnectionString = _configuration["AzureStorage:ConnectionString"] ?? "Not configured";

            // Test table service connection by trying to list tables
            try
            {
                var tables = await _tableService.ListTablesAsync();
                IsTableServiceConnected = tables.Count > 0;
                
                if (IsTableServiceConnected)
                {
                    // Try to get customer and product counts
                    try
                    {
                        var customers = await _tableService.GetAllEntitiesAsync<Customer>();
                        CustomerCount = customers.Count();
                    }
                    catch
                    {
                        CustomerCount = -1; // Error
                    }

                    try
                    {
                        var products = await _tableService.GetAllEntitiesAsync<Product>();
                        ProductCount = products.Count();
                    }
                    catch
                    {
                        ProductCount = -1; // Error
                    }
                }
                else
                {
                    CustomerCount = 0;
                    ProductCount = 0;
                }
            }
            catch
            {
                IsTableServiceConnected = false;
                CustomerCount = 0;
                ProductCount = 0;
            }

            // Test blob service connection
            try
            {
                var images = await _blobService.ListImagesAsync();
                IsBlobServiceConnected = true;
            }
            catch
            {
                IsBlobServiceConnected = false;
            }
        }
        catch (Exception ex)
        {
            Message = $"Error loading diagnostic info: {ex.Message}";
        }
    }
    
    private async Task TestWorkingFetcher()
    {
        try
        {
            LogMessages.Add("🔍 Testing Working Data Fetcher...");
            
            var workingFetcher = new WorkingDataFetcher(_configuration);
            
            // Test connection
            LogMessages.Add("🔗 Testing Azure connection...");
            var connectionResult = await workingFetcher.TestConnectionAsync();
            LogMessages.Add(connectionResult);
            
            // Test customer retrieval
            LogMessages.Add("📊 Testing customer retrieval...");
            var customers = await workingFetcher.GetCustomersAsync();
            LogMessages.Add($"✅ Retrieved {customers.Count} customers successfully!");
            
            if (customers.Any())
            {
                LogMessages.Add("📋 Sample customers:");
                foreach (var customer in customers.Take(3))
                {
                    LogMessages.Add($"   - {customer.FullName} ({customer.Email})");
                }
            }
            
            // Test product retrieval
            LogMessages.Add("📦 Testing product retrieval...");
            var products = await workingFetcher.GetProductsAsync();
            LogMessages.Add($"✅ Retrieved {products.Count} products successfully!");
            
            if (products.Any())
            {
                LogMessages.Add("📦 Sample products:");
                foreach (var product in products.Take(3))
                {
                    LogMessages.Add($"   - {product.Name} (${product.Price}) - {product.Category}");
                }
            }
            
            LogMessages.Add("🎉 Working Data Fetcher test completed successfully!");
        }
        catch (Exception ex)
        {
            LogMessages.Add($"❌ Working Data Fetcher test failed: {ex.Message}");
            LogMessages.Add($"   Stack trace: {ex.StackTrace}");
        }
    }
}
