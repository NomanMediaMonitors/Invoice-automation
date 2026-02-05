using InvoiceAutomation.Web.Core.Entities;
using InvoiceAutomation.Web.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceAutomation.Web.Controllers;

/// <summary>
/// Approval workflow controller
/// </summary>
[Authorize]
public class ApprovalController : Controller
{
    private readonly UserManager<User> _userManager;
    private readonly IApprovalService _approvalService;
    private readonly ILogger<ApprovalController> _logger;

    public ApprovalController(
        UserManager<User> userManager,
        IApprovalService approvalService,
        ILogger<ApprovalController> logger)
    {
        _userManager = userManager;
        _approvalService = approvalService;
        _logger = logger;
    }

    private Guid GetCurrentUserId() => Guid.Parse(_userManager.GetUserId(User)!);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(Guid id, string? comments)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _approvalService.ApproveAsync(id, userId, comments);
            TempData["Success"] = "Invoice approved successfully";
        }
        catch (UnauthorizedAccessException)
        {
            TempData["Error"] = "You do not have permission to approve this invoice";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Approval failed for invoice {InvoiceId}", id);
            TempData["Error"] = $"Approval failed: {ex.Message}";
        }

        return RedirectToAction("Review", "Invoice", new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(Guid id, string comments)
    {
        if (string.IsNullOrWhiteSpace(comments))
        {
            TempData["Error"] = "Rejection comments are required";
            return RedirectToAction("Review", "Invoice", new { id });
        }

        try
        {
            var userId = GetCurrentUserId();
            await _approvalService.RejectAsync(id, userId, comments);
            TempData["Success"] = "Invoice rejected";
        }
        catch (UnauthorizedAccessException)
        {
            TempData["Error"] = "You do not have permission to reject this invoice";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Rejection failed for invoice {InvoiceId}", id);
            TempData["Error"] = $"Rejection failed: {ex.Message}";
        }

        return RedirectToAction("Index", "Invoice");
    }
}
