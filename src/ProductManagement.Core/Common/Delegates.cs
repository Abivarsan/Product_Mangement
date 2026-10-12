using ProductManagement.Core.DTOs;

namespace ProductManagement.Core.Common;

/// <summary>
/// Delegate for post-operation callbacks (e.g. audit logging, telemetry, or notification hooks).
/// </summary>
/// <param name="operation">The operation name (e.g. "Create", "Update", "Delete").</param>
/// <param name="product">The Product DTO affected (or null if not applicable).</param>
/// <param name="isSuccess">Whether the operation succeeded.</param>
/// <param name="message">Informational status message or error detail.</param>
public delegate void ProductOperationCallback(string operation, ProductDto? product, bool isSuccess, string? message);

/// <summary>
/// Delegate for low stock alerts or threshold monitoring callbacks.
/// Invoked when a product's stock drops below or is set below a critical threshold (e.g., < 10).
/// </summary>
/// <param name="productId">The ID of the product.</param>
/// <param name="productName">The name of the product.</param>
/// <param name="currentStock">The current quantity in stock.</param>
public delegate void StockAlertCallback(int productId, string productName, int currentStock);
