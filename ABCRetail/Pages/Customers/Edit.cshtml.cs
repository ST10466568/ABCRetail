using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ABCRetail.Models;
using ABCRetail.Services;

namespace ABCRetail.Pages.Customers;

public class EditModel : PageModel
{
    private readonly IAzureTableServiceV2 _azureTableService;

    public EditModel(IAzureTableServiceV2 azureTableService)
    {
        _azureTableService = azureTableService;
    }

    [BindProperty]
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
            var detailedError = $"❌ Error loading customer:\nException: {ex.Message}";
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

        if (Customer == null)
        {
            return NotFound();
        }

        try
        {
            // Update the customer in Azure Table Storage using Azure SDK
            // This will throw an exception if it fails, which will be caught below
            await _azureTableService.UpdateCustomerAsync(Customer);
            
            // If we get here, the update was successful
            TempData["SuccessMessage"] = $"Customer '{Customer.FullName}' updated successfully!";
            return RedirectToPage("./Index");
        }
        catch (Exception ex)
        {
            var detailedError = $"❌ Error updating customer:\nException: {ex.Message}";
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


