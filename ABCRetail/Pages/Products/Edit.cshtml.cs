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

    public EditModel(IAzureTableServiceV2 azureTableService, IAzureBlobService blobService, IInventoryQueueService inventoryQueueService)
    {
        _azureTableService = azureTableService;
        _blobService = blobService;
        _inventoryQueueService = inventoryQueueService;
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
                
                await _inventoryQueueService.SendMessageAsync(inventoryMessage);
                
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


