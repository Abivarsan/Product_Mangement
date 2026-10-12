using ProductManagement.Core.DTOs;

namespace ProductManagement.Web.Models;

/// <summary>
/// ViewModel for the Report analytics dashboard.
/// Encapsulates the results of the two stored procedure calculations.
/// </summary>
public class ReportDashboardViewModel
{
    public ReportSummaryDto Summary { get; set; } = new();
    public bool IsCachedLong { get; set; }
}
