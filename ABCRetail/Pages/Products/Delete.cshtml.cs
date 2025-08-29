using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ABCRetail.Models;
using ABCRetail.Services;

namespace ABCRetail.Pages.Products;

public class DeleteModel : PageModel
{
    private readonly IAzureTableServiceV2 _azureTableService;
    private readonly IAzureBlobService _blobService;
    private readonly IInventoryQueueService _inventoryQueueService;

    public DeleteModel(IAzureTableServiceV2 azureTableService, IAzureBlobService blobService, IInventoryQueueService inventoryQueueService)
    {
        _azureTableService = azureTableService;
        _blobService = blobService;
        _inventoryQueueService = inventoryQueueService;
    }

    public Product? Product { get; set; }

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
            ModelState.AddModelError("", $"Error loading product: {ex.Message}");
            return Page();
        }
    }

    public async Task<IActionResult> OnPostAsync(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return NotFound();
        }

        try
        {
            // Delete the product using Azure SDK
            var success = await _azureTableService.DeleteProductAsync(id);
            
            if (success)
            {
                // Send inventory queue message for product deletion
                var inventoryMessage = new InventoryQueueMessage
                {
                    Type = "inventory_update",
                    ProductId = id,
                    ProductName = Product?.Name ?? "Unknown Product",
                    Quantity = 0,
                    Action = "remove",
                    Priority = "high",
                    Status = "pending",
                    Notes = $"Product '{Product?.Name ?? "Unknown"}' deleted from inventory",
                    UserId = "system"
                };
                
                await _inventoryQueueService.SendMessageAsync(inventoryMessage);
                
                TempData["SuccessMessage"] = "Product deleted successfully! Inventory message queued.";
                return RedirectToPage("./Index");
            }
            else
            {
                ModelState.AddModelError("", "Failed to delete product. Please try again.");
                return Page();
            }
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", $"Error deleting product: {ex.Message}");
            return Page();
        }
    }
}
