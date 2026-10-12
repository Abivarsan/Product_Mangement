using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProductManagement.Core.Entities;
using ProductManagement.Core.Interfaces;
using ProductManagement.Data.Context;

namespace ProductManagement.Data.Repositories;

/// <summary>
/// Repository implementation executing database operations against MSSQL using EF Core.
/// Handles CRUD operations and dispatches calls to MSSQL Stored Procedures.
/// </summary>
public class ProductRepository : IProductRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ProductRepository> _logger;

    public ProductRepository(ApplicationDbContext context, ILogger<ProductRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IEnumerable<Product>> GetAllAsync()
    {
        return await _context.Products
            .AsNoTracking()
            .OrderByDescending(p => p.Id)
            .ToListAsync();
    }

    public async Task<Product?> GetByIdAsync(int id)
    {
        return await _context.Products.FindAsync(id);
    }

    public async Task<Product> AddAsync(Product product)
    {
        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Saved new product to database: Id={Id}, Name={Name}", product.Id, product.Name);
        return product;
    }

    public async Task<Product> UpdateAsync(Product product)
    {
        _context.Products.Update(product);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Updated product in database: Id={Id}", product.Id);
        return product;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
        {
            return false;
        }

        _context.Products.Remove(product);
        var rows = await _context.SaveChangesAsync();
        _logger.LogInformation("Deleted product Id={Id}. Rows affected: {Rows}", id, rows);
        return rows > 0;
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.Products.AnyAsync(p => p.Id == id);
    }

    /// <summary>
    /// Executes Stored Procedure: sp_GetAveragePricePerCategory
    /// Calculation 1: Average price of products in each category.
    /// </summary>
    public async Task<IEnumerable<CategoryAveragePriceReport>> GetAveragePricePerCategoryFromSpAsync()
    {
        try
        {
            _logger.LogInformation("Executing Stored Procedure 'sp_GetAveragePricePerCategory'...");
            var results = await _context.CategoryAveragePrices
                .FromSqlRaw("EXEC sp_GetAveragePricePerCategory")
                .AsNoTracking()
                .ToListAsync();

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing sp_GetAveragePricePerCategory.");
            throw;
        }
    }

    /// <summary>
    /// Executes Stored Procedure: sp_GetHighestStockValueCategory
    /// Calculation 2: The highest stock value category.
    /// </summary>
    public async Task<HighestStockValueCategoryReport?> GetHighestStockValueCategoryFromSpAsync()
    {
        try
        {
            _logger.LogInformation("Executing Stored Procedure 'sp_GetHighestStockValueCategory'...");
            var results = await _context.HighestStockValueCategories
                .FromSqlRaw("EXEC sp_GetHighestStockValueCategory")
                .AsNoTracking()
                .ToListAsync();

            return results.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing sp_GetHighestStockValueCategory.");
            throw;
        }
    }
}
