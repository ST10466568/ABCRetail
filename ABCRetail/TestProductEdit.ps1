# Test Product Edit Functionality - Simple Approach
# This script tests basic Azure Table Storage operations

Write-Host "🔍 Testing Product Edit Functionality - Simple Approach" -ForegroundColor Cyan
Write-Host "=====================================================" -ForegroundColor Cyan

# Test 1: List all tables
Write-Host "`n📋 Test 1: Listing available tables..." -ForegroundColor Yellow
try {
    $tableUrl = "https://abcretailstoragevuyo.table.core.windows.net/Tables"
    $headers = @{
        "x-ms-version" = "2020-04-08"
        "x-ms-date" = (Get-Date).ToString("R")
    }
    
    $response = Invoke-WebRequest -Uri $tableUrl -Headers $headers -Method GET
    Write-Host "✅ Tables listed successfully" -ForegroundColor Green
    Write-Host "Response: $($response.Content)" -ForegroundColor Gray
} catch {
    Write-Host "❌ Failed to list tables: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 2: List all products
Write-Host "`n📦 Test 2: Listing all products..." -ForegroundColor Yellow
try {
    $productsUrl = "https://abcretailstoragevuyo.table.core.windows.net/Products"
    $headers = @{
        "x-ms-version" = "2020-04-08"
        "x-ms-date" = (Get-Date).ToString("R")
    }
    
    $response = Invoke-WebRequest -Uri $productsUrl -Headers $headers -Method GET
    Write-Host "✅ Products listed successfully" -ForegroundColor Green
    Write-Host "Response: $($response.Content)" -ForegroundColor Gray
} catch {
    Write-Host "❌ Failed to list products: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n🏁 Simple Product Edit Testing Complete!" -ForegroundColor Cyan
Write-Host "Check the responses above to see what data is available." -ForegroundColor Cyan

# Test 3: Send and receive a message from inventory-queue
Write-Host "`n Test 3: Send and receive message from inventory-queue..." -ForegroundColor Yellow
try {
    $queueUrl = "https://abcretailstoragevuyo.queue.core.windows.net/inventory-queue/messages"
    $sasToken = "?sv=2024-11-04&ss=bfqt&srt=sco&sp=rwdlacupiytfx&se=2025-11-30T01:38:13Z&st=2025-08-29T17:23:13Z&spr=https&sig=q4FrdN8d0gvfX9CNrOWhI5rakBULEc8/omd9QhlQ1qw%3D"
    $headers = @{
        "x-ms-version" = "2020-04-08"
        "x-ms-date" = (Get-Date).ToString("R")
        "Content-Type" = "application/xml"
    }
    $body = "<QueueMessage><MessageText>Test message from PowerShell $(Get-Date -Format o)</MessageText></QueueMessage>"
    $sendUrl = $queueUrl + $sasToken
    $response = Invoke-WebRequest -Uri $sendUrl -Headers $headers -Method POST -Body $body
    Write-Host " Message sent to queue successfully" -ForegroundColor Green
    Start-Sleep -Seconds 2
    # Peek message (does not remove from queue)
    $peekUrl = "https://abcretailstoragevuyo.queue.core.windows.net/inventory-queue/messages/peek" + $sasToken + "&numofmessages=1"
    $response = Invoke-WebRequest -Uri $peekUrl -Headers $headers -Method GET
    Write-Host " Peeked message from queue:" -ForegroundColor Green
    Write-Host $response.Content -ForegroundColor Gray
} catch {
    Write-Host " Failed to send/receive message: $($_.Exception.Message)" -ForegroundColor Red
}
