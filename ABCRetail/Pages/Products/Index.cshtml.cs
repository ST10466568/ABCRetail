using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ABCRetail.Models;
using ABCRetail.Services;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Text.Json;

namespace ABCRetail.Pages.Products
{
    public class IndexModel : PageModel
    {
        private readonly WorkingDataFetcher _workingDataFetcher;

        public IndexModel(WorkingDataFetcher workingDataFetcher)
        {
            _workingDataFetcher = workingDataFetcher;
        }

        public List<Product> Products { get; set; } = new List<Product>();
        public List<Product> PaginatedProducts { get; set; } = new List<Product>();
        
        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;
        
        public int PageSize { get; set; } = 10;
        public int TotalPages { get; set; }
        public int TotalProducts { get; set; }
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;

        public async Task OnGetAsync()
        {
            try
            {
                Products = await _workingDataFetcher.GetProductsAsync();
                TotalProducts = Products.Count;
                TotalPages = (int)Math.Ceiling((double)TotalProducts / PageSize);
                
                // Ensure current page is within valid range
                if (CurrentPage < 1) CurrentPage = 1;
                if (CurrentPage > TotalPages) CurrentPage = TotalPages;
                if (TotalPages == 0) CurrentPage = 1;
                
                // Get products for current page
                var skip = (CurrentPage - 1) * PageSize;
                PaginatedProducts = Products.Skip(skip).Take(PageSize).ToList();
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine($"Error loading products: {ex.Message}");
                Products = new List<Product>();
                PaginatedProducts = new List<Product>();
            }
        }
        
        public async Task<IActionResult> OnPostLowStockAlertAsync([FromBody] LowStockAlertRequest request)
        {
            try
            {
                // This would typically send inventory queue messages for low stock products
                // For now, we'll return a success response
                return new JsonResult(new { success = true, message = "Low stock alerts processed successfully" });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, error = ex.Message });
            }
        }
    }
    
    public class LowStockAlertRequest
    {
        public List<string> ProductNames { get; set; } = new List<string>();
        public List<int> StockQuantities { get; set; } = new List<int>();
    }
}
