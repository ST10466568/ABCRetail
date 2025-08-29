using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ABCRetail.Models;
using ABCRetail.Services;

namespace ABCRetail.Pages.Customers;

public class DetailsModel : PageModel
{
    private readonly WorkingDataFetcher _workingDataFetcher;

    public DetailsModel(WorkingDataFetcher workingDataFetcher)
    {
        _workingDataFetcher = workingDataFetcher;
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
            Customer = await _workingDataFetcher.GetCustomerAsync(id);
            
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
}


