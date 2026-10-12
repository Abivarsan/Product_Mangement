using ProductManagement.Core.DTOs;

namespace ProductManagement.Web.Models;

/// <summary>
/// ViewModel for the Product List view.
/// </summary>
public class ProductListViewModel
{
    public IEnumerable<ProductDto> Products { get; set; } = new List<ProductDto>();
    public List<string> Categories { get; set; } = new();
    public int TotalProducts { get; set; }
    public int TotalStockUnits { get; set; }
    public decimal TotalInventoryValue { get; set; }
    public int LowStockCount { get; set; }
}
