using InvoiceAutomation.Web.Core.DTOs;
using InvoiceAutomation.Web.Core.Enums;
using InvoiceAutomation.Web.Core.Interfaces;

namespace InvoiceAutomation.Web.Infrastructure.ExternalApis;

/// <summary>
/// Mock accounting API client for testing and demo purposes
/// Returns sample data when no accounting system is connected
/// </summary>
public class MockAccountingApiClient : IAccountingApiClient
{
    private readonly List<AccountDto> _sampleAccounts;

    public MockAccountingApiClient()
    {
        _sampleAccounts = GenerateSampleAccounts();
    }

    public Task<List<AccountDto>> GetAccountsAsync()
    {
        return Task.FromResult(_sampleAccounts);
    }

    public Task<List<AccountDto>> GetAccountsByTypeAsync(AccountType type)
    {
        var accounts = _sampleAccounts.Where(a => a.Type == type).ToList();
        return Task.FromResult(accounts);
    }

    public Task<AccountDto?> GetAccountByIdAsync(string accountId)
    {
        var account = _sampleAccounts.FirstOrDefault(a => a.ExternalId == accountId);
        return Task.FromResult(account);
    }

    public Task<JournalEntryResultDto> CreateJournalEntryAsync(JournalEntryDto entry)
    {
        return Task.FromResult(new JournalEntryResultDto
        {
            Success = true,
            ExternalId = $"JE-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid():N}".Substring(0, 20),
            ReferenceNumber = $"JE-{DateTime.Now:yyyyMMddHHmmss}"
        });
    }

    public Task<VendorSyncResultDto> SyncVendorAsync(VendorSyncDto vendor)
    {
        return Task.FromResult(new VendorSyncResultDto
        {
            Success = true,
            ExternalId = vendor.ExternalId ?? Guid.NewGuid().ToString()
        });
    }

    public Task<BillResultDto> CreateBillAsync(BillDto bill)
    {
        return Task.FromResult(new BillResultDto
        {
            Success = true,
            ExternalId = $"BILL-{Guid.NewGuid():N}".Substring(0, 20)
        });
    }

    public Task<PaymentResultDto> RecordPaymentAsync(PaymentRecordDto payment)
    {
        return Task.FromResult(new PaymentResultDto
        {
            Success = true,
            ExternalId = $"PAY-{Guid.NewGuid():N}".Substring(0, 20)
        });
    }

    public Task<bool> TestConnectionAsync()
    {
        return Task.FromResult(true);
    }

    public Task<TokenRefreshResult?> RefreshTokensAsync()
    {
        return Task.FromResult<TokenRefreshResult?>(null);
    }

    private static List<AccountDto> GenerateSampleAccounts()
    {
        return new List<AccountDto>
        {
            // Asset Accounts
            new() { ExternalId = "1001", Code = "1001", Name = "Cash in Hand", Type = AccountType.Asset, SubType = AccountSubType.Cash, IsActive = true },
            new() { ExternalId = "1002", Code = "1002", Name = "HBL Current Account", Type = AccountType.Asset, SubType = AccountSubType.Bank, IsActive = true },
            new() { ExternalId = "1003", Code = "1003", Name = "MCB Current Account", Type = AccountType.Asset, SubType = AccountSubType.Bank, IsActive = true },
            new() { ExternalId = "1004", Code = "1004", Name = "Allied Bank Account", Type = AccountType.Asset, SubType = AccountSubType.Bank, IsActive = true },
            new() { ExternalId = "1101", Code = "1101", Name = "Accounts Receivable", Type = AccountType.Asset, SubType = AccountSubType.AccountsReceivable, IsActive = true },
            new() { ExternalId = "1201", Code = "1201", Name = "Inventory", Type = AccountType.Asset, SubType = AccountSubType.OtherCurrentAsset, IsActive = true },
            new() { ExternalId = "1301", Code = "1301", Name = "Furniture & Fixtures", Type = AccountType.Asset, SubType = AccountSubType.FixedAsset, IsActive = true },
            new() { ExternalId = "1302", Code = "1302", Name = "Computer Equipment", Type = AccountType.Asset, SubType = AccountSubType.FixedAsset, IsActive = true },

            // Liability Accounts
            new() { ExternalId = "2001", Code = "2001", Name = "Accounts Payable", Type = AccountType.Liability, SubType = AccountSubType.AccountsPayable, IsActive = true },
            new() { ExternalId = "2101", Code = "2101", Name = "Salaries Payable", Type = AccountType.Liability, SubType = AccountSubType.OtherCurrentLiability, IsActive = true },
            new() { ExternalId = "2201", Code = "2201", Name = "GST Payable", Type = AccountType.Liability, SubType = AccountSubType.OtherCurrentLiability, IsActive = true },
            new() { ExternalId = "2301", Code = "2301", Name = "Bank Loan", Type = AccountType.Liability, SubType = AccountSubType.LongTermLiability, IsActive = true },

            // Revenue Accounts
            new() { ExternalId = "4001", Code = "4001", Name = "Sales Revenue", Type = AccountType.Revenue, SubType = AccountSubType.Income, IsActive = true },
            new() { ExternalId = "4002", Code = "4002", Name = "Service Revenue", Type = AccountType.Revenue, SubType = AccountSubType.Income, IsActive = true },
            new() { ExternalId = "4101", Code = "4101", Name = "Interest Income", Type = AccountType.Revenue, SubType = AccountSubType.OtherIncome, IsActive = true },

            // Expense Accounts
            new() { ExternalId = "5001", Code = "5001", Name = "Cost of Goods Sold", Type = AccountType.Expense, SubType = AccountSubType.CostOfGoodsSold, IsActive = true },
            new() { ExternalId = "5101", Code = "5101", Name = "Office Supplies", Type = AccountType.Expense, IsActive = true },
            new() { ExternalId = "5102", Code = "5102", Name = "Utilities", Type = AccountType.Expense, IsActive = true },
            new() { ExternalId = "5103", Code = "5103", Name = "Telephone & Internet", Type = AccountType.Expense, IsActive = true },
            new() { ExternalId = "5104", Code = "5104", Name = "Rent Expense", Type = AccountType.Expense, IsActive = true },
            new() { ExternalId = "5105", Code = "5105", Name = "Salaries & Wages", Type = AccountType.Expense, IsActive = true },
            new() { ExternalId = "5106", Code = "5106", Name = "Insurance", Type = AccountType.Expense, IsActive = true },
            new() { ExternalId = "5107", Code = "5107", Name = "Repairs & Maintenance", Type = AccountType.Expense, IsActive = true },
            new() { ExternalId = "5108", Code = "5108", Name = "Travel & Transportation", Type = AccountType.Expense, IsActive = true },
            new() { ExternalId = "5109", Code = "5109", Name = "Advertising & Marketing", Type = AccountType.Expense, IsActive = true },
            new() { ExternalId = "5110", Code = "5110", Name = "Professional Fees", Type = AccountType.Expense, IsActive = true },
            new() { ExternalId = "5111", Code = "5111", Name = "Bank Charges", Type = AccountType.Expense, IsActive = true },
            new() { ExternalId = "5112", Code = "5112", Name = "Depreciation Expense", Type = AccountType.Expense, IsActive = true },
            new() { ExternalId = "5201", Code = "5201", Name = "Interest Expense", Type = AccountType.Expense, SubType = AccountSubType.OtherExpense, IsActive = true },
            new() { ExternalId = "5202", Code = "5202", Name = "Miscellaneous Expense", Type = AccountType.Expense, SubType = AccountSubType.OtherExpense, IsActive = true },
        };
    }
}
