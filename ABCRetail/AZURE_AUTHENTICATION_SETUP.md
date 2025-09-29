# Azure Authentication Setup Guide

This guide will help you set up secure authentication for your ABCRetail application and Azure Functions in production.

## 1. Azure Active Directory (Azure AD) Setup

### Step 1: Create App Registration
1. Go to Azure Portal
2. Navigate to "Azure Active Directory"
3. Go to "App registrations"
4. Click "New registration"
5. Fill in details:
   - **Name**: `ABCRetail App`
   - **Supported account types**: `Accounts in this organizational directory only`
   - **Redirect URI**: `Web` - `https://your-app-domain.com/signin-oidc`
6. Click "Register"

### Step 2: Configure Authentication
1. Go to "Authentication" in your app registration
2. Add platform configurations:
   - **Web**: `https://your-app-domain.com/signin-oidc`
   - **Single-page application**: `https://your-app-domain.com`
3. Enable:
   - ✅ ID tokens
   - ✅ Access tokens
   - ✅ Refresh tokens
4. Set logout URL: `https://your-app-domain.com/signout-oidc`

### Step 3: Create Client Secret
1. Go to "Certificates & secrets"
2. Click "New client secret"
3. Add description: `ABCRetail Client Secret`
4. Set expiration: `24 months`
5. Click "Add"
6. **IMPORTANT**: Copy the secret value immediately (you won't see it again)

### Step 4: Configure API Permissions
1. Go to "API permissions"
2. Click "Add a permission"
3. Select "Microsoft Graph"
4. Add permissions:
   - `User.Read` (Delegated)
   - `User.ReadBasic.All` (Delegated)
5. Click "Grant admin consent"

## 2. Function App Authentication

### Step 1: Enable Authentication
1. Go to your Function App: `abcretail-functions-3195`
2. Go to "Authentication"
3. Click "Add identity provider"
4. Select "Microsoft"
5. Configure:
   - **App registration type**: `Pick an existing app registration in this directory`
   - **App registration**: Select your `ABCRetail App`
   - **Restrict access**: `Require authentication`
6. Click "Add"

### Step 2: Configure Function App Settings
Add these settings to your Function App:

```bash
az functionapp config appsettings set --name abcretail-functions-3195 --resource-group AZ-JHB-RSG-RCNA-ST10466568-TER --settings \
  "AZURE_CLIENT_ID=YOUR_CLIENT_ID" \
  "AZURE_CLIENT_SECRET=YOUR_CLIENT_SECRET" \
  "AZURE_TENANT_ID=YOUR_TENANT_ID" \
  "WEBSITE_AUTH_ENABLED=true" \
  "WEBSITE_AUTH_DEFAULT_PROVIDER=AzureActiveDirectory"
```

## 3. Main Application Authentication

### Step 1: Install Required Packages
```bash
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add package Microsoft.AspNetCore.Authentication.OpenIdConnect
dotnet add package Microsoft.Identity.Web
```

### Step 2: Update appsettings.json
```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "YOUR_TENANT_ID",
    "ClientId": "YOUR_CLIENT_ID",
    "ClientSecret": "YOUR_CLIENT_SECRET",
    "CallbackPath": "/signin-oidc",
    "SignedOutCallbackPath": "/signout-oidc"
  },
  "AzureFunctions": {
    "BaseUrl": "https://abcretail-functions-3195.azurewebsites.net",
    "FunctionKey": "YOUR_FUNCTION_KEY",
    "UseAuthentication": true
  }
}
```

### Step 3: Update Program.cs
```csharp
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Identity.Web;

var builder = WebApplication.CreateBuilder(args);

// Add authentication services
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"))
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddInMemoryTokenCaches();

builder.Services.AddAuthorization();

// Add services to the container.
builder.Services.AddRazorPages();

// ... rest of your existing code ...

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();
```

### Step 4: Add Authorization to Pages
Update your page models to require authentication:

```csharp
[Authorize]
public class IndexModel : PageModel
{
    // Your existing code
}
```

## 4. API Key Management

### Step 1: Create Key Vault
1. Go to Azure Portal
2. Create "Key Vault" resource
3. Name: `abcretail-keyvault`
4. Resource Group: `AZ-JHB-RSG-RCNA-ST10466568-TER`
5. Region: `South Africa North`
6. Pricing tier: `Standard`

### Step 2: Store Secrets
1. Go to your Key Vault
2. Go to "Secrets"
3. Create secrets for:
   - `FunctionAppKey`: Your function key
   - `StorageConnectionString`: Your storage connection string
   - `ClientSecret`: Your Azure AD client secret

### Step 3: Configure Managed Identity
1. Go to your Function App
2. Go to "Identity"
3. Turn on "System assigned managed identity"
4. Note the Object ID

### Step 4: Grant Key Vault Access
1. Go to your Key Vault
2. Go to "Access policies"
3. Add access policy:
   - **Secret permissions**: Get, List
   - **Principal**: Select your Function App's managed identity
4. Click "Add" then "Save"

## 5. Secure Configuration

### Step 1: Update Function App to Use Key Vault
Add these settings to your Function App:

```bash
az functionapp config appsettings set --name abcretail-functions-3195 --resource-group AZ-JHB-RSG-RCNA-ST10466568-TER --settings \
  "@Microsoft.KeyVault(SecretUri=https://abcretail-keyvault.vault.azure.net/secrets/FunctionAppKey/)" \
  "@Microsoft.KeyVault(SecretUri=https://abcretail-keyvault.vault.azure.net/secrets/StorageConnectionString/)" \
  "WEBSITE_LOAD_CERTIFICATES=*"
```

### Step 2: Update Main App Configuration
```json
{
  "AzureKeyVault": {
    "VaultUrl": "https://abcretail-keyvault.vault.azure.net/",
    "TenantId": "YOUR_TENANT_ID",
    "ClientId": "YOUR_CLIENT_ID"
  }
}
```

## 6. CORS Configuration

### Step 1: Configure Function App CORS
```bash
az functionapp cors add --name abcretail-functions-3195 --resource-group AZ-JHB-RSG-RCNA-ST10466568-TER --allowed-origins "https://your-app-domain.com"
```

### Step 2: Update Function App Settings
```bash
az functionapp config appsettings set --name abcretail-functions-3195 --resource-group AZ-JHB-RSG-RCNA-ST10466568-TER --settings \
  "CORS_ALLOWED_ORIGINS=https://your-app-domain.com" \
  "CORS_SUPPORT_CREDENTIALS=true"
```

## 7. Rate Limiting

### Step 1: Implement Rate Limiting
Add to your Function App:

```csharp
// In your function code
[Function("TableOperations")]
public async Task<HttpResponseData> Run(
    [HttpTrigger(AuthorizationLevel.Function, "get", "post", "put", "delete", Route = "table/{operation}")] HttpRequestData req,
    string operation)
{
    // Check rate limiting
    var clientIp = GetClientIpAddress(req);
    if (await IsRateLimited(clientIp))
    {
        var response = req.CreateResponse(HttpStatusCode.TooManyRequests);
        await response.WriteStringAsync("Rate limit exceeded");
        return response;
    }
    
    // Your existing function logic
}
```

### Step 2: Configure Azure API Management (Optional)
1. Create API Management service
2. Import your Function App as an API
3. Configure rate limiting policies
4. Set up throttling rules

## 8. Security Headers

### Step 1: Add Security Headers to Function App
Create a `web.config` file in your Function App:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <system.webServer>
    <httpProtocol>
      <customHeaders>
        <add name="X-Content-Type-Options" value="nosniff" />
        <add name="X-Frame-Options" value="DENY" />
        <add name="X-XSS-Protection" value="1; mode=block" />
        <add name="Strict-Transport-Security" value="max-age=31536000; includeSubDomains" />
        <add name="Content-Security-Policy" value="default-src 'self'" />
      </customHeaders>
    </httpProtocol>
  </system.webServer>
</configuration>
```

## 9. Monitoring and Alerting

### Step 1: Set Up Security Alerts
1. Go to "Security Center"
2. Configure security alerts for:
   - Failed authentication attempts
   - Unusual access patterns
   - Privilege escalation attempts
   - Data exfiltration attempts

### Step 2: Monitor Authentication
- Track login success/failure rates
- Monitor token usage
- Alert on suspicious activities
- Review access logs regularly

## 10. Testing Authentication

### Step 1: Test User Authentication
1. Deploy your application
2. Try accessing protected pages
3. Verify redirect to login
4. Test login/logout flow
5. Verify token validation

### Step 2: Test Function Authentication
1. Test function calls without authentication (should fail)
2. Test with valid tokens (should succeed)
3. Test with expired tokens (should fail)
4. Verify proper error responses

## 11. Production Checklist

- [ ] Azure AD app registration configured
- [ ] Client secret stored securely
- [ ] Function App authentication enabled
- [ ] Key Vault configured with secrets
- [ ] CORS properly configured
- [ ] Rate limiting implemented
- [ ] Security headers added
- [ ] Monitoring and alerting set up
- [ ] Authentication tested thoroughly
- [ ] Documentation updated
- [ ] Team trained on security procedures

## 12. Maintenance Tasks

### Monthly
- Rotate client secrets
- Review access logs
- Update security policies
- Review user permissions

### Quarterly
- Security audit
- Penetration testing
- Update authentication libraries
- Review and update security documentation

This authentication setup will provide enterprise-grade security for your ABCRetail application and Azure Functions.
