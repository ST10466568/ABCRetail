using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Net;
using Azure.Storage.Queues;
using ABCRetail.Functions.Models;
using System.Text.Json;
using System.Text;
using System.Linq;

namespace ABCRetail.Functions.Functions
{
    public class QueueOperationsFunction
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;

        public QueueOperationsFunction(ILoggerFactory loggerFactory, IConfiguration configuration)
        {
            _logger = loggerFactory.CreateLogger<QueueOperationsFunction>();
            _configuration = configuration;
        }

        [Function("QueueOperations")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", "put", "delete", "options", Route = "queue/{operation}")] HttpRequestData req,
            string operation)
        {
            _logger.LogInformation($"QueueOperations function processed a request for operation: {operation}");

            // Handle CORS preflight requests
            if (req.Method == "OPTIONS")
            {
                var response = req.CreateResponse(HttpStatusCode.OK);
                AddCorsHeaders(response);
                return response;
            }

            // Check authentication for non-OPTIONS requests
            if (!IsAuthenticated(req))
            {
                var authResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
                AddCorsHeaders(authResponse);
                await authResponse.WriteStringAsync(JsonSerializer.Serialize(new { error = "Unauthorized" }));
                return authResponse;
            }

            try
            {
                var connectionString = _configuration["AzureStorage:ConnectionString"];
                var queueSasUrl = _configuration["AzureStorage:QueueSasUrl"];
                var queueName = _configuration["AzureStorage:QueueName"] ?? "inventory-queue";

                QueueServiceClient? queueServiceClient = null;

                if (!string.IsNullOrEmpty(queueSasUrl))
                {
                    queueServiceClient = new QueueServiceClient(new Uri(queueSasUrl));
                }
                else if (!string.IsNullOrEmpty(connectionString))
                {
                    queueServiceClient = new QueueServiceClient(connectionString);
                }
                else
                {
                    return await CreateErrorResponse(req, "Azure Queue Storage connection not configured", HttpStatusCode.BadRequest);
                }

                var queueClient = queueServiceClient.GetQueueClient(queueName);
                await queueClient.CreateIfNotExistsAsync();

                switch (operation.ToLower())
                {
                    case "send":
                        return await HandleSend(req, queueClient);
                    case "receive":
                        return await HandleReceive(req, queueClient);
                    case "peek":
                        return await HandlePeek(req, queueClient);
                    case "clear":
                        return await HandleClear(req, queueClient);
                    case "length":
                        return await HandleLength(req, queueClient);
                    default:
                        return await CreateErrorResponse(req, $"Unknown operation: {operation}", HttpStatusCode.BadRequest);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in QueueOperations function");
                return await CreateErrorResponse(req, $"Internal server error: {ex.Message}", HttpStatusCode.InternalServerError);
            }
        }

        private async Task<HttpResponseData> HandleSend(HttpRequestData req, QueueClient queueClient)
        {
            var body = await new StreamReader(req.Body).ReadToEndAsync();
            var message = JsonSerializer.Deserialize<InventoryQueueMessage>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (message == null)
            {
                return await CreateErrorResponse(req, "Invalid message data", HttpStatusCode.BadRequest);
            }

            // Serialize message to JSON and encode as base64 (Azure Queue requirement)
            var jsonMessage = JsonSerializer.Serialize(message, new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
            });
            var base64Message = Convert.ToBase64String(Encoding.UTF8.GetBytes(jsonMessage));

            var response = await queueClient.SendMessageAsync(base64Message);
            
            var httpResponse = req.CreateResponse(HttpStatusCode.OK);
            AddCorsHeaders(httpResponse);
            await httpResponse.WriteStringAsync(JsonSerializer.Serialize(new { 
                message = "Message sent successfully", 
                messageId = response.Value.MessageId,
                popReceipt = response.Value.PopReceipt
            }));
            return httpResponse;
        }

        private async Task<HttpResponseData> HandleReceive(HttpRequestData req, QueueClient queueClient)
        {
            var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
            var maxMessages = int.TryParse(query["maxMessages"], out var max) ? max : 10;

            var messages = new List<InventoryQueueMessage>();
            var response = await queueClient.ReceiveMessagesAsync(maxMessages: maxMessages, visibilityTimeout: TimeSpan.FromSeconds(30));

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
                        inventoryMessage.PopReceipt = queueMessage.PopReceipt;
                        messages.Add(inventoryMessage);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error parsing message {MessageId}", queueMessage.MessageId);
                }
            }

            var httpResponse = req.CreateResponse(HttpStatusCode.OK);
            AddCorsHeaders(httpResponse);
            await httpResponse.WriteStringAsync(JsonSerializer.Serialize(messages));
            return httpResponse;
        }

        private async Task<HttpResponseData> HandlePeek(HttpRequestData req, QueueClient queueClient)
        {
            var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
            var maxMessages = int.TryParse(query["maxMessages"], out var max) ? max : 10;

            var messages = new List<InventoryQueueMessage>();
            var response = await queueClient.PeekMessagesAsync(maxMessages: maxMessages);

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
                        // Add Azure Queue metadata (no pop receipt for peek)
                        inventoryMessage.Id = queueMessage.MessageId;
                        messages.Add(inventoryMessage);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error parsing peeked message {MessageId}", queueMessage.MessageId);
                }
            }

            var httpResponse = req.CreateResponse(HttpStatusCode.OK);
            AddCorsHeaders(httpResponse);
            await httpResponse.WriteStringAsync(JsonSerializer.Serialize(messages));
            return httpResponse;
        }

        private async Task<HttpResponseData> HandleClear(HttpRequestData req, QueueClient queueClient)
        {
            // Azure Queue doesn't have a direct clear method, so we'll delete and recreate
            await queueClient.DeleteIfExistsAsync();
            await queueClient.CreateIfNotExistsAsync();

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteStringAsync(JsonSerializer.Serialize(new { 
                message = "Queue cleared successfully" 
            }));
            return response;
        }

        private async Task<HttpResponseData> HandleLength(HttpRequestData req, QueueClient queueClient)
        {
            var properties = await queueClient.GetPropertiesAsync();
            var messageCount = properties.Value.ApproximateMessagesCount;

            var response = req.CreateResponse(HttpStatusCode.OK);
            AddCorsHeaders(response);
            await response.WriteStringAsync(JsonSerializer.Serialize(new { 
                messageCount = messageCount 
            }));
            return response;
        }

        private async Task<HttpResponseData> CreateErrorResponse(HttpRequestData req, string message, HttpStatusCode statusCode)
        {
            var response = req.CreateResponse(statusCode);
            AddCorsHeaders(response);
            await response.WriteStringAsync(JsonSerializer.Serialize(new { error = message }));
            return response;
        }

        private void AddCorsHeaders(HttpResponseData response)
        {
            response.Headers.Add("Access-Control-Allow-Origin", "*");
            response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
            response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization, x-functions-key");
            response.Headers.Add("Content-Type", "application/json");
        }

        private bool IsAuthenticated(HttpRequestData req)
        {
            // Check for function key in headers
            if (req.Headers.TryGetValues("x-functions-key", out var functionKeys))
            {
                var expectedKey = _configuration["AzureFunctions:FunctionKey"] ?? "DsCwx-G16RtXqJu-VrOodO4Hc6-twvBGRX_8gNA_ftlwAzFuq7z2rg==";
                return functionKeys.Any(key => key == expectedKey);
            }
            return false;
        }
    }
}
