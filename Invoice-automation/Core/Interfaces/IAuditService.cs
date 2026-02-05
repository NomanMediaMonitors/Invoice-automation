using InvoiceAutomation.Web.Core.Entities;

namespace InvoiceAutomation.Web.Core.Interfaces;

/// <summary>
/// Interface for audit logging
/// </summary>
public interface IAuditService
{
    /// <summary>
    /// Log an action
    /// </summary>
    Task LogAsync(AuditLog log);

    /// <summary>
    /// Log an action with automatic change detection
    /// </summary>
    Task LogAsync<T>(Guid? userId, Guid? companyId, string action, T? oldValue, T? newValue, Guid entityId) where T : class;

    /// <summary>
    /// Get audit history for an entity
    /// </summary>
    Task<List<AuditLog>> GetEntityHistoryAsync(string entityType, Guid entityId);

    /// <summary>
    /// Get audit history for a company
    /// </summary>
    Task<List<AuditLog>> GetCompanyHistoryAsync(Guid companyId, DateTime? from = null, DateTime? to = null);

    /// <summary>
    /// Get audit history for a user
    /// </summary>
    Task<List<AuditLog>> GetUserHistoryAsync(Guid userId, DateTime? from = null, DateTime? to = null);
}
