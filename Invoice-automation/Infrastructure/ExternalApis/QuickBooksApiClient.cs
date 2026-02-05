using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using InvoiceAutomation.Web.Core.DTOs;
using InvoiceAutomation.Web.Core.Enums;
using InvoiceAutomation.Web.Core.Interfaces;

namespace InvoiceAutomation.Web.Infrastructure.ExternalApis;

/// <summary>
/// QuickBooks Online API client
/// </summary>
public class QuickBooksApiClient : IAccountingApiClient
{
    private readonly HttpClient _httpClient;
    private readonly string _accessToken;
    private readonly string? _realmId;
    private readonly ILogger<QuickBooksApiClient> _logger;
    private const string BaseUrl = "https://quickbooks.api.intuit.com/v3/company";

    public QuickBooksApiClient(
        IHttpClientFactory httpClientFactory,
        string accessToken,
        string? realmId,
        ILogger<QuickBooksApiClient> logger)
    {
        _httpClient = httpClientFactory.CreateClient("QuickBooks");
        _accessToken = accessToken;
        _realmId = realmId;
        _logger = logger;

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    private string GetBaseUrl() => $"{BaseUrl}/{_realmId}";

    public async Task<List<AccountDto>> GetAccountsAsync()
    {
        try
        {
            var query = "SELECT * FROM Account WHERE Active = true MAXRESULTS 1000";
            var response = await _httpClient.GetAsync($"{GetBaseUrl()}/query?query={Uri.EscapeDataString(query)}");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<QBQueryResponse>(content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return result?.QueryResponse?.Account?.Select(MapToAccountDto).ToList() ?? new List<AccountDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch accounts from QuickBooks");
            throw;
        }
    }

    public async Task<List<AccountDto>> GetAccountsByTypeAsync(AccountType type)
    {
        try
        {
            var qbType = type switch
            {
                AccountType.Asset => "'Bank', 'Other Current Asset', 'Fixed Asset', 'Other Asset'",
                AccountType.Liability => "'Accounts Payable', 'Credit Card', 'Other Current Liability', 'Long Term Liability'",
                AccountType.Equity => "'Equity'",
                AccountType.Revenue => "'Income', 'Other Income'",
                AccountType.Expense => "'Expense', 'Other Expense', 'Cost of Goods Sold'",
                _ => "'Expense'"
            };

            var query = $"SELECT * FROM Account WHERE AccountType IN ({qbType}) AND Active = true MAXRESULTS 1000";
            var response = await _httpClient.GetAsync($"{GetBaseUrl()}/query?query={Uri.EscapeDataString(query)}");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<QBQueryResponse>(content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return result?.QueryResponse?.Account?.Select(MapToAccountDto).ToList() ?? new List<AccountDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch {Type} accounts from QuickBooks", type);
            throw;
        }
    }

    public async Task<AccountDto?> GetAccountByIdAsync(string accountId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{GetBaseUrl()}/account/{accountId}");
            if (!response.IsSuccessStatusCode)
                return null;

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<QBAccountResponse>(content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return result?.Account != null ? MapToAccountDto(result.Account) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch account {AccountId} from QuickBooks", accountId);
            return null;
        }
    }

    public async Task<JournalEntryResultDto> CreateJournalEntryAsync(JournalEntryDto entry)
    {
        try
        {
            var qbEntry = new
            {
                TxnDate = entry.Date.ToString("yyyy-MM-dd"),
                PrivateNote = entry.Memo,
                DocNumber = entry.ReferenceNumber,
                Line = entry.Lines.Select(l => new
                {
                    DetailType = "JournalEntryLineDetail",
                    Amount = l.DebitAmount > 0 ? l.DebitAmount : l.CreditAmount,
                    JournalEntryLineDetail = new
                    {
                        PostingType = l.DebitAmount > 0 ? "Debit" : "Credit",
                        AccountRef = new { value = l.AccountId }
                    },
                    Description = l.Description
                })
            };

            var json = JsonSerializer.Serialize(qbEntry);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{GetBaseUrl()}/journalentry", content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<QBJournalEntryResponse>(responseContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return new JournalEntryResultDto
            {
                Success = true,
                ExternalId = result?.JournalEntry?.Id,
                ReferenceNumber = result?.JournalEntry?.DocNumber
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create journal entry in QuickBooks");
            return new JournalEntryResultDto
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<VendorSyncResultDto> SyncVendorAsync(VendorSyncDto vendor)
    {
        try
        {
            var qbVendor = new
            {
                DisplayName = vendor.Name,
                PrimaryEmailAddr = new { Address = vendor.Email },
                PrimaryPhone = new { FreeFormNumber = vendor.Phone },
                BillAddr = new { Line1 = vendor.Address },
                TaxIdentifier = vendor.TaxNumber
            };

            var json = JsonSerializer.Serialize(qbVendor);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response;
            if (string.IsNullOrEmpty(vendor.ExternalId))
            {
                response = await _httpClient.PostAsync($"{GetBaseUrl()}/vendor", content);
            }
            else
            {
                // For update, we need to get the sync token first
                var existing = await _httpClient.GetAsync($"{GetBaseUrl()}/vendor/{vendor.ExternalId}");
                var existingContent = await existing.Content.ReadAsStringAsync();
                var existingVendor = JsonSerializer.Deserialize<QBVendorResponse>(existingContent,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                var updateVendor = new
                {
                    Id = vendor.ExternalId,
                    SyncToken = existingVendor?.Vendor?.SyncToken,
                    DisplayName = vendor.Name,
                    PrimaryEmailAddr = new { Address = vendor.Email },
                    PrimaryPhone = new { FreeFormNumber = vendor.Phone },
                    BillAddr = new { Line1 = vendor.Address },
                    TaxIdentifier = vendor.TaxNumber
                };

                json = JsonSerializer.Serialize(updateVendor);
                content = new StringContent(json, Encoding.UTF8, "application/json");
                response = await _httpClient.PostAsync($"{GetBaseUrl()}/vendor", content);
            }

            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<QBVendorResponse>(responseContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return new VendorSyncResultDto
            {
                Success = true,
                ExternalId = result?.Vendor?.Id
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync vendor in QuickBooks");
            return new VendorSyncResultDto
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<BillResultDto> CreateBillAsync(BillDto bill)
    {
        try
        {
            var qbBill = new
            {
                VendorRef = new { value = bill.VendorId },
                DocNumber = bill.BillNumber,
                TxnDate = bill.BillDate.ToString("yyyy-MM-dd"),
                DueDate = bill.DueDate?.ToString("yyyy-MM-dd"),
                Line = bill.Lines.Select(l => new
                {
                    DetailType = "AccountBasedExpenseLineDetail",
                    Amount = l.Amount,
                    AccountBasedExpenseLineDetail = new
                    {
                        AccountRef = new { value = l.AccountId }
                    },
                    Description = l.Description
                })
            };

            var json = JsonSerializer.Serialize(qbBill);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{GetBaseUrl()}/bill", content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<QBBillResponse>(responseContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return new BillResultDto
            {
                Success = true,
                ExternalId = result?.Bill?.Id
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create bill in QuickBooks");
            return new BillResultDto
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<PaymentResultDto> RecordPaymentAsync(PaymentRecordDto payment)
    {
        try
        {
            var qbPayment = new
            {
                PayType = "Check",
                TxnDate = payment.PaymentDate.ToString("yyyy-MM-dd"),
                TotalAmt = payment.Amount,
                PrivateNote = payment.Memo,
                DocNumber = payment.ReferenceNumber,
                BankAccountRef = new { value = payment.BankAccountId },
                Line = new[]
                {
                    new
                    {
                        Amount = payment.Amount,
                        LinkedTxn = new[]
                        {
                            new
                            {
                                TxnId = payment.BillId,
                                TxnType = "Bill"
                            }
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(qbPayment);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{GetBaseUrl()}/billpayment", content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<QBBillPaymentResponse>(responseContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return new PaymentResultDto
            {
                Success = true,
                ExternalId = result?.BillPayment?.Id
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record payment in QuickBooks");
            return new PaymentResultDto
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{GetBaseUrl()}/companyinfo/{_realmId}");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public Task<TokenRefreshResult?> RefreshTokensAsync()
    {
        // Implement OAuth 2.0 token refresh
        return Task.FromResult<TokenRefreshResult?>(null);
    }

    private static AccountDto MapToAccountDto(QBAccount account)
    {
        return new AccountDto
        {
            ExternalId = account.Id ?? string.Empty,
            Code = account.AcctNum ?? string.Empty,
            Name = account.Name ?? string.Empty,
            Type = MapAccountType(account.AccountType),
            SubType = MapAccountSubType(account.AccountSubType),
            Balance = account.CurrentBalance,
            Currency = account.CurrencyRef?.Value ?? "PKR",
            IsActive = account.Active ?? true,
            Description = account.Description
        };
    }

    private static AccountType MapAccountType(string? type)
    {
        return type switch
        {
            "Bank" or "Other Current Asset" or "Fixed Asset" or "Other Asset" => AccountType.Asset,
            "Accounts Payable" or "Credit Card" or "Other Current Liability" or "Long Term Liability" => AccountType.Liability,
            "Equity" => AccountType.Equity,
            "Income" or "Other Income" => AccountType.Revenue,
            "Expense" or "Other Expense" or "Cost of Goods Sold" => AccountType.Expense,
            _ => AccountType.Expense
        };
    }

    private static AccountSubType? MapAccountSubType(string? subType)
    {
        return subType switch
        {
            "Checking" or "Savings" or "MoneyMarket" => AccountSubType.Bank,
            "CashOnHand" => AccountSubType.Cash,
            "AccountsReceivable" => AccountSubType.AccountsReceivable,
            "AccountsPayable" => AccountSubType.AccountsPayable,
            _ => null
        };
    }

    // QuickBooks response DTOs
    private class QBQueryResponse
    {
        public QBQueryResponseData? QueryResponse { get; set; }
    }

    private class QBQueryResponseData
    {
        public List<QBAccount>? Account { get; set; }
    }

    private class QBAccountResponse
    {
        public QBAccount? Account { get; set; }
    }

    private class QBAccount
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? AcctNum { get; set; }
        public string? AccountType { get; set; }
        public string? AccountSubType { get; set; }
        public decimal? CurrentBalance { get; set; }
        public bool? Active { get; set; }
        public string? Description { get; set; }
        public QBCurrencyRef? CurrencyRef { get; set; }
    }

    private class QBCurrencyRef
    {
        public string? Value { get; set; }
    }

    private class QBJournalEntryResponse
    {
        public QBJournalEntry? JournalEntry { get; set; }
    }

    private class QBJournalEntry
    {
        public string? Id { get; set; }
        public string? DocNumber { get; set; }
    }

    private class QBVendorResponse
    {
        public QBVendor? Vendor { get; set; }
    }

    private class QBVendor
    {
        public string? Id { get; set; }
        public string? SyncToken { get; set; }
    }

    private class QBBillResponse
    {
        public QBBill? Bill { get; set; }
    }

    private class QBBill
    {
        public string? Id { get; set; }
    }

    private class QBBillPaymentResponse
    {
        public QBBillPayment? BillPayment { get; set; }
    }

    private class QBBillPayment
    {
        public string? Id { get; set; }
    }
}
