using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Net;
using Azure.Data.Tables;
using ABCRetail.Functions.Models;
using System.Text.Json;
using System.Linq;

namespace ABCRetail.Functions.Functions
{
    public class TableOperationsFunction
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;

        public TableOperationsFunction(ILoggerFactory loggerFactory, IConfiguration configuration)
        {
            _logger = loggerFactory.CreateLogger<TableOperationsFunction>();
            _configuration = configuration;
        }

        [Function("TableOperations")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", "put", "delete", "options", Route = "table/{operation}")] HttpRequestData req,
            string operation)
        {
            _logger.LogInformation($"TableOperations function processed a request for operation: {operation}");

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
                if (string.IsNullOrEmpty(connectionString))
                {
                    return await CreateErrorResponse(req, "Azure Storage connection string not configured", HttpStatusCode.BadRequest);
                }

                var tableServiceClient = new TableServiceClient(connectionString);
                var tableName = _configuration["AzureStorage:TableName"] ?? "Customers";
                var tableClient = tableServiceClient.GetTableClient(tableName);

                // Ensure table exists
                await tableClient.CreateIfNotExistsAsync();

                switch (operation.ToLower())
                {
                    case "create":
                        return await HandleCreate(req, tableClient);
                    case "read":
                        return await HandleRead(req, tableClient);
                    case "update":
                        return await HandleUpdate(req, tableClient);
                    case "delete":
                        return await HandleDelete(req, tableClient);
                    case "list":
                        return await HandleList(req, tableClient);
                    default:
                        return await CreateErrorResponse(req, $"Unknown operation: {operation}", HttpStatusCode.BadRequest);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in TableOperations function");
                return await CreateErrorResponse(req, $"Internal server error: {ex.Message}", HttpStatusCode.InternalServerError);
            }
        }

        private async Task<HttpResponseData> HandleCreate(HttpRequestData req, TableClient tableClient)
        {
            var body = await new StreamReader(req.Body).ReadToEndAsync();
            var customer = JsonSerializer.Deserialize<Customer>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (customer == null)
            {
                return await CreateErrorResponse(req, "Invalid customer data", HttpStatusCode.BadRequest);
            }

            customer.RowKey = Guid.NewGuid().ToString();
            customer.PartitionKey = "CUSTOMER";
            customer.Timestamp = DateTimeOffset.UtcNow;

            await tableClient.AddEntityAsync(customer);

            var response = req.CreateResponse(HttpStatusCode.Created);
            AddCorsHeaders(response);
            await response.WriteStringAsync(JsonSerializer.Serialize(new { 
                message = "Customer created successfully", 
                id = customer.RowKey 
            }));
            return response;
        }

        private async Task<HttpResponseData> HandleRead(HttpRequestData req, TableClient tableClient)
        {
            var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
            var partitionKey = query["partitionKey"];
            var rowKey = query["rowKey"];

            if (string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(rowKey))
            {
                return await CreateErrorResponse(req, "partitionKey and rowKey are required", HttpStatusCode.BadRequest);
            }

            var entity = await tableClient.GetEntityAsync<Customer>(partitionKey, rowKey);
            
            var response = req.CreateResponse(HttpStatusCode.OK);
            AddCorsHeaders(response);
            await response.WriteStringAsync(JsonSerializer.Serialize(entity.Value));
            return response;
        }

        private async Task<HttpResponseData> HandleUpdate(HttpRequestData req, TableClient tableClient)
        {
            var body = await new StreamReader(req.Body).ReadToEndAsync();
            var customer = JsonSerializer.Deserialize<Customer>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (customer == null || string.IsNullOrEmpty(customer.PartitionKey) || string.IsNullOrEmpty(customer.RowKey))
            {
                return await CreateErrorResponse(req, "Invalid customer data or missing keys", HttpStatusCode.BadRequest);
            }

            customer.LastModified = DateTime.UtcNow;
            await tableClient.UpdateEntityAsync(customer, customer.ETag, TableUpdateMode.Replace);

            var response = req.CreateResponse(HttpStatusCode.OK);
            AddCorsHeaders(response);
            await response.WriteStringAsync(JsonSerializer.Serialize(new { 
                message = "Customer updated successfully", 
                id = customer.RowKey 
            }));
            return response;
        }

        private async Task<HttpResponseData> HandleDelete(HttpRequestData req, TableClient tableClient)
        {
            var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
            var partitionKey = query["partitionKey"];
            var rowKey = query["rowKey"];

            if (string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(rowKey))
            {
                return await CreateErrorResponse(req, "partitionKey and rowKey are required", HttpStatusCode.BadRequest);
            }

            await tableClient.DeleteEntityAsync(partitionKey, rowKey);

            var response = req.CreateResponse(HttpStatusCode.OK);
            AddCorsHeaders(response);
            await response.WriteStringAsync(JsonSerializer.Serialize(new { 
                message = "Customer deleted successfully" 
            }));
            return response;
        }

        private async Task<HttpResponseData> HandleList(HttpRequestData req, TableClient tableClient)
        {
            var customers = new List<Customer>();
            await foreach (var customer in tableClient.QueryAsync<Customer>())
            {
                customers.Add(customer);
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            AddCorsHeaders(response);
            await response.WriteStringAsync(JsonSerializer.Serialize(customers));
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
                var expectedKey = _configuration["AzureFunctions:FunctionKey"] ?? "YOUR_FUNCTION_KEY";
                return functionKeys.Any(key => key == expectedKey);
            }
            return false;
        }
    }
}
