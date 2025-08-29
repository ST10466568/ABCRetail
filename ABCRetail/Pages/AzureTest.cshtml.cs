using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using ABCRetail.Models;
using ABCRetail.Services;

namespace ABCRetail.Pages;

public class AzureTestModel : PageModel
{
    private readonly IConfiguration _configuration;
    private readonly IAzureTableService _tableService;
    private readonly IAzureBlobService _blobService;

    [BindProperty]
    public string ConnectionString { get; set; } = string.Empty;

    [BindProperty]
    public string TableSasToken { get; set; } = string.Empty;

    [BindProperty]
    public string BlobSasToken { get; set; } = string.Empty;

    [BindProperty]
    public bool TableServiceConnected { get; set; }

    [BindProperty]
    public bool BlobServiceConnected { get; set; }

    [BindProperty]
    public string TestResult { get; set; } = string.Empty;

    [BindProperty]
    public string ErrorMessage { get; set; } = string.Empty;

    public AzureTestModel(IConfiguration configuration, IAzureTableService tableService, IAzureBlobService blobService)
    {
        _configuration = configuration;
        _tableService = tableService;
        _blobService = blobService;
        
        // Load configuration values
        ConnectionString = _configuration["AzureStorage:ConnectionString"] ?? string.Empty;
        TableSasToken = _configuration["AzureStorage:TableSasToken"] ?? string.Empty;
        BlobSasToken = _configuration["AzureStorage:BlobSasToken"] ?? string.Empty;
    }

    public void OnGet()
    {
        // Test basic connectivity
        TestBasicConnectivity();
    }

    public async Task<IActionResult> OnPostTestTableConnectionAsync()
    {
        try
        {
            TestResult = "🧪 Testing Azure Table Storage Connection...\n\n";
            
            // Test 1: Check configuration
            TestResult += "1. Checking configuration...\n";
            if (!string.IsNullOrEmpty(ConnectionString))
            {
                TestResult += $"   ✅ Connection string found: {ConnectionString.Substring(0, Math.Min(50, ConnectionString.Length))}...\n";
            }
            else
            {
                TestResult += "   ❌ No connection string found\n";
            }

            if (!string.IsNullOrEmpty(TableSasToken))
            {
                TestResult += $"   ✅ Table SAS token found: {TableSasToken.Substring(0, Math.Min(30, TableSasToken.Length))}...\n";
            }
            else
            {
                TestResult += "   ❌ No table SAS token found\n";
            }

            // Test 2: Test service connectivity
            TestResult += "\n2. Testing service connectivity...\n";
            try
            {
                // Try to create a simple test entity
                var testCustomer = new Customer
                {
                    RowKey = Guid.NewGuid().ToString(),
                    FirstName = "Test",
                    LastName = "Connection",
                    Email = "test@connection.com",
                    Phone = "(555) 000-0000",
                    Address = "123 Test St",
                    City = "Test City",
                    State = "TS",
                    ZipCode = "00000"
                };

                var result = await _tableService.CreateEntityAsync(testCustomer);
                if (result)
                {
                    TestResult += "   ✅ Successfully created test customer!\n";
                    
                    // Try to retrieve it
                    var retrieved = await _tableService.GetEntityAsync<Customer>("Customer", testCustomer.RowKey);
                    if (retrieved != null)
                    {
                        TestResult += "   ✅ Successfully retrieved test customer!\n";
                        TestResult += $"   Name: {retrieved.FirstName} {retrieved.LastName}\n";
                        TestResult += $"   Email: {retrieved.Email}\n";
                    }
                    else
                    {
                        TestResult += "   ⚠️ Customer created but couldn't retrieve it\n";
                    }
                }
                else
                {
                    TestResult += "   ❌ Failed to create test customer\n";
                }
            }
            catch (Exception ex)
            {
                TestResult += $"   ❌ Service test failed: {ex.Message}\n";
            }

            TestResult += "\n✅ Table connection test completed!";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Table connection test failed: {ex.Message}\n\nStack trace:\n{ex.StackTrace}";
        }

        TestBasicConnectivity();
        return Page();
    }

    public async Task<IActionResult> OnPostTestBlobConnectionAsync()
    {
        try
        {
            TestResult = "🧪 Testing Azure Blob Storage Connection...\n\n";
            
            // Test 1: Check configuration
            TestResult += "1. Checking configuration...\n";
            var blobConnectionString = _configuration["AzureStorage:BlobConnectionString"];
            if (!string.IsNullOrEmpty(blobConnectionString))
            {
                TestResult += $"   ✅ Blob connection string found: {blobConnectionString.Substring(0, Math.Min(50, blobConnectionString.Length))}...\n";
            }
            else
            {
                TestResult += "   ❌ No blob connection string found\n";
            }

            if (!string.IsNullOrEmpty(BlobSasToken))
            {
                TestResult += $"   ✅ Blob SAS token found: {BlobSasToken.Substring(0, Math.Min(30, BlobSasToken.Length))}...\n";
            }
            else
            {
                TestResult += "   ❌ No blob SAS token found\n";
            }

            // Test 2: Test service connectivity
            TestResult += "\n2. Testing service connectivity...\n";
            try
            {
                // Test if we can list images
                var images = await _blobService.ListImagesAsync();
                TestResult += $"   ✅ Successfully listed {images.Count} images\n";
                
                if (images.Count > 0)
                {
                    TestResult += "   Sample images:\n";
                    foreach (var image in images.Take(3))
                    {
                        TestResult += $"     - {image}\n";
                    }
                }
            }
            catch (Exception ex)
            {
                TestResult += $"   ❌ Service test failed: {ex.Message}\n";
            }

            TestResult += "\n✅ Blob connection test completed!";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Blob connection test failed: {ex.Message}\n\nStack trace:\n{ex.StackTrace}";
        }

        TestBasicConnectivity();
        return Page();
    }

    public async Task<IActionResult> OnPostCreateTestTableAsync()
    {
        try
        {
            TestResult = "🧪 Creating Test Table...\n\n";
            
            // Try to create a test table by creating a test entity
            TestResult += "1. Creating test entity to trigger table creation...\n";
            
            try
            {
                var testCustomer = new Customer
                {
                    RowKey = Guid.NewGuid().ToString(),
                    FirstName = "Table",
                    LastName = "Test",
                    Email = "table@test.com",
                    Phone = "(555) 111-1111",
                    Address = "456 Table St",
                    City = "Table City",
                    State = "TB",
                    ZipCode = "11111"
                };

                var result = await _tableService.CreateEntityAsync(testCustomer);
                if (result)
                {
                    TestResult += "   ✅ Test entity created successfully!\n";
                    TestResult += "   This should have triggered table creation if it didn't exist\n";
                }
                else
                {
                    TestResult += "   ❌ Failed to create test entity\n";
                }
            }
            catch (Exception ex)
            {
                TestResult += $"   ❌ Failed to create test entity: {ex.Message}\n";
            }

            TestResult += "\n✅ Test table creation completed!";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Test table creation failed: {ex.Message}\n\nStack trace:\n{ex.StackTrace}";
        }

        TestBasicConnectivity();
        return Page();
    }

    public async Task<IActionResult> OnPostSeedTestDataAsync()
    {
        try
        {
            TestResult = "🧪 Seeding Test Data...\n\n";
            
            // Try to seed test data using our services
            TestResult += "1. Testing data seeding with our services...\n";
            
            try
            {
                // Test creating a simple customer
                var testCustomer = new Customer
                {
                    RowKey = Guid.NewGuid().ToString(),
                    FirstName = "Seed",
                    LastName = "Test",
                    Email = "seed@test.com",
                    Phone = "(555) 222-2222",
                    Address = "789 Seed St",
                    City = "Seed City",
                    State = "SD",
                    ZipCode = "22222"
                };

                var result = await _tableService.CreateEntityAsync(testCustomer);
                if (result)
                {
                    TestResult += "   ✅ Successfully created test customer!\n";
                    
                    // Try to retrieve it
                    var retrieved = await _tableService.GetEntityAsync<Customer>("Customer", testCustomer.RowKey);
                    if (retrieved != null)
                    {
                        TestResult += "   ✅ Successfully retrieved test customer!\n";
                        TestResult += $"   Name: {retrieved.FirstName} {retrieved.LastName}\n";
                        TestResult += $"   Email: {retrieved.Email}\n";
                    }
                    else
                    {
                        TestResult += "   ⚠️ Customer created but couldn't retrieve it\n";
                    }
                }
                else
                {
                    TestResult += "   ❌ Failed to create test customer\n";
                }
            }
            catch (Exception ex)
            {
                TestResult += $"   ❌ Data seeding test failed: {ex.Message}\n";
            }

            TestResult += "\n✅ Test data seeding completed!";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Test data seeding failed: {ex.Message}\n\nStack trace:\n{ex.StackTrace}";
        }

        TestBasicConnectivity();
        return Page();
    }

    private void TestBasicConnectivity()
    {
        // Test if our services think they're connected
        try
        {
            // We'll use reflection to check the private _isAzureConnected field
            var tableServiceType = _tableService.GetType();
            var tableConnectedField = tableServiceType.GetField("_isAzureConnected", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            TableServiceConnected = tableConnectedField?.GetValue(_tableService) as bool? ?? false;

            var blobServiceType = _blobService.GetType();
            var blobConnectedField = blobServiceType.GetField("_isAzureConnected", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            BlobServiceConnected = blobConnectedField?.GetValue(_blobService) as bool? ?? false;
        }
        catch
        {
            TableServiceConnected = false;
            BlobServiceConnected = false;
        }
    }
}
