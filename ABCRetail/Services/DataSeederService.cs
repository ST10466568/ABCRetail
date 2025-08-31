using ABCRetail.Models;
using ABCRetail.Services;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ABCRetail.Services
{
    public class DataSeederService
    {
        private readonly IAzureTableService _tableService;
        private readonly IConfiguration _configuration;

        public DataSeederService(IAzureTableService tableService, IConfiguration configuration)
        {
            _tableService = tableService;
            _configuration = configuration;
        }

        public async Task SeedDataAsync()
        {
            try
            {
                Console.WriteLine("Starting data seeding process...");
                
                // Seed customers
                Console.WriteLine("Seeding customers...");
                await SeedCustomersAsync();
                
                // Seed products
                Console.WriteLine("Seeding products...");
                await SeedProductsAsync();
                
                // Seed queues
                Console.WriteLine("Seeding queues...");
                await SeedQueuesAsync();
                
                Console.WriteLine("Data seeding completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Data seeding failed: {ex.Message}");
                // Log error but don't stop application startup
                throw;
            }
        }

        private async Task SeedCustomersAsync()
        {
            var customers = new List<Customer>
            {
                new Customer
                {
                    PartitionKey = "Customer",
                    RowKey = "1",
                    FirstName = "John",
                    LastName = "Doe",
                    Email = "john.doe@email.com",
                    Phone = "555-0101",
                    Address = "123 Main St",
                    City = "Anytown",
                    State = "CA",
                    ZipCode = "90210",
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                },
                new Customer
                {
                    PartitionKey = "Customer",
                    RowKey = "2",
                    FirstName = "Jane",
                    LastName = "Smith",
                    Email = "jane.smith@email.com",
                    Phone = "555-0102",
                    Address = "456 Oak Ave",
                    City = "Somewhere",
                    State = "NY",
                    ZipCode = "10001",
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                }
            };

            foreach (var customer in customers)
            {
                try
                {
                    // Check if customer already exists
                    var exists = await _tableService.EntityExistsAsync<Customer>(customer.PartitionKey, customer.RowKey);
                    if (!exists)
                    {
                        await _tableService.CreateEntityAsync(customer);
                        Console.WriteLine($"Created customer: {customer.FirstName} {customer.LastName}");
                    }
                    else
                    {
                        Console.WriteLine($"Customer already exists: {customer.FirstName} {customer.LastName}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error with customer {customer.FirstName} {customer.LastName}: {ex.Message}");
                    // Continue with next customer if one fails
                }
            }
        }

        private async Task SeedProductsAsync()
        {
            var products = new List<Product>
            {
                new Product
                {
                    PartitionKey = "Product",
                    RowKey = "1",
                    Name = "Wireless Headphones",
                    Description = "High-quality wireless headphones with noise cancellation",
                    Price = 199.99m,
                    Category = "Electronics",
                    StockQuantity = 50,
                    ImageUrl = "/images/headphones.jpg",
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow,
                    LastModifiedDate = DateTime.UtcNow
                },
                new Product
                {
                    PartitionKey = "Product",
                    RowKey = "2",
                    Name = "Smartphone",
                    Description = "Latest smartphone with advanced features",
                    Price = 799.99m,
                    Category = "Electronics",
                    StockQuantity = 25,
                    ImageUrl = "/images/smartphone.jpg",
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow,
                    LastModifiedDate = DateTime.UtcNow
                },
                new Product
                {
                    PartitionKey = "Product",
                    RowKey = "3",
                    Name = "Laptop",
                    Description = "High-performance laptop for work and gaming",
                    Price = 1299.99m,
                    Category = "Electronics",
                    StockQuantity = 15,
                    ImageUrl = "/images/laptop.jpg",
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow,
                    LastModifiedDate = DateTime.UtcNow
                }
            };

            foreach (var product in products)
            {
                try
                {
                    // Check if product already exists
                    var exists = await _tableService.EntityExistsAsync<Product>(product.PartitionKey, product.RowKey);
                    if (!exists)
                    {
                        await _tableService.CreateEntityAsync(product);
                        Console.WriteLine($"Created product: {product.Name}");
                    }
                    else
                    {
                        Console.WriteLine($"Product already exists: {product.Name}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error with product {product.Name}: {ex.Message}");
                    // Continue with next product if one fails
                }
            }
        }

        private async Task SeedQueuesAsync()
        {
            // Queue seeding is handled by InventoryQueueSeeder
            Console.WriteLine("Queue seeding handled by InventoryQueueSeeder");
        }
    }
}
