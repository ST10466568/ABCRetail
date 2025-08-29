# ABCRetail - ASP.NET Core Retail Management System

## 🚀 Project Overview

ABCRetail is a comprehensive retail management system built with ASP.NET Core that provides full CRUD (Create, Read, Update, Delete) functionality for managing customers and products. The system integrates with Azure cloud services for data storage and management.

## ✨ Features

### ✅ Complete CRUD Functionality
- **Customers Management**: Full CRUD operations for customer records
- **Products Management**: Complete product lifecycle management
- **Real-time Data**: Live data synchronization with Azure services
- **Responsive UI**: Modern, user-friendly interface

### 🔧 Technical Features
- **ASP.NET Core 8.0**: Latest .NET framework
- **Azure Integration**: Azure Table Storage, Blob Storage, and Queue Storage
- **Entity Framework**: Robust data access layer
- **Razor Pages**: Clean, maintainable web interface
- **Azure SDK**: Native Azure service integration

## 🏗️ Architecture

The application follows a clean architecture pattern with:
- **Presentation Layer**: Razor Pages for web interface
- **Business Logic Layer**: Services for business operations
- **Data Access Layer**: Azure SDK integration
- **Infrastructure Layer**: Azure cloud services

## 🚀 Getting Started

### Prerequisites
- .NET 8.0 SDK
- Azure subscription with storage account
- Visual Studio 2022 or VS Code

### Installation
1. Clone the repository
```bash
git clone https://github.com/ST10466568/ABCRetail.git
cd ABCRetail
```

2. Restore dependencies
```bash
dotnet restore
```

3. Configure Azure connection strings:
   - Copy `appsettings.example.json` to `appsettings.json`
   - Fill in your Azure Storage Account details
   - **Never commit real connection strings to source control**

4. Run the application
```bash
dotnet run
```

## 📊 CRUD Operations Status

### ✅ Customers CRUD - FULLY FUNCTIONAL
- **Create**: ✅ Add new customers with validation
- **Read**: ✅ List, search, and view customer details
- **Update**: ✅ Modify customer information with ETag handling
- **Delete**: ✅ Remove customers with confirmation

### ✅ Products CRUD - FULLY FUNCTIONAL
- **Create**: ✅ Add new products with image upload
- **Read**: ✅ List, search, and view product details
- **Update**: ✅ Modify product information and images
- **Delete**: ✅ Remove products with confirmation

## 🔗 Azure Services Integration

- **Azure Table Storage**: Customer and Product data
- **Azure Blob Storage**: Product image management
- **Azure Queue Storage**: Inventory management messages
- **Azure SDK**: Native .NET integration

## 🧪 Testing

The application includes comprehensive testing for all CRUD operations:
- Unit tests for business logic
- Integration tests for Azure services
- End-to-end testing for user workflows

## 📱 User Interface

- **Modern Design**: Clean, responsive interface
- **Mobile Friendly**: Optimized for all device sizes
- **Intuitive Navigation**: Easy-to-use CRUD operations
- **Real-time Updates**: Live data synchronization

## 🚀 Deployment

The application is designed for easy deployment to:
- Azure App Service
- Docker containers
- On-premises servers

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Create a Pull Request

## 📄 License

This project is licensed under the MIT License.

## 🎯 Project Status

**CRUD Functionality**: ✅ **100% COMPLETE AND WORKING**

All Create, Read, Update, and Delete operations are fully functional for both Customers and Products entities. The system has been thoroughly tested and verified to work correctly with Azure cloud services.

---

**Built with ❤️ using ASP.NET Core and Azure**
