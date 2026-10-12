using Microsoft.Extensions.Logging;
using ProductManagement.Core.Common;
using ProductManagement.Core.DTOs;
using ProductManagement.Core.Entities;
using ProductManagement.Core.Interfaces;

namespace ProductManagement.Core.Services;

/// <summary>
/// Service layer implementing product business logic, cache synchronization,
/// and delegate callback execution.
/// </summary>
public class ProductService : IProductService
{
    private readonly IProductRepository _repository;
    private readonly ICacheService _cacheService;
    private readonly ILogger<ProductService> _logger;

    // Cache keys
    public const string AllProductsCacheKey = "products:all";
    public const string SingleProductCacheKeyPrefix = "products:id:";
    public const string ReportSummary5MinCacheKey = "reports:summary:5min";
    public const string ReportSummaryLongCacheKey = "reports:summary:long";

    public ProductService(
        IProductRepository repository,
        ICacheService cacheService,
        ILogger<ProductService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Retrieves all products with 5-minute caching (Cached).
    /// </summary>
    public async Task<Result<IEnumerable<ProductDto>>> GetAllProductsAsync()
    {
        try
        {
            var dtos = await _cacheService.CachedAsync(AllProductsCacheKey, async () =>
            {
                _logger.LogInformation("Fetching products from repository...");
                var entities = await _repository.GetAllAsync();
                return entities.Select(MapToDto).ToList();
            });

            return Result<IEnumerable<ProductDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all products.");
            return Result<IEnumerable<ProductDto>>.Failure("Unable to retrieve products at this time.", ex.Message);
        }
    }

    /// <summary>
    /// Retrieves a single product by ID with caching.
    /// </summary>
    public async Task<Result<ProductDto>> GetProductByIdAsync(int id)
    {
        if (id <= 0)
        {
            return Result<ProductDto>.Failure("Invalid product ID.");
        }

        try
        {
            var cacheKey = $"{SingleProductCacheKeyPrefix}{id}";
            var dto = await _cacheService.GetOrCreateAsync(cacheKey, async () =>
            {
                var entity = await _repository.GetByIdAsync(id);
                return entity != null ? MapToDto(entity) : null!;
            }, TimeSpan.FromMinutes(5));

            if (dto == null)
            {
                return Result<ProductDto>.Failure($"Product with ID {id} was not found.");
            }

            return Result<ProductDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving product with ID {Id}", id);
            return Result<ProductDto>.Failure($"Failed to retrieve product {id}.", ex.Message);
        }
    }

    /// <summary>
    /// Creates a new product, invalidates stale caches, and triggers delegate callbacks.
    /// </summary>
    public async Task<Result<ProductDto>> CreateProductAsync(
        CreateProductDto dto,
        ProductOperationCallback? callback = null,
        StockAlertCallback? stockAlertCallback = null)
    {
        // 1. Validation Guards
        var validationErrors = ValidateProductInput(dto.Name, dto.Category, dto.Price, dto.Stock);
        if (validationErrors.Any())
        {
            var errorMsg = string.Join("; ", validationErrors);
            _logger.LogWarning("CreateProduct validation failed: {Errors}", errorMsg);
            callback?.Invoke("Create", null, false, errorMsg);
            return Result<ProductDto>.Failure("Product validation failed.", validationErrors);
        }

        try
        {
            // 2. Map DTO to Entity
            var entity = new Product
            {
                Name = dto.Name.Trim(),
                Category = dto.Category.Trim(),
                Price = dto.Price,
                Stock = dto.Stock,
                CreatedAt = DateTime.UtcNow
            };

            // 3. Persist via Repository
            var createdEntity = await _repository.AddAsync(entity);
            var resultDto = MapToDto(createdEntity);

            // 4. Invalidate Cache to ensure fresh data
            InvalidateAllProductCaches();

            // 5. Trigger Stock Alert Delegate if stock is critically low (< 10)
            if (resultDto.Stock < 10)
            {
                _logger.LogInformation("Low stock alert triggered for product {Name} (Stock: {Stock})", resultDto.Name, resultDto.Stock);
                stockAlertCallback?.Invoke(resultDto.Id, resultDto.Name, resultDto.Stock);
            }

            // 6. Trigger Operation Callback Delegate
            callback?.Invoke("Create", resultDto, true, $"Product '{resultDto.Name}' created successfully.");

            _logger.LogInformation("Successfully created product {Id} - {Name}", resultDto.Id, resultDto.Name);
            return Result<ProductDto>.Success(resultDto, "Product created successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while creating product {Name}", dto.Name);
            callback?.Invoke("Create", null, false, ex.Message);
            return Result<ProductDto>.Failure("Failed to create product.", ex.Message);
        }
    }

    /// <summary>
    /// Updates an existing product, synchronizes cache, and executes callbacks.
    /// </summary>
    public async Task<Result<ProductDto>> UpdateProductAsync(
        UpdateProductDto dto,
        ProductOperationCallback? callback = null,
        StockAlertCallback? stockAlertCallback = null)
    {
        if (dto.Id <= 0)
        {
            callback?.Invoke("Update", null, false, "Invalid product ID.");
            return Result<ProductDto>.Failure("Invalid product ID.");
        }

        var validationErrors = ValidateProductInput(dto.Name, dto.Category, dto.Price, dto.Stock);
        if (validationErrors.Any())
        {
            var errorMsg = string.Join("; ", validationErrors);
            callback?.Invoke("Update", null, false, errorMsg);
            return Result<ProductDto>.Failure("Product validation failed.", validationErrors);
        }

        try
        {
            var existing = await _repository.GetByIdAsync(dto.Id);
            if (existing == null)
            {
                callback?.Invoke("Update", null, false, $"Product with ID {dto.Id} does not exist.");
                return Result<ProductDto>.Failure($"Product with ID {dto.Id} was not found.");
            }

            // Update entity properties
            existing.Name = dto.Name.Trim();
            existing.Category = dto.Category.Trim();
            existing.Price = dto.Price;
            existing.Stock = dto.Stock;
            existing.UpdatedAt = DateTime.UtcNow;

            var updated = await _repository.UpdateAsync(existing);
            var resultDto = MapToDto(updated);

            // Invalidate caches
            InvalidateAllProductCaches(dto.Id);

            // Low stock delegate alert
            if (resultDto.Stock < 10)
            {
                stockAlertCallback?.Invoke(resultDto.Id, resultDto.Name, resultDto.Stock);
            }

            // Operation callback
            callback?.Invoke("Update", resultDto, true, $"Product '{resultDto.Name}' updated successfully.");

            return Result<ProductDto>.Success(resultDto, "Product updated successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product {Id}", dto.Id);
            callback?.Invoke("Update", null, false, ex.Message);
            return Result<ProductDto>.Failure($"Failed to update product {dto.Id}.", ex.Message);
        }
    }

    /// <summary>
    /// Deletes a product, evicts related caches, and triggers callback.
    /// </summary>
    public async Task<Result<bool>> DeleteProductAsync(int id, ProductOperationCallback? callback = null)
    {
        if (id <= 0)
        {
            callback?.Invoke("Delete", null, false, "Invalid product ID.");
            return Result<bool>.Failure("Invalid product ID.");
        }

        try
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
            {
                callback?.Invoke("Delete", null, false, $"Product with ID {id} does not exist.");
                return Result<bool>.Failure($"Product with ID {id} was not found.");
            }

            var deletedDto = MapToDto(existing);
            var success = await _repository.DeleteAsync(id);

            if (success)
            {
                InvalidateAllProductCaches(id);
                callback?.Invoke("Delete", deletedDto, true, $"Product '{deletedDto.Name}' was deleted.");
                return Result<bool>.Success(true, "Product deleted successfully.");
            }

            callback?.Invoke("Delete", deletedDto, false, "Database delete operation returned false.");
            return Result<bool>.Failure("Unable to delete product from database.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting product with ID {Id}", id);
            callback?.Invoke("Delete", null, false, ex.Message);
            return Result<bool>.Failure($"Failed to delete product {id}.", ex.Message);
        }
    }

    /// <summary>
    /// Generates calculation reports using Stored Procedures.
    /// Uses either 5-minute cache (Cached) or long-term retention (CachedLong) as requested.
    /// </summary>
    public async Task<Result<ReportSummaryDto>> GetReportSummaryAsync(bool useLongCache = false)
    {
        try
        {
            var cacheKey = useLongCache ? ReportSummaryLongCacheKey : ReportSummary5MinCacheKey;

            Func<Task<ReportSummaryDto>> computeReport = async () =>
            {
                _logger.LogInformation("Calculating reports via Stored Procedures from Database...");

                // Calculation 1 via Stored Procedure: sp_GetAveragePricePerCategory
                var avgPriceReport = await _repository.GetAveragePricePerCategoryFromSpAsync();

                // Calculation 2 via Stored Procedure: sp_GetHighestStockValueCategory
                var highestStockReport = await _repository.GetHighestStockValueCategoryFromSpAsync();

                var allProducts = await _repository.GetAllAsync();
                var productsList = allProducts.ToList();

                var summary = new ReportSummaryDto
                {
                    CategoryAveragePrices = avgPriceReport.Select(a => new CategoryAveragePriceDto
                    {
                        Category = a.Category,
                        AveragePrice = a.AveragePrice,
                        ProductCount = a.ProductCount
                    }).ToList(),

                    HighestStockValueCategory = highestStockReport != null
                        ? new HighestStockValueCategoryDto
                        {
                            Category = highestStockReport.Category,
                            TotalStockValue = highestStockReport.TotalStockValue,
                            TotalStockQuantity = highestStockReport.TotalStockQuantity
                        }
                        : null,

                    TotalProductsCount = productsList.Count,
                    TotalInventoryValue = productsList.Sum(p => p.Price * p.Stock),
                    GeneratedAt = DateTime.UtcNow,
                    CacheTierUsed = useLongCache ? "CachedLong (Persistent until invalidated)" : "Cached (5-Minute Window)"
                };

                return summary;
            };

            ReportSummaryDto result = useLongCache
                ? await _cacheService.CachedLongAsync(cacheKey, computeReport)
                : await _cacheService.CachedAsync(cacheKey, computeReport);

            return Result<ReportSummaryDto>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating report summary.");
            return Result<ReportSummaryDto>.Failure("Failed to generate report.", ex.Message);
        }
    }

    #region Helper Methods

    private void InvalidateAllProductCaches(int? specificId = null)
    {
        _cacheService.Remove(AllProductsCacheKey);
        _cacheService.Remove(ReportSummary5MinCacheKey);
        _cacheService.Remove(ReportSummaryLongCacheKey);

        if (specificId.HasValue)
        {
            _cacheService.Remove($"{SingleProductCacheKeyPrefix}{specificId.Value}");
        }

        _cacheService.RemoveByPrefix("reports:");
        _logger.LogInformation("All related product and report caches invalidated.");
    }

    private static List<string> ValidateProductInput(string name, string category, decimal price, int stock)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(name))
            errors.Add("Product name is required.");
        else if (name.Length < 2 || name.Length > 100)
            errors.Add("Product name must be between 2 and 100 characters.");

        if (string.IsNullOrWhiteSpace(category))
            errors.Add("Category is required.");
        else if (category.Length < 2 || category.Length > 50)
            errors.Add("Category must be between 2 and 50 characters.");

        if (price <= 0)
            errors.Add("Price must be greater than zero.");

        if (stock < 0)
            errors.Add("Stock cannot be negative.");

        return errors;
    }

    private static ProductDto MapToDto(Product entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Category = entity.Category,
        Price = entity.Price,
        Stock = entity.Stock,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt
    };

    #endregion
}
