using Microsoft.AspNetCore.Mvc;
using ProductManagement.Core.Common;
using ProductManagement.Core.DTOs;
using ProductManagement.Core.Interfaces;
using ProductManagement.Web.Models;

namespace ProductManagement.Web.Controllers;

/// <summary>
/// Handles Product CRUD operations and coordinates with the Service Layer.
/// Strictly adheres to Separation of Concerns: Zero direct database operations.
/// Utilizes C# delegate callbacks for audit logs and stock alert thresholds.
/// </summary>
public class ProductController : Controller
{
    private readonly IProductService _productService;
    private readonly ILogger<ProductController> _logger;

    public ProductController(IProductService productService, ILogger<ProductController> logger)
    {
        _productService = productService ?? throw new ArgumentNullException(nameof(productService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Displays the Product List page.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var result = await _productService.GetAllProductsAsync();
        var products = result.Data ?? Enumerable.Empty<ProductDto>();
        var list = products.ToList();

        var viewModel = new ProductListViewModel
        {
            Products = list,
            Categories = list.Select(p => p.Category).Distinct().OrderBy(c => c).ToList(),
            TotalProducts = list.Count,
            TotalStockUnits = list.Sum(p => p.Stock),
            TotalInventoryValue = list.Sum(p => p.Price * p.Stock),
            LowStockCount = list.Count(p => p.Stock < 10)
        };

        return View(viewModel);
    }

    /// <summary>
    /// JSON API endpoint returning all products for jQuery AJAX dynamic refresh.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _productService.GetAllProductsAsync();
        return Json(result);
    }

    /// <summary>
    /// JSON API endpoint returning a single product by ID for populating the Edit modal.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _productService.GetProductByIdAsync(id);
        if (!result.IsSuccess)
        {
            return NotFound(result);
        }
        return Json(result);
    }

    /// <summary>
    /// Creates a new product. 
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromBody] CreateProductDto dto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            return BadRequest(Result<ProductDto>.Failure("Invalid product data submitted.", errors));
        }

        // Delegate Callback 1: Audit Logging Callback
        string? callbackAuditMessage = null;
        ProductOperationCallback auditCallback = (operation, product, isSuccess, message) =>
        {
            callbackAuditMessage = $"[Audit Callback] {operation} succeeded for product '{product?.Name}' (Id: {product?.Id})";
            _logger.LogInformation(callbackAuditMessage);
        };

        // Delegate Callback 2: Low Stock Alert Callback
        string? lowStockWarning = null;
        StockAlertCallback stockCallback = (id, name, stock) =>
        {
            lowStockWarning = $"Low stock alert triggered: '{name}' has only {stock} units in stock!";
            _logger.LogWarning("[Stock Alert Callback] Product ID {Id} ('{Name}') has low stock: {Stock}", id, name, stock);
        };

        var result = await _productService.CreateProductAsync(dto, auditCallback, stockCallback);

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        return Json(new
        {
            Success = true,
            Message = result.Message,
            Data = result.Data,
            AuditNotice = callbackAuditMessage,
            LowStockWarning = lowStockWarning
        });
    }

    /// <summary>
    /// Updates an existing product.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update([FromBody] UpdateProductDto dto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            return BadRequest(Result<ProductDto>.Failure("Invalid update data.", errors));
        }

        string? callbackAuditMessage = null;
        ProductOperationCallback auditCallback = (operation, product, isSuccess, message) =>
        {
            callbackAuditMessage = $"[Audit Callback] {operation} confirmed for product Id {product?.Id}";
            _logger.LogInformation(callbackAuditMessage);
        };

        string? lowStockWarning = null;
        StockAlertCallback stockCallback = (id, name, stock) =>
        {
            lowStockWarning = $"Warning: '{name}' currently has low inventory ({stock} units remaining).";
        };

        var result = await _productService.UpdateProductAsync(dto, auditCallback, stockCallback);

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        return Json(new
        {
            Success = true,
            Message = result.Message,
            Data = result.Data,
            AuditNotice = callbackAuditMessage,
            LowStockWarning = lowStockWarning
        });
    }

    /// <summary>
    /// Deletes a product by ID.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        string? callbackAuditMessage = null;
        ProductOperationCallback auditCallback = (operation, product, isSuccess, message) =>
        {
            callbackAuditMessage = $"[Audit Callback] {operation} completed for product '{product?.Name}'.";
            _logger.LogInformation(callbackAuditMessage);
        };

        var result = await _productService.DeleteProductAsync(id, auditCallback);

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        return Json(new
        {
            Success = true,
            Message = result.Message,
            AuditNotice = callbackAuditMessage
        });
    }
}
