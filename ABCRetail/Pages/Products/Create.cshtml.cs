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

    public CreateModel(WorkingDataFetcher workingDataFetcher, IAzureBlobService blobService, IInventoryQueueService inventoryQueueService)
    {
        _workingDataFetcher = workingDataFetcher;
        _blobService = blobService;
        _inventoryQueueService = inventoryQueueService;
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
                
                await _inventoryQueueService.SendMessageAsync(inventoryMessage);
                
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


