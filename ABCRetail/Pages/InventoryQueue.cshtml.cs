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
        public List<InventoryQueueMessage> PaginatedMessages { get; set; } = new();
        public int QueueLength { get; set; }
        public int PendingCount { get; set; }
        public int ProcessingCount { get; set; }
        public int CompletedCount { get; set; }
        
        // Pagination properties
        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;
        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 5;
        public int TotalPages { get; set; }
        public int TotalMessages { get; set; }
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;

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
                
                // First test the connection
                _logger.LogInformation("🔍 Testing Azure Queue connection...");
                var connectionTest = await _inventoryQueueService.TestConnectionAsync();
                if (!connectionTest)
                {
                    _logger.LogWarning("⚠️ Azure Queue connection test failed");
                    TempData["ErrorMessage"] = "Azure Queue connection test failed. Please check configuration and permissions.";
                }
                else
                {
                    _logger.LogInformation("✅ Azure Queue connection test successful");
                }
                
                // Get queue statistics
                QueueLength = await _inventoryQueueService.GetQueueLengthAsync();
                _logger.LogInformation("Queue length retrieved: {QueueLength}", QueueLength);
                
                // Get messages from queue using PEEK (read-only, no messages removed from queue)
                // This ensures messages remain in the queue for other consumers
                // Azure Queue Storage limits peek to 32 messages maximum
                Messages = await _inventoryQueueService.PeekMessagesAsync(32);
                _logger.LogInformation("Messages peeked from queue: {MessageCount} (messages remain in queue, max 32 allowed)", Messages.Count);
                foreach (var msg in Messages)
                {
                    _logger.LogInformation("[QUEUE FRONTEND] Peeked message: ID={Id}, Type={Type}, Product={ProductName}, Action={Action}, Status={Status}, Timestamp={Timestamp}",
                        msg.Id, msg.Type, msg.ProductName, msg.Action, msg.Status, msg.Timestamp);
                }
                
                // Sort messages by timestamp (latest first)
                Messages = Messages.OrderByDescending(m => m.Timestamp).ToList();
                
                // Calculate counts by status
                PendingCount = Messages.Count(m => m.Status == "pending");
                ProcessingCount = Messages.Count(m => m.Status == "processing");
                CompletedCount = Messages.Count(m => m.Status == "completed");
                
                // Implement pagination
                TotalMessages = Messages.Count;
                TotalPages = (int)Math.Ceiling((double)TotalMessages / PageSize);
                
                // Ensure current page is within valid range
                if (CurrentPage < 1) CurrentPage = 1;
                if (CurrentPage > TotalPages) CurrentPage = TotalPages;
                if (TotalPages == 0) CurrentPage = 1;
                
                // Get messages for current page
                var skip = (CurrentPage - 1) * PageSize;
                PaginatedMessages = Messages.Skip(skip).Take(PageSize).ToList();
                
                _logger.LogInformation("Inventory queue loaded successfully. Queue length: {QueueLength}, Total Messages: {TotalMessages}, Current Page: {CurrentPage}/{TotalPages}, Page Messages: {PageMessageCount}", 
                    QueueLength, TotalMessages, CurrentPage, TotalPages, PaginatedMessages.Count);
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
                PaginatedMessages = new List<InventoryQueueMessage>();
            }
        }

        public async Task<IActionResult> OnPostSendMessageAsync()
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid model state for sending inventory message");
                    return new JsonResult(new { success = false, error = "Invalid model state" });
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
                
                // Reset the form
                NewMessage = new InventoryQueueMessage();
                
                return new JsonResult(new { success = true, message = "Inventory message sent successfully!" });
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
                return new JsonResult(new { success = false, error = detailedError });
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
                
                return new JsonResult(new { success = true, message = "Inventory queue cleared successfully!" });
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
                return new JsonResult(new { success = false, error = detailedError });
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
