using InvoiceAutomation.Web.Core.DTOs;
using InvoiceAutomation.Web.Core.Enums;

namespace InvoiceAutomation.Web.Core.Interfaces;

/// <summary>
/// Interface for external accounting system API client
/// </summary>
public interface IAccountingApiClient
{
    /// <summary>
    /// Get all accounts from the accounting system
    /// </summary>
    Task<List<AccountDto>> GetAccountsAsync();

    /// <summary>
    /// Get accounts filtered by type
    /// </summary>
    Task<List<AccountDto>> GetAccountsByTypeAsync(AccountType type);

    /// <summary>
    /// Get a single account by ID
    /// </summary>
    Task<AccountDto?> GetAccountByIdAsync(string accountId);

    /// <summary>
    /// Create a journal entry in the accounting system
    /// </summary>
    Task<JournalEntryResultDto> CreateJournalEntryAsync(JournalEntryDto entry);

    /// <summary>
    /// Create or update a vendor/supplier
    /// </summary>
    Task<VendorSyncResultDto> SyncVendorAsync(VendorSyncDto vendor);

    /// <summary>
    /// Create a bill/invoice in the accounting system
    /// </summary>
    Task<BillResultDto> CreateBillAsync(BillDto bill);

    /// <summary>
    /// Record a payment in the accounting system
    /// </summary>
    Task<PaymentResultDto> RecordPaymentAsync(PaymentRecordDto payment);

    /// <summary>
    /// Test the connection to the accounting system
    /// </summary>
    Task<bool> TestConnectionAsync();

    /// <summary>
    /// Refresh OAuth tokens if needed
    /// </summary>
    Task<TokenRefreshResult?> RefreshTokensAsync();
}

/// <summary>
/// Factory for creating accounting API clients
/// </summary>
public interface IAccountingApiClientFactory
{
    /// <summary>
    /// Create an API client for a company
    /// </summary>
    IAccountingApiClient CreateClient(Guid companyId);

    /// <summary>
    /// Create an API client with specific credentials
    /// </summary>
    IAccountingApiClient CreateClient(AccountingProvider provider, string accessToken, string? companyId = null);
}
