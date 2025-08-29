# Test Product Edit Functionality - Direct Azure API Testing
# This script tests Azure Table Storage operations directly

Write-Host "🔍 Testing Product Edit Functionality - Direct Azure API" -ForegroundColor Cyan
Write-Host "=====================================================" -ForegroundColor Cyan

# Azure Storage Account Details
$accountName = "YOUR_STORAGE_ACCOUNT_NAME"
$accountKey = "YOUR_STORAGE_ACCOUNT_KEY"
$tableName = "Products"

# Generate Shared Access Signature
$expiry = (Get-Date).AddHours(1).ToString("yyyy-MM-ddTHH:mm:ssZ")
$stringToSign = "GET`n`n`n$expiry`n/$accountName/$tableName"
$hmac = New-Object System.Security.Cryptography.HMACSHA256
$hmac.Key = [System.Convert]::FromBase64String($accountKey)
$signature = [System.Convert]::ToBase64String($hmac.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($stringToSign)))
$sas = "?sv=2020-08-04&tn=$tableName&sig=$signature&se=$expiry"

# Test 1: List all products
Write-Host "`n📦 Test 1: Listing all products..." -ForegroundColor Yellow
try {
    $productsUrl = "https://$accountName.table.core.windows.net/$tableName$sas"
    $headers = @{
        "x-ms-version" = "2020-04-08"
        "x-ms-date" = (Get-Date).ToString("R")
        "Accept" = "application/json;odata=fullmetadata"
    }
    
    $response = Invoke-WebRequest -Uri $productsUrl -Headers $headers -Method GET
    Write-Host "✅ Products listed successfully" -ForegroundColor Green
    Write-Host "Response Status: $($response.StatusCode)" -ForegroundColor Gray
    
    # Parse the response
    $content = $response.Content | ConvertFrom-Json
    if ($content.value) {
        Write-Host "📊 Found $($content.value.Count) products:" -ForegroundColor Green
        foreach ($product in $content.value) {
            Write-Host "  - $($product.Name) (ID: $($product.RowKey), PartitionKey: $($product.PartitionKey))" -ForegroundColor White
        }
    } else {
        Write-Host "⚠️ No products found in response" -ForegroundColor Yellow
        Write-Host "Response Content: $($response.Content)" -ForegroundColor Gray
    }
} catch {
    Write-Host "❌ Failed to list products: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 2: Try to get the specific missing product
Write-Host "`n🔍 Test 2: Looking for missing product..." -ForegroundColor Yellow
$missingProductId = "02397bda-437e-47f7-aa19-c0e1574e342e"
try {
    $productUrl = "https://$accountName.table.core.windows.net/$tableName(PartitionKey='Product',RowKey='$missingProductId')$sas"
    $headers = @{
        "x-ms-version" = "2020-08-04"
        "x-ms-date" = (Get-Date).ToString("R")
        "Accept" = "application/json;odata=fullmetadata"
    }
    
    $response = Invoke-WebRequest -Uri $productUrl -Headers $headers -Method GET
    Write-Host "✅ Product found!" -ForegroundColor Green
    Write-Host "Response: $($response.Content)" -ForegroundColor Gray
} catch {
    Write-Host "❌ Product not found: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n🏁 Direct Azure API Testing Complete!" -ForegroundColor Cyan
