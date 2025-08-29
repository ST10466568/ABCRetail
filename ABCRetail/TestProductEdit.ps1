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
