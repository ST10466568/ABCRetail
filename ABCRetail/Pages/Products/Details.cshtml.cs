using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ABCRetail.Models;
using ABCRetail.Services;

namespace ABCRetail.Pages.Products;

public class DetailsModel : PageModel
{
    private readonly WorkingDataFetcher _workingDataFetcher;

    public DetailsModel(WorkingDataFetcher workingDataFetcher)
    {
        _workingDataFetcher = workingDataFetcher;
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
            Product = await _workingDataFetcher.GetProductAsync(id);
            
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
}


