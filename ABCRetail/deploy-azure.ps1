# ABCRetail Azure Deployment Script
# This script deploys the ABCRetail application to Azure App Service

Write-Host "🚀 ABCRetail Azure Deployment" -ForegroundColor Cyan
Write-Host "=============================" -ForegroundColor Cyan

# Configuration
$resourceGroup = "AZ-JHB-RSG-RCNA-ST10466568-TER"
$webAppName = "abcretail123"
$publishPath = ".\bin\Release\net8.0\publish"

# Set error action preference
$ErrorActionPreference = "Stop"

try {
    # Step 1: Clean and build the application
    Write-Host "1️⃣ Cleaning previous builds..." -ForegroundColor Yellow
    dotnet clean --configuration Release
    
    Write-Host "2️⃣ Building application for Azure..." -ForegroundColor Yellow
    dotnet build --configuration Release --no-restore
    
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed with exit code $LASTEXITCODE"
    }
    
    Write-Host "✅ Build completed successfully!" -ForegroundColor Green
    
    # Step 2: Publish the application
    Write-Host "3️⃣ Publishing application..." -ForegroundColor Yellow
    
    if (Test-Path $publishPath) {
        Remove-Item $publishPath -Recurse -Force
    }
    
    dotnet publish --configuration Release --output $publishPath --no-build
    
    if ($LASTEXITCODE -ne 0) {
        throw "Publish failed with exit code $LASTEXITCODE"
    }
    
    Write-Host "✅ Application published successfully!" -ForegroundColor Green
    Write-Host "📁 Published to: $publishPath" -ForegroundColor Cyan
    
    # Step 3: Create deployment package
    Write-Host "4️⃣ Creating deployment package..." -ForegroundColor Yellow
    
    $deploymentPackage = "ABCRetail-Azure-Deployment.zip"
    
    if (Test-Path $deploymentPackage) {
        Remove-Item $deploymentPackage -Force
    }
    
    Compress-Archive -Path "$publishPath\*" -DestinationPath $deploymentPackage
    
    Write-Host "✅ Deployment package created: $deploymentPackage" -ForegroundColor Green
    
    # Step 4: Deploy to Azure
    Write-Host "5️⃣ Deploying to Azure App Service..." -ForegroundColor Yellow
    
    # Stop the web app first
    Write-Host "   Stopping web app..." -ForegroundColor Cyan
    az webapp stop --name $webAppName --resource-group $resourceGroup
    
    # Deploy the package
    Write-Host "   Uploading deployment package..." -ForegroundColor Cyan
    az webapp deployment source config-zip --resource-group $resourceGroup --name $webAppName --src $deploymentPackage
    
    if ($LASTEXITCODE -ne 0) {
        throw "Azure deployment failed with exit code $LASTEXITCODE"
    }
    
    # Start the web app
    Write-Host "   Starting web app..." -ForegroundColor Cyan
    az webapp start --name $webAppName --resource-group $resourceGroup
    
    # Step 5: Configure application settings
    Write-Host "6️⃣ Configuring application settings..." -ForegroundColor Yellow
    
    # Set environment to Production
    az webapp config appsettings set --resource-group $resourceGroup --name $webAppName --settings ASPNETCORE_ENVIRONMENT=Production
    
    # Configure CORS for Azure
    az webapp cors add --resource-group $resourceGroup --name $webAppName --allowed-origins "*"
    
    Write-Host "✅ Application settings configured!" -ForegroundColor Green
    
    # Step 6: Get the web app URL
    Write-Host "7️⃣ Getting deployment information..." -ForegroundColor Yellow
    
    $webAppUrl = az webapp show --name $webAppName --resource-group $resourceGroup --query "defaultHostName" --output tsv
    $fullUrl = "https://$webAppUrl"
    
    Write-Host "✅ Deployment completed successfully!" -ForegroundColor Green
    Write-Host "`n🎉 AZURE DEPLOYMENT COMPLETED!" -ForegroundColor Green
    Write-Host "===============================" -ForegroundColor Green
    Write-Host "🌐 Web App URL: $fullUrl" -ForegroundColor Cyan
    Write-Host "🧪 Test Page: $fullUrl/AzureFunctionsTest" -ForegroundColor Cyan
    Write-Host "🔧 CORS Proxy: $fullUrl/api/corsproxy/test" -ForegroundColor Cyan
    Write-Host "`n📋 Next Steps:" -ForegroundColor Yellow
    Write-Host "1. Test the deployed application" -ForegroundColor White
    Write-Host "2. Verify CORS proxy functionality" -ForegroundColor White
    Write-Host "3. Check Azure App Service logs if needed" -ForegroundColor White
    Write-Host "4. Configure custom domain if required" -ForegroundColor White
    
    # Clean up deployment package
    Remove-Item $deploymentPackage -Force
    Write-Host "`n🧹 Cleaned up deployment package" -ForegroundColor Green
    
}
catch {
    Write-Host "❌ DEPLOYMENT FAILED!" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
