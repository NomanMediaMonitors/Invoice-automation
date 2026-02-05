using InvoiceAutomation.Web.Core.Enums;

namespace InvoiceAutomation.Web.Core.Entities;

/// <summary>
/// Many-to-many relationship between users and companies with role
/// </summary>
public class UserCompany
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid CompanyId { get; set; }

    /// <summary>
    /// User's role within this specific company
    /// </summary>
    public UserRole Role { get; set; } = UserRole.Viewer;

    /// <summary>
    /// Whether this is the user's default company
    /// </summary>
    public bool IsDefault { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual User User { get; set; } = null!;
    public virtual Company Company { get; set; } = null!;
}
