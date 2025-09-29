using Microsoft.AspNetCore.Mvc;
using System.Text;
using ABCRetail.Services;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace ABCRetail.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CorsProxyController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<CorsProxyController> _logger;
        private readonly IAzureTableService _tableService;
        private readonly IAzureBlobService _blobService;
        private readonly IAzureQueueService _queueService;
        private readonly IAzureFileService _fileService;

        public CorsProxyController(
            HttpClient httpClient, 
            IConfiguration configuration, 
            ILogger<CorsProxyController> logger,
            IAzureTableService tableService,
            IAzureBlobService blobService,
            IAzureQueueService queueService,
            IAzureFileService fileService)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            _tableService = tableService;
            _blobService = blobService;
            _queueService = queueService;
            _fileService = fileService;
        }

        [HttpGet("test")]
        public IActionResult Test()
        {
            _logger.LogInformation("CORS proxy test endpoint called");
            
            // Add CORS headers
            Response.Headers.Add("Access-Control-Allow-Origin", "*");
            Response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
            Response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization, x-functions-key");
            
            return Ok(new { message = "CORS proxy is working with real Azure Storage!", timestamp = DateTime.UtcNow });
        }

        [HttpOptions("{*path}")]
        public IActionResult HandleOptions(string path)
        {
            _logger.LogInformation($"Handling OPTIONS request for path: {path}");
            
            // Add CORS headers
            Response.Headers.Add("Access-Control-Allow-Origin", "*");
            Response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
            Response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization, x-functions-key");
            Response.Headers.Add("Access-Control-Max-Age", "86400");
            
            return Ok();
        }

        [HttpGet("{*path}")]
        public async Task<IActionResult> Get(string path)
        {
            return await HandleRequest(HttpMethod.Get, path, null);
        }

        [HttpPost("{*path}")]
        public async Task<IActionResult> Post(string path)
        {
            string body = null;
            using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
            {
                body = await reader.ReadToEndAsync();
            }
            return await HandleRequest(HttpMethod.Post, path, body);
        }

        [HttpPut("{*path}")]
        public async Task<IActionResult> Put(string path)
        {
            string body = null;
            using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
            {
                body = await reader.ReadToEndAsync();
            }
            return await HandleRequest(HttpMethod.Put, path, body);
        }

        [HttpDelete("{*path}")]
        public async Task<IActionResult> Delete(string path)
        {
            return await HandleRequest(HttpMethod.Delete, path, null);
        }

        private async Task<IActionResult> HandleRequest(HttpMethod method, string path, string body)
        {
            try
            {
                // Add CORS headers to response
                Response.Headers.Add("Access-Control-Allow-Origin", "*");
                Response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
                Response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization, x-functions-key");

                _logger.LogInformation($"Handling {method} request for path: {path}");

                // Route to appropriate Azure Storage service based on path
                switch (path.ToLower())
                {
                    case "table/list":
                        return await HandleTableList();
                    
                    case "table/create":
                        return await HandleTableCreate(body);
                    
                    case "queue/length":
                        return await HandleQueueLength();
                    
                    case "queue/send":
                        return await HandleQueueSend(body);
                    
                    case "blob/list":
                        return await HandleBlobList();
                    
                    case "blob/upload":
                        return await HandleBlobUpload(body);
                    
                    case "file/list":
                        return await HandleFileList();
                    
                    case "file/write":
                        return await HandleFileWrite(body);
                    
                    default:
                        return Ok(new { 
                            message = "Real Azure Storage Response", 
                            path = path, 
                            method = method.ToString(),
                            timestamp = DateTime.UtcNow 
                        });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error handling request for {path}");
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        private async Task<IActionResult> HandleTableList()
        {
            try
            {
                var customers = await _tableService.GetAllEntitiesAsync<ABCRetail.Models.Customer>();
                return Ok(new { 
                    message = "Real Table List Response", 
                    data = customers,
                    count = customers.Count(),
                    timestamp = DateTime.UtcNow 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting table list");
                return StatusCode(500, new { error = "Failed to get table data", message = ex.Message });
            }
        }

        private async Task<IActionResult> HandleTableCreate(string body)
        {
            try
            {
                if (string.IsNullOrEmpty(body))
                {
                    return BadRequest(new { error = "Request body is required" });
                }

                var customer = JsonSerializer.Deserialize<Dictionary<string, object>>(body);
                if (customer == null)
                {
                    return BadRequest(new { error = "Invalid customer data" });
                }

                // Create a new customer entity
                var newCustomer = new ABCRetail.Models.Customer
                {
                    RowKey = Guid.NewGuid().ToString(),
                    PartitionKey = "Customer",
                    FirstName = customer.GetValueOrDefault("firstName")?.ToString() ?? "Unknown",
                    LastName = customer.GetValueOrDefault("lastName")?.ToString() ?? "Unknown",
                    Email = customer.GetValueOrDefault("email")?.ToString() ?? "",
                    Phone = customer.GetValueOrDefault("phone")?.ToString() ?? "",
                    Address = customer.GetValueOrDefault("address")?.ToString() ?? "",
                    City = customer.GetValueOrDefault("city")?.ToString() ?? "",
                    State = customer.GetValueOrDefault("state")?.ToString() ?? "",
                    ZipCode = customer.GetValueOrDefault("zipCode")?.ToString() ?? ""
                };

                await _tableService.CreateEntityAsync(newCustomer);

                return Ok(new { 
                    message = "Real Table Create Response", 
                    success = true, 
                    id = newCustomer.RowKey,
                    customer = newCustomer,
                    timestamp = DateTime.UtcNow 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating table entity");
                return StatusCode(500, new { error = "Failed to create table entity", message = ex.Message });
            }
        }

        private async Task<IActionResult> HandleQueueLength()
        {
            try
            {
                var length = await _queueService.GetQueueLengthAsync("inventory-queue");
                return Ok(new { 
                    message = "Real Queue Length Response", 
                    length = length,
                    timestamp = DateTime.UtcNow 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting queue length");
                return StatusCode(500, new { error = "Failed to get queue length", message = ex.Message });
            }
        }

        private async Task<IActionResult> HandleQueueSend(string body)
        {
            try
            {
                if (string.IsNullOrEmpty(body))
                {
                    return BadRequest(new { error = "Request body is required" });
                }

                var messageData = JsonSerializer.Deserialize<Dictionary<string, object>>(body);
                if (messageData == null)
                {
                    return BadRequest(new { error = "Invalid message data" });
                }

                var message = JsonSerializer.Serialize(messageData);
                await _queueService.EnqueueMessageAsync("inventory-queue", message);

                return Ok(new { 
                    message = "Real Queue Send Response", 
                    success = true, 
                    messageId = Guid.NewGuid().ToString(),
                    timestamp = DateTime.UtcNow 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending queue message");
                return StatusCode(500, new { error = "Failed to send queue message", message = ex.Message });
            }
        }

        private async Task<IActionResult> HandleBlobList()
        {
            try
            {
                var blobs = await _blobService.ListImagesAsync();
                return Ok(new { 
                    message = "Real Blob List Response", 
                    files = blobs,
                    count = blobs.Count,
                    timestamp = DateTime.UtcNow 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting blob list");
                return StatusCode(500, new { error = "Failed to get blob list", message = ex.Message });
            }
        }

        private async Task<IActionResult> HandleBlobUpload(string body)
        {
            try
            {
                if (string.IsNullOrEmpty(body))
                {
                    return BadRequest(new { error = "Request body is required" });
                }

                var uploadData = JsonSerializer.Deserialize<Dictionary<string, object>>(body);
                if (uploadData == null)
                {
                    return BadRequest(new { error = "Invalid upload data" });
                }

                var fileName = uploadData.GetValueOrDefault("fileName")?.ToString() ?? $"upload-{Guid.NewGuid()}.txt";
                var content = uploadData.GetValueOrDefault("content")?.ToString() ?? "Default content";

                // Convert base64 content if needed
                byte[] fileBytes;
                if (uploadData.ContainsKey("isBase64") && uploadData["isBase64"].ToString() == "true")
                {
                    fileBytes = Convert.FromBase64String(content);
                }
                else
                {
                    fileBytes = Encoding.UTF8.GetBytes(content);
                }

                // Create a mock IFormFile for the blob service
                var mockFile = new MockFormFile(fileBytes, fileName);
                var url = await _blobService.UploadImageAsync(mockFile, fileName);

                return Ok(new { 
                    message = "Real Blob Upload Response", 
                    success = true, 
                    fileName = fileName,
                    url = url,
                    timestamp = DateTime.UtcNow 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading blob");
                return StatusCode(500, new { error = "Failed to upload blob", message = ex.Message });
            }
        }

        private async Task<IActionResult> HandleFileList()
        {
            try
            {
                var files = await _fileService.ListLogFilesAsync();
                return Ok(new { 
                    message = "Real File List Response", 
                    files = files,
                    count = files.Count,
                    timestamp = DateTime.UtcNow 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting file list");
                return StatusCode(500, new { error = "Failed to get file list", message = ex.Message });
            }
        }

        private async Task<IActionResult> HandleFileWrite(string body)
        {
            try
            {
                if (string.IsNullOrEmpty(body))
                {
                    return BadRequest(new { error = "Request body is required" });
                }

                var writeData = JsonSerializer.Deserialize<Dictionary<string, object>>(body);
                if (writeData == null)
                {
                    return BadRequest(new { error = "Invalid write data" });
                }

                var fileName = writeData.GetValueOrDefault("fileName")?.ToString() ?? $"log-{DateTime.UtcNow:yyyy-MM-dd-HH-mm-ss}.txt";
                var content = writeData.GetValueOrDefault("content")?.ToString() ?? "Default log content";

                await _fileService.WriteLogAsync(content, fileName);

                return Ok(new { 
                    message = "Real File Write Response", 
                    success = true, 
                    fileName = fileName,
                    timestamp = DateTime.UtcNow 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error writing file");
                return StatusCode(500, new { error = "Failed to write file", message = ex.Message });
            }
        }
    }

    // Mock IFormFile implementation for blob uploads
    public class MockFormFile : IFormFile
    {
        private readonly byte[] _content;
        private readonly string _fileName;

        public MockFormFile(byte[] content, string fileName)
        {
            _content = content;
            _fileName = fileName;
        }

        public string ContentType => "application/octet-stream";
        public string ContentDisposition => $"form-data; name=\"file\"; filename=\"{_fileName}\"";
        public IHeaderDictionary Headers => new HeaderDictionary();
        public long Length => _content.Length;
        public string Name => "file";
        public string FileName => _fileName;

        public void CopyTo(Stream target)
        {
            target.Write(_content, 0, _content.Length);
        }

        public Task CopyToAsync(Stream target, CancellationToken cancellationToken = default)
        {
            return target.WriteAsync(_content, 0, _content.Length, cancellationToken);
        }

        public Stream OpenReadStream()
        {
            return new MemoryStream(_content);
        }
    }
}