namespace ProductManagement.Core.Entities;

/// <summary>
/// Projection model for stored procedure sp_GetAveragePricePerCategory.
/// Calculation 1: The average price of products in each category.
/// </summary>
public class CategoryAveragePriceReport
{
    public string Category { get; set; } = string.Empty;
    public decimal AveragePrice { get; set; }
    public int ProductCount { get; set; }
}

/// <summary>
/// Projection model for stored procedure sp_GetHighestStockValueCategory.
/// Calculation 2: The highest stock value category (Stock * Price).
/// </summary>
public class HighestStockValueCategoryReport
{
    public string Category { get; set; } = string.Empty;
    public decimal TotalStockValue { get; set; }
    public int TotalStockQuantity { get; set; }
}
