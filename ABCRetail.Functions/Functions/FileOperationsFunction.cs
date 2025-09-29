using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Net;
using Azure.Storage.Files.Shares;
using System.Text.Json;
using System.Linq;

namespace ABCRetail.Functions.Functions
{
    public class FileOperationsFunction
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;

        public FileOperationsFunction(ILoggerFactory loggerFactory, IConfiguration configuration)
        {
            _logger = loggerFactory.CreateLogger<FileOperationsFunction>();
            _configuration = configuration;
        }

        [Function("FileOperations")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", "put", "delete", "options", Route = "file/{operation}")] HttpRequestData req,
            string operation)
        {
            _logger.LogInformation($"FileOperations function processed a request for operation: {operation}");

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
                var fileSasUrl = _configuration["AzureStorage:FileSasUrl"];
                var shareName = _configuration["AzureStorage:ShareName"] ?? "applogs";

                ShareServiceClient? shareServiceClient = null;

                if (!string.IsNullOrEmpty(fileSasUrl))
                {
                    shareServiceClient = new ShareServiceClient(new Uri(fileSasUrl));
                }
                else if (!string.IsNullOrEmpty(connectionString))
                {
                    shareServiceClient = new ShareServiceClient(connectionString);
                }
                else
                {
                    return await CreateErrorResponse(req, "Azure File Storage connection not configured", HttpStatusCode.BadRequest);
                }

                var shareClient = shareServiceClient.GetShareClient(shareName);
                await shareClient.CreateIfNotExistsAsync();

                var directoryClient = shareClient.GetRootDirectoryClient();

                switch (operation.ToLower())
                {
                    case "write":
                        return await HandleWrite(req, directoryClient);
                    case "read":
                        return await HandleRead(req, directoryClient);
                    case "list":
                        return await HandleList(req, directoryClient);
                    case "delete":
                        return await HandleDelete(req, directoryClient);
                    case "download":
                        return await HandleDownload(req, directoryClient);
                    default:
                        return await CreateErrorResponse(req, $"Unknown operation: {operation}", HttpStatusCode.BadRequest);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in FileOperations function");
                return await CreateErrorResponse(req, $"Internal server error: {ex.Message}", HttpStatusCode.InternalServerError);
            }
        }

        private async Task<HttpResponseData> HandleWrite(HttpRequestData req, ShareDirectoryClient directoryClient)
        {
            var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
            var fileName = query["fileName"];

            if (string.IsNullOrEmpty(fileName))
            {
                return await CreateErrorResponse(req, "fileName parameter is required", HttpStatusCode.BadRequest);
            }

            var body = await new StreamReader(req.Body).ReadToEndAsync();
            var fileData = JsonSerializer.Deserialize<FileWriteRequest>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (fileData == null || string.IsNullOrEmpty(fileData.Content))
            {
                return await CreateErrorResponse(req, "Invalid file data or content is required", HttpStatusCode.BadRequest);
            }

            var fileClient = directoryClient.GetFileClient(fileName);
            
            // Decode base64 content if provided
            byte[] contentBytes;
            if (fileData.IsBase64)
            {
                contentBytes = Convert.FromBase64String(fileData.Content);
            }
            else
            {
                contentBytes = System.Text.Encoding.UTF8.GetBytes(fileData.Content);
            }

            using var stream = new MemoryStream(contentBytes);
            await fileClient.CreateAsync(stream.Length);
            await fileClient.UploadRangeAsync(new Azure.HttpRange(0, stream.Length), stream);

            var response = req.CreateResponse(HttpStatusCode.OK);
            AddCorsHeaders(response);
            await response.WriteStringAsync(JsonSerializer.Serialize(new { 
                message = "File written successfully", 
                fileName = fileName,
                size = contentBytes.Length
            }));
            return response;
        }

        private async Task<HttpResponseData> HandleRead(HttpRequestData req, ShareDirectoryClient directoryClient)
        {
            var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
            var fileName = query["fileName"];

            if (string.IsNullOrEmpty(fileName))
            {
                return await CreateErrorResponse(req, "fileName parameter is required", HttpStatusCode.BadRequest);
            }

            var fileClient = directoryClient.GetFileClient(fileName);

            if (!await fileClient.ExistsAsync())
            {
                return await CreateErrorResponse(req, "File not found", HttpStatusCode.NotFound);
            }

            var response = await fileClient.DownloadAsync();
            using var streamReader = new StreamReader(response.Value.Content);
            var content = await streamReader.ReadToEndAsync();

            var httpResponse = req.CreateResponse(HttpStatusCode.OK);
            AddCorsHeaders(httpResponse);
            await httpResponse.WriteStringAsync(JsonSerializer.Serialize(new { 
                fileName = fileName,
                content = content,
                size = content.Length
            }));
            return httpResponse;
        }

        private async Task<HttpResponseData> HandleList(HttpRequestData req, ShareDirectoryClient directoryClient)
        {
            var files = new List<object>();
            await foreach (var fileItem in directoryClient.GetFilesAndDirectoriesAsync())
            {
                if (fileItem.IsDirectory == false)
                {
                    files.Add(new
                    {
                        name = fileItem.Name,
                        isDirectory = fileItem.IsDirectory
                    });
                }
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            AddCorsHeaders(response);
            await response.WriteStringAsync(JsonSerializer.Serialize(files));
            return response;
        }

        private async Task<HttpResponseData> HandleDelete(HttpRequestData req, ShareDirectoryClient directoryClient)
        {
            var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
            var fileName = query["fileName"];

            if (string.IsNullOrEmpty(fileName))
            {
                return await CreateErrorResponse(req, "fileName parameter is required", HttpStatusCode.BadRequest);
            }

            var fileClient = directoryClient.GetFileClient(fileName);
            var deleted = await fileClient.DeleteIfExistsAsync();

            var response = req.CreateResponse(HttpStatusCode.OK);
            AddCorsHeaders(response);
            await response.WriteStringAsync(JsonSerializer.Serialize(new { 
                message = deleted ? "File deleted successfully" : "File not found",
                fileName = fileName
            }));
            return response;
        }

        private async Task<HttpResponseData> HandleDownload(HttpRequestData req, ShareDirectoryClient directoryClient)
        {
            var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
            var fileName = query["fileName"];

            if (string.IsNullOrEmpty(fileName))
            {
                return await CreateErrorResponse(req, "fileName parameter is required", HttpStatusCode.BadRequest);
            }

            var fileClient = directoryClient.GetFileClient(fileName);

            if (!await fileClient.ExistsAsync())
            {
                return await CreateErrorResponse(req, "File not found", HttpStatusCode.NotFound);
            }

            var response = await fileClient.DownloadAsync();
            using var memoryStream = new MemoryStream();
            await response.Value.Content.CopyToAsync(memoryStream);
            var content = Convert.ToBase64String(memoryStream.ToArray());

            var httpResponse = req.CreateResponse(HttpStatusCode.OK);
            AddCorsHeaders(httpResponse);
            await httpResponse.WriteStringAsync(JsonSerializer.Serialize(new { 
                fileName = fileName,
                content = content,
                size = memoryStream.Length
            }));
            return httpResponse;
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

    public class FileWriteRequest
    {
        public string Content { get; set; } = string.Empty;
        public bool IsBase64 { get; set; } = false;
    }
}
