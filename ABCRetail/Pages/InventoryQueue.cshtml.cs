using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ABCRetail.Models;
using ABCRetail.Services;
using System.Text.Json;

namespace ABCRetail.Pages
{
    public class InventoryQueueModel : PageModel
    {
        private readonly IInventoryQueueService _inventoryQueueService;
        private readonly ILogger<InventoryQueueModel> _logger;

        [BindProperty]
        public InventoryQueueMessage NewMessage { get; set; } = new();

        public List<InventoryQueueMessage> Messages { get; set; } = new();
        public int QueueLength { get; set; }
        public int PendingCount { get; set; }
        public int ProcessingCount { get; set; }
        public int CompletedCount { get; set; }

        public InventoryQueueModel(IInventoryQueueService inventoryQueueService, ILogger<InventoryQueueModel> logger)
        {
            _inventoryQueueService = inventoryQueueService;
            _logger = logger;
        }

        public async Task OnGetAsync()
        {
            try
            {
                _logger.LogInformation("Loading inventory queue data");
                
                // Get queue statistics
                QueueLength = await _inventoryQueueService.GetQueueLengthAsync();
                
                // Get messages from queue
                Messages = await _inventoryQueueService.PeekMessagesAsync(50);
                
                // Calculate counts by status
                PendingCount = Messages.Count(m => m.Status == "pending");
                ProcessingCount = Messages.Count(m => m.Status == "processing");
                CompletedCount = Messages.Count(m => m.Status == "completed");
                
                _logger.LogInformation("Inventory queue loaded successfully. Queue length: {QueueLength}, Messages: {MessageCount}", 
                    QueueLength, Messages.Count);
            }
            catch (Exception ex)
            {
                var detailedError = $"❌ Error loading inventory queue data: {ex.Message}";
                if (ex.InnerException != null)
                {
                    detailedError += $"\nInner Exception: {ex.InnerException.Message}";
                }
                detailedError += $"\nStack Trace: {ex.StackTrace}";
                
                _logger.LogError(detailedError);
                TempData["ErrorMessage"] = detailedError;
                Messages = new List<InventoryQueueMessage>();
            }
        }

        public async Task<IActionResult> OnPostSendMessageAsync()
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid model state for sending inventory message");
                    return Page();
                }

                _logger.LogInformation("Sending inventory message: {MessageType} for product {ProductName}", 
                    NewMessage.Type, NewMessage.ProductName);

                // Set default values
                NewMessage.Id = Guid.NewGuid().ToString();
                NewMessage.Timestamp = DateTime.UtcNow;
                NewMessage.Status = "pending";

                // This will throw an exception if it fails, which will be caught below
                await _inventoryQueueService.SendMessageAsync(NewMessage);
                
                // If we get here, the message was sent successfully
                _logger.LogInformation("Successfully sent inventory message with ID: {MessageId}", NewMessage.Id);
                TempData["SuccessMessage"] = "Inventory message sent successfully!";
                
                // Reset the form
                NewMessage = new InventoryQueueMessage();
                
                // Redirect to refresh the page
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                var detailedError = $"❌ Error sending inventory message: {ex.Message}";
                if (ex.InnerException != null)
                {
                    detailedError += $"\nInner Exception: {ex.InnerException.Message}";
                }
                detailedError += $"\nStack Trace: {ex.StackTrace}";
                
                _logger.LogError(detailedError);
                TempData["ErrorMessage"] = detailedError;
                return Page();
            }
        }

        public async Task<IActionResult> OnPostClearQueueAsync()
        {
            try
            {
                _logger.LogInformation("Clearing inventory queue");
                
                // This will throw an exception if it fails, which will be caught below
                await _inventoryQueueService.ClearQueueAsync();
                
                // If we get here, the queue was cleared successfully
                _logger.LogInformation("Successfully cleared inventory queue");
                TempData["SuccessMessage"] = "Inventory queue cleared successfully!";
                
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                var detailedError = $"❌ Error clearing inventory queue: {ex.Message}";
                if (ex.InnerException != null)
                {
                    detailedError += $"\nInner Exception: {ex.InnerException.Message}";
                }
                detailedError += $"\nStack Trace: {ex.StackTrace}";
                
                _logger.LogError(detailedError);
                TempData["ErrorMessage"] = detailedError;
                return RedirectToPage();
            }
        }

        public async Task<IActionResult> OnPostDeleteMessageAsync([FromBody] DeleteMessageRequest request)
        {
            try
            {
                _logger.LogInformation("Deleting inventory message: {MessageId}", request.MessageId);
                
                // This will throw an exception if it fails, which will be caught below
                await _inventoryQueueService.DeleteMessageAsync(request.MessageId, "placeholder");
                
                // If we get here, the message was deleted successfully
                _logger.LogInformation("Successfully deleted inventory message: {MessageId}", request.MessageId);
                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                var detailedError = $"❌ Error deleting inventory message {request.MessageId}: {ex.Message}";
                if (ex.InnerException != null)
                {
                    detailedError += $"\nInner Exception: {ex.InnerException.Message}";
                }
                detailedError += $"\nStack Trace: {ex.StackTrace}";
                
                _logger.LogError(detailedError);
                return new JsonResult(new { success = false, error = detailedError });
            }
        }

        public async Task<IActionResult> OnPostProcessMessageAsync([FromBody] ProcessMessageRequest request)
        {
            try
            {
                _logger.LogInformation("Processing inventory message: {MessageId}", request.MessageId);
                
                // Find the message in our current list
                var message = Messages.FirstOrDefault(m => m.Id == request.MessageId);
                if (message != null)
                {
                    message.Status = "processing";
                    
                    // For now, we'll just update the local message since we're not implementing full Azure Queue functionality
                    _logger.LogInformation("Successfully marked message as processing: {MessageId}", request.MessageId);
                    return new JsonResult(new { success = true });
                }
                else
                {
                    _logger.LogWarning("Message not found for processing: {MessageId}", request.MessageId);
                    return new JsonResult(new { success = false, error = "Message not found" });
                }
            }
            catch (Exception ex)
            {
                var detailedError = $"❌ Error processing inventory message {request.MessageId}: {ex.Message}";
                if (ex.InnerException != null)
                {
                    detailedError += $"\nInner Exception: {ex.InnerException.Message}";
                }
                detailedError += $"\nStack Trace: {ex.StackTrace}";
                
                _logger.LogError(detailedError);
                return new JsonResult(new { success = false, error = detailedError });
            }
        }

        public async Task<IActionResult> OnGetGetMessageAsync(string messageId)
        {
            try
            {
                _logger.LogInformation("Getting message details: {MessageId}", messageId);
                
                var message = Messages.FirstOrDefault(m => m.Id == messageId);
                if (message != null)
                {
                    return new JsonResult(message);
                }
                else
                {
                    _logger.LogWarning("Message not found: {MessageId}", messageId);
                    return new JsonResult(new { error = "Message not found" });
                }
            }
            catch (Exception ex)
            {
                var detailedError = $"❌ Error getting message details {messageId}: {ex.Message}";
                if (ex.InnerException != null)
                {
                    detailedError += $"\nInner Exception: {ex.InnerException.Message}";
                }
                detailedError += $"\nStack Trace: {ex.StackTrace}";
                
                _logger.LogError(detailedError);
                return new JsonResult(new { error = detailedError });
            }
        }
    }

    public class DeleteMessageRequest
    {
        public string MessageId { get; set; } = string.Empty;
    }

    public class ProcessMessageRequest
    {
        public string MessageId { get; set; } = string.Empty;
    }
}
