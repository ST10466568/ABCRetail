using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;

namespace ABCRetail.Functions.Functions
{
    public class TestFunction
    {
        private readonly ILogger _logger;

        public TestFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<TestFunction>();
        }

        [Function("TestFunction")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "options", Route = "test")] HttpRequestData req)
        {
            _logger.LogInformation("TestFunction processed a request");

            // Handle CORS preflight requests
            if (req.Method == "OPTIONS")
            {
                var optionsResponse = req.CreateResponse(HttpStatusCode.OK);
                AddCorsHeaders(optionsResponse);
                return optionsResponse;
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            AddCorsHeaders(response);
            
            var result = new { message = "Test function is working!", timestamp = DateTime.UtcNow };
            await response.WriteStringAsync(System.Text.Json.JsonSerializer.Serialize(result));
            
            return response;
        }

        private void AddCorsHeaders(HttpResponseData response)
        {
            response.Headers.Add("Access-Control-Allow-Origin", "*");
            response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
            response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization, x-functions-key");
            response.Headers.Add("Content-Type", "application/json");
        }
    }
}
