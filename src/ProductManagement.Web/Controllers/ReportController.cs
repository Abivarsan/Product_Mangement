using Microsoft.AspNetCore.Mvc;
using ProductManagement.Core.Interfaces;
using ProductManagement.Web.Models;

namespace ProductManagement.Web.Controllers;

/// <summary>
/// Controller for rendering and serving calculation reports.
/// 1. The average price of products in each category (via sp_GetAveragePricePerCategory).
/// 2. The highest stock value category (via sp_GetHighestStockValueCategory).
/// 3. Demonstrates Cached (5-min) vs CachedLong service caching tiers.
/// </summary>
public class ReportController : Controller
{
    private readonly IProductService _productService;
    private readonly ILogger<ReportController> _logger;

    public ReportController(IProductService productService, ILogger<ReportController> logger)
    {
        _productService = productService ?? throw new ArgumentNullException(nameof(productService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Displays the Report Dashboard page.
    /// </summary>
    /// <param name="useLongCache">Toggle between 5-min cache and CachedLong.</param>
    [HttpGet]
    public async Task<IActionResult> Index(bool useLongCache = false)
    {
        var result = await _productService.GetReportSummaryAsync(useLongCache);

        var viewModel = new ReportDashboardViewModel
        {
            Summary = result.Data ?? new(),
            IsCachedLong = useLongCache
        };

        return View(viewModel);
    }

    /// <summary>
    /// JSON endpoint for dynamic AJAX report refreshing and cache-tier demonstration.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetSummary(bool useLongCache = false)
    {
        var result = await _productService.GetReportSummaryAsync(useLongCache);
        return Json(result);
    }
}
