using ProductManagement.Core.Common;
using ProductManagement.Core.DTOs;

namespace ProductManagement.Core.Interfaces;

/// <summary>
/// Service layer interface handling business logic, validation, caching coordination,
/// and invoking delegate callbacks. Acts as the sole intermediary between controllers and repository.
/// </summary>
public interface IProductService
{
    Task<Result<IEnumerable<ProductDto>>> GetAllProductsAsync();
    
    Task<Result<ProductDto>> GetProductByIdAsync(int id);
    
    Task<Result<ProductDto>> CreateProductAsync(
        CreateProductDto dto, 
        ProductOperationCallback? callback = null, 
        StockAlertCallback? stockAlertCallback = null);
        
    Task<Result<ProductDto>> UpdateProductAsync(
        UpdateProductDto dto, 
        ProductOperationCallback? callback = null, 
        StockAlertCallback? stockAlertCallback = null);
        
    Task<Result<bool>> DeleteProductAsync(
        int id, 
        ProductOperationCallback? callback = null);
        
    Task<Result<ReportSummaryDto>> GetReportSummaryAsync(bool useLongCache = false);
}
