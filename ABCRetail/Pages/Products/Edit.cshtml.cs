using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ABCRetail.Models;
using ABCRetail.Services;
using Microsoft.AspNetCore.Http;

namespace ABCRetail.Pages.Products;

public class EditModel : PageModel
{
    private readonly IAzureTableServiceV2 _azureTableService;
    private readonly IAzureBlobService _blobService;
    private readonly IInventoryQueueService _inventoryQueueService;
    private readonly ILogger<EditModel> _logger;

    public EditModel(IAzureTableServiceV2 azureTableService, IAzureBlobService blobService, IInventoryQueueService inventoryQueueService, ILogger<EditModel> logger)
    {
        _azureTableService = azureTableService;
        _blobService = blobService;
        _inventoryQueueService = inventoryQueueService;
        _logger = logger;
    }

    [BindProperty]
    public Product? Product { get; set; }

    [BindProperty]
    public IFormFile? ProductImage { get; set; }

    public async Task<IActionResult> OnGetAsync(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return NotFound();
        }

        try
        {
            // Get the actual product from the database using Azure SDK
            Product = await _azureTableService.GetProductAsync(id);
            
            if (Product == null)
            {
                return NotFound();
            }
            
            return Page();
        }
        catch (Exception ex)
        {
            var detailedError = $"❌ Error loading product:\nException: {ex.Message}";
            if (ex.InnerException != null)
            {
                detailedError += $"\nInner Exception: {ex.InnerException.Message}";
            }
            detailedError += $"\nStack Trace: {ex.StackTrace}";
            
            ModelState.AddModelError("", detailedError);
            return Page();
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        if (Product == null)
        {
            return NotFound();
        }

        try
        {
            // Handle new image upload if provided
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

            // Update the product in Azure Table Storage using Azure SDK
            // This will throw an exception if it fails, which will be caught below
            await _azureTableService.UpdateProductAsync(Product);
            
            // If we get here, the update was successful
            // Send inventory queue message for product update
            try
            {
                // Validate that required properties are set before creating queue message
                if (string.IsNullOrEmpty(Product?.Name))
                {
                    throw new InvalidOperationException("Product name is null or empty");
                }
                
                if (string.IsNullOrEmpty(Product?.RowKey))
                {
                    throw new InvalidOperationException("Product RowKey is null or empty");
                }
                
                var inventoryMessage = new InventoryQueueMessage
                {
                    Type = "inventory_update",
                    ProductId = Product.RowKey,
                    ProductName = Product.Name,
                    Quantity = Product.StockQuantity,
                    Action = "update",
                    Priority = "normal",
                    Status = "pending",
                    Notes = $"Product '{Product.Name}' updated. New stock quantity: {Product.StockQuantity}",
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
                
                // Send low stock alert if stock quantity is 10 or less
                if (Product.StockQuantity <= 10)
                {
                    _logger.LogWarning("🚨 Low stock detected for {ProductName}: {Quantity} units", Product.Name, Product.StockQuantity);
                    
                    var lowStockMessage = new InventoryQueueMessage
                    {
                        Type = "low_stock_alert",
                        ProductId = Product.RowKey,
                        ProductName = Product.Name,
                        Quantity = Product.StockQuantity,
                        Action = "alert",
                        Priority = Product.StockQuantity <= 5 ? "urgent" : "high",
                        Status = "pending",
                        Notes = $"Low stock alert: Product '{Product.Name}' stock reduced to {Product.StockQuantity} units",
                        UserId = "system"
                    };
                    
                    var lowStockResult = await _inventoryQueueService.SendMessageAsync(lowStockMessage);
                    
                    if (!lowStockResult)
                    {
                        throw new InvalidOperationException("Failed to send low stock alert message");
                    }
                    
                    _logger.LogInformation("✅ Low stock alert sent successfully for {ProductName}", Product.Name);
                }
                
                TempData["SuccessMessage"] = $"Product '{Product.Name}' updated successfully! Inventory message queued.";
                return RedirectToPage("./Index");
            }
            catch (Exception queueEx)
            {
                // Product update succeeded but queue message failed
                var queueError = $"Product '{Product.Name}' updated successfully, but failed to queue inventory message: {queueEx.Message}";
                if (queueEx.InnerException != null)
                {
                    queueError += $"\nInner Exception: {queueEx.InnerException.Message}";
                }
                queueError += $"\nStack Trace: {queueEx.StackTrace}";
                
                TempData["WarningMessage"] = queueError;
                return RedirectToPage("./Index");
            }
        }
        catch (Exception ex)
        {
            var detailedError = $"❌ Error updating product:\nException: {ex.Message}";
            if (ex.InnerException != null)
            {
                detailedError += $"\nInner Exception: {ex.InnerException.Message}";
            }
            detailedError += $"\nStack Trace: {ex.StackTrace}";
            
            ModelState.AddModelError("", detailedError);
            return Page();
        }
    }
}


