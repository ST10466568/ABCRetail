# Direct Azure Table Storage Test Script
# This bypasses the .NET application entirely

Write-Host "🔍 Testing Azure Table Storage directly with PowerShell..." -ForegroundColor Green

# Test Customers Table
$customersUrl = "https://abcretailstoragevuyo.table.core.windows.net/Customers?sv=2024-11-04&ss=bfqt&srt=so&sp=rwdlacupiytfx&se=2025-08-29T04:04:35Z&st=2025-08-28T19:49:35Z&spr=https&sig=H1kGzZT9hliQpPFsA6Sz0meKDtQNynBTx7M2e5DyEZw%3D"

Write-Host "📊 Testing Customers Table..." -ForegroundColor Yellow
Write-Host "URL: $customersUrl"

try {
    $headers = @{
        'Accept' = 'application/json;odata=minimalmetadata'
        'User-Agent' = 'PowerShell/7.0'
    }
    
    $response = Invoke-RestMethod -Uri $customersUrl -Method Get -Headers $headers
    Write-Host "✅ Customers response received!" -ForegroundColor Green
    Write-Host "Response type: $($response.GetType().Name)"
    
    if ($response.value) {
        Write-Host "📋 Found $($response.value.Count) customers" -ForegroundColor Green
        foreach ($customer in $response.value | Select-Object -First 3) {
            Write-Host "   - $($customer.FirstName) $($customer.LastName) (PK: $($customer.PartitionKey), RK: $($customer.RowKey))"
        }
    } else {
        Write-Host "⚠️ No 'value' property found in response" -ForegroundColor Yellow
        Write-Host "Available properties: $($response | Get-Member -MemberType NoteProperty | Select-Object -ExpandProperty Name)"
    }
} catch {
    Write-Host "❌ Customers request failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n📦 Testing Products Table..." -ForegroundColor Yellow

# Test Products Table
$productsUrl = "https://abcretailstoragevuyo.table.core.windows.net/Products?sv=2024-11-04&ss=bfqt&srt=so&sp=rwdlacupiytfx&se=2025-08-29T04:04:35Z&st=2025-08-28T19:49:35Z&spr=https&sig=H1kGzZT9hliQpPFsA6Sz0meKDtQNynBTx7M2e5DyEZw%3D"

Write-Host "URL: $productsUrl"

try {
    $response = Invoke-RestMethod -Uri $productsUrl -Method Get -Headers $headers
    Write-Host "✅ Products response received!" -ForegroundColor Green
    Write-Host "Response type: $($response.GetType().Name)"
    
    if ($response.value) {
        Write-Host "📦 Found $($response.value.Count) products" -ForegroundColor Green
        foreach ($product in $response.value | Select-Object -First 3) {
            Write-Host "   - $($product.Name) (PK: $($product.PartitionKey), RK: $($product.RowKey))"
        }
    } else {
        Write-Host "⚠️ No 'value' property found in response" -ForegroundColor Yellow
        Write-Host "Available properties: $($response | Get-Member -MemberType NoteProperty | Select-Object -ExpandProperty Name)"
    }
} catch {
    Write-Host "❌ Products request failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n🔍 Testing with different Accept headers..." -ForegroundColor Yellow

# Test with different Accept header
try {
    $altHeaders = @{
        'Accept' = 'application/json'
        'User-Agent' = 'PowerShell/7.0'
    }
    
    Write-Host "Testing with Accept: application/json"
    $response = Invoke-RestMethod -Uri $customersUrl -Method Get -Headers $altHeaders
    
    if ($response.value) {
        Write-Host "✅ Alternative headers worked! Found $($response.value.Count) customers" -ForegroundColor Green
    } else {
        Write-Host "⚠️ Alternative headers didn't help" -ForegroundColor Yellow
    }
} catch {
    Write-Host "❌ Alternative headers test failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n🏁 Direct Azure testing completed!" -ForegroundColor Green


