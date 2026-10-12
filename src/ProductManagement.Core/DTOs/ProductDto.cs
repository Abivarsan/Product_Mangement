namespace ProductManagement.Core.DTOs;

/// <summary>
/// Data Transfer Object representing product data sent to consumers (views, API endpoints).
/// </summary>
public class ProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public decimal StockValue => Price * Stock;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
