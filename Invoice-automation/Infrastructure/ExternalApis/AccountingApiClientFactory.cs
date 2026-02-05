using InvoiceAutomation.Web.Core.Entities;
using InvoiceAutomation.Web.Core.Enums;
using InvoiceAutomation.Web.Core.Interfaces;
using InvoiceAutomation.Web.Infrastructure.Data;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace InvoiceAutomation.Web.Infrastructure.ExternalApis;

/// <summary>
/// Factory for creating accounting API clients
/// </summary>
public class AccountingApiClientFactory : IAccountingApiClientFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IDataProtector _protector;
    private readonly ILogger<AccountingApiClientFactory> _logger;

    public AccountingApiClientFactory(
        IServiceProvider serviceProvider,
        IDataProtectionProvider dataProtectionProvider,
        ILogger<AccountingApiClientFactory> logger)
    {
        _serviceProvider = serviceProvider;
        _protector = dataProtectionProvider.CreateProtector("AccountingTokens");
        _logger = logger;
    }

    public IAccountingApiClient CreateClient(Guid companyId)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var company = dbContext.Companies
            .AsNoTracking()
            .FirstOrDefault(c => c.Id == companyId);

        if (company == null)
        {
            throw new ArgumentException($"Company not found: {companyId}");
        }

        if (company.AccountingProvider == AccountingProvider.None ||
            string.IsNullOrEmpty(company.EncryptedAccessToken))
        {
            _logger.LogWarning("No accounting system connected for company {CompanyId}", companyId);
            // Return a mock client for testing/demo purposes
            return new MockAccountingApiClient();
        }

        // Decrypt access token
        var accessToken = _protector.Unprotect(company.EncryptedAccessToken);

        return CreateClient(company.AccountingProvider, accessToken, company.ExternalCompanyId);
    }

    public IAccountingApiClient CreateClient(AccountingProvider provider, string accessToken, string? companyId = null)
    {
        var httpClientFactory = _serviceProvider.GetRequiredService<IHttpClientFactory>();

        return provider switch
        {
            AccountingProvider.Endraaj => new EndraajApiClient(
                httpClientFactory,
                accessToken,
                companyId,
                _serviceProvider.GetRequiredService<ILogger<EndraajApiClient>>()),

            AccountingProvider.QuickBooks => new QuickBooksApiClient(
                httpClientFactory,
                accessToken,
                companyId,
                _serviceProvider.GetRequiredService<ILogger<QuickBooksApiClient>>()),

            _ => new MockAccountingApiClient()
        };
    }
}
