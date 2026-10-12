# Product Management & Analytics Suite


---

## 🌟 Executive Summary

This solution is built in response to the **Waterlily Labs C# Web Full-Stack Development Test - 2**. It implements an enterprise-grade ASP.NET Core MVC web application with dual-tier performance caching, Code-First MSSQL database migrations, stored-procedure-backed analytical calculations, C# delegate callbacks, a modern responsive UI powered by Bootstrap 5 and jQuery, and a comprehensive MSTest unit test suite using Moq.

---

## 🏗️ Architecture & Project Structure

The solution strictly adheres to **Clean Architecture** and **SOLID principles**, completely isolating the presentation layer from database operations:

```text
ProductManagement/
│
├── ProductManagement.sln                   --> Classic Visual Studio solution file
├── ProductManagement.slnx                  --> Modern XML solution file
├── PROJECT_PLAN.md                         --> Formal Project Plan (Markdown format)
├── Waterlily_Labs_Project_Plan.docx        --> Project Plan (Microsoft Word format)
├── Waterlily_Labs_Project_Plan.pdf         --> Project Plan (Printable PDF format)
├── README.md                               --> Complete system documentation
│
├── src/
│   ├── ProductManagement.Core/             --> Domain entities, DTOs, interfaces, delegates, services
│   │   ├── Common/                         --> Result pattern, C# delegate callback declarations
│   │   ├── DTOs/                           --> Validated Data Transfer Objects
│   │   ├── Entities/                       --> Domain models and Stored Procedure projections
│   │   ├── Interfaces/                     --> IProductRepository, IProductService, ICacheService
│   │   └── Services/                       --> ProductService, MemoryCacheService
│   │
│   ├── ProductManagement.Data/             --> Data persistence layer
│   │   ├── Context/                        --> ApplicationDbContext (Fluent API, seed data)
│   │   ├── Repositories/                   --> ProductRepository (EF Core + Stored Procedures)
│   │   ├── Migrations/                     --> EF Core Code-First migrations (with SP DDL)
│   │   └── Scripts/                        --> Standalone Stored Procedure SQL definitions
│   │
│   └── ProductManagement.Web/              --> Presentation layer (ASP.NET Core MVC)
│       ├── Controllers/                    --> ProductController, ReportController, HomeController
│       ├── Models/                         --> ViewModels (ProductListViewModel, ReportDashboardViewModel)
│       ├── Views/                          --> Razor views (Product List, Report Dashboard)
│       └── wwwroot/                        --> Custom CSS, jQuery AJAX scripts (product-crud.js)
│
└── tests/
    └── ProductManagement.Tests/            --> MSTest unit testing suite
        ├── Services/                       --> ProductServiceTests (CRUD, Moq isolation, delegates)
        └── Caching/                        --> CacheServiceTests (Cached 5-min vs CachedLong)
```

---

## 📋 Requirements Compliance Matrix

| # | Assessment Requirement | Implementation Detail in Solution |
| :--- | :--- | :--- |
| **1** | **.NET Core Project** | Built with **.NET 8 (LTS)** targeting ASP.NET Core MVC. |
| **2** | **CRUD Operations** | Create, Read, Update, and Delete operations for the `Product` entity with interactive modals and jQuery AJAX. |
| **3** | **Service & Repository Pattern** | `IProductRepository` manages all database calls; `IProductService` encapsulates business logic. Controllers never access the DbContext. |
| **4** | **Code-First Database Pattern** | EF Core migrations targeting **MSSQL LocalDB**. Embedded Stored Procedures (`sp_GetAveragePricePerCategory`, `sp_GetHighestStockValueCategory`). |
| **5** | **Caching Mechanism** | `ICacheService` with: <br>&bull; `Cached`: Generic result cached up to **5 minutes**.<br>&bull; `CachedLong`: Retained indefinitely until removed. Coordinated cache eviction upon mutations. |
| **6** | **C# Delegates** | `ProductOperationCallback` handles post-operation audit logging; `StockAlertCallback` detects and alerts on low-stock inventory (&lt; 10 units). |
| **7** | **UI / Scripting** | Responsive Bootstrap 5 layout with custom CSS (`site.css`), plus **jQuery** for modal handling, form submission, and live filtering. |
| **8** | **Unit Testing** | **MSTest** test project with **Moq** isolating the repository and cache dependencies. 15 passing unit tests. |
| **9** | **Project Plan Document** | Standalone [`PROJECT_PLAN.md`](PROJECT_PLAN.md) detailing requirements understanding, approach, potential issues, and pseudocode. |

---

## 🚀 Getting Started

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later
- [SQL Server LocalDB](https://learn.microsoft.com/sql/database-engine/configure-windows/sql-server-express-localdb) (installed automatically with Visual Studio) or SQL Server Express

### 1. Running the Web Application
```powershell
# From the solution root:
dotnet run --project src/ProductManagement.Web
```
The application will automatically:
1. Connect to MSSQL (default: `Server=(localdb)\MSSQLLocalDB;Database=ProductDb`, or any local instance like `.\SQLEXPRESS`).
2. Apply pending EF Core migrations automatically on startup.
3. Create the `Products` table and seed realistic initial records.
4. Create and compile both Stored Procedures.
5. Launch at `http://localhost:5223` (or the configured HTTPS port).

### 2. Running the Unit Tests
```powershell
dotnet test ProductManagement.sln
```
All 15 MSTest tests will execute and verify service layer CRUD operations, boundary conditions, delegate triggers, and caching behaviors.

---

## 💡 Key Architectural Decisions & Explanations

### 1. Stored Procedures via EF Core Migrations
Rather than running manual scripts outside the repository, the two required stored procedures are embedded directly inside the Code-First migration (`20261008154955_InitialCreate.cs`):
```csharp
migrationBuilder.Sql(@"
    CREATE OR ALTER PROCEDURE sp_GetAveragePricePerCategory
    AS
    BEGIN
        SET NOCOUNT ON;
        SELECT Category, CAST(AVG(Price) AS DECIMAL(18,2)) AS AveragePrice, COUNT(Id) AS ProductCount
        FROM Products
        GROUP BY Category;
    END
");
```
This guarantees automated, idempotent deployment on any development or CI/CD environment.

### 2. Delegate-Driven Callbacks
In `ProductManagement.Core.Common.Delegates`:
```csharp
public delegate void ProductOperationCallback(string operation, ProductDto? product, bool isSuccess, string? message);
public delegate void StockAlertCallback(int productId, string productName, int currentStock);
```
These delegates allow controllers to supply callback handlers for cross-cutting concerns (audit logging and inventory threshold alerts) without tightly coupling the service layer to logging or notification frameworks.

### 3. Dual-Tier Caching & Consistency
- `Cached<T>`: Applied to frequently accessed lists with a 5-minute time-to-live.
- `CachedLong<T>`: Retained indefinitely for long-term calculations.
- **Cache Eviction**: Any mutation in `ProductService` (`Create`, `Update`, `Delete`) invalidates all product catalog and reporting cache entries immediately to prevent stale data.

---

## 🎯 Technical Interview Talking Points

1. **Why Layered Architecture?**
   > *"Decoupling into Core, Data, and Web enforces the Dependency Inversion Principle. Core holds pure business logic and contracts with zero external database dependencies, making it 100% unit-testable in isolation using Moq."*

2. **How is the Database Isolated?**
   > *"Controllers only inject `IProductService`. Views only receive ViewModels and DTOs. Entity Framework's `ApplicationDbContext` is strictly internal to `ProductManagement.Data`."*

3. **How are Stored Procedures Queried in EF Core?**
   > *"Using keyless entity types configured with `.HasNoKey()` and mapped to Stored Procedures via `_context.Set<T>().FromSqlRaw('EXEC sp_...').AsNoTracking().ToListAsync()`. This maintains strong typing while leveraging database-side aggregations."*
