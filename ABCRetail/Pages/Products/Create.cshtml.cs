using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ABCRetail.Models;
using ABCRetail.Services;
using Microsoft.AspNetCore.Http;

namespace ABCRetail.Pages.Products;

public class CreateModel : PageModel
{
    private readonly WorkingDataFetcher _workingDataFetcher;
    private readonly IAzureBlobService _blobService;
    private readonly IInventoryQueueService _inventoryQueueService;
    private readonly ILogger<CreateModel> _logger;

    public CreateModel(WorkingDataFetcher workingDataFetcher, IAzureBlobService blobService, IInventoryQueueService inventoryQueueService, ILogger<CreateModel> logger)
    {
        _workingDataFetcher = workingDataFetcher;
        _blobService = blobService;
        _inventoryQueueService = inventoryQueueService;
        _logger = logger;
    }

    [BindProperty]
    public Product Product { get; set; } = new();

    [BindProperty]
    public IFormFile? ProductImage { get; set; }

    public void OnGet()
    {
        // Initialize with default values
        Product.CreatedDate = DateTime.UtcNow;
        Product.IsActive = true;
        Product.StockQuantity = 0;
        Product.Price = 0;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            // Generate unique RowKey
            Product.RowKey = Guid.NewGuid().ToString();
            Product.CreatedDate = DateTime.UtcNow;
            Product.LastModifiedDate = DateTime.UtcNow;

            // Handle image upload if provided
            if (ProductImage != null && ProductImage.Length > 0)
            {
                try
                {
                    var fileName = $"{Product.RowKey}_{ProductImage.FileName}";
                    var imageUrl = await _blobService.UploadImageAsync(ProductImage, fileName);
                    Product.ImageUrl = imageUrl;
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("ProductImage", $"Failed to upload image: {ex.Message}");
                    return Page();
                }
            }

            // For now, we'll simulate success since we're using WorkingDataFetcher
            // In a real implementation, you would save to Azure Table Storage here
            var success = true; // Simulated success
            
            if (success)
            {
                // Send inventory queue message for product creation
                var inventoryMessage = new InventoryQueueMessage
                {
                    Type = "inventory_update",
                    ProductId = Product.RowKey,
                    ProductName = Product.Name,
                    Quantity = Product.StockQuantity,
                    Action = "add",
                    Priority = "normal",
                    Status = "pending",
                    Notes = $"New product '{Product.Name}' created with initial stock of {Product.StockQuantity}",
                    UserId = "system"
                };
                
                // Log the message being sent for debugging
                _logger.LogInformation("📤 Sending inventory queue message: {MessageType} for {ProductName}", inventoryMessage.Type, inventoryMessage.ProductName);
                
                var sendResult = await _inventoryQueueService.SendMessageAsync(inventoryMessage);
                
                if (!sendResult)
                {
                    throw new InvalidOperationException("Failed to send inventory queue message");
                }
                
                _logger.LogInformation("✅ Inventory queue message sent successfully for {ProductName}", inventoryMessage.ProductName);
                
                // Send low stock alert if initial stock is 10 or less
                if (Product.StockQuantity <= 10)
                {
                    _logger.LogWarning("🚨 Low stock detected for new product {ProductName}: {Quantity} units", Product.Name, Product.StockQuantity);
                    
                    var lowStockMessage = new InventoryQueueMessage
                    {
                        Type = "low_stock_alert",
                        ProductId = Product.RowKey,
                        ProductName = Product.Name,
                        Quantity = Product.StockQuantity,
                        Action = "alert",
                        Priority = Product.StockQuantity <= 5 ? "urgent" : "high",
                        Status = "pending",
                        Notes = $"Low stock alert: New product '{Product.Name}' created with only {Product.StockQuantity} units",
                        UserId = "system"
                    };
                    
                    var lowStockResult = await _inventoryQueueService.SendMessageAsync(lowStockMessage);
                    
                    if (!lowStockResult)
                    {
                        throw new InvalidOperationException("Failed to send low stock alert message");
                    }
                    
                    _logger.LogInformation("✅ Low stock alert sent successfully for new product {ProductName}", Product.Name);
                }
                
                TempData["SuccessMessage"] = $"Product '{Product.Name}' created successfully! Inventory message queued.";
                return RedirectToPage("./Index");
            }
            else
            {
                ModelState.AddModelError("", "Failed to create product. Please try again.");
                return Page();
            }
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", $"Error creating product: {ex.Message}");
            return Page();
        }
    }
}


