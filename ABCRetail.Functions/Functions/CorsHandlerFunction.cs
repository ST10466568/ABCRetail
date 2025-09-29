using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;

namespace ABCRetail.Functions.Functions
{
    public class CorsHandlerFunction
    {
        private readonly ILogger _logger;

        public CorsHandlerFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<CorsHandlerFunction>();
        }

        [Function("CorsHandler")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "options", Route = "{*path}")] HttpRequestData req)
        {
            _logger.LogInformation("CorsHandler function processed an OPTIONS request");

            var response = req.CreateResponse(HttpStatusCode.OK);
            AddCorsHeaders(response);
            
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
