using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ABCRetail.Models;
using Azure.Storage.Queues;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ABCRetail.Services
{
    public interface IInventoryQueueService
    {
        Task<bool> SendMessageAsync(InventoryQueueMessage message);
        
        /// <summary>
        /// Receives and removes messages from the queue (use with caution)
        /// </summary>
        Task<List<InventoryQueueMessage>> ReceiveMessagesAsync(int maxMessages = 10);
        
        Task<bool> DeleteMessageAsync(string messageId, string popReceipt);
        Task<bool> UpdateMessageAsync(string messageId, string popReceipt, InventoryQueueMessage updatedMessage);
        Task<int> GetQueueLengthAsync();
        Task<bool> ClearQueueAsync();
        
        /// <summary>
        /// Peeks at messages without removing them (safe for monitoring/display)
        /// </summary>
        Task<List<InventoryQueueMessage>> PeekMessagesAsync(int maxMessages = 10);
        
        Task<bool> TestConnectionAsync();
    }

    public class InventoryQueueService : IInventoryQueueService
    {
        private readonly QueueClient _queueClient;
        private readonly ILogger<InventoryQueueService> _logger;
        private readonly bool _isAzureConnected;

        public InventoryQueueService(IConfiguration configuration, ILogger<InventoryQueueService> logger)
        {
            _logger = logger;
            
            try
            {
                // Try to use the official Azure SDK with connection string first
                var connectionString = configuration.GetConnectionString("AzureStorage");
                _logger.LogInformation("🔍 Connection string found: {HasConnectionString}", !string.IsNullOrEmpty(connectionString));
                
                if (!string.IsNullOrEmpty(connectionString))
                {
                    try
                    {
                        var queueServiceClient = new QueueServiceClient(connectionString);
                        _queueClient = queueServiceClient.GetQueueClient("inventory-queue");
                        _isAzureConnected = true;
                        _logger.LogInformation("✅ InventoryQueueService initialized with Azure SDK using connection string for queue: inventory-queue");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "⚠️ Failed to initialize with connection string, trying SAS URL fallback");
                        _isAzureConnected = false;
                    }
                }
                
                // If connection string failed or wasn't available, try SAS URL approach
                if (!_isAzureConnected)
                {
                    var queueSasUrl = configuration["AzureStorage:QueueSasUrl"];
                    _logger.LogInformation("🔍 SAS URL found: {HasSasUrl}", !string.IsNullOrEmpty(queueSasUrl));
                    
                    if (!string.IsNullOrEmpty(queueSasUrl))
                    {
                        try
                        {
                            _queueClient = new QueueClient(new Uri(queueSasUrl));
                            _isAzureConnected = true;
                            _logger.LogInformation("✅ InventoryQueueService initialized with Azure SDK using SAS URL for queue: inventory-queue");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "❌ Failed to initialize with SAS URL");
                            _isAzureConnected = false;
                        }
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ No connection string or SAS URL found, Azure Queue operations will fail");
                        _isAzureConnected = false;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to initialize Azure Queue client");
                _isAzureConnected = false;
            }
        }

        public async Task<bool> SendMessageAsync(InventoryQueueMessage message)
        {
            if (!_isAzureConnected || _queueClient == null)
            {
                _logger.LogWarning("⚠️ Azure Queue not connected, cannot send message");
                return false;
            }

            try
            {
                _logger.LogInformation("📤 Sending inventory message: {MessageType} for product {ProductName}", 
                    message.Type, message.ProductName);

                // Ensure queue exists
                await _queueClient.CreateIfNotExistsAsync();

                // Serialize message to JSON and encode as base64 (Azure Queue requirement)
                var jsonMessage = JsonSerializer.Serialize(message, new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
                });
                var base64Message = Convert.ToBase64String(Encoding.UTF8.GetBytes(jsonMessage));

                // Send message to Azure Queue
                var response = await _queueClient.SendMessageAsync(base64Message);
                
                if (response.Value != null)
                {
                    _logger.LogInformation("✅ Successfully sent inventory message with ID: {MessageId}", response.Value.MessageId);
                    return true;
                }
                else
                {
                    _logger.LogError("❌ Failed to send message: No response from Azure Queue");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error sending inventory message {MessageId}: {Message}", message.Id, ex.Message);
                return false;
            }
        }

        public async Task<List<InventoryQueueMessage>> ReceiveMessagesAsync(int maxMessages = 10)
        {
            if (!_isAzureConnected || _queueClient == null)
            {
                _logger.LogWarning("⚠️ Azure Queue not connected, returning empty list");
                return new List<InventoryQueueMessage>();
            }

            try
            {
                _logger.LogInformation("📥 Receiving up to {MaxMessages} inventory messages", maxMessages);

                // Ensure queue exists
                await _queueClient.CreateIfNotExistsAsync();

                var messages = new List<InventoryQueueMessage>();
                var response = await _queueClient.ReceiveMessagesAsync(maxMessages: maxMessages, visibilityTimeout: TimeSpan.FromSeconds(30));

                foreach (var queueMessage in response.Value)
                {
                    try
                    {
                        // Decode base64 message and deserialize
                        var jsonBytes = Convert.FromBase64String(queueMessage.MessageText);
                        var jsonMessage = Encoding.UTF8.GetString(jsonBytes);
                        
                        var inventoryMessage = JsonSerializer.Deserialize<InventoryQueueMessage>(jsonMessage, new JsonSerializerOptions 
                        { 
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
                        });

                        if (inventoryMessage != null)
                        {
                            // Add Azure Queue metadata including pop receipt for deletion/updates
                            inventoryMessage.Id = queueMessage.MessageId;
                            inventoryMessage.PopReceipt = queueMessage.PopReceipt; // Store pop receipt for operations
                            messages.Add(inventoryMessage);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "❌ Error parsing message {MessageId}: {Message}", queueMessage.MessageId, ex.Message);
                    }
                }

                _logger.LogInformation("✅ Successfully received {MessageCount} messages", messages.Count);
                return messages;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error receiving inventory messages: {Message}", ex.Message);
                return new List<InventoryQueueMessage>();
            }
        }

        public async Task<bool> DeleteMessageAsync(string messageId, string popReceipt)
        {
            if (!_isAzureConnected || _queueClient == null)
            {
                _logger.LogWarning("⚠️ Azure Queue not connected, cannot delete message");
                return false;
            }

            try
            {
                _logger.LogInformation("🗑️ Deleting inventory message: {MessageId}", messageId);

                var response = await _queueClient.DeleteMessageAsync(messageId, popReceipt);
                
                _logger.LogInformation("✅ Successfully deleted inventory message: {MessageId}", messageId);
                    return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error deleting inventory message {MessageId}: {Message}", messageId, ex.Message);
                return false;
            }
        }

        public async Task<bool> UpdateMessageAsync(string messageId, string popReceipt, InventoryQueueMessage updatedMessage)
        {
            if (!_isAzureConnected || _queueClient == null)
            {
                _logger.LogWarning("⚠️ Azure Queue not connected, cannot update message");
                return false;
            }

            try
            {
                _logger.LogInformation("✏️ Updating inventory message: {MessageId}", messageId);

                // Azure Queue doesn't support direct message updates, so we'll delete and recreate
                // First delete the old message
                await _queueClient.DeleteMessageAsync(messageId, popReceipt);
                
                // Then send the updated message
                var jsonMessage = JsonSerializer.Serialize(updatedMessage, new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
                });
                var base64Message = Convert.ToBase64String(Encoding.UTF8.GetBytes(jsonMessage));
                
                await _queueClient.SendMessageAsync(base64Message);
                
                _logger.LogInformation("✅ Successfully updated inventory message: {MessageId}", messageId);
                    return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error updating inventory message {MessageId}: {Message}", messageId, ex.Message);
                return false;
            }
        }

        // Enhanced method to delete messages with proper error handling
        public async Task<bool> DeleteMessageWithRetryAsync(string messageId, string popReceipt, int maxRetries = 3)
        {
            if (!_isAzureConnected || _queueClient == null)
            {
                _logger.LogWarning("⚠️ Azure Queue not connected, cannot delete message");
                return false;
            }

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    _logger.LogInformation("🗑️ Deleting inventory message: {MessageId} (Attempt {Attempt}/{MaxRetries})", 
                        messageId, attempt, maxRetries);

                    await _queueClient.DeleteMessageAsync(messageId, popReceipt);
                    
                    _logger.LogInformation("✅ Successfully deleted inventory message: {MessageId}", messageId);
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "⚠️ Delete attempt {Attempt} failed for message {MessageId}: {Message}", 
                        attempt, messageId, ex.Message);
                    
                    if (attempt == maxRetries)
                    {
                        _logger.LogError(ex, "❌ All delete attempts failed for message {MessageId}", messageId);
                        return false;
                    }
                    
                    // Wait before retry (exponential backoff)
                    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)));
                }
            }
            
            return false;
        }

        public async Task<int> GetQueueLengthAsync()
        {
            if (!_isAzureConnected || _queueClient == null)
            {
                _logger.LogWarning("⚠️ Azure Queue not connected, returning 0");
                return 0;
            }

            try
            {
                _logger.LogInformation("🔢 Getting inventory queue length");

                // Ensure queue exists
                await _queueClient.CreateIfNotExistsAsync();

                var properties = await _queueClient.GetPropertiesAsync();
                var messageCount = properties.Value.ApproximateMessagesCount;
                
                _logger.LogInformation("✅ Queue length: {MessageCount} messages", messageCount);
                    return messageCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error getting queue length: {Message}", ex.Message);
                return 0;
            }
        }

        public async Task<bool> ClearQueueAsync()
        {
            if (!_isAzureConnected || _queueClient == null)
            {
                _logger.LogWarning("⚠️ Azure Queue not connected, cannot clear queue");
                return false;
            }

            try
            {
                _logger.LogInformation("🧹 Clearing inventory queue");

                // Azure Queue doesn't have a direct clear method, so we'll delete and recreate
                await _queueClient.DeleteIfExistsAsync();
                await _queueClient.CreateIfNotExistsAsync();
                
                _logger.LogInformation("✅ Successfully cleared inventory queue");
                    return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error clearing queue: {Message}", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Peeks at messages in the queue without removing them.
        /// Messages remain in the queue for other consumers.
        /// </summary>
        /// <param name="maxMessages">Maximum number of messages to peek at</param>
        /// <returns>List of messages (read-only view)</returns>
        public async Task<List<InventoryQueueMessage>> PeekMessagesAsync(int maxMessages = 10)
        {
            if (!_isAzureConnected || _queueClient == null)
            {
                _logger.LogWarning("⚠️ Azure Queue not connected, returning empty list");
                return new List<InventoryQueueMessage>();
            }

            try
            {
                _logger.LogInformation("👀 Peeking at up to {MaxMessages} inventory messages (read-only, no messages removed)", maxMessages);

                // Ensure queue exists
                await _queueClient.CreateIfNotExistsAsync();

                var messages = new List<InventoryQueueMessage>();
                var response = await _queueClient.PeekMessagesAsync(maxMessages: maxMessages);

                foreach (var queueMessage in response.Value)
                {
                    try
                    {
                        // Decode base64 message and deserialize
                        var jsonBytes = Convert.FromBase64String(queueMessage.MessageText);
                        var jsonMessage = Encoding.UTF8.GetString(jsonBytes);
                        
                        var inventoryMessage = JsonSerializer.Deserialize<InventoryQueueMessage>(jsonMessage, new JsonSerializerOptions 
                        { 
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
                        });

                        if (inventoryMessage != null)
                        {
                            // Add Azure Queue metadata
                            inventoryMessage.Id = queueMessage.MessageId;
                            messages.Add(inventoryMessage);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "❌ Error parsing peeked message {MessageId}: {Message}", queueMessage.MessageId, ex.Message);
                    }
                }

                _logger.LogInformation("✅ Successfully peeked at {MessageCount} messages", messages.Count);
                return messages;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error peeking at inventory messages: {Message}", ex.Message);
                return new List<InventoryQueueMessage>();
            }
        }

        public async Task<bool> TestConnectionAsync()
        {
            if (!_isAzureConnected || _queueClient == null)
            {
                _logger.LogWarning("⚠️ Azure Queue not connected, cannot test connection");
                return false;
            }

            try
            {
                _logger.LogInformation("🔍 Testing Azure Queue connection...");

                // Try to get queue properties to test connection
                var properties = await _queueClient.GetPropertiesAsync();
                var messageCount = properties.Value.ApproximateMessagesCount;
                
                _logger.LogInformation("✅ Connection test successful! Queue length: {MessageCount} messages", messageCount);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Connection test failed: {Message}", ex.Message);
                return false;
            }
        }

        public void Dispose()
        {
            // Azure SDK handles disposal automatically
        }
    }
}
