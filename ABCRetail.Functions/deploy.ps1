# Azure Functions Deployment Script for ABCRetail
# This script helps deploy the Azure Functions to Azure

param(
    [Parameter(Mandatory=$true)]
    [string]$FunctionAppName,
    
    [Parameter(Mandatory=$false)]
    [string]$ResourceGroupName,
    
    [Parameter(Mandatory=$false)]
    [string]$Location = "East US",
    
    [Parameter(Mandatory=$false)]
    [string]$StorageAccountName
)

Write-Host "🚀 Starting Azure Functions deployment for ABCRetail..." -ForegroundColor Green

# Check if Azure CLI is installed
if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
    Write-Error "Azure CLI is not installed. Please install it from https://docs.microsoft.com/en-us/cli/azure/install-azure-cli"
    exit 1
}

# Check if user is logged in
$account = az account show 2>$null
if (-not $account) {
    Write-Host "Please log in to Azure CLI first:" -ForegroundColor Yellow
    az login
}

# Create resource group if not specified
if (-not $ResourceGroupName) {
    $ResourceGroupName = "$FunctionAppName-rg"
    Write-Host "Creating resource group: $ResourceGroupName" -ForegroundColor Yellow
    az group create --name $ResourceGroupName --location $Location
}

# Use existing storage account
if (-not $StorageAccountName) {
    $StorageAccountName = "abcretailstoragevuyo"
    Write-Host "Using existing storage account: $StorageAccountName" -ForegroundColor Yellow
}

# Create Function App
Write-Host "Creating Function App: $FunctionAppName" -ForegroundColor Yellow
az functionapp create `
    --resource-group $ResourceGroupName `
    --consumption-plan-location $Location `
    --runtime dotnet-isolated `
    --runtime-version 8.0 `
    --functions-version 4 `
    --name $FunctionAppName `
    --storage-account $StorageAccountName

# Use the existing storage connection string
$storageConnectionString = "DefaultEndpointsProtocol=https;AccountName=abcretailstoragevuyo;AccountKey=Td7zP5UiTyD9JboCb3ECw05BllzFChZKInZO7LYm5FacXdzb7uDGRq/os/N7Sco7vZFOlpP9kpNA+ASt+PVnMA==;EndpointSuffix=core.windows.net"

# Configure application settings
Write-Host "Configuring application settings..." -ForegroundColor Yellow
az functionapp config appsettings set --name $FunctionAppName --resource-group $ResourceGroupName --settings `
    "AzureWebJobsStorage=$storageConnectionString" `
    "FUNCTIONS_WORKER_RUNTIME=dotnet-isolated" `
    "AzureStorage:ConnectionString=$storageConnectionString" `
    "AzureStorage:BlobConnectionString=$storageConnectionString" `
    "AzureStorage:BlobContainerName=product-images" `
    "AzureStorage:TableName=Customers" `
    "AzureStorage:QueueName=inventory-queue" `
    "AzureStorage:ShareName=logs" `
    "AzureStorage:BlobSasUrl=https://abcretailstoragevuyo.blob.core.windows.net?sv=2024-11-04&ss=bfqt&srt=sco&sp=rwdlacupiytfx&se=2025-11-30T01:38:13Z&st=2025-08-29T17:23:13Z&spr=https&sig=q4FrdN8d0gvfX9CNrOWhI5rakBULEc8%2Fomd9QhlQ1qw%3D" `
    "AzureStorage:QueueSasUrl=https://abcretailstoragevuyo.queue.core.windows.net/inventory-queue?sv=2024-11-04&ss=bfqt&srt=sco&sp=rwdlacupiytfx&se=2025-11-30T01:38:13Z&st=2025-08-29T17:23:13Z&spr=https&sig=q4FrdN8d0gvfX9CNrOWhI5rakBULEc8%2Fomd9QhlQ1qw%3D" `
    "AzureStorage:FileSasUrl=https://abcretailstoragevuyo.file.core.windows.net/logs?sv=2024-11-04&ss=bfqt&srt=sco&sp=rwdlacupiytfx&se=2025-11-30T01:38:13Z&st=2025-08-29T17:23:13Z&spr=https&sig=q4FrdN8d0gvfX9CNrOWhI5rakBULEc8%2Fomd9QhlQ3D"

# Create required storage resources
Write-Host "Creating storage containers and shares..." -ForegroundColor Yellow

# Create blob container
az storage container create --name "product-images" --account-name $StorageAccountName --connection-string $storageConnectionString

# Create file share
az storage share create --name "logs" --account-name $StorageAccountName --connection-string $storageConnectionString

# Create queue
az storage queue create --name "inventory-queue" --account-name $StorageAccountName --connection-string $storageConnectionString

# Deploy the function app
Write-Host "Deploying function app..." -ForegroundColor Yellow
func azure functionapp publish $FunctionAppName --csharp

Write-Host "✅ Deployment completed successfully!" -ForegroundColor Green
Write-Host "Function App URL: https://$FunctionAppName.azurewebsites.net" -ForegroundColor Cyan
Write-Host "Storage Account: $StorageAccountName" -ForegroundColor Cyan
Write-Host "Resource Group: $ResourceGroupName" -ForegroundColor Cyan

Write-Host "`n📋 Next Steps:" -ForegroundColor Yellow
Write-Host "1. Test your functions using the URLs above" -ForegroundColor White
Write-Host "2. Configure authentication if needed" -ForegroundColor White
Write-Host "3. Set up monitoring and alerts" -ForegroundColor White
Write-Host "4. Update your main application to use these function endpoints" -ForegroundColor White
