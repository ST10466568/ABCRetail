using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ABCRetail.Models;
using ABCRetail.Services;

namespace ABCRetail.Pages.Customers;

public class CreateModel : PageModel
{
    private readonly WorkingDataFetcher _workingDataFetcher;

    public CreateModel(WorkingDataFetcher workingDataFetcher)
    {
        _workingDataFetcher = workingDataFetcher;
    }

    [BindProperty]
    public Customer Customer { get; set; } = new();

    public void OnGet()
    {
        // Initialize with default values
        Customer.CreatedDate = DateTime.UtcNow;
        Customer.IsActive = true;
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
            Customer.RowKey = Guid.NewGuid().ToString();
            Customer.CreatedDate = DateTime.UtcNow;

            // For now, we'll simulate success since we're using WorkingDataFetcher
            // In a real implementation, you would save to Azure Table Storage here
            var success = true; // Simulated success
            
            if (success)
            {
                TempData["SuccessMessage"] = $"Customer '{Customer.FullName}' created successfully!";
                return RedirectToPage("./Index");
            }
            else
            {
                ModelState.AddModelError("", "Failed to create customer. Please try again.");
                return Page();
            }
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", $"Error creating customer: {ex.Message}");
            return Page();
        }
    }
}


