using ABCRetail.Models;
using Microsoft.Extensions.Logging;

namespace ABCRetail.Services
{
    public class InventoryQueueSeeder
    {
        private readonly IInventoryQueueService _inventoryQueueService;
        private readonly ILogger<InventoryQueueSeeder> _logger;

        public InventoryQueueSeeder(IInventoryQueueService inventoryQueueService, ILogger<InventoryQueueSeeder> logger)
        {
            _inventoryQueueService = inventoryQueueService;
            _logger = logger;
        }

        public async Task SeedDemoMessagesAsync()
        {
            try
            {
                _logger.LogInformation("Seeding demo inventory queue messages...");

                var demoMessages = new List<InventoryQueueMessage>
                {
                    new InventoryQueueMessage
                    {
                        Type = "low_stock_alert",
                        ProductId = "demo-product-001",
                        ProductName = "Laptop Computer",
                        Quantity = 5,
                        Action = "alert",
                        Priority = "high",
                        Status = "pending",
                        Notes = "Stock level below minimum threshold. Consider restocking.",
                        UserId = "system"
                    },
                    new InventoryQueueMessage
                    {
                        Type = "inventory_update",
                        ProductId = "demo-product-002",
                        ProductName = "Wireless Mouse",
                        Quantity = 25,
                        Action = "add",
                        Priority = "normal",
                        Status = "pending",
                        Notes = "New shipment received. Update inventory count.",
                        UserId = "warehouse-001"
                    },
                    new InventoryQueueMessage
                    {
                        Type = "restock_request",
                        ProductId = "demo-product-003",
                        ProductName = "USB Keyboard",
                        Quantity = 50,
                        Action = "request",
                        Priority = "urgent",
                        Status = "pending",
                        Notes = "Out of stock. Immediate restock required.",
                        UserId = "sales-001"
                    },
                    new InventoryQueueMessage
                    {
                        Type = "inventory_audit",
                        ProductId = "demo-product-004",
                        ProductName = "Monitor Stand",
                        Quantity = 12,
                        Action = "update",
                        Priority = "low",
                        Status = "pending",
                        Notes = "Physical count differs from system count. Audit required.",
                        UserId = "audit-001"
                    },
                    new InventoryQueueMessage
                    {
                        Type = "out_of_stock_alert",
                        ProductId = "demo-product-005",
                        ProductName = "Gaming Headset",
                        Quantity = 0,
                        Action = "alert",
                        Priority = "high",
                        Status = "pending",
                        Notes = "Product completely out of stock. Customer orders pending.",
                        UserId = "system"
                    }
                };

                int successCount = 0;
                foreach (var message in demoMessages)
                {
                    try
                    {
                        var success = await _inventoryQueueService.SendMessageAsync(message);
                        if (success)
                        {
                            successCount++;
                            _logger.LogInformation("Successfully seeded demo message: {ProductName}", message.ProductName);
                        }
                        else
                        {
                            _logger.LogWarning("Failed to seed demo message: {ProductName}", message.ProductName);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error seeding demo message: {ProductName}", message.ProductName);
                    }
                }

                _logger.LogInformation("Demo inventory queue seeding completed. Successfully seeded {SuccessCount} out of {TotalCount} messages.", 
                    successCount, demoMessages.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during demo inventory queue seeding");
            }
        }
    }
}
