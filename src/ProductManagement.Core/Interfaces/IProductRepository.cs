using ProductManagement.Core.Entities;

namespace ProductManagement.Core.Interfaces;

/// <summary>
/// Repository interface for Product entity persistence and Stored Procedure queries.
/// Decouples data access from business logic (Service Layer).
/// </summary>
public interface IProductRepository
{
    Task<IEnumerable<Product>> GetAllAsync();
    Task<Product?> GetByIdAsync(int id);
    Task<Product> AddAsync(Product product);
    Task<Product> UpdateAsync(Product product);
    Task<bool> DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);

    /// <summary>
    /// Executes stored procedure sp_GetAveragePricePerCategory.
    /// Calculation 1: The average price of products in each category.
    /// </summary>
    Task<IEnumerable<CategoryAveragePriceReport>> GetAveragePricePerCategoryFromSpAsync();

    /// <summary>
    /// Executes stored procedure sp_GetHighestStockValueCategory.
    /// Calculation 2: The highest stock value category.
    /// </summary>
    Task<HighestStockValueCategoryReport?> GetHighestStockValueCategoryFromSpAsync();
}
