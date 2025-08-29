Write-Host "🔍 Testing Azure Table Storage Data Fetching..." -ForegroundColor Green
Write-Host "📊 Testing Customers Table..." -ForegroundColor Yellow
$customersUrl = "https://abcretailstoragevuyo.table.core.windows.net/Customers?sv=2024-11-04&ss=bfqt&srt=so&sp=rwdlacupiytfx&se=2025-08-29T04:04:35Z&st=2025-08-28T19:49:35Z&spr=https&sig=H1kGzZT9hliQpPFsA6Sz0meKDtQNynBTx7M2e5DyEZw%3D"
try {
    $headers = @{"Accept" = "application/json;odata=minimalmetadata"; "User-Agent" = "PowerShell/7.0"}
