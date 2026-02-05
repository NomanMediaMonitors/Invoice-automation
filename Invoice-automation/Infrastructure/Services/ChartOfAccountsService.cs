using InvoiceAutomation.Web.Core.DTOs;
using InvoiceAutomation.Web.Core.Enums;
using InvoiceAutomation.Web.Core.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace InvoiceAutomation.Web.Infrastructure.Services;

/// <summary>
/// Chart of Accounts service - fetches accounts from external accounting system at runtime
/// Accounts are NEVER stored locally, only cached briefly
/// </summary>
public class ChartOfAccountsService : IChartOfAccountsService
{
    private readonly IAccountingApiClientFactory _apiClientFactory;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ChartOfAccountsService> _logger;

    private const int CacheDurationMinutes = 10;
    private const string CacheKeyPrefix = "coa_";

    public ChartOfAccountsService(
        IAccountingApiClientFactory apiClientFactory,
        IMemoryCache cache,
        ILogger<ChartOfAccountsService> logger)
    {
        _apiClientFactory = apiClientFactory;
        _cache = cache;
        _logger = logger;
    }

    public async Task<List<AccountDto>> GetAllAccountsAsync(Guid companyId)
    {
        var cacheKey = $"{CacheKeyPrefix}all_{companyId}";

        if (_cache.TryGetValue(cacheKey, out List<AccountDto>? cached) && cached != null)
        {
            _logger.LogDebug("Returning cached accounts for company {CompanyId}", companyId);
            return cached;
        }

        try
        {
            var client = _apiClientFactory.CreateClient(companyId);
            var accounts = await client.GetAccountsAsync();

            _cache.Set(cacheKey, accounts, TimeSpan.FromMinutes(CacheDurationMinutes));

            _logger.LogInformation("Fetched {Count} accounts for company {CompanyId}", accounts.Count, companyId);
            return accounts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch accounts for company {CompanyId}", companyId);
            return new List<AccountDto>();
        }
    }

    public async Task<List<AccountDto>> GetExpenseAccountsAsync(Guid companyId)
    {
        var cacheKey = $"{CacheKeyPrefix}expense_{companyId}";

        if (_cache.TryGetValue(cacheKey, out List<AccountDto>? cached) && cached != null)
        {
            return cached;
        }

        try
        {
            var client = _apiClientFactory.CreateClient(companyId);
            var accounts = await client.GetAccountsByTypeAsync(AccountType.Expense);

            _cache.Set(cacheKey, accounts, TimeSpan.FromMinutes(CacheDurationMinutes));

            _logger.LogInformation("Fetched {Count} expense accounts for company {CompanyId}", accounts.Count, companyId);
            return accounts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch expense accounts for company {CompanyId}", companyId);
            return new List<AccountDto>();
        }
    }

    public async Task<List<AccountDto>> GetPaymentAccountsAsync(Guid companyId)
    {
        var cacheKey = $"{CacheKeyPrefix}payment_{companyId}";

        if (_cache.TryGetValue(cacheKey, out List<AccountDto>? cached) && cached != null)
        {
            return cached;
        }

        try
        {
            var client = _apiClientFactory.CreateClient(companyId);
            var allAssets = await client.GetAccountsByTypeAsync(AccountType.Asset);

            // Filter to only Bank and Cash accounts
            var paymentAccounts = allAssets
                .Where(a => a.SubType == AccountSubType.Bank || a.SubType == AccountSubType.Cash)
                .ToList();

            _cache.Set(cacheKey, paymentAccounts, TimeSpan.FromMinutes(CacheDurationMinutes));

            _logger.LogInformation("Fetched {Count} payment accounts for company {CompanyId}", paymentAccounts.Count, companyId);
            return paymentAccounts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch payment accounts for company {CompanyId}", companyId);
            return new List<AccountDto>();
        }
    }

    public async Task<List<AccountDto>> GetAccountsByTypeAsync(Guid companyId, AccountType type)
    {
        var cacheKey = $"{CacheKeyPrefix}{type}_{companyId}";

        if (_cache.TryGetValue(cacheKey, out List<AccountDto>? cached) && cached != null)
        {
            return cached;
        }

        try
        {
            var client = _apiClientFactory.CreateClient(companyId);
            var accounts = await client.GetAccountsByTypeAsync(type);

            _cache.Set(cacheKey, accounts, TimeSpan.FromMinutes(CacheDurationMinutes));

            return accounts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch {Type} accounts for company {CompanyId}", type, companyId);
            return new List<AccountDto>();
        }
    }

    public async Task<AccountDto?> GetAccountByIdAsync(Guid companyId, string externalAccountId)
    {
        // Try to get from cache first
        var allAccounts = await GetAllAccountsAsync(companyId);
        return allAccounts.FirstOrDefault(a => a.ExternalId == externalAccountId);
    }

    public async Task<List<AccountDto>> SearchAccountsAsync(Guid companyId, string searchTerm)
    {
        var allAccounts = await GetAllAccountsAsync(companyId);

        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return allAccounts;
        }

        var term = searchTerm.ToLowerInvariant();
        return allAccounts
            .Where(a => a.Name.ToLowerInvariant().Contains(term) ||
                        a.Code.ToLowerInvariant().Contains(term))
            .ToList();
    }

    public void InvalidateCache(Guid companyId)
    {
        var prefixes = new[] { "all", "expense", "payment", "Asset", "Liability", "Equity", "Revenue", "Expense" };

        foreach (var prefix in prefixes)
        {
            _cache.Remove($"{CacheKeyPrefix}{prefix}_{companyId}");
        }

        _logger.LogInformation("Cache invalidated for company {CompanyId}", companyId);
    }
}
