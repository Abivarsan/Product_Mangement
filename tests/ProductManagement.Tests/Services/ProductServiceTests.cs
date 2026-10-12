#pragma warning disable MSTEST0037

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using ProductManagement.Core.Common;
using ProductManagement.Core.DTOs;
using ProductManagement.Core.Entities;
using ProductManagement.Core.Interfaces;
using ProductManagement.Core.Services;

namespace ProductManagement.Tests.Services;

/// <summary>
/// Unit tests verifying ProductService CRUD operations, business logic,
/// cache synchronization, and delegate callbacks.
/// </summary>
[TestClass]
public class ProductServiceTests
{
    private Mock<IProductRepository> _mockRepo = null!;
    private Mock<ICacheService> _mockCache = null!;
    private Mock<ILogger<ProductService>> _mockLogger = null!;
    private ProductService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockRepo = new Mock<IProductRepository>();
        _mockCache = new Mock<ICacheService>();
        _mockLogger = new Mock<ILogger<ProductService>>();

        // Configure default cache behavior to invoke the factory delegate (simulating cache miss)
        _mockCache.Setup(c => c.CachedAsync(It.IsAny<string>(), It.IsAny<Func<Task<List<ProductDto>>>>()))
            .Returns<string, Func<Task<List<ProductDto>>>>((_, factory) => factory());

        _mockCache.Setup(c => c.GetOrCreateAsync(It.IsAny<string>(), It.IsAny<Func<Task<ProductDto>>>(), It.IsAny<TimeSpan?>()))
            .Returns<string, Func<Task<ProductDto>>, TimeSpan?>((_, factory, _) => factory());

        _service = new ProductService(_mockRepo.Object, _mockCache.Object, _mockLogger.Object);
    }

    #region Read Tests

    [TestMethod]
    public async Task GetAllProductsAsync_ReturnsAllProducts_WhenDataExists()
    {
        // Arrange
        var products = new List<Product>
        {
            new() { Id = 1, Name = "Laptop", Category = "Electronics", Price = 999m, Stock = 10 },
            new() { Id = 2, Name = "Desk", Category = "Furniture", Price = 250m, Stock = 15 }
        };
        _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(products);

        // Act
        var result = await _service.GetAllProductsAsync();

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Data);
        Assert.AreEqual(2, result.Data.Count());
        _mockRepo.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [TestMethod]
    public async Task GetProductByIdAsync_ExistingId_ReturnsSuccessWithProduct()
    {
        // Arrange
        var product = new Product { Id = 1, Name = "Laptop", Category = "Electronics", Price = 999m, Stock = 10 };
        _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(product);

        // Act
        var result = await _service.GetProductByIdAsync(1);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Data);
        Assert.AreEqual("Laptop", result.Data.Name);
        Assert.AreEqual(999m, result.Data.Price);
    }

    [TestMethod]
    public async Task GetProductByIdAsync_NonExistingId_ReturnsFailure()
    {
        // Arrange
        _mockRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Product?)null);

        // Act
        var result = await _service.GetProductByIdAsync(999);

        // Assert
        Assert.IsFalse(result.IsSuccess);
        Assert.IsTrue(result.Message!.Contains("not found", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public async Task GetProductByIdAsync_InvalidNegativeId_ReturnsFailure()
    {
        // Act
        var result = await _service.GetProductByIdAsync(-5);

        // Assert
        Assert.IsFalse(result.IsSuccess);
        _mockRepo.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
    }

    #endregion

    #region Create Tests

    [TestMethod]
    public async Task CreateProductAsync_ValidData_AddsProduct_InvalidatesCache_AndInvokesCallback()
    {
        // Arrange
        var dto = new CreateProductDto
        {
            Name = "Mechanical Keyboard",
            Category = "Electronics",
            Price = 129.99m,
            Stock = 25
        };

        var savedEntity = new Product
        {
            Id = 101,
            Name = dto.Name,
            Category = dto.Category,
            Price = dto.Price,
            Stock = dto.Stock,
            CreatedAt = DateTime.UtcNow
        };

        _mockRepo.Setup(r => r.AddAsync(It.IsAny<Product>())).ReturnsAsync(savedEntity);

        bool callbackInvoked = false;
        string? callbackOp = null;
        ProductOperationCallback callback = (op, product, success, msg) =>
        {
            callbackInvoked = true;
            callbackOp = op;
            Assert.IsTrue(success);
            Assert.IsNotNull(product);
        };

        // Act
        var result = await _service.CreateProductAsync(dto, callback);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Data);
        Assert.AreEqual(101, result.Data.Id);
        Assert.AreEqual("Mechanical Keyboard", result.Data.Name);

        // Verify delegate callback was invoked
        Assert.IsTrue(callbackInvoked);
        Assert.AreEqual("Create", callbackOp);

        // Verify cache invalidation occurred
        _mockCache.Verify(c => c.Remove(ProductService.AllProductsCacheKey), Times.Once);
        _mockRepo.Verify(r => r.AddAsync(It.IsAny<Product>()), Times.Once);
    }

    [TestMethod]
    public async Task CreateProductAsync_LowStock_TriggersStockAlertCallback()
    {
        // Arrange
        var dto = new CreateProductDto
        {
            Name = "Wireless Earbuds",
            Category = "Electronics",
            Price = 49.99m,
            Stock = 4 // < 10 threshold
        };

        var savedEntity = new Product
        {
            Id = 102,
            Name = dto.Name,
            Category = dto.Category,
            Price = dto.Price,
            Stock = dto.Stock
        };

        _mockRepo.Setup(r => r.AddAsync(It.IsAny<Product>())).ReturnsAsync(savedEntity);

        bool stockAlertTriggered = false;
        int alertedStock = 0;
        StockAlertCallback stockCallback = (id, name, stock) =>
        {
            stockAlertTriggered = true;
            alertedStock = stock;
        };

        // Act
        var result = await _service.CreateProductAsync(dto, stockAlertCallback: stockCallback);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsTrue(stockAlertTriggered);
        Assert.AreEqual(4, alertedStock);
    }

    [TestMethod]
    public async Task CreateProductAsync_InvalidZeroPrice_ReturnsFailure_AndDoesNotCallRepository()
    {
        // Arrange
        var dto = new CreateProductDto
        {
            Name = "Free Item",
            Category = "Promotional",
            Price = 0m, // Invalid
            Stock = 10
        };

        bool callbackInvoked = false;
        ProductOperationCallback callback = (op, product, success, msg) =>
        {
            callbackInvoked = true;
            Assert.IsFalse(success);
        };

        // Act
        var result = await _service.CreateProductAsync(dto, callback);

        // Assert
        Assert.IsFalse(result.IsSuccess);
        Assert.IsTrue(callbackInvoked);
        _mockRepo.Verify(r => r.AddAsync(It.IsAny<Product>()), Times.Never);
    }

    #endregion

    #region Update Tests

    [TestMethod]
    public async Task UpdateProductAsync_ExistingProduct_UpdatesAndInvalidatesCache()
    {
        // Arrange
        var dto = new UpdateProductDto
        {
            Id = 5,
            Name = "Updated Standing Desk",
            Category = "Furniture",
            Price = 699.00m,
            Stock = 15
        };

        var existingEntity = new Product { Id = 5, Name = "Old Desk", Category = "Furniture", Price = 500m, Stock = 10 };
        _mockRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(existingEntity);
        _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<Product>())).ReturnsAsync(existingEntity);

        bool callbackInvoked = false;
        ProductOperationCallback callback = (op, p, s, msg) => { callbackInvoked = true; };

        // Act
        var result = await _service.UpdateProductAsync(dto, callback);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsTrue(callbackInvoked);
        Assert.AreEqual("Updated Standing Desk", result.Data!.Name);
        _mockCache.Verify(c => c.Remove(ProductService.AllProductsCacheKey), Times.Once);
        _mockRepo.Verify(r => r.UpdateAsync(It.IsAny<Product>()), Times.Once);
    }

    [TestMethod]
    public async Task UpdateProductAsync_NonExistingProduct_ReturnsFailure()
    {
        // Arrange
        var dto = new UpdateProductDto { Id = 99, Name = "Non-existent", Category = "General", Price = 10m, Stock = 5 };
        _mockRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Product?)null);

        // Act
        var result = await _service.UpdateProductAsync(dto);

        // Assert
        Assert.IsFalse(result.IsSuccess);
        _mockRepo.Verify(r => r.UpdateAsync(It.IsAny<Product>()), Times.Never);
    }

    #endregion

    #region Delete Tests

    [TestMethod]
    public async Task DeleteProductAsync_ExistingProduct_DeletesAndInvalidatesCache()
    {
        // Arrange
        var product = new Product { Id = 7, Name = "Book", Category = "Books", Price = 25m, Stock = 50 };
        _mockRepo.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(product);
        _mockRepo.Setup(r => r.DeleteAsync(7)).ReturnsAsync(true);

        bool callbackInvoked = false;
        ProductOperationCallback callback = (op, p, s, msg) => { callbackInvoked = true; };

        // Act
        var result = await _service.DeleteProductAsync(7, callback);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsTrue(callbackInvoked);
        _mockCache.Verify(c => c.Remove(ProductService.AllProductsCacheKey), Times.Once);
        _mockRepo.Verify(r => r.DeleteAsync(7), Times.Once);
    }

    [TestMethod]
    public async Task DeleteProductAsync_NonExistingProduct_ReturnsFailure()
    {
        // Arrange
        _mockRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Product?)null);

        // Act
        var result = await _service.DeleteProductAsync(999);

        // Assert
        Assert.IsFalse(result.IsSuccess);
        _mockRepo.Verify(r => r.DeleteAsync(It.IsAny<int>()), Times.Never);
    }

    #endregion

    #region Report & Stored Procedure Tests

    [TestMethod]
    public async Task GetReportSummaryAsync_ExecutesStoredProcedures_AndReturnsMappedSummary()
    {
        // Arrange
        var avgPrices = new List<CategoryAveragePriceReport>
        {
            new() { Category = "Electronics", AveragePrice = 500m, ProductCount = 3 },
            new() { Category = "Furniture", AveragePrice = 300m, ProductCount = 2 }
        };

        var highestStock = new HighestStockValueCategoryReport
        {
            Category = "Electronics",
            TotalStockValue = 45000m,
            TotalStockQuantity = 90
        };

        var allProducts = new List<Product>
        {
            new() { Id = 1, Name = "P1", Category = "Electronics", Price = 500m, Stock = 50 },
            new() { Id = 2, Name = "P2", Category = "Furniture", Price = 300m, Stock = 20 }
        };

        _mockRepo.Setup(r => r.GetAveragePricePerCategoryFromSpAsync()).ReturnsAsync(avgPrices);
        _mockRepo.Setup(r => r.GetHighestStockValueCategoryFromSpAsync()).ReturnsAsync(highestStock);
        _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(allProducts);

        _mockCache.Setup(c => c.CachedAsync(It.IsAny<string>(), It.IsAny<Func<Task<ReportSummaryDto>>>()))
            .Returns<string, Func<Task<ReportSummaryDto>>>((_, factory) => factory());

        // Act
        var result = await _service.GetReportSummaryAsync(useLongCache: false);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Data);
        Assert.AreEqual(2, result.Data.CategoryAveragePrices.Count);
        Assert.AreEqual("Electronics", result.Data.HighestStockValueCategory!.Category);
        Assert.AreEqual(45000m, result.Data.HighestStockValueCategory.TotalStockValue);
        Assert.AreEqual(2, result.Data.TotalProductsCount);
        Assert.AreEqual((500m * 50) + (300m * 20), result.Data.TotalInventoryValue);
    }

    #endregion
}
