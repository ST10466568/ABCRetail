using Microsoft.AspNetCore.Mvc.RazorPages;
using ABCRetail.Models;
using ABCRetail.Services;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace ABCRetail.Pages
{
    public class IndexModel : PageModel
    {
        private readonly IAzureTableService _azureTableService;

        public IndexModel(IAzureTableService azureTableService)
        {
            _azureTableService = azureTableService;
        }

        public List<Customer> Customers { get; set; } = new List<Customer>();
        public List<Product> Products { get; set; } = new List<Product>();
        public int CustomerCount => Customers.Count;
        public int ProductCount => Products.Count;

        public async Task OnGetAsync()
        {
            try
            {
                Console.WriteLine("Home Page: Starting to fetch data for dashboard...");
                
                // Fetch customers and products using AzureTableService (with fallback)
                var customersTask = _azureTableService.GetAllEntitiesAsync<Customer>();
                var productsTask = _azureTableService.GetAllEntitiesAsync<Product>();
                
                // Wait for both tasks to complete
                await Task.WhenAll(customersTask, productsTask);
                
                Customers = customersTask.Result.ToList();
                Products = productsTask.Result.ToList();
                
                Console.WriteLine($"Home Page: Successfully retrieved {CustomerCount} customers and {ProductCount} products");
            }
            catch (System.Exception ex)
            {
                Console.WriteLine($"Home Page: Error fetching data: {ex.Message}");
                Customers = new List<Customer>();
                Products = new List<Product>();
            }
        }
    }
}
