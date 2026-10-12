namespace ProductManagement.Core.DTOs;

/// <summary>
/// DTO representing the average price of products in a category.
/// Calculation 1: The average price of products in each category.
/// </summary>
public class CategoryAveragePriceDto
{
    public string Category { get; set; } = string.Empty;
    public decimal AveragePrice { get; set; }
    public int ProductCount { get; set; }
}

/// <summary>
/// DTO representing the category with the highest total stock inventory value.
/// Calculation 2: The highest stock value category.
/// </summary>
public class HighestStockValueCategoryDto
{
    public string Category { get; set; } = string.Empty;
    public decimal TotalStockValue { get; set; }
    public int TotalStockQuantity { get; set; }
}

/// <summary>
/// Composite DTO representing the aggregated report dashboard payload.
/// </summary>
public class ReportSummaryDto
{
    public List<CategoryAveragePriceDto> CategoryAveragePrices { get; set; } = new();
    public HighestStockValueCategoryDto? HighestStockValueCategory { get; set; }
    public int TotalProductsCount { get; set; }
    public decimal TotalInventoryValue { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public string CacheTierUsed { get; set; } = string.Empty;
}
