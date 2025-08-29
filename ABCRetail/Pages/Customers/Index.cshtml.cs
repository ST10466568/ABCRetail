using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ABCRetail.Models;
using ABCRetail.Services;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace ABCRetail.Pages.Customers
{
    public class IndexModel : PageModel
    {
        private readonly WorkingDataFetcher _workingDataFetcher;

        public IndexModel(WorkingDataFetcher workingDataFetcher)
        {
            _workingDataFetcher = workingDataFetcher;
        }

        public List<Customer> Customers { get; set; } = new List<Customer>();
        public List<Customer> PaginatedCustomers { get; set; } = new List<Customer>();
        
        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;
        
        public int PageSize { get; set; } = 10;
        public int TotalPages { get; set; }
        public int TotalCustomers { get; set; }
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;

        public async Task OnGetAsync()
        {
            try
            {
                Customers = await _workingDataFetcher.GetCustomersAsync();
                TotalCustomers = Customers.Count;
                TotalPages = (int)Math.Ceiling((double)TotalCustomers / PageSize);
                
                // Ensure current page is within valid range
                if (CurrentPage < 1) CurrentPage = 1;
                if (CurrentPage > TotalPages) CurrentPage = TotalPages;
                if (TotalPages == 0) CurrentPage = 1;
                
                // Get customers for current page
                var skip = (CurrentPage - 1) * PageSize;
                PaginatedCustomers = Customers.Skip(skip).Take(PageSize).ToList();
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine($"Error loading customers: {ex.Message}");
                Customers = new List<Customer>();
                PaginatedCustomers = new List<Customer>();
            }
        }
    }
}
