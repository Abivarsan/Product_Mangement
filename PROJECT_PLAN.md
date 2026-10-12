# Waterlily Labs - Mid-Level C# Developer Assessment
## Project Plan Document: Product Management & Reporting System

**Candidate Submission**  
**Role**: .NET Core C# Developer – Mid-Level  
**Date**: October 2026  

---

### 1. Understanding of the Requirements

The goal is to develop an enterprise-ready ASP.NET Core MVC web application for managing a product catalog and displaying aggregated calculation reports.

#### Key Functional Deliverables:
1. **Product List Page**:
   - Display product records containing: `Product ID`, `Name`, `Category`, `Price`, and `Stock`.
   - Full CRUD operations (Create, Read, Update, Delete) with interactive modal dialogs and AJAX feedback.
2. **Report Page**:
   - **Calculation 1**: The average price of products in each category (without requiring a separate Category table).
   - **Calculation 2**: The category with the highest total stock inventory value (`Price * Stock`).
3. **Architectural & Technical Constraints**:
   - **Service & Repository Pattern**: Strict separation between Data Access (Repository) and Business Logic (Service). Controllers and views must never interact directly with database contexts.
   - **Code-First Database Pattern**: Entity Framework Core with Code-First migrations targeting **MSSQL**.
   - **Stored Procedures**: Aggregation calculations must be executed via database stored procedures (`sp_GetAveragePricePerCategory` and `sp_GetHighestStockValueCategory`).
   - **Caching Strategy**: Service-layer caching with two distinct retention tiers:
     - `Cached`: Retains generic results for up to 5 minutes.
     - `CachedLong`: Retains generic results indefinitely until explicitly invalidated.
   - **C# Delegates**: Native callback mechanisms using C# delegates (`ProductOperationCallback`, `StockAlertCallback`) for auditing and notification workflows.
   - **UI & Client Scripting**: Bootstrap 5 + custom modern CSS alongside jQuery for asynchronous operations.
   - **Unit Testing**: Automated tests written with **MSTest** isolating data access dependencies using **Moq**.

---

### 2. Implementation Approach & Technical Plan

```
┌─────────────────────────────────────────────────────────────┐
│                 Presentation: MVC / Razor                   │
│   (Controllers, jQuery AJAX Modals, Bootstrap 5 UI Cards)   │
└──────────────────────────────┬──────────────────────────────┘
                               │ Calls via DI
┌──────────────────────────────▼──────────────────────────────┐
│                  Service Layer (Business Logic)             │
│   (IProductService, MemoryCacheService, Delegate Callbacks) │
└──────────────────────────────┬──────────────────────────────┘
                               │ Calls via DI
┌──────────────────────────────▼──────────────────────────────┐
│                  Repository Layer (Data Access)             │
│  (IProductRepository, EF Core DbContext, Stored Procedures) │
└──────────────────────────────┬──────────────────────────────┘
                               │ Code-First Migrations
┌──────────────────────────────▼──────────────────────────────┐
│                    MSSQL Database Engine                    │
│    (Products Table, sp_GetAvgPrice, sp_GetHighestStock)     │
└─────────────────────────────────────────────────────────────┘
```

1. **Solution Structure**:
   - `ProductManagement.Core`: Domain Entities, DTOs, Interfaces, Service implementations, Caching, and Delegates.
   - `ProductManagement.Data`: EF Core DbContext, Migrations, Repository implementation, and Stored Procedure scripts.
   - `ProductManagement.Web`: ASP.NET Core MVC controllers, ViewModels, Views, and static client assets.
   - `ProductManagement.Tests`: MSTest project with Moq for service isolation tests.

2. **Data & Storage Strategy**:
   - EF Core Code-First entity mapping with decimal precision configuration (`decimal(18,2)`).
   - Embedded Stored Procedures within EF Core migrations using `migrationBuilder.Sql(...)`.

3. **Performance & Caching Strategy**:
   - `ICacheService` wraps `IMemoryCache` offering both `Cached` (5 min) and `CachedLong` (no fixed expiration).
   - Dynamic cache eviction: Mutation operations (`Create`, `Update`, `Delete`) invalidate product list and report cache entries to prevent stale reads.

4. **Delegates & Callbacks**:
   - `ProductOperationCallback`: Invoked after CRUD operations to record auditing history or telemetry.
   - `StockAlertCallback`: Invoked when stock levels are modified below threshold (< 10 units) to trigger low inventory notifications.

---

### 3. Potential Issues & Mitigation Strategies

| Issue / Risk | Potential Impact | Mitigation Strategy |
| :--- | :--- | :--- |
| **Cache Invalidation Drift** | Stale analytical reports shown after a product price or stock update. | Coordinated cache eviction in `ProductService` triggered automatically upon any mutating operation. |
| **Division by Zero in SQL** | Arithmetic error when calculating category averages if category row counts are zero. | Handled safely in SQL stored procedure via `COUNT` and aggregate arithmetic. |
| **Floating Point Inaccuracy** | Monetary rounding discrepancies across calculations. | Consistent use of `decimal(18,2)` in SQL, C# DTOs, and view formatting. |
| **Tampered Client Input** | Negative price or stock injected past browser forms. | Comprehensive server-side validation via Data Annotations and explicit domain validation guards. |
| **Environment Portability** | Database connection failure on different developer machines. | Default connection string configured for `(localdb)\MSSQLLocalDB` (Database: `ProductDb`) with automated migration application on startup. |

---

### 4. Pseudocode Covering Required Logic

#### 4.1 Caching Mechanism (`Cached` vs `CachedLong`)
```text
CLASS MemoryCacheService IMPLEMENTS ICacheService:
    FUNCTION Cached<T>(key, factory):
        RETURN GetOrCreate(key, factory, Expiration = 5 Minutes)

    FUNCTION CachedLong<T>(key, factory):
        RETURN GetOrCreate(key, factory, Expiration = NULL) // Retained indefinitely

    FUNCTION GetOrCreate<T>(key, factory, expiration):
        IF cache.TryGetValue(key, out existingValue) THEN:
            RETURN existingValue
        value = factory()
        IF expiration HAS VALUE THEN:
            cache.Set(key, value, AbsoluteExpirationRelativeToNow = expiration)
        ELSE:
            cache.Set(key, value) // No expiration
        RETURN value
```

#### 4.2 Product Creation with Validation, Invalidation & Delegate Callback
```text
FUNCTION CreateProductAsync(dto, callbackDelegate, stockAlertDelegate):
    // 1. Validation Guard
    IF dto.Price <= 0 OR dto.Stock < 0 THEN:
        callbackDelegate?.Invoke("Create", NULL, FALSE, "Price and Stock must be non-negative.")
        RETURN Result.Failure("Invalid values.")

    // 2. Persist Entity
    product = MapToEntity(dto)
    savedProduct = repository.AddAsync(product)

    // 3. Cache Eviction (Prevent Stale Cache)
    cacheService.Remove("products:all")
    cacheService.Remove("reports:summary")

    // 4. Delegate Callbacks
    IF savedProduct.Stock < 10 THEN:
        stockAlertDelegate?.Invoke(savedProduct.Id, savedProduct.Name, savedProduct.Stock)

    productDto = MapToDto(savedProduct)
    callbackDelegate?.Invoke("Create", productDto, TRUE, "Product created successfully.")

    RETURN Result.Success(productDto)
```

#### 4.3 Stored Procedure Aggregations
```sql
-- Calculation 1: Average Price Per Category
CREATE PROCEDURE sp_GetAveragePricePerCategory AS
BEGIN
    SET NOCOUNT ON;
    SELECT 
        Category,
        CAST(AVG(Price) AS DECIMAL(18,2)) AS AveragePrice,
        COUNT(Id) AS ProductCount
    FROM Products
    GROUP BY Category
    ORDER BY Category ASC;
END;

-- Calculation 2: Highest Stock Value Category
CREATE PROCEDURE sp_GetHighestStockValueCategory AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP 1
        Category,
        CAST(SUM(Price * Stock) AS DECIMAL(18,2)) AS TotalStockValue,
        SUM(Stock) AS TotalStockQuantity
    FROM Products
    GROUP BY Category
    ORDER BY TotalStockValue DESC;
END;
```

---

### 5. Development Verification Plan
- [x] Solution and project structure setup (.NET 8 Clean Architecture)
- [x] Domain entity and DTO definitions
- [x] Repository and Service contracts with Delegate signatures
- [ ] EF Core DbContext and Code-First Migrations with embedded Stored Procedures
- [ ] Repository implementation for CRUD & Stored Procedure execution
- [ ] MemoryCacheService (`Cached` 5-min & `CachedLong`) and ProductService implementation
- [ ] MVC Controllers and ViewModels
- [ ] Responsive UI Views (Bootstrap 5 + Custom Styling + jQuery AJAX Modal)
- [ ] Comprehensive MSTest test suite with Moq
- [ ] Application build, database seeding, and end-to-end verification
