using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ABCRetail.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ABCRetail.Services
{
    public interface IInventoryQueueService
    {
        Task<bool> SendMessageAsync(InventoryQueueMessage message);
        Task<List<InventoryQueueMessage>> ReceiveMessagesAsync(int maxMessages = 10);
        Task<bool> DeleteMessageAsync(string messageId, string popReceipt);
        Task<bool> UpdateMessageAsync(string messageId, string popReceipt, InventoryQueueMessage updatedMessage);
        Task<int> GetQueueLengthAsync();
        Task<bool> ClearQueueAsync();
        Task<List<InventoryQueueMessage>> PeekMessagesAsync(int maxMessages = 10);
    }

    public class InventoryQueueService : IInventoryQueueService
    {
        private readonly HttpClient _httpClient;
        private readonly string _queueUrl;
        private readonly ILogger<InventoryQueueService> _logger;

        public InventoryQueueService(IConfiguration configuration, ILogger<InventoryQueueService> logger)
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "ABCRetail-InventoryQueue/1.0");
            _httpClient.DefaultRequestHeaders.Add("x-ms-version", "2020-04-08");
            
            // Use the full queue URL directly since it already includes the queue name and messages endpoint
            _queueUrl = configuration["AzureStorage:QueueSasUrl"];
            
            _logger = logger;
            
            _logger.LogInformation("InventoryQueueService initialized with URL: {QueueUrl}", _queueUrl);
        }

        public async Task<bool> SendMessageAsync(InventoryQueueMessage message)
        {
            try
            {
                _logger.LogInformation("Sending inventory message: {MessageType} for product {ProductName}", 
                    message.Type, message.ProductName);

                // Azure Queue Storage expects XML format with proper structure
                var xmlContent = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<QueueMessage>
    <MessageText>{Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })))}</MessageText>
</QueueMessage>";
                
                var content = new StringContent(xmlContent, Encoding.UTF8, "application/xml");
                
                // Add message to queue using Azure Queue Storage REST API
                // The URL already includes /messages, so we don't need to append it
                var response = await _httpClient.PostAsync(_queueUrl, content);
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Successfully sent inventory message with ID: {MessageId}", message.Id);
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    var errorMessage = $"❌ InventoryQueueService: Failed to send inventory message. Status: {response.StatusCode}, Error: {errorContent}";
                    _logger.LogError(errorMessage);
                    throw new Exception(errorMessage);
                }
            }
            catch (Exception ex)
            {
                var detailedError = $"❌ InventoryQueueService: Error sending inventory message {message.Id}: {ex.Message}";
                if (ex.InnerException != null)
                {
                    detailedError += $"\nInner Exception: {ex.InnerException.Message}";
                }
                detailedError += $"\nStack Trace: {ex.StackTrace}";
                _logger.LogError(detailedError);
                throw new Exception(detailedError, ex);
            }
        }

        public async Task<List<InventoryQueueMessage>> ReceiveMessagesAsync(int maxMessages = 10)
        {
            try
            {
                _logger.LogInformation("Receiving up to {MaxMessages} inventory messages", maxMessages);

                var response = await _httpClient.GetAsync($"{_queueUrl}?numofmessages={maxMessages}&visibilitytimeout=30");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation("Raw queue response: {Content}", content);
                    
                    // Parse XML response from Azure Queue Storage
                    var messages = ParseQueueMessagesFromXml(content);
                    _logger.LogInformation("Successfully parsed {MessageCount} messages from XML", messages.Count);
                    return messages;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    var errorMessage = $"❌ InventoryQueueService: Failed to receive inventory messages. Status: {response.StatusCode}, Error: {errorContent}";
                    _logger.LogError(errorMessage);
                    throw new Exception(errorMessage);
                }
            }
            catch (Exception ex)
            {
                var detailedError = $"❌ InventoryQueueService: Error receiving inventory messages: {ex.Message}";
                if (ex.InnerException != null)
                {
                    detailedError += $"\nInner Exception: {ex.InnerException.Message}";
                }
                detailedError += $"\nStack Trace: {ex.StackTrace}";
                _logger.LogError(detailedError);
                throw new Exception(detailedError, ex);
            }
        }

        public async Task<bool> DeleteMessageAsync(string messageId, string popReceipt)
        {
            try
            {
                _logger.LogInformation("Deleting inventory message: {MessageId}", messageId);

                var response = await _httpClient.DeleteAsync($"{_queueUrl}/{messageId}?popreceipt={popReceipt}");
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Successfully deleted inventory message: {MessageId}", messageId);
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    var errorMessage = $"❌ InventoryQueueService: Failed to delete inventory message. Status: {response.StatusCode}, Error: {errorContent}";
                    _logger.LogError(errorMessage);
                    throw new Exception(errorMessage);
                }
            }
            catch (Exception ex)
            {
                var detailedError = $"❌ InventoryQueueService: Error deleting inventory message {messageId}: {ex.Message}";
                if (ex.InnerException != null)
                {
                    detailedError += $"\nInner Exception: {ex.InnerException.Message}";
                }
                detailedError += $"\nStack Trace: {ex.StackTrace}";
                _logger.LogError(detailedError);
                throw new Exception(detailedError, ex);
            }
        }

        public async Task<bool> UpdateMessageAsync(string messageId, string popReceipt, InventoryQueueMessage updatedMessage)
        {
            try
            {
                _logger.LogInformation("Updating inventory message: {MessageId}", messageId);

                var json = JsonSerializer.Serialize(updatedMessage, new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
                });
                
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PutAsync($"{_queueUrl}/{messageId}?popreceipt={popReceipt}", content);
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Successfully updated inventory message: {MessageId}", messageId);
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    var errorMessage = $"❌ InventoryQueueService: Failed to update inventory message. Status: {response.StatusCode}, Error: {errorContent}";
                    _logger.LogError(errorMessage);
                    throw new Exception(errorMessage);
                }
            }
            catch (Exception ex)
            {
                var detailedError = $"❌ InventoryQueueService: Error updating inventory message {messageId}: {ex.Message}";
                if (ex.InnerException != null)
                {
                    detailedError += $"\nInner Exception: {ex.InnerException.Message}";
                }
                detailedError += $"\nStack Trace: {ex.StackTrace}";
                _logger.LogError(detailedError);
                throw new Exception(detailedError, ex);
            }
        }

        public async Task<int> GetQueueLengthAsync()
        {
            try
            {
                _logger.LogInformation("Getting inventory queue length");

                var response = await _httpClient.GetAsync($"{_queueUrl}?comp=metadata");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation("Queue metadata response: {Content}", content);
                    
                    // Parse the XML response to get approximate message count
                    var messageCount = ParseMessageCountFromXml(content);
                    _logger.LogInformation("Successfully retrieved queue metadata (XML format) - Message count: {Count}", messageCount);
                    return messageCount;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    var errorMessage = $"❌ InventoryQueueService: Failed to get queue length. Status: {response.StatusCode}, Error: {errorContent}";
                    _logger.LogError(errorMessage);
                    throw new Exception(errorMessage);
                }
            }
            catch (Exception ex)
            {
                var detailedError = $"❌ InventoryQueueService: Error getting queue length: {ex.Message}";
                if (ex.InnerException != null)
                {
                    detailedError += $"\nInner Exception: {ex.InnerException.Message}";
                }
                detailedError += $"\nStack Trace: {ex.StackTrace}";
                _logger.LogError(detailedError);
                throw new Exception(detailedError, ex);
            }
        }

        public async Task<bool> ClearQueueAsync()
        {
            try
            {
                _logger.LogInformation("Clearing inventory queue");

                var response = await _httpClient.DeleteAsync(_queueUrl);
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Successfully cleared inventory queue");
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    var errorMessage = $"❌ InventoryQueueService: Failed to clear queue. Status: {response.StatusCode}, Error: {errorContent}";
                    _logger.LogError(errorMessage);
                    throw new Exception(errorMessage);
                }
            }
            catch (Exception ex)
            {
                var detailedError = $"❌ InventoryQueueService: Error clearing queue: {ex.Message}";
                if (ex.InnerException != null)
                {
                    detailedError += $"\nInner Exception: {ex.InnerException.Message}";
                }
                detailedError += $"\nStack Trace: {ex.StackTrace}";
                _logger.LogError(detailedError);
                throw new Exception(detailedError, ex);
            }
        }

        public async Task<List<InventoryQueueMessage>> PeekMessagesAsync(int maxMessages = 10)
        {
            try
            {
                _logger.LogInformation("Peeking at up to {MaxMessages} inventory messages", maxMessages);

                var response = await _httpClient.GetAsync($"{_queueUrl}?numofmessages={maxMessages}&peekonly=true");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation("Raw queue response: {Content}", content);
                    
                    // Parse XML response from Azure Queue Storage
                    var messages = ParseQueueMessagesFromXml(content);
                    _logger.LogInformation("Successfully peeked at {MessageCount} messages from XML", messages.Count);
                    return messages;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    var errorMessage = $"❌ InventoryQueueService: Failed to peek at inventory messages. Status: {response.StatusCode}, Error: {errorContent}";
                    _logger.LogError(errorMessage);
                    throw new Exception(errorMessage);
                }
            }
            catch (Exception ex)
            {
                var detailedError = $"❌ InventoryQueueService: Error peeking at inventory messages: {ex.Message}";
                if (ex.InnerException != null)
                {
                    detailedError += $"\nInner Exception: {ex.InnerException.Message}";
                }
                detailedError += $"\nStack Trace: {ex.StackTrace}";
                _logger.LogError(detailedError);
                throw new Exception(detailedError, ex);
            }
        }

        private List<InventoryQueueMessage> ParseQueueMessagesFromXml(string xmlContent)
        {
            try
            {
                var messages = new List<InventoryQueueMessage>();
                
                // Simple XML parsing for QueueMessagesList
                if (xmlContent.Contains("<QueueMessagesList>") && xmlContent.Contains("<QueueMessage>"))
                {
                    // Extract message IDs and content
                    var messageStart = xmlContent.IndexOf("<QueueMessage>");
                    while (messageStart >= 0)
                    {
                        var messageEnd = xmlContent.IndexOf("</QueueMessage>", messageStart);
                        if (messageEnd < 0) break;
                        
                        var messageXml = xmlContent.Substring(messageStart, messageEnd - messageStart + 16);
                        
                        // Extract MessageId
                        var idStart = messageXml.IndexOf("<MessageId>") + 11;
                        var idEnd = messageXml.IndexOf("</MessageId>");
                        var messageId = idStart < idEnd ? messageXml.Substring(idStart, idEnd - idStart) : Guid.NewGuid().ToString();
                        
                        // Extract MessageText (base64 encoded)
                        var textStart = messageXml.IndexOf("<MessageText>") + 13;
                        var textEnd = messageXml.IndexOf("</MessageText>");
                        var messageText = textStart < textEnd ? messageXml.Substring(textStart, textEnd - textStart) : "";
                        
                        // Decode base64 message text
                        string decodedText = "";
                        try
                        {
                            if (!string.IsNullOrEmpty(messageText))
                            {
                                var bytes = Convert.FromBase64String(messageText);
                                decodedText = System.Text.Encoding.UTF8.GetString(bytes);
                            }
                        }
                        catch
                        {
                            decodedText = messageText; // Use raw text if decoding fails
                        }
                        
                        // Create InventoryQueueMessage from parsed data
                        var message = new InventoryQueueMessage
                        {
                            Id = messageId,
                            Type = "inventory_message",
                            ProductName = decodedText.Contains("Product") ? decodedText : "Unknown Product",
                            Action = decodedText.Contains("updated") ? "update" : "alert",
                            Notes = decodedText,
                            Status = "pending",
                            Priority = "normal",
                            Timestamp = DateTime.UtcNow
                        };
                        
                        messages.Add(message);
                        
                        // Find next message
                        messageStart = xmlContent.IndexOf("<QueueMessage>", messageEnd);
                    }
                }
                
                _logger.LogInformation("Parsed {MessageCount} messages from XML", messages.Count);
                return messages;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing XML queue messages");
                return new List<InventoryQueueMessage>();
            }
        }

        private int ParseMessageCountFromXml(string xmlContent)
        {
            try
            {
                // Look for ApproximateMessageCount in the XML
                if (xmlContent.Contains("<ApproximateMessageCount>"))
                {
                    var start = xmlContent.IndexOf("<ApproximateMessageCount>") + 26;
                    var end = xmlContent.IndexOf("</ApproximateMessageCount>");
                    if (start < end)
                    {
                        var countText = xmlContent.Substring(start, end - start);
                        if (int.TryParse(countText, out int count))
                        {
                            return count;
                        }
                    }
                }
                
                // Fallback: count QueueMessage tags
                var messageCount = 0;
                var index = 0;
                while ((index = xmlContent.IndexOf("<QueueMessage>", index)) >= 0)
                {
                    messageCount++;
                    index += 14;
                }
                
                return messageCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing message count from XML");
                return 0;
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
