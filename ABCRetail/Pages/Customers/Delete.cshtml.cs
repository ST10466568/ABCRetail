using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ABCRetail.Models;
using ABCRetail.Services;

namespace ABCRetail.Pages.Customers;

public class DeleteModel : PageModel
{
    private readonly IAzureTableServiceV2 _azureTableService;

    public DeleteModel(IAzureTableServiceV2 azureTableService)
    {
        _azureTableService = azureTableService;
    }

    public Customer? Customer { get; set; }

    public async Task<IActionResult> OnGetAsync(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return NotFound();
        }

        try
        {
            // Get the actual customer from the database using Azure SDK
            Customer = await _azureTableService.GetCustomerAsync(id);
            
            if (Customer == null)
            {
                return NotFound();
            }
            
            return Page();
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", $"Error loading customer: {ex.Message}");
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
            // Delete the customer using Azure SDK
            var success = await _azureTableService.DeleteCustomerAsync(id);
            
            if (success)
            {
                TempData["SuccessMessage"] = "Customer deleted successfully!";
                return RedirectToPage("./Index");
            }
            else
            {
                ModelState.AddModelError("", "Failed to delete customer. Please try again.");
                return Page();
            }
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", $"Error deleting customer: {ex.Message}");
            return Page();
        }
    }
}
