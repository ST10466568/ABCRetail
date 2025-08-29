using ABCRetail.Models;
using ABCRetail.Services;

namespace ABCRetail.Services;

public class DataSeederService
{
    private readonly IAzureTableService _tableService;
    private readonly IAzureQueueService _queueService;
    private readonly IAzureFileService _fileService;

    public DataSeederService(
        IAzureTableService tableService,
        IAzureQueueService queueService,
        IAzureFileService fileService)
    {
        _tableService = tableService;
        _queueService = queueService;
        _fileService = fileService;
    }

    public async Task SeedDataAsync()
    {
        Console.WriteLine("🌱 Starting data seeding process...");
        
        Console.WriteLine("📊 Seeding customers...");
        await SeedCustomersAsync();
        
        Console.WriteLine("📦 Seeding products...");
        await SeedProductsAsync();
        
        Console.WriteLine("📋 Seeding queues...");
        await SeedQueuesAsync();
        
        Console.WriteLine("📝 Seeding logs...");
        await SeedLogsAsync();
        
        Console.WriteLine("✅ Data seeding completed successfully!");
    }

    private async Task SeedCustomersAsync()
    {
        Console.WriteLine("   - Creating customer: John Doe");
        var customers = new List<Customer>
        {
            new Customer
            {
                RowKey = Guid.NewGuid().ToString(),
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@email.com",
                Phone = "(555) 123-4567",
                Address = "123 Main St",
                City = "New York",
                State = "NY",
                ZipCode = "10001"
            },
            new Customer
            {
                RowKey = Guid.NewGuid().ToString(),
                FirstName = "Jane",
                LastName = "Smith",
                Email = "jane.smith@email.com",
                Phone = "(555) 987-6543",
                Address = "456 Oak Ave",
                City = "Los Angeles",
                State = "CA",
                ZipCode = "90210"
            },
            new Customer
            {
                RowKey = Guid.NewGuid().ToString(),
                FirstName = "Mike",
                LastName = "Johnson",
                Email = "mike.johnson@email.com",
                Phone = "(555) 456-7890",
                Address = "789 Pine Rd",
                City = "Chicago",
                State = "IL",
                ZipCode = "60601"
            }
        };

        foreach (var customer in customers)
        {
            Console.WriteLine($"   - Saving customer: {customer.FirstName} {customer.LastName}");
            var result = await _tableService.CreateEntityAsync(customer);
            if (result)
            {
                Console.WriteLine($"     ✅ Customer {customer.FirstName} {customer.LastName} saved successfully");
            }
            else
            {
                Console.WriteLine($"     ❌ Failed to save customer {customer.FirstName} {customer.LastName}");
            }
        }
    }

    private async Task SeedProductsAsync()
    {
        Console.WriteLine("   - Creating product: Wireless Headphones");
        var products = new List<Product>
        {
            new Product
            {
                RowKey = Guid.NewGuid().ToString(),
                Name = "Wireless Headphones",
                Description = "Premium noise-canceling wireless headphones with 30-hour battery life",
                Price = 199.99m,
                StockQuantity = 50,
                Category = "Electronics",
                Brand = "TechAudio",
                ImageUrl = "headphones.jpg"
            },
            new Product
            {
                RowKey = Guid.NewGuid().ToString(),
                Name = "Smart Watch",
                Description = "Feature-rich smartwatch with health monitoring and GPS",
                Price = 299.99m,
                StockQuantity = 30,
                Category = "Electronics",
                Brand = "SmartTech",
                ImageUrl = "smartwatch.jpg"
            },
            new Product
            {
                RowKey = Guid.NewGuid().ToString(),
                Name = "Coffee Maker",
                Description = "Programmable coffee maker with thermal carafe",
                Price = 89.99m,
                StockQuantity = 25,
                Category = "Home & Kitchen",
                Brand = "BrewMaster",
                ImageUrl = "coffeemaker.jpg"
            },
            new Product
            {
                RowKey = Guid.NewGuid().ToString(),
                Name = "Yoga Mat",
                Description = "Non-slip yoga mat with carrying strap",
                Price = 39.99m,
                StockQuantity = 100,
                Category = "Sports & Fitness",
                Brand = "FitLife",
                ImageUrl = "yogamat.jpg"
            }
        };

        foreach (var product in products)
        {
            Console.WriteLine($"   - Saving product: {product.Name}");
            var result = await _tableService.CreateEntityAsync(product);
            if (result)
            {
                Console.WriteLine($"     ✅ Product {product.Name} saved successfully");
            }
            else
            {
                Console.WriteLine($"     ❌ Failed to save product {product.Name}");
            }
        }
    }

    private async Task SeedQueuesAsync()
    {
        var orderMessages = new[]
        {
            "Processing order ORD-001 for customer John Doe",
            "Order ORD-002 shipped successfully",
            "New order ORD-003 received from Jane Smith"
        };

        var inventoryMessages = new[]
        {
            "Low stock alert: Wireless Headphones (5 remaining)",
            "Inventory updated: Smart Watch stock increased by 10",
            "Product Coffee Maker marked as discontinued"
        };

        var imageMessages = new[]
        {
            "Image headphones.jpg uploaded successfully",
            "Image smartwatch.jpg processed and optimized",
            "Image coffeemaker.jpg thumbnail generated"
        };

        foreach (var message in orderMessages)
        {
            await _queueService.EnqueueMessageAsync("order-queue", message);
        }

        foreach (var message in inventoryMessages)
        {
            await _queueService.EnqueueMessageAsync("inventory-queue", message);
        }

        foreach (var message in imageMessages)
        {
            await _queueService.EnqueueMessageAsync("image-queue", message);
        }
    }

    private async Task SeedLogsAsync()
    {
        var logEntries = new[]
        {
            "2024-01-15 10:30:00 [INFO] Application started successfully",
            "2024-01-15 10:30:15 [INFO] Azure Storage services initialized",
            "2024-01-15 10:30:30 [INFO] Demo data seeding completed",
            "2024-01-15 10:31:00 [INFO] Database connection established",
            "2024-01-15 10:31:15 [INFO] User authentication service ready"
        };

        var logContent = string.Join("\n", logEntries);
        await _fileService.WriteLogAsync(logContent, "startup.log");
    }
}
