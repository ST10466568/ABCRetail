using ABCRetail.Services;
using ABCRetail.Models;
using Microsoft.Extensions.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Test Azure connectivity before starting the app
Console.WriteLine("🔍 Testing Azure Storage Connection...");
var connectionString = builder.Configuration.GetSection("AzureStorage:ConnectionString").Value;
var tableSasUrl = builder.Configuration.GetSection("AzureStorage:TableSasUrl").Value;
Console.WriteLine($"Connection String: {connectionString?.Substring(0, Math.Min(50, connectionString?.Length ?? 0))}...");
Console.WriteLine($"Table SAS URL: {tableSasUrl?.Substring(0, Math.Min(50, tableSasUrl?.Length ?? 0))}...");

// Test Azure configuration
try
{
    Console.WriteLine("✅ Azure Configuration loaded successfully");
    
    // Test if the connection string is valid (not placeholder)
    if (!string.IsNullOrEmpty(connectionString) && 
        !connectionString.Contains("YOUR_ACCOUNT_NAME") && 
        !connectionString.Contains("YOUR_ACCOUNT_KEY"))
    {
        Console.WriteLine("✅ Connection string appears valid");
    }
    else
    {
        Console.WriteLine("⚠️ Connection string contains placeholder values");
    }
    
    if (!string.IsNullOrEmpty(tableSasUrl))
    {
        Console.WriteLine("✅ Table SAS URL found");
    }
    else
    {
        Console.WriteLine("⚠️ No Table SAS URL found");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Azure Configuration Error: {ex.Message}");
}

Console.WriteLine("\n🎯 Azure Connection Test Complete!");
Console.WriteLine("Starting ABC Retail application...\n");

// Add services to the container.
builder.Services.AddRazorPages();

// Register Azure Storage services
builder.Services.AddScoped<IAzureTableServiceV2, ConnectionStringService>(); // Using Azure SDK with connection string (new approach)
builder.Services.AddScoped<IAzureTableService, AzureTableService>(); // Keep original for backward compatibility
builder.Services.AddScoped<IAzureBlobService, AzureBlobService>();
builder.Services.AddScoped<IAzureQueueService, AzureQueueService>();
builder.Services.AddScoped<IAzureFileService, AzureFileService>();

// Register HTTP-based table service (Postman-style)
builder.Services.AddScoped<HttpTableService>();

// Register working data fetcher (bypasses Azure SDK)
builder.Services.AddScoped<WorkingDataFetcher>();

// Register inventory queue service
builder.Services.AddScoped<IInventoryQueueService, InventoryQueueService>();

// Register inventory queue seeder
builder.Services.AddScoped<InventoryQueueSeeder>();

// Register data seeder service
builder.Services.AddScoped<DataSeederService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

// Seed demo data on startup
Console.WriteLine("🌱 Starting data seeding process...");
using (var scope = app.Services.CreateScope())
{
    try
    {
        var seeder = scope.ServiceProvider.GetRequiredService<DataSeederService>();
        Console.WriteLine("✅ DataSeederService created successfully");
        
        // Test Azure Table Service connection
        var tableService = scope.ServiceProvider.GetRequiredService<IAzureTableService>();
        Console.WriteLine("✅ AzureTableService retrieved successfully");
        
        // Test if we can list tables
        var tables = await tableService.ListTablesAsync();
        Console.WriteLine($"📋 Found {tables.Count} tables: {string.Join(", ", tables)}");
        
        await seeder.SeedDataAsync();
        Console.WriteLine("✅ Data seeding completed");
        
        // Test direct data fetching from Azure
        Console.WriteLine("\n🔍 Testing direct data fetching from Azure...");
        await TestDirectDataFetching();
        
        // Test the working data fetcher
        Console.WriteLine("\n🔍 Testing WorkingDataFetcher...");
        var workingFetcher = scope.ServiceProvider.GetRequiredService<WorkingDataFetcher>();
        var connectionTest = await workingFetcher.TestConnectionAsync();
        Console.WriteLine($"🔗 {connectionTest}");
        
        var workingCustomers = await workingFetcher.GetCustomersAsync();
        Console.WriteLine($"📊 WorkingDataFetcher found {workingCustomers.Count} customers!");
        
        var workingProducts = await workingFetcher.GetProductsAsync();
        Console.WriteLine($"📦 WorkingDataFetcher found {workingProducts.Count} products!");
        
        // Verify data was seeded by trying to retrieve it
        Console.WriteLine("🔍 Verifying seeded data...");
        var customers = await tableService.GetAllEntitiesAsync<Customer>();
        Console.WriteLine($"📊 Retrieved {customers.Count()} customers");
        var products = await tableService.GetAllEntitiesAsync<Product>();
        Console.WriteLine($"📦 Retrieved {products.Count()} products");
        
        // Seed inventory queue demo messages
        Console.WriteLine("\n📬 Seeding inventory queue demo messages...");
        var inventoryQueueSeeder = scope.ServiceProvider.GetRequiredService<InventoryQueueSeeder>();
        await inventoryQueueSeeder.SeedDemoMessagesAsync();
        Console.WriteLine("✅ Inventory queue seeding completed");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Data seeding failed: {ex.Message}");
        Console.WriteLine($"Stack trace: {ex.StackTrace}");
        
        // Try to get more details about the error
        if (ex.InnerException != null)
        {
            Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
        }
    }
}

Console.WriteLine("🚀 Application startup complete!");
app.Run();

// Test method for direct data fetching
static async Task TestDirectDataFetching()
{
    try
    {
        Console.WriteLine("🔍 Testing direct HTTP requests to Azure...");
        
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("Accept", "application/json;odata=minimalmetadata");
        httpClient.DefaultRequestHeaders.Add("User-Agent", "DirectTest/1.0");
        
        // Test Customers
        Console.WriteLine("📊 Testing Customers table...");
        var customersUrl = "https://abcretailstoragevuyo.table.core.windows.net/Customers?sv=2024-11-04&ss=bfqt&srt=so&sp=rwdlacupiytfx&se=2025-08-29T04:04:35Z&st=2025-08-28T19:49:35Z&spr=https&sig=H1kGzZT9hliQpPFsA6Sz0meKDtQNynBTx7M2e5DyEZw%3D";
        
        var customersResponse = await httpClient.GetStringAsync(customersUrl);
        Console.WriteLine($"✅ Customers response: {customersResponse.Length} characters");
        
        // Test Products
        Console.WriteLine("📦 Testing Products table...");
        var productsUrl = "https://abcretailstoragevuyo.table.core.windows.net/Products?sv=2024-11-04&ss=bfqt&srt=so&sp=rwdlacupiytfx&se=2025-08-29T04:04:35Z&st=2025-08-28T19:49:35Z&spr=https&sig=H1kGzZT9hliQpPFsA6Sz0meKDtQNynBTx7M2e5DyEZw%3D";
        
        var productsResponse = await httpClient.GetStringAsync(productsUrl);
        Console.WriteLine($"✅ Products response: {productsResponse.Length} characters");
        
        Console.WriteLine("🎉 Direct Azure testing completed successfully!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Direct Azure testing failed: {ex.Message}");
    }
}
