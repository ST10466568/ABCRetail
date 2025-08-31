using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ABCRetail.Models;
using ABCRetail.Services;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace ABCRetail.Pages.Products
{
    public class IndexModel : PageModel
    {
        private readonly IAzureTableService _azureTableService;
        private readonly IInventoryQueueService _inventoryQueueService;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(IAzureTableService azureTableService, 
                         IInventoryQueueService inventoryQueueService,
                         ILogger<IndexModel> logger)
        {
            _azureTableService = azureTableService;
            _inventoryQueueService = inventoryQueueService;
            _logger = logger;
        }

        public List<Product> Products { get; set; } = new List<Product>();
        public List<Product> PaginatedProducts { get; set; } = new List<Product>();
        
        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;
        
        public int PageSize { get; set; } = 10;
        public int TotalPages { get; set; }
        public int TotalProducts { get; set; }
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;

        public async Task OnGetAsync()
        {
            try
            {
                Console.WriteLine("Products Index: Starting to fetch products...");
                var productsEnumerable = await _azureTableService.GetAllEntitiesAsync<Product>();
                Products = productsEnumerable.ToList();
                Console.WriteLine($"Products Index: Retrieved {Products.Count} products");
                
                TotalProducts = Products.Count;
                TotalPages = (int)Math.Ceiling((double)TotalProducts / PageSize);
                
                // Ensure current page is within valid range
                if (CurrentPage < 1) CurrentPage = 1;
                if (CurrentPage > TotalPages) CurrentPage = TotalPages;
                if (TotalPages == 0) CurrentPage = 1;
                
                // Get products for current page
                var skip = (CurrentPage - 1) * PageSize;
                PaginatedProducts = Products.Skip(skip).Take(PageSize).ToList();
                Console.WriteLine($"Products Index: Paginated to {PaginatedProducts.Count} products for page {CurrentPage}");
            }
            catch (System.Exception ex)
            {
                Console.WriteLine($"Products Index: Error fetching products: {ex.Message}");
                Products = new List<Product>();
                PaginatedProducts = new List<Product>();
            }
        }
        
        public async Task<IActionResult> OnPostLowStockAlertAsync([FromBody] LowStockAlertRequest request)
        {
            try
            {
                _logger.LogInformation("Processing low stock alerts for {ProductCount} products", request.ProductNames.Count);
                
                var messagesSent = 0;
                for (int i = 0; i < request.ProductNames.Count; i++)
                {
                    var productName = request.ProductNames[i];
                    var stockQuantity = request.StockQuantities[i];
                    
                    var queueMessage = new InventoryQueueMessage
                    {
                        Id = Guid.NewGuid().ToString(),
                        Type = "low_stock_alert",
                        ProductName = productName,
                        Action = "alert",
                        Quantity = stockQuantity,
                        Priority = stockQuantity <= 5 ? "urgent" : stockQuantity <= 10 ? "high" : "normal",
                        Status = "pending",
                        UserId = "system",
                        Timestamp = DateTime.UtcNow,
                        Notes = $"Low stock alert: {productName} has only {stockQuantity} units remaining"
                    };
                    
                    var result = await _inventoryQueueService.SendMessageAsync(queueMessage);
                    if (result)
                    {
                        messagesSent++;
                        _logger.LogInformation("✅ Low stock alert sent for {ProductName} (Quantity: {Quantity})", productName, stockQuantity);
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ Failed to send low stock alert for {ProductName}", productName);
                    }
                }
                
                return new JsonResult(new { 
                    success = true, 
                    message = $"Low stock alerts processed successfully. {messagesSent} messages sent to queue." 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error processing low stock alerts");
                return new JsonResult(new { success = false, error = ex.Message });
            }
        }

        // New method to send inventory update message
        public async Task<IActionResult> OnPostInventoryUpdateAsync([FromBody] InventoryUpdateRequest request)
        {
            try
            {
                _logger.LogInformation("Processing inventory update for product: {ProductName}", request.ProductName);
                
                var queueMessage = new InventoryQueueMessage
                {
                    Id = Guid.NewGuid().ToString(),
                    Type = "inventory_update",
                    ProductId = request.ProductId,
                    ProductName = request.ProductName,
                    Action = request.Action,
                    Quantity = request.NewQuantity,
                    Priority = "normal",
                    Status = "pending",
                    UserId = request.UserId ?? "system",
                    Timestamp = DateTime.UtcNow,
                    Notes = $"Inventory {request.Action}: {request.ProductName} quantity changed to {request.NewQuantity}"
                };
                
                var result = await _inventoryQueueService.SendMessageAsync(queueMessage);
                if (result)
                {
                    _logger.LogInformation("✅ Inventory update message sent for {ProductName}", request.ProductName);
                    return new JsonResult(new { success = true, message = "Inventory update message sent to queue" });
                }
                else
                {
                    _logger.LogWarning("⚠️ Failed to send inventory update message for {ProductName}", request.ProductName);
                    return new JsonResult(new { success = false, error = "Failed to send message to queue" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error processing inventory update");
                return new JsonResult(new { success = false, error = ex.Message });
            }
        }

        // New method to send restock request
        public async Task<IActionResult> OnPostRestockRequestAsync([FromBody] RestockRequest request)
        {
            try
            {
                _logger.LogInformation("Processing restock request for product: {ProductName}", request.ProductName);
                
                var queueMessage = new InventoryQueueMessage
                {
                    Id = Guid.NewGuid().ToString(),
                    Type = "restock_request",
                    ProductId = request.ProductId,
                    ProductName = request.ProductName,
                    Action = "request",
                    Quantity = request.RequestedQuantity,
                    Priority = request.IsUrgent ? "urgent" : "normal",
                    Status = "pending",
                    UserId = request.UserId ?? "system",
                    Timestamp = DateTime.UtcNow,
                    Notes = $"Restock request: {request.RequestedQuantity} units of {request.ProductName}"
                };
                
                var result = await _inventoryQueueService.SendMessageAsync(queueMessage);
                if (result)
                {
                    _logger.LogInformation("✅ Restock request message sent for {ProductName}", request.ProductName);
                    return new JsonResult(new { success = true, message = "Restock request sent to queue" });
                }
                else
                {
                    _logger.LogWarning("⚠️ Failed to send restock request message for {ProductName}", request.ProductName);
                    return new JsonResult(new { success = false, error = "Failed to send message to queue" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error processing restock request");
                return new JsonResult(new { success = false, error = ex.Message });
            }
        }
    }
    
    public class LowStockAlertRequest
    {
        public List<string> ProductNames { get; set; } = new List<string>();
        public List<int> StockQuantities { get; set; } = new List<int>();
    }
    
    public class InventoryUpdateRequest
    {
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty; // "add", "remove", "update"
        public int NewQuantity { get; set; }
        public string? UserId { get; set; }
    }
    
    public class RestockRequest
    {
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int RequestedQuantity { get; set; }
        public bool IsUrgent { get; set; }
        public string? UserId { get; set; }
    }
}
