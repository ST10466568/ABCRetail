# ABCRetail Azure Functions

This project contains 4 Azure Functions that provide HTTP endpoints for interacting with Azure Storage services in the ABCRetail application.

## Functions Overview

### 1. TableOperationsFunction
**Route:** `/api/table/{operation}`
**Operations:** create, read, update, delete, list

Handles Azure Table Storage operations for customer data.

**Example Usage:**
- `POST /api/table/create` - Create a new customer
- `GET /api/table/read?partitionKey=CUSTOMER&rowKey={id}` - Read a customer
- `PUT /api/table/update` - Update a customer
- `DELETE /api/table/delete?partitionKey=CUSTOMER&rowKey={id}` - Delete a customer
- `GET /api/table/list` - List all customers

### 2. BlobOperationsFunction
**Route:** `/api/blob/{operation}`
**Operations:** upload, download, delete, list, geturl

Handles Azure Blob Storage operations for file management.

**Example Usage:**
- `POST /api/blob/upload?fileName=image.jpg` - Upload a file (base64 content in body)
- `GET /api/blob/download?fileName=image.jpg` - Download a file
- `DELETE /api/blob/delete?fileName=image.jpg` - Delete a file
- `GET /api/blob/list` - List all files
- `GET /api/blob/geturl?fileName=image.jpg` - Get file URL

### 3. QueueOperationsFunction
**Route:** `/api/queue/{operation}`
**Operations:** send, receive, peek, clear, length

Handles Azure Queue Storage operations for inventory messages.

**Example Usage:**
- `POST /api/queue/send` - Send a message to queue
- `GET /api/queue/receive?maxMessages=10` - Receive messages from queue
- `GET /api/queue/peek?maxMessages=10` - Peek at messages without removing
- `DELETE /api/queue/clear` - Clear all messages
- `GET /api/queue/length` - Get queue length

### 4. FileOperationsFunction
**Route:** `/api/file/{operation}`
**Operations:** write, read, list, delete, download

Handles Azure File Storage operations for log files.

**Example Usage:**
- `POST /api/file/write?fileName=log.txt` - Write content to file
- `GET /api/file/read?fileName=log.txt` - Read file content
- `GET /api/file/list` - List all files
- `DELETE /api/file/delete?fileName=log.txt` - Delete a file
- `GET /api/file/download?fileName=log.txt` - Download file as base64

## Configuration

Update `local.settings.json` with your Azure Storage connection details:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "AzureStorage:ConnectionString": "DefaultEndpointsProtocol=https;AccountName=YOUR_ACCOUNT_NAME;AccountKey=YOUR_ACCOUNT_KEY;EndpointSuffix=core.windows.net",
    "AzureStorage:BlobConnectionString": "DefaultEndpointsProtocol=https;AccountName=YOUR_ACCOUNT_NAME;AccountKey=YOUR_ACCOUNT_KEY;EndpointSuffix=core.windows.net",
    "AzureStorage:BlobSasUrl": "https://YOUR_ACCOUNT_NAME.blob.core.windows.net/?sv=YOUR_SAS_TOKEN",
    "AzureStorage:BlobContainerName": "product-images",
    "AzureStorage:QueueSasUrl": "https://YOUR_ACCOUNT_NAME.queue.core.windows.net/?sv=YOUR_SAS_TOKEN",
    "AzureStorage:FileSasUrl": "https://YOUR_ACCOUNT_NAME.file.core.windows.net/?sv=YOUR_SAS_TOKEN",
    "AzureStorage:TableName": "Customers",
    "AzureStorage:QueueName": "inventory-queue",
    "AzureStorage:ShareName": "applogs"
  }
}
```

## Running Locally

1. Install Azure Functions Core Tools:
   ```bash
   npm install -g azure-functions-core-tools@4 --unsafe-perm true
   ```

2. Navigate to the functions directory:
   ```bash
   cd ABCRetail.Functions
   ```

3. Start the functions:
   ```bash
   func start
   ```

The functions will be available at `http://localhost:7071/api/`

## Deployment

### Prerequisites
- Azure subscription
- Azure Storage Account
- Azure Functions App

### Steps to Deploy

1. **Create Azure Storage Account:**
   - Create a new storage account in Azure Portal
   - Note the connection string and account name

2. **Create Azure Functions App:**
   - Create a new Function App in Azure Portal
   - Choose .NET 8 as the runtime stack
   - Select the same region as your storage account

3. **Configure Application Settings:**
   - In the Function App, go to Configuration > Application settings
   - Add all the settings from `local.settings.json` (without the `Values` wrapper)
   - Set `FUNCTIONS_WORKER_RUNTIME` to `dotnet-isolated`

4. **Deploy the Functions:**
   ```bash
   func azure functionapp publish YOUR_FUNCTION_APP_NAME
   ```

## Required Azure Resources

You need to create the following Azure resources:

1. **Azure Storage Account** with the following services enabled:
   - Blob Storage
   - Queue Storage
   - File Storage
   - Table Storage

2. **Storage Containers/Shares:**
   - Blob container: `product-images` (or configure via settings)
   - File share: `applogs` (or configure via settings)
   - Tables: `Customers` (or configure via settings)
   - Queue: `inventory-queue` (or configure via settings)

3. **Access Policies:**
   - Generate SAS tokens for each service if using SAS URLs
   - Or use connection strings for full access

## Security Considerations

- Use Managed Identity when possible instead of connection strings
- Rotate SAS tokens regularly
- Use Key Vault for sensitive configuration
- Implement proper authentication/authorization for production use
- Consider using Azure AD authentication for the Function App

## Monitoring

- Enable Application Insights for monitoring and logging
- Set up alerts for function failures
- Monitor storage account usage and costs
- Use Azure Monitor for performance insights
