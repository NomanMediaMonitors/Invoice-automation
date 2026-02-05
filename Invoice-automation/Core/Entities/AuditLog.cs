namespace InvoiceAutomation.Web.Core.Entities;

/// <summary>
/// Audit log entry for tracking all system changes
/// </summary>
public class AuditLog
{
    public Guid Id { get; set; }

    /// <summary>
    /// User who performed the action
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Company context for the action
    /// </summary>
    public Guid? CompanyId { get; set; }

    /// <summary>
    /// Type of entity affected (Invoice, Payment, etc.)
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// ID of the affected entity
    /// </summary>
    public Guid EntityId { get; set; }

    /// <summary>
    /// Action performed (Create, Update, Delete, Approve, etc.)
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Previous values (JSON)
    /// </summary>
    public string? OldValues { get; set; }

    /// <summary>
    /// New values (JSON)
    /// </summary>
    public string? NewValues { get; set; }

    /// <summary>
    /// IP address of the request
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent string
    /// </summary>
    public string? UserAgent { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual User? User { get; set; }
    public virtual Company? Company { get; set; }
}
