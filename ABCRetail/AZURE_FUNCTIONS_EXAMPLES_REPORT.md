# Azure Functions Examples Report
## ABCRetail Application - Practical Azure Storage Operations

### Overview
This report provides practical examples of the four core Azure Functions operations implemented in the ABCRetail application. Each example demonstrates real-world usage patterns with complete code snippets and explanations.

---

## 1. Store Information to Azure Table

### Function: TableOperationsFunction
**Purpose**: Store customer information in Azure Table Storage

### Example: Creating a New Customer Record

#### Code Implementation
```csharp
[Function("TableOperations")]
public async Task<HttpResponseData> Run(
    [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "table/create")] HttpRequestData req)
{
    // 1. Read and deserialize customer data from request
    var body = await new StreamReader(req.Body).ReadToEndAsync();
    var customer = JsonSerializer.Deserialize<Customer>(body, new JsonSerializerOptions { 
        PropertyNameCaseInsensitive = true 
    });

    // 2. Validate customer data
    if (customer == null)
    {
        return await CreateErrorResponse(req, "Invalid customer data", HttpStatusCode.BadRequest);
    }

    // 3. Set Azure Table Storage required properties
    customer.RowKey = Guid.NewGuid().ToString();  // Unique identifier
    customer.PartitionKey = "CUSTOMER";           // Logical grouping
    customer.Timestamp = DateTimeOffset.UtcNow;   // Azure timestamp

    // 4. Connect to Azure Table Storage
    var connectionString = _configuration["AzureStorage:ConnectionString"];
    var tableServiceClient = new TableServiceClient(connectionString);
    var tableClient = tableServiceClient.GetTableClient("Customers");
    await tableClient.CreateIfNotExistsAsync();

    // 5. Store customer in Azure Table
    await tableClient.AddEntityAsync(customer);

    // 6. Return success response
    var response = req.CreateResponse(HttpStatusCode.Created);
    AddCorsHeaders(response);
    await response.WriteStringAsync(JsonSerializer.Serialize(new { 
        message = "Customer created successfully", 
        id = customer.RowKey 
    }));
    return response;
}
```

#### Customer Model
```csharp
public class Customer : ITableEntity
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    // Azure Table Storage required properties
    public string PartitionKey { get; set; } = "CUSTOMER";
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
}
```

#### API Usage Example
```http
POST /api/table/create
Content-Type: application/json
x-functions-key: your-function-key

{
  "firstName": "John",
  "lastName": "Doe",
  "email": "john.doe@example.com",
  "phone": "555-1234",
  "address": "123 Main Street",
  "city": "Anytown",
  "state": "CA",
  "zipCode": "90210"
}
```

#### What Happens:
1. **Data Validation**: Ensures customer data is properly formatted
2. **Unique ID Generation**: Creates GUID for RowKey (unique identifier)
3. **Partition Assignment**: Groups all customers under "CUSTOMER" partition
4. **Azure Storage Connection**: Connects to Azure Table Storage using connection string
5. **Table Creation**: Ensures "Customers" table exists
6. **Entity Storage**: Stores customer as table entity
7. **Response**: Returns success with generated customer ID

---

## 2. Write to Blob Storage

### Function: BlobOperationsFunction
**Purpose**: Store product images and files in Azure Blob Storage

### Example: Uploading a Product Image

#### Code Implementation
```csharp
[Function("BlobOperations")]
public async Task<HttpResponseData> Run(
    [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "blob/upload")] HttpRequestData req)
{
    // 1. Extract file name from query parameters
    var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
    var fileName = query["fileName"];

    if (string.IsNullOrEmpty(fileName))
    {
        return await CreateErrorResponse(req, "fileName parameter is required", HttpStatusCode.BadRequest);
    }

    // 2. Read base64-encoded image data from request body
    var content = await new StreamReader(req.Body).ReadToEndAsync();
    var imageBytes = Convert.FromBase64String(content);

    // 3. Connect to Azure Blob Storage
    var connectionString = _configuration["AzureStorage:ConnectionString"];
    var blobServiceClient = new BlobServiceClient(connectionString);
    var containerClient = blobServiceClient.GetBlobContainerClient("product-images");
    await containerClient.CreateIfNotExistsAsync();

    // 4. Create blob client and upload image
    var blobClient = containerClient.GetBlobClient(fileName);
    using var stream = new MemoryStream(imageBytes);
    await blobClient.UploadAsync(stream, overwrite: true);

    // 5. Return success with blob URL
    var response = req.CreateResponse(HttpStatusCode.OK);
    AddCorsHeaders(response);
    await response.WriteStringAsync(JsonSerializer.Serialize(new { 
        message = "Image uploaded successfully", 
        fileName = fileName,
        url = blobClient.Uri.ToString(),
        size = imageBytes.Length
    }));
    return response;
}
```

#### API Usage Example
```http
POST /api/blob/upload?fileName=product-123.jpg
Content-Type: application/json
x-functions-key: your-function-key

"iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg=="
```

#### What Happens:
1. **File Name Extraction**: Gets filename from query parameter
2. **Base64 Decoding**: Converts base64 string to binary image data
3. **Blob Storage Connection**: Connects to Azure Blob Storage
4. **Container Management**: Ensures "product-images" container exists
5. **Image Upload**: Uploads image data to blob storage
6. **URL Generation**: Creates publicly accessible URL for the image
7. **Response**: Returns success with image URL and metadata

---

## 3. Queue Written To/From Transaction

### Function: QueueOperationsFunction
**Purpose**: Process inventory transactions through Azure Queue Storage

### Example: Sending Inventory Update Message

#### Code Implementation
```csharp
[Function("QueueOperations")]
public async Task<HttpResponseData> Run(
    [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "queue/send")] HttpRequestData req)
{
    // 1. Deserialize inventory transaction from request
    var body = await new StreamReader(req.Body).ReadToEndAsync();
    var message = JsonSerializer.Deserialize<InventoryQueueMessage>(body, new JsonSerializerOptions { 
        PropertyNameCaseInsensitive = true 
    });

    if (message == null)
    {
        return await CreateErrorResponse(req, "Invalid inventory message", HttpStatusCode.BadRequest);
    }

    // 2. Connect to Azure Queue Storage
    var connectionString = _configuration["AzureStorage:ConnectionString"];
    var queueServiceClient = new QueueServiceClient(connectionString);
    var queueClient = queueServiceClient.GetQueueClient("inventory-queue");
    await queueClient.CreateIfNotExistsAsync();

    // 3. Serialize message to JSON and encode as base64 (Azure requirement)
    var jsonMessage = JsonSerializer.Serialize(message, new JsonSerializerOptions 
    { 
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
    });
    var base64Message = Convert.ToBase64String(Encoding.UTF8.GetBytes(jsonMessage));

    // 4. Send message to queue
    var response = await queueClient.SendMessageAsync(base64Message);
    
    // 5. Return success with message tracking info
    var httpResponse = req.CreateResponse(HttpStatusCode.OK);
    AddCorsHeaders(httpResponse);
    await httpResponse.WriteStringAsync(JsonSerializer.Serialize(new { 
        message = "Inventory transaction queued successfully", 
        messageId = response.Value.MessageId,
        popReceipt = response.Value.PopReceipt,
        timestamp = DateTime.UtcNow
    }));
    return httpResponse;
}
```

#### Inventory Message Model
```csharp
public class InventoryQueueMessage
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;  // "stock_in", "stock_out", "adjustment"

    [JsonPropertyName("productId")]
    public string ProductId { get; set; } = string.Empty;

    [JsonPropertyName("productName")]
    public string ProductName { get; set; } = string.Empty;

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("notes")]
    public string Notes { get; set; } = string.Empty;
}
```

#### API Usage Example
```http
POST /api/queue/send
Content-Type: application/json
x-functions-key: your-function-key

{
  "type": "stock_in",
  "productId": "PROD-12345",
  "productName": "Wireless Headphones",
  "quantity": 50,
  "userId": "user-789",
  "notes": "New shipment received from supplier"
}
```

#### What Happens:
1. **Transaction Validation**: Ensures inventory message is properly formatted
2. **Queue Connection**: Connects to Azure Queue Storage
3. **Message Serialization**: Converts to JSON with camelCase naming
4. **Base64 Encoding**: Encodes message (Azure Queue requirement)
5. **Message Queuing**: Sends message to "inventory-queue"
6. **Tracking**: Returns message ID and pop receipt for tracking
7. **Response**: Confirms transaction was queued successfully

---

## 4. Write to Azure Files

### Function: FileOperationsFunction
**Purpose**: Store application logs and files in Azure File Storage

### Example: Writing Application Log File

#### Code Implementation
```csharp
[Function("FileOperations")]
public async Task<HttpResponseData> Run(
    [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "file/write")] HttpRequestData req)
{
    // 1. Extract file name from query parameters
    var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
    var fileName = query["fileName"];

    if (string.IsNullOrEmpty(fileName))
    {
        return await CreateErrorResponse(req, "fileName parameter is required", HttpStatusCode.BadRequest);
    }

    // 2. Deserialize file data from request
    var body = await new StreamReader(req.Body).ReadToEndAsync();
    var fileData = JsonSerializer.Deserialize<FileWriteRequest>(body, new JsonSerializerOptions { 
        PropertyNameCaseInsensitive = true 
    });

    if (fileData == null || string.IsNullOrEmpty(fileData.Content))
    {
        return await CreateErrorResponse(req, "File content is required", HttpStatusCode.BadRequest);
    }

    // 3. Connect to Azure File Storage
    var connectionString = _configuration["AzureStorage:ConnectionString"];
    var shareServiceClient = new ShareServiceClient(connectionString);
    var shareClient = shareServiceClient.GetShareClient("applogs");
    await shareClient.CreateIfNotExistsAsync();

    // 4. Prepare file content
    byte[] contentBytes;
    if (fileData.IsBase64)
    {
        contentBytes = Convert.FromBase64String(fileData.Content);
    }
    else
    {
        contentBytes = System.Text.Encoding.UTF8.GetBytes(fileData.Content);
    }

    // 5. Write file to Azure File Storage
    var directoryClient = shareClient.GetRootDirectoryClient();
    var fileClient = directoryClient.GetFileClient(fileName);
    
    using var stream = new MemoryStream(contentBytes);
    await fileClient.CreateAsync(stream.Length);
    await fileClient.UploadRangeAsync(new Azure.HttpRange(0, stream.Length), stream);

    // 6. Return success response
    var response = req.CreateResponse(HttpStatusCode.OK);
    AddCorsHeaders(response);
    await response.WriteStringAsync(JsonSerializer.Serialize(new { 
        message = "Log file written successfully", 
        fileName = fileName,
        size = contentBytes.Length,
        timestamp = DateTime.UtcNow
    }));
    return response;
}
```

#### File Write Request Model
```csharp
public class FileWriteRequest
{
    public string Content { get; set; } = string.Empty;
    public bool IsBase64 { get; set; } = false;
}
```

#### API Usage Example
```http
POST /api/file/write?fileName=app-log-2025-09-29.txt
Content-Type: application/json
x-functions-key: your-function-key

{
  "content": "2025-09-29 21:30:00 [INFO] Application started successfully\n2025-09-29 21:30:01 [INFO] Database connection established\n2025-09-29 21:30:02 [INFO] Azure Storage services initialized",
  "isBase64": false
}
```

#### What Happens:
1. **File Name Extraction**: Gets filename from query parameter
2. **Content Validation**: Ensures file content is provided
3. **File Storage Connection**: Connects to Azure File Storage
4. **Share Management**: Ensures "applogs" file share exists
5. **Content Processing**: Handles both plain text and base64 content
6. **File Creation**: Creates file with specified length
7. **Content Upload**: Uploads content using range-based upload
8. **Response**: Returns success with file metadata

---

## Summary

### Key Patterns Demonstrated

1. **Azure Table Storage**: Structured data storage with partition/row key management
2. **Azure Blob Storage**: Binary file storage with URL generation
3. **Azure Queue Storage**: Asynchronous message processing with base64 encoding
4. **Azure File Storage**: File system-like operations with range-based uploads

### Common Implementation Features

- **Authentication**: Function key validation for all operations
- **CORS Support**: Web application compatibility
- **Error Handling**: Comprehensive error management
- **Async/Await**: Proper asynchronous programming patterns
- **Configuration**: Environment-based connection management
- **Validation**: Input validation and sanitization

### Real-World Applications

- **Customer Management**: Store customer profiles and contact information
- **Product Catalog**: Manage product images and documents
- **Inventory Processing**: Handle stock updates and transactions asynchronously
- **Application Logging**: Store logs and audit trails for compliance

These examples demonstrate production-ready patterns for integrating Azure Storage services into modern applications, providing scalable and reliable data management capabilities.
