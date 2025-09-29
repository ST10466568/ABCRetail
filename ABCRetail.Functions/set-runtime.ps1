# Set Azure Function App runtime to dotnet-isolated
$subscriptionId = "4dd12a2f-0a9e-4b69-ba01-a9d26e10e611"
$resourceGroup = "AZ-JHB-RSG-RCNA-ST10466568-TER"
$functionAppName = "abcretail-functions-v2-3195"

# Get access token
$token = (az account get-access-token --query accessToken --output tsv)

# Set runtime configuration
$uri = "https://management.azure.com/subscriptions/$subscriptionId/resourceGroups/$resourceGroup/providers/Microsoft.Web/sites/$functionAppName/config?api-version=2023-01-01"

$body = @{
    properties = @{
        linuxFxVersion = "DOTNET-ISOLATED|8"
    }
} | ConvertTo-Json -Depth 3

$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

try {
    Write-Host "Setting runtime to dotnet-isolated..." -ForegroundColor Green
    $response = Invoke-RestMethod -Uri $uri -Method PUT -Body $body -Headers $headers
    Write-Host "✅ Runtime set successfully" -ForegroundColor Green
    $response | ConvertTo-Json -Depth 3
} catch {
    Write-Host "❌ Failed to set runtime: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $responseBody = $reader.ReadToEnd()
        Write-Host "Response: $responseBody" -ForegroundColor Yellow
    }
}
