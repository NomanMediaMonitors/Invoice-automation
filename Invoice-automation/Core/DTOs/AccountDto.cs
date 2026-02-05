using InvoiceAutomation.Web.Core.Enums;

namespace InvoiceAutomation.Web.Core.DTOs;

/// <summary>
/// Account data from external accounting system
/// </summary>
public class AccountDto
{
    /// <summary>
    /// External account ID
    /// </summary>
    public string ExternalId { get; set; } = string.Empty;

    /// <summary>
    /// Account code (e.g., "5101")
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Account name (e.g., "Office Supplies")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Full display name combining code and name
    /// </summary>
    public string DisplayName => $"{Code} - {Name}";

    /// <summary>
    /// Account type
    /// </summary>
    public AccountType Type { get; set; }

    /// <summary>
    /// Account sub-type
    /// </summary>
    public AccountSubType? SubType { get; set; }

    /// <summary>
    /// Parent account ID for hierarchy
    /// </summary>
    public string? ParentAccountId { get; set; }

    /// <summary>
    /// Current balance
    /// </summary>
    public decimal? Balance { get; set; }

    /// <summary>
    /// Currency code
    /// </summary>
    public string Currency { get; set; } = "PKR";

    /// <summary>
    /// Whether the account is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Account description
    /// </summary>
    public string? Description { get; set; }
}
