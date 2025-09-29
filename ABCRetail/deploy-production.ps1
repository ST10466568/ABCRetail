# ABCRetail Production Deployment Script
# This script builds and deploys the ABCRetail application for production

Write-Host "🚀 ABCRetail Production Deployment" -ForegroundColor Cyan
Write-Host "=================================" -ForegroundColor Cyan

# Set error action preference
$ErrorActionPreference = "Stop"

try {
    # Step 1: Clean and build the application
    Write-Host "1️⃣ Cleaning previous builds..." -ForegroundColor Yellow
    dotnet clean --configuration Release
    
    Write-Host "2️⃣ Building application for production..." -ForegroundColor Yellow
    dotnet build --configuration Release --no-restore
    
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed with exit code $LASTEXITCODE"
    }
    
    Write-Host "✅ Build completed successfully!" -ForegroundColor Green
    
    # Step 2: Publish the application
    Write-Host "3️⃣ Publishing application..." -ForegroundColor Yellow
    $publishPath = ".\bin\Release\net8.0\publish"
    
    if (Test-Path $publishPath) {
        Remove-Item $publishPath -Recurse -Force
    }
    
    dotnet publish --configuration Release --output $publishPath --no-build
    
    if ($LASTEXITCODE -ne 0) {
        throw "Publish failed with exit code $LASTEXITCODE"
    }
    
    Write-Host "✅ Application published successfully!" -ForegroundColor Green
    Write-Host "📁 Published to: $publishPath" -ForegroundColor Cyan
    
    # Step 3: Test the published application
    Write-Host "4️⃣ Testing published application..." -ForegroundColor Yellow
    
    # Start the application in background
    $process = Start-Process -FilePath "dotnet" -ArgumentList "$publishPath\ABCRetail.dll" -PassThru -WindowStyle Hidden
    
    # Wait for application to start
    Start-Sleep -Seconds 10
    
    try {
        # Test the CORS proxy endpoints
        Write-Host "   Testing CORS proxy endpoints..." -ForegroundColor Cyan
        
        $testEndpoints = @(
            "http://localhost:5000/api/corsproxy/test",
            "http://localhost:5000/api/corsproxy/table/list",
            "http://localhost:5000/api/corsproxy/queue/length",
            "http://localhost:5000/api/corsproxy/blob/list",
            "http://localhost:5000/api/corsproxy/file/list"
        )
        
        $allTestsPassed = $true
        
        foreach ($endpoint in $testEndpoints) {
            try {
                $response = Invoke-RestMethod -Uri $endpoint -Method GET -TimeoutSec 10
                Write-Host "   ✅ $endpoint - WORKING" -ForegroundColor Green
            }
            catch {
                Write-Host "   ❌ $endpoint - FAILED: $($_.Exception.Message)" -ForegroundColor Red
                $allTestsPassed = $false
            }
        }
        
        if ($allTestsPassed) {
            Write-Host "✅ All production tests passed!" -ForegroundColor Green
        } else {
            Write-Host "❌ Some production tests failed!" -ForegroundColor Red
        }
    }
    finally {
        # Stop the test application
        if ($process -and !$process.HasExited) {
            $process.Kill()
            $process.WaitForExit(5000)
        }
    }
    
    # Step 4: Create deployment package
    Write-Host "5️⃣ Creating deployment package..." -ForegroundColor Yellow
    
    $deploymentPackage = "ABCRetail-Production-$(Get-Date -Format 'yyyyMMdd-HHmmss').zip"
    
    if (Test-Path $deploymentPackage) {
        Remove-Item $deploymentPackage -Force
    }
    
    Compress-Archive -Path "$publishPath\*" -DestinationPath $deploymentPackage
    
    Write-Host "✅ Deployment package created: $deploymentPackage" -ForegroundColor Green
    
    # Step 5: Display deployment summary
    Write-Host "`n🎉 DEPLOYMENT COMPLETED SUCCESSFULLY!" -ForegroundColor Green
    Write-Host "=====================================" -ForegroundColor Green
    Write-Host "📦 Deployment Package: $deploymentPackage" -ForegroundColor Cyan
    Write-Host "📁 Publish Directory: $publishPath" -ForegroundColor Cyan
    Write-Host "🌐 Application URL: http://localhost:5000" -ForegroundColor Cyan
    Write-Host "🧪 Test Page: http://localhost:5000/AzureFunctionsTest" -ForegroundColor Cyan
    Write-Host "`n📋 Next Steps:" -ForegroundColor Yellow
    Write-Host "1. Deploy the package to your production server" -ForegroundColor White
    Write-Host "2. Configure production environment variables" -ForegroundColor White
    Write-Host "3. Set up reverse proxy (nginx/IIS) if needed" -ForegroundColor White
    Write-Host "4. Configure SSL certificates for HTTPS" -ForegroundColor White
    Write-Host "5. Set up monitoring and logging" -ForegroundColor White
    
}
catch {
    Write-Host "❌ DEPLOYMENT FAILED!" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
