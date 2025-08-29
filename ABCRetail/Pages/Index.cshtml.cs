using Microsoft.AspNetCore.Mvc.RazorPages;
using ABCRetail.Models;
using ABCRetail.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ABCRetail.Pages
{
    public class IndexModel : PageModel
    {
        private readonly WorkingDataFetcher _workingDataFetcher;

        public IndexModel(WorkingDataFetcher workingDataFetcher)
        {
            _workingDataFetcher = workingDataFetcher;
        }

        public List<Customer> Customers { get; set; } = new List<Customer>();
        public List<Product> Products { get; set; } = new List<Product>();
        public int CustomerCount => Customers.Count;
        public int ProductCount => Products.Count;

        public async Task OnGetAsync()
        {
            try
            {
                Customers = await _workingDataFetcher.GetCustomersAsync();
                Products = await _workingDataFetcher.GetProductsAsync();
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine($"Error loading dashboard data: {ex.Message}");
                Customers = new List<Customer>();
                Products = new List<Product>();
            }
        }
    }
}
