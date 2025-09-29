# Azure Monitoring Setup Guide

This guide will help you set up comprehensive monitoring for your ABCRetail application and Azure Functions.

## 1. Application Insights Setup

### Step 1: Create Application Insights Resource
1. Go to Azure Portal (https://portal.azure.com)
2. Click "Create a resource"
3. Search for "Application Insights"
4. Click "Create"
5. Fill in the details:
   - **Name**: `abcretail-app-insights`
   - **Resource Group**: `AZ-JHB-RSG-RCNA-ST10466568-TER`
   - **Region**: `South Africa North`
   - **Application Type**: `ASP.NET Core`
6. Click "Review + Create" then "Create"

### Step 2: Get Instrumentation Key
1. Once created, go to your Application Insights resource
2. Copy the **Instrumentation Key** from the Overview page
3. Copy the **Connection String** from the Overview page

### Step 3: Configure in Your Application
Add to your `appsettings.json`:

```json
{
  "ApplicationInsights": {
    "ConnectionString": "YOUR_CONNECTION_STRING_HERE",
    "InstrumentationKey": "YOUR_INSTRUMENTATION_KEY_HERE"
  }
}
```

### Step 4: Install NuGet Package
```bash
dotnet add package Microsoft.ApplicationInsights.AspNetCore
```

### Step 5: Update Program.cs
Add to your `Program.cs`:

```csharp
builder.Services.AddApplicationInsightsTelemetry();
```

## 2. Function App Monitoring

### Step 1: Enable Application Insights for Function App
1. Go to your Function App: `abcretail-functions-3195`
2. Go to "Settings" > "Application Insights"
3. Click "Turn on Application Insights"
4. Select your Application Insights resource created above
5. Click "Apply"

### Step 2: Configure Function App Settings
Add these settings to your Function App:

```bash
az functionapp config appsettings set --name abcretail-functions-3195 --resource-group AZ-JHB-RSG-RCNA-ST10466568-TER --settings \
  "APPINSIGHTS_INSTRUMENTATIONKEY=YOUR_INSTRUMENTATION_KEY" \
  "APPLICATIONINSIGHTS_CONNECTION_STRING=YOUR_CONNECTION_STRING" \
  "FUNCTIONS_WORKER_RUNTIME=dotnet-isolated" \
  "WEBSITE_APPINSIGHTS_ENABLED=true"
```

## 3. Storage Account Monitoring

### Step 1: Enable Diagnostic Settings
1. Go to your Storage Account: `abcretailstoragevuyo`
2. Go to "Monitoring" > "Diagnostic settings"
3. Click "Add diagnostic setting"
4. Configure:
   - **Name**: `storage-diagnostics`
   - **Destination**: Send to Log Analytics workspace
   - **Metrics**: Select all available metrics
   - **Logs**: Select all available logs
5. Click "Save"

### Step 2: Create Log Analytics Workspace
1. Go to Azure Portal
2. Click "Create a resource"
3. Search for "Log Analytics workspace"
4. Click "Create"
5. Fill in details:
   - **Name**: `abcretail-logs`
   - **Resource Group**: `AZ-JHB-RSG-RCNA-ST10466568-TER`
   - **Region**: `South Africa North`
6. Click "Review + Create" then "Create"

## 4. Set Up Alerts

### Step 1: Create Alert Rules
1. Go to "Monitor" > "Alerts" in Azure Portal
2. Click "Create" > "Alert rule"
3. Configure alerts for:
   - **Function App Errors**: Alert when error rate > 5%
   - **Function App Response Time**: Alert when response time > 5 seconds
   - **Storage Account Failures**: Alert when storage operations fail
   - **High CPU Usage**: Alert when CPU > 80%
   - **Memory Usage**: Alert when memory > 85%

### Step 2: Configure Action Groups
1. Go to "Monitor" > "Action groups"
2. Click "Create action group"
3. Add your email address for notifications
4. Configure SMS/email notifications

## 5. Dashboard Setup

### Step 1: Create Custom Dashboard
1. Go to Azure Portal
2. Click "Dashboard" > "New dashboard"
3. Name it: `ABCRetail Monitoring Dashboard`
4. Add tiles for:
   - Function App metrics
   - Storage Account metrics
   - Application Insights performance
   - Error rates and exceptions
   - Custom queries

### Step 2: Add Key Metrics
- **Function Execution Count**
- **Function Duration**
- **Error Rate**
- **Storage Transaction Count**
- **Storage Success Rate**
- **Queue Message Count**

## 6. Log Queries

### Useful Kusto Queries for Application Insights:

```kusto
// Function execution summary
requests
| where timestamp > ago(1h)
| where name contains "api/"
| summarize count(), avg(duration), max(duration) by name
| order by count_ desc

// Error analysis
exceptions
| where timestamp > ago(1h)
| summarize count() by type, outerMessage
| order by count_ desc

// Performance analysis
requests
| where timestamp > ago(1h)
| where duration > 1000
| project timestamp, name, duration, success
| order by duration desc

// Storage operations
dependencies
| where timestamp > ago(1h)
| where type contains "Azure"
| summarize count(), avg(duration) by type, name
```

## 7. Cost Monitoring

### Step 1: Set Up Budget Alerts
1. Go to "Cost Management + Billing"
2. Click "Budgets"
3. Create budget for your resource group
4. Set monthly budget limit
5. Configure alerts at 50%, 75%, 90%, 100%

### Step 2: Monitor Usage
- Check daily usage reports
- Monitor Function App execution costs
- Track storage transaction costs
- Review Application Insights data ingestion costs

## 8. Security Monitoring

### Step 1: Enable Security Center
1. Go to "Security Center"
2. Enable Standard tier
3. Configure security recommendations
4. Set up security alerts

### Step 2: Monitor Access
- Review access logs regularly
- Monitor failed authentication attempts
- Check for unusual access patterns
- Review function execution logs

## 9. Performance Optimization

### Step 1: Monitor Performance
- Track function cold start times
- Monitor memory usage patterns
- Analyze execution frequency
- Review storage operation efficiency

### Step 2: Optimize Based on Data
- Scale Function App based on usage
- Optimize storage operations
- Implement caching where appropriate
- Adjust timeout settings

## 10. Maintenance Tasks

### Daily
- Check error rates and exceptions
- Review performance metrics
- Monitor cost usage

### Weekly
- Analyze usage patterns
- Review security alerts
- Check storage capacity

### Monthly
- Review and optimize costs
- Update monitoring queries
- Review and update alert rules
- Performance optimization review

## Troubleshooting Common Issues

### Function App Not Logging
- Check Application Insights configuration
- Verify instrumentation key is correct
- Check function app settings

### High Costs
- Review function execution frequency
- Check storage transaction volume
- Analyze Application Insights data ingestion

### Performance Issues
- Check function cold start times
- Review memory usage
- Analyze storage operation efficiency

This monitoring setup will give you comprehensive visibility into your ABCRetail application's performance, costs, and security.
