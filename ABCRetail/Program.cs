using ABCRetail.Services;
using ABCRetail.Models;
using Microsoft.Extensions.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// Configure custom logging to Azure File Storage
var loggingConnectionString = builder.Configuration.GetConnectionString("AzureStorage");
if (!string.IsNullOrEmpty(loggingConnectionString))
{
    builder.Logging.AddProvider(new AzureFileLoggerProvider(loggingConnectionString));
}

// Register Azure Storage services
builder.Services.AddScoped<IAzureTableServiceV2, ConnectionStringService>();
builder.Services.AddScoped<IAzureTableService, AzureTableService>();
builder.Services.AddScoped<IAzureBlobService, AzureBlobService>();
builder.Services.AddScoped<IAzureQueueService, AzureQueueService>();
builder.Services.AddScoped<IAzureFileService, AzureFileService>();



// Register working data fetcher
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
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.MapRazorPages();

// Seed demo data on startup
using (var scope = app.Services.CreateScope())
{
    try
    {
        var seeder = scope.ServiceProvider.GetRequiredService<DataSeederService>();
        await seeder.SeedDataAsync();
        
        // Seed inventory queue demo messages
        var inventoryQueueSeeder = scope.ServiceProvider.GetRequiredService<InventoryQueueSeeder>();
        await inventoryQueueSeeder.SeedDemoMessagesAsync();
    }
    catch (Exception ex)
    {
        // Log error but don't stop application startup
        Console.WriteLine($"Data seeding failed: {ex.Message}");
    }
}

app.Run();


