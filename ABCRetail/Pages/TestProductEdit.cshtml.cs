using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ABCRetail.Models;
using ABCRetail.Services;
using System.Text.Json;

namespace ABCRetail.Pages
{
    public class TestProductEditModel : PageModel
    {
        private readonly IAzureTableServiceV2 _tableService;
        private readonly ILogger<TestProductEditModel> _logger;

        public TestProductEditModel(IAzureTableServiceV2 tableService, ILogger<TestProductEditModel> logger)
        {
            _tableService = tableService;
            _logger = logger;
        }

        [BindProperty]
        public List<Product> Products { get; set; } = new List<Product>();

        [BindProperty]
        public string TestResult { get; set; } = "";

        [BindProperty]
        public string UpdateResult { get; set; } = "";
        
        [BindProperty]
        public string RecoveryResult { get; set; } = "";
        
        [BindProperty]
        public string DiagnosisResult { get; set; } = "";

        [BindProperty]
        public string DeleteResult { get; set; } = "";

        [BindProperty]
        public string DeleteCustomerResult { get; set; } = "";

        public async Task OnGetAsync()
        {
            try
            {
                _logger.LogInformation("🔍 TestProductEdit page loaded - testing product functionality");
                await LoadProductsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error loading TestProductEdit page");
                TestResult = $"Error loading page: {ex.Message}";
            }
        }

        public async Task<IActionResult> OnPostListProductsAsync()
        {
            try
            {
                _logger.LogInformation("📋 Listing all products for testing");
                await LoadProductsAsync();
                TestResult = $"✅ Successfully loaded {Products.Count} products";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error listing products");
                TestResult = $"❌ Error listing products: {ex.Message}\n\nStack Trace: {ex.StackTrace}";
            }
            return Page();
        }

        public async Task<IActionResult> OnPostTestGetProductAsync(string productId)
        {
            try
            {
                _logger.LogInformation("🔍 Testing GetProductAsync with ID: {ProductId}", productId);
                
                if (string.IsNullOrEmpty(productId))
                {
                    TestResult = "❌ Product ID is required";
                    return Page();
                }

                var product = await _tableService.GetProductAsync(productId);
                
                if (product != null)
                {
                    TestResult = $"✅ Product found successfully!\n\n" +
                                $"ID: {product.RowKey}\n" +
                                $"Name: {product.Name}\n" +
                                $"Price: ${product.Price}\n" +
                                $"Category: {product.Category}\n" +
                                $"PartitionKey: {product.PartitionKey}\n" +
                                $"Created: {product.CreatedDate}\n" +
                                $"Last Modified: {product.LastModifiedDate}";
                    
                    _logger.LogInformation("✅ Product retrieved successfully: {ProductName}", product.Name);
                }
                else
                {
                    TestResult = $"⚠️ Product with ID '{productId}' not found";
                    _logger.LogWarning("⚠️ Product with ID {ProductId} not found", productId);
                }
            }
            catch (Exception ex)
            {
                var detailedError = $"❌ Error testing GetProductAsync:\n" +
                                  $"Exception: {ex.Message}\n";
                
                if (ex.InnerException != null)
                {
                    detailedError += $"Inner Exception: {ex.InnerException.Message}\n";
                }
                
                detailedError += $"Stack Trace: {ex.StackTrace}";
                
                TestResult = detailedError;
                _logger.LogError(ex, "❌ Error testing GetProductAsync for ID: {ProductId}", productId);
            }
            
            await LoadProductsAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostTestUpdateProductAsync(string productId, string newName, decimal newPrice)
        {
            try
            {
                _logger.LogInformation("🔧 Testing UpdateProductAsync with ID: {ProductId}", productId);
                
                if (string.IsNullOrEmpty(productId))
                {
                    UpdateResult = "❌ Product ID is required";
                    return Page();
                }

                // First, get the existing product
                var existingProduct = await _tableService.GetProductAsync(productId);
                
                if (existingProduct == null)
                {
                    UpdateResult = $"❌ Cannot update: Product with ID '{productId}' not found";
                    _logger.LogWarning("⚠️ Cannot update product with ID {ProductId} - not found", productId);
                    return Page();
                }

                // Store original values for comparison
                var originalName = existingProduct.Name;
                var originalPrice = existingProduct.Price;

                // Update the product
                existingProduct.Name = newName ?? existingProduct.Name;
                existingProduct.Price = newPrice > 0 ? newPrice : existingProduct.Price;
                
                _logger.LogInformation("🔧 Attempting to update product: {ProductName} -> {NewName}, Price: {OriginalPrice} -> {NewPrice}", 
                    originalName, existingProduct.Name, originalPrice, existingProduct.Price);

                var updateSuccess = await _tableService.UpdateProductAsync(existingProduct);
                
                if (updateSuccess)
                {
                    // Verify the update by retrieving the product again
                    var updatedProduct = await _tableService.GetProductAsync(productId);
                    
                    if (updatedProduct != null)
                    {
                        UpdateResult = $"🎉 Product updated successfully!\n\n" +
                                      $"Original Name: {originalName}\n" +
                                      $"New Name: {updatedProduct.Name}\n" +
                                      $"Original Price: ${originalPrice}\n" +
                                      $"New Price: ${updatedProduct.Price}\n" +
                                      $"Last Modified: {updatedProduct.LastModifiedDate}\n" +
                                      $"Update Success: {updateSuccess}";
                        
                        _logger.LogInformation("✅ Product updated successfully and verified: {ProductName}", updatedProduct.Name);
                    }
                    else
                    {
                        UpdateResult = $"⚠️ Product updated but could not be retrieved for verification";
                        _logger.LogWarning("⚠️ Product updated but could not be retrieved for verification");
                    }
                }
                else
                {
                    UpdateResult = $"❌ Product update failed - UpdateProductAsync returned false";
                    _logger.LogError("❌ Product update failed - UpdateProductAsync returned false");
                }
            }
            catch (Exception ex)
            {
                var detailedError = $"❌ Error testing UpdateProductAsync:\n" +
                                  $"Exception: {ex.Message}\n";
                
                if (ex.InnerException != null)
                {
                    detailedError += $"Inner Exception: {ex.InnerException.Message}\n";
                }
                
                detailedError += $"Stack Trace: {ex.StackTrace}";
                
                UpdateResult = detailedError;
                _logger.LogError(ex, "❌ Error testing UpdateProductAsync for ID: {ProductId}", productId);
            }
            
            await LoadProductsAsync();
            return Page();
        }

        private async Task LoadProductsAsync()
        {
            try
            {
                Products = await _tableService.GetAllProductsAsync();
                _logger.LogInformation("📦 Loaded {Count} products for testing", Products.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error loading products");
                Products = new List<Product>();
            }
        }
        
        public async Task<IActionResult> OnPostRecoverProductAsync(string productId)
        {
            try
            {
                _logger.LogInformation("🔍 Attempting to recover missing product: {ProductId}", productId);
                
                if (string.IsNullOrEmpty(productId))
                {
                    RecoveryResult = "❌ Product ID is required";
                    return Page();
                }
                
                var recoveredProduct = await _tableService.RecoverMissingProductAsync(productId);
                
                if (recoveredProduct != null)
                {
                    RecoveryResult = $"🎉 Product recovered successfully!\n\n" +
                                   $"ID: {recoveredProduct.RowKey}\n" +
                                   $"Name: {recoveredProduct.Name}\n" +
                                   $"Price: ${recoveredProduct.Price}\n" +
                                   $"Category: {recoveredProduct.Category}\n" +
                                   $"PartitionKey: {recoveredProduct.PartitionKey}\n" +
                                   $"ETag: {recoveredProduct.ETag}\n" +
                                   $"Created: {recoveredProduct.CreatedDate}\n" +
                                   $"Last Modified: {recoveredProduct.LastModifiedDate}";
                    
                    _logger.LogInformation("✅ Product recovered successfully: {ProductName}", recoveredProduct.Name);
                }
                else
                {
                    RecoveryResult = $"❌ Could not recover product with ID: {productId}\n\n" +
                                   $"The product may have been permanently deleted or corrupted.";
                    _logger.LogWarning("⚠️ Could not recover product with ID {ProductId}", productId);
                }
            }
            catch (Exception ex)
            {
                var detailedError = $"❌ Error during product recovery:\n" +
                                  $"Exception: {ex.Message}\n";
                
                if (ex.InnerException != null)
                {
                    detailedError += $"Inner Exception: {ex.InnerException.Message}\n";
                }
                
                detailedError += $"Stack Trace: {ex.StackTrace}";
                
                RecoveryResult = detailedError;
                _logger.LogError(ex, "❌ Error during product recovery for ID: {ProductId}", productId);
            }
            
            await LoadProductsAsync();
            return Page();
        }
        
        public async Task<IActionResult> OnPostDiagnoseProductAsync(string productId)
        {
            try
            {
                _logger.LogInformation("🔬 Diagnosing product status: {ProductId}", productId);
                
                if (string.IsNullOrEmpty(productId))
                {
                    DiagnosisResult = "❌ Product ID is required";
                    return Page();
                }
                
                var diagnosis = await _tableService.DiagnoseProductStatusAsync(productId);
                DiagnosisResult = diagnosis;
                
                _logger.LogInformation("🔬 Product diagnosis completed for ID: {ProductId}", productId);
            }
            catch (Exception ex)
            {
                var detailedError = $"❌ Error during product diagnosis:\n" +
                                  $"Exception: {ex.Message}\n";
                
                if (ex.InnerException != null)
                {
                    detailedError += $"Inner Exception: {ex.InnerException.Message}\n";
                }
                
                detailedError += $"Stack Trace: {ex.StackTrace}";
                
                DiagnosisResult = detailedError;
                _logger.LogError(ex, "❌ Error during product diagnosis for ID: {ProductId}", productId);
            }
            
            await LoadProductsAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostTestDeleteProductAsync(string productId)
        {
            try
            {
                _logger.LogInformation("🗑️ Testing delete product: {ProductId}", productId);
                
                // First, verify the product exists
                var existingProduct = await _tableService.GetProductAsync(productId);
                if (existingProduct == null)
                {
                    DeleteResult = $"❌ Product with ID {productId} not found - cannot delete";
                    return Page();
                }

                // Attempt to delete the product
                var deleteSuccess = await _tableService.DeleteProductAsync(productId);
                
                if (deleteSuccess)
                {
                    // Verify deletion by trying to retrieve the product again
                    var deletedProduct = await _tableService.GetProductAsync(productId);
                    if (deletedProduct == null)
                    {
                        DeleteResult = $"✅ Product '{existingProduct.Name}' (ID: {productId}) deleted successfully and verified";
                    }
                    else
                    {
                        DeleteResult = $"⚠️ Product deletion reported success but product still exists. Product: {deletedProduct.Name}";
                    }
                }
                else
                {
                    DeleteResult = $"❌ Product deletion failed for ID: {productId}";
                }
                
                return Page();
            }
            catch (Exception ex)
            {
                DeleteResult = $"❌ Error during product deletion test: {ex.Message}\n\nStack Trace: {ex.StackTrace}";
                return Page();
            }
        }

        public async Task<IActionResult> OnPostTestDeleteCustomerAsync(string customerId)
        {
            try
            {
                _logger.LogInformation("🗑️ Testing delete customer: {CustomerId}", customerId);
                
                // First, verify the customer exists
                var existingCustomer = await _tableService.GetCustomerAsync(customerId);
                if (existingCustomer == null)
                {
                    DeleteCustomerResult = $"❌ Customer with ID {customerId} not found - cannot delete";
                    return Page();
                }

                // Attempt to delete the customer
                var deleteSuccess = await _tableService.DeleteCustomerAsync(customerId);
                
                if (deleteSuccess)
                {
                    // Verify deletion by trying to retrieve the customer again
                    var deletedCustomer = await _tableService.GetCustomerAsync(customerId);
                    if (deletedCustomer == null)
                    {
                        DeleteCustomerResult = $"✅ Customer '{existingCustomer.FullName}' (ID: {customerId}) deleted successfully and verified";
                    }
                    else
                    {
                        DeleteCustomerResult = $"⚠️ Customer deletion reported success but customer still exists. Customer: {deletedCustomer.FullName}";
                    }
                }
                else
                {
                    DeleteCustomerResult = $"❌ Customer deletion failed for ID: {customerId}";
                }
                
                return Page();
            }
            catch (Exception ex)
            {
                DeleteCustomerResult = $"❌ Error during customer deletion test: {ex.Message}\n\nStack Trace: {ex.StackTrace}";
                return Page();
            }
        }
    }
}
