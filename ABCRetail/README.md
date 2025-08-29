# ABC Retail - .NET 8 Razor Pages Web Application

A modern, professional web application for ABC Retail that integrates with Azure Storage Services, featuring a Lovable-inspired design system.

## 🚀 Features

### Core Functionality
- **Customer Management**: Full CRUD operations for customer profiles
- **Product Catalog**: Product management with image support
- **Order Tracking**: Order management and status tracking
- **Log Management**: Application log viewing and download
- **Real-time Dashboard**: Live KPI monitoring and queue activity

### Azure Integration
- **Azure Tables**: Customer and product data storage
- **Azure Blob Storage**: Product image management
- **Azure Queue Storage**: Order, inventory, and image processing queues
- **Azure File Storage**: Application log storage

### Design Features
- **Modern UI/UX**: Clean, professional interface inspired by Lovable design
- **Responsive Design**: Mobile-first approach with Bootstrap 5
- **Interactive Elements**: Hover effects, animations, and smooth transitions
- **Professional Color Palette**: Soft shadows, rounded corners, and pastel colors

## 🛠️ Technology Stack

- **.NET 8**: Latest .NET framework with Razor Pages
- **Azure Storage SDK**: Full integration with Azure services
- **Bootstrap 5**: Modern CSS framework for responsive design
- **Bootstrap Icons**: Professional icon library
- **JavaScript**: Interactive functionality and real-time updates

## 📋 Prerequisites

- .NET 8 SDK
- Azure Storage Account
- Visual Studio 2022 or VS Code
- Modern web browser

## 🔧 Setup Instructions

### 1. Clone the Repository
```bash
git clone <repository-url>
cd ABCRetail
```

### 2. Configure Azure Storage
1. Create an Azure Storage Account
2. Create the following resources:
   - **Tables**: `Customers`, `Products`, `Orders`
   - **Blob Container**: `product-images`
   - **Queues**: `order-queue`, `inventory-queue`, `image-queue`
   - **File Share**: `applogs`

### 3. Update Configuration
Edit `appsettings.json` and replace the placeholder connection string:
```json
{
  "AzureStorage": {
    "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=YOUR_ACCOUNT_NAME;AccountKey=YOUR_ACCOUNT_KEY;EndpointSuffix=core.windows.net"
  }
}
```

### 4. Install Dependencies
```bash
dotnet restore
```

### 5. Run the Application
```bash
dotnet run
```

The application will be available at `https://localhost:5001`

## 🏗️ Project Structure

```
ABCRetail/
├── Models/                 # Data models
│   ├── Customer.cs        # Customer entity
│   ├── Product.cs         # Product entity
│   ├── Order.cs           # Order entity
│   └── LogEntry.cs        # Log entry model
├── Services/              # Azure service implementations
│   ├── IAzureTableService.cs
│   ├── AzureTableService.cs
│   ├── IAzureBlobService.cs
│   ├── AzureBlobService.cs
│   ├── IAzureQueueService.cs
│   ├── AzureQueueService.cs
│   ├── IAzureFileService.cs
│   ├── AzureFileService.cs
│   └── DataSeederService.cs
├── Pages/                 # Razor Pages
│   ├── Index.cshtml      # Dashboard
│   ├── Customers/        # Customer management
│   ├── Products/         # Product management
│   ├── Orders/           # Order management
│   └── Logs/             # Log viewing
├── wwwroot/              # Static files
│   ├── css/             # Custom stylesheets
│   ├── js/              # JavaScript files
│   └── images/          # Product images
└── Program.cs            # Application entry point
```

## 🎨 Design System

### Color Palette
- **Primary**: #6366f1 (Indigo)
- **Secondary**: #f8fafc (Light Gray)
- **Accent**: #f1f5f9 (Subtle Gray)
- **Text**: #1e293b (Dark Gray)

### Typography
- **Font Family**: Inter, system fonts
- **Font Weights**: 400 (Regular), 500 (Medium), 600 (Semi-bold), 700 (Bold)

### Components
- **Cards**: Rounded corners (16px), soft shadows
- **Buttons**: Rounded (12px), hover animations
- **Forms**: Floating labels, focus states
- **Tables**: Striped rows, hover effects

## 🔄 Azure Services Integration

### Azure Tables
- **CustomerProfiles**: Customer information storage
- **Products**: Product catalog and inventory
- **Orders**: Order tracking and management

### Azure Blob Storage
- **Container**: `product-images`
- **Features**: Upload, download, delete, list images
- **Formats**: JPG, PNG, GIF support

### Azure Queue Storage
- **order-queue**: Order processing messages
- **inventory-queue**: Stock level alerts
- **image-queue**: Image processing notifications

### Azure File Storage
- **Share**: `applogs`
- **Features**: Log file storage, retrieval, and download
- **Formats**: .log, .txt files

## 📱 Responsive Design

- **Mobile First**: Optimized for mobile devices
- **Breakpoints**: 576px, 768px, 992px, 1200px
- **Grid System**: Bootstrap 5 responsive grid
- **Navigation**: Collapsible sidebar for mobile

## 🚀 Deployment

### Azure App Service
1. Create an Azure App Service
2. Configure environment variables
3. Deploy using Azure DevOps or GitHub Actions

### Local Development
```bash
dotnet run --environment Development
```

### Production
```bash
dotnet publish -c Release
```

## 🔍 Key Features

### Dashboard
- Real-time KPI monitoring
- Recent queue activity
- Featured products gallery
- Quick action buttons

### Customer Management
- Customer profile creation
- Search and filtering
- Address management
- Status tracking

### Product Catalog
- Grid and list views
- Category and brand filtering
- Image upload and management
- Stock level monitoring

### Log Management
- File explorer interface
- Log content viewing
- Download functionality
- File deletion

## 🧪 Testing

### Demo Data
The application includes a data seeder service that populates:
- Sample customers
- Demo products
- Queue messages
- Log files

### Manual Testing
1. Navigate through all pages
2. Test CRUD operations
3. Verify Azure service connections
4. Check responsive design

## 🐛 Troubleshooting

### Common Issues
1. **Azure Connection**: Verify connection string and permissions
2. **Image Upload**: Check blob container permissions
3. **Queue Messages**: Ensure queue names match configuration
4. **Log Files**: Verify file share access

### Debug Mode
Enable detailed logging in `appsettings.Development.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information"
    }
  }
}
```

## 📄 License

This project is licensed under the MIT License.

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Submit a pull request

## 📞 Support

For support and questions:
- Create an issue in the repository
- Contact the development team
- Check Azure documentation for service-specific issues

---

**Built with ❤️ using .NET 8 and Azure Services**


