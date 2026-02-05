using InvoiceAutomation.Web.Core.DTOs;
using InvoiceAutomation.Web.Core.Enums;

namespace InvoiceAutomation.Web.Core.Interfaces;

/// <summary>
/// Interface for runtime Chart of Accounts operations
/// Accounts are fetched from external accounting system, never stored locally
/// </summary>
public interface IChartOfAccountsService
{
    /// <summary>
    /// Get all accounts for a company
    /// </summary>
    Task<List<AccountDto>> GetAllAccountsAsync(Guid companyId);

    /// <summary>
    /// Get expense accounts (for invoice line item mapping)
    /// </summary>
    Task<List<AccountDto>> GetExpenseAccountsAsync(Guid companyId);

    /// <summary>
    /// Get payment accounts (Bank and Cash accounts for payments)
    /// </summary>
    Task<List<AccountDto>> GetPaymentAccountsAsync(Guid companyId);

    /// <summary>
    /// Get accounts by type
    /// </summary>
    Task<List<AccountDto>> GetAccountsByTypeAsync(Guid companyId, AccountType type);

    /// <summary>
    /// Get a single account by its external ID
    /// </summary>
    Task<AccountDto?> GetAccountByIdAsync(Guid companyId, string externalAccountId);

    /// <summary>
    /// Search accounts by name or code
    /// </summary>
    Task<List<AccountDto>> SearchAccountsAsync(Guid companyId, string searchTerm);

    /// <summary>
    /// Clear cached accounts for a company (force refresh)
    /// </summary>
    void InvalidateCache(Guid companyId);
}
