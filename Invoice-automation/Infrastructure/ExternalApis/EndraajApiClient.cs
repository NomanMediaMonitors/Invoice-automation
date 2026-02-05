using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using InvoiceAutomation.Web.Core.DTOs;
using InvoiceAutomation.Web.Core.Enums;
using InvoiceAutomation.Web.Core.Interfaces;

namespace InvoiceAutomation.Web.Infrastructure.ExternalApis;

/// <summary>
/// Endraaj accounting API client
/// </summary>
public class EndraajApiClient : IAccountingApiClient
{
    private readonly HttpClient _httpClient;
    private readonly string _accessToken;
    private readonly string? _companyId;
    private readonly ILogger<EndraajApiClient> _logger;
    private const string BaseUrl = "https://api.endraaj.com/v1";

    public EndraajApiClient(
        IHttpClientFactory httpClientFactory,
        string accessToken,
        string? companyId,
        ILogger<EndraajApiClient> logger)
    {
        _httpClient = httpClientFactory.CreateClient("Endraaj");
        _accessToken = accessToken;
        _companyId = companyId;
        _logger = logger;

        _httpClient.BaseAddress = new Uri(BaseUrl);
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        if (!string.IsNullOrEmpty(_companyId))
        {
            _httpClient.DefaultRequestHeaders.Add("X-Company-Id", _companyId);
        }
    }

    public async Task<List<AccountDto>> GetAccountsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/accounts");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var accounts = JsonSerializer.Deserialize<EndraajAccountsResponse>(content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return accounts?.Data?.Select(MapToAccountDto).ToList() ?? new List<AccountDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch accounts from Endraaj");
            throw;
        }
    }

    public async Task<List<AccountDto>> GetAccountsByTypeAsync(AccountType type)
    {
        try
        {
            var typeParam = type switch
            {
                AccountType.Asset => "asset",
                AccountType.Liability => "liability",
                AccountType.Equity => "equity",
                AccountType.Revenue => "revenue",
                AccountType.Expense => "expense",
                _ => "all"
            };

            var response = await _httpClient.GetAsync($"/accounts?type={typeParam}");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var accounts = JsonSerializer.Deserialize<EndraajAccountsResponse>(content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return accounts?.Data?.Select(MapToAccountDto).ToList() ?? new List<AccountDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch {Type} accounts from Endraaj", type);
            throw;
        }
    }

    public async Task<AccountDto?> GetAccountByIdAsync(string accountId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/accounts/{accountId}");
            if (!response.IsSuccessStatusCode)
                return null;

            var content = await response.Content.ReadAsStringAsync();
            var account = JsonSerializer.Deserialize<EndraajAccountResponse>(content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return account?.Data != null ? MapToAccountDto(account.Data) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch account {AccountId} from Endraaj", accountId);
            return null;
        }
    }

    public async Task<JournalEntryResultDto> CreateJournalEntryAsync(JournalEntryDto entry)
    {
        try
        {
            var request = new
            {
                date = entry.Date.ToString("yyyy-MM-dd"),
                memo = entry.Memo,
                reference = entry.ReferenceNumber,
                lines = entry.Lines.Select(l => new
                {
                    account_id = l.AccountId,
                    debit = l.DebitAmount,
                    credit = l.CreditAmount,
                    description = l.Description
                })
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/journal-entries", content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<EndraajJournalResponse>(responseContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return new JournalEntryResultDto
            {
                Success = true,
                ExternalId = result?.Data?.Id,
                ReferenceNumber = result?.Data?.Reference
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create journal entry in Endraaj");
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
            var request = new
            {
                name = vendor.Name,
                email = vendor.Email,
                phone = vendor.Phone,
                address = vendor.Address,
                tax_number = vendor.TaxNumber
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response;
            if (string.IsNullOrEmpty(vendor.ExternalId))
            {
                response = await _httpClient.PostAsync("/vendors", content);
            }
            else
            {
                response = await _httpClient.PutAsync($"/vendors/{vendor.ExternalId}", content);
            }

            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<EndraajVendorResponse>(responseContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return new VendorSyncResultDto
            {
                Success = true,
                ExternalId = result?.Data?.Id
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync vendor in Endraaj");
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
            var request = new
            {
                vendor_id = bill.VendorId,
                bill_number = bill.BillNumber,
                bill_date = bill.BillDate.ToString("yyyy-MM-dd"),
                due_date = bill.DueDate?.ToString("yyyy-MM-dd"),
                total = bill.TotalAmount,
                lines = bill.Lines.Select(l => new
                {
                    account_id = l.AccountId,
                    description = l.Description,
                    amount = l.Amount
                })
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/bills", content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<EndraajBillResponse>(responseContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return new BillResultDto
            {
                Success = true,
                ExternalId = result?.Data?.Id
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create bill in Endraaj");
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
            var request = new
            {
                bill_id = payment.BillId,
                bank_account_id = payment.BankAccountId,
                amount = payment.Amount,
                payment_date = payment.PaymentDate.ToString("yyyy-MM-dd"),
                reference = payment.ReferenceNumber,
                memo = payment.Memo
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/payments", content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<EndraajPaymentResponse>(responseContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return new PaymentResultDto
            {
                Success = true,
                ExternalId = result?.Data?.Id
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record payment in Endraaj");
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
            var response = await _httpClient.GetAsync("/ping");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<TokenRefreshResult?> RefreshTokensAsync()
    {
        // Implement OAuth token refresh logic
        // This would typically involve making a request to the OAuth token endpoint
        return null;
    }

    private static AccountDto MapToAccountDto(EndraajAccount account)
    {
        return new AccountDto
        {
            ExternalId = account.Id ?? string.Empty,
            Code = account.Code ?? string.Empty,
            Name = account.Name ?? string.Empty,
            Type = MapAccountType(account.Type),
            SubType = MapAccountSubType(account.SubType),
            ParentAccountId = account.ParentId,
            Balance = account.Balance,
            Currency = account.Currency ?? "PKR",
            IsActive = account.IsActive ?? true,
            Description = account.Description
        };
    }

    private static AccountType MapAccountType(string? type)
    {
        return type?.ToLowerInvariant() switch
        {
            "asset" => AccountType.Asset,
            "liability" => AccountType.Liability,
            "equity" => AccountType.Equity,
            "revenue" or "income" => AccountType.Revenue,
            "expense" => AccountType.Expense,
            _ => AccountType.Expense
        };
    }

    private static AccountSubType? MapAccountSubType(string? subType)
    {
        return subType?.ToLowerInvariant() switch
        {
            "bank" => AccountSubType.Bank,
            "cash" => AccountSubType.Cash,
            "accounts_receivable" => AccountSubType.AccountsReceivable,
            "accounts_payable" => AccountSubType.AccountsPayable,
            _ => null
        };
    }

    // Response DTOs for Endraaj API
    private class EndraajAccountsResponse
    {
        public List<EndraajAccount>? Data { get; set; }
    }

    private class EndraajAccountResponse
    {
        public EndraajAccount? Data { get; set; }
    }

    private class EndraajAccount
    {
        public string? Id { get; set; }
        public string? Code { get; set; }
        public string? Name { get; set; }
        public string? Type { get; set; }
        public string? SubType { get; set; }
        public string? ParentId { get; set; }
        public decimal? Balance { get; set; }
        public string? Currency { get; set; }
        public bool? IsActive { get; set; }
        public string? Description { get; set; }
    }

    private class EndraajJournalResponse
    {
        public EndraajJournalData? Data { get; set; }
    }

    private class EndraajJournalData
    {
        public string? Id { get; set; }
        public string? Reference { get; set; }
    }

    private class EndraajVendorResponse
    {
        public EndraajVendorData? Data { get; set; }
    }

    private class EndraajVendorData
    {
        public string? Id { get; set; }
    }

    private class EndraajBillResponse
    {
        public EndraajBillData? Data { get; set; }
    }

    private class EndraajBillData
    {
        public string? Id { get; set; }
    }

    private class EndraajPaymentResponse
    {
        public EndraajPaymentData? Data { get; set; }
    }

    private class EndraajPaymentData
    {
        public string? Id { get; set; }
    }
}
