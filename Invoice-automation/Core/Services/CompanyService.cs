using InvoiceAutomation.Web.Core.DTOs;
using InvoiceAutomation.Web.Core.Entities;
using InvoiceAutomation.Web.Core.Enums;
using InvoiceAutomation.Web.Core.Interfaces;
using InvoiceAutomation.Web.Infrastructure.Data;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace InvoiceAutomation.Web.Core.Services;

/// <summary>
/// Company management service
/// </summary>
public class CompanyService : ICompanyService
{
    private readonly ApplicationDbContext _context;
    private readonly IDataProtector _protector;
    private readonly ILogger<CompanyService> _logger;

    public CompanyService(
        ApplicationDbContext context,
        IDataProtectionProvider dataProtectionProvider,
        ILogger<CompanyService> logger)
    {
        _context = context;
        _protector = dataProtectionProvider.CreateProtector("AccountingTokens");
        _logger = logger;
    }

    public async Task<Company> CreateAsync(CompanyCreateDto dto, Guid creatorUserId)
    {
        // Check for duplicate NTN
        var existing = await _context.Companies
            .FirstOrDefaultAsync(c => c.Ntn == dto.Ntn);

        if (existing != null)
        {
            throw new InvalidOperationException($"A company with NTN {dto.Ntn} already exists");
        }

        var company = new Company
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Ntn = dto.Ntn,
            Strn = dto.Strn,
            Address = dto.Address,
            City = dto.City,
            State = dto.State,
            Country = dto.Country,
            PostalCode = dto.PostalCode,
            Email = dto.Email,
            Phone = dto.Phone,
            Website = dto.Website,
            FiscalYearStartMonth = dto.FiscalYearStartMonth,
            DefaultCurrency = dto.DefaultCurrency,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // Add creator as admin
        company.UserCompanies.Add(new UserCompany
        {
            Id = Guid.NewGuid(),
            UserId = creatorUserId,
            CompanyId = company.Id,
            Role = UserRole.Admin,
            IsDefault = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });

        _context.Companies.Add(company);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Company created: {CompanyId} - {CompanyName}", company.Id, company.Name);
        return company;
    }

    public async Task<Company> UpdateAsync(Guid id, CompanyUpdateDto dto)
    {
        var company = await _context.Companies.FindAsync(id)
            ?? throw new KeyNotFoundException($"Company not found: {id}");

        if (dto.Name != null) company.Name = dto.Name;
        if (dto.Strn != null) company.Strn = dto.Strn;
        if (dto.Address != null) company.Address = dto.Address;
        if (dto.City != null) company.City = dto.City;
        if (dto.State != null) company.State = dto.State;
        if (dto.Country != null) company.Country = dto.Country;
        if (dto.PostalCode != null) company.PostalCode = dto.PostalCode;
        if (dto.Email != null) company.Email = dto.Email;
        if (dto.Phone != null) company.Phone = dto.Phone;
        if (dto.Website != null) company.Website = dto.Website;
        if (dto.FiscalYearStartMonth.HasValue) company.FiscalYearStartMonth = dto.FiscalYearStartMonth.Value;
        if (dto.IsActive.HasValue) company.IsActive = dto.IsActive.Value;

        company.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return company;
    }

    public async Task<Company?> GetByIdAsync(Guid id)
    {
        return await _context.Companies
            .Include(c => c.UserCompanies)
                .ThenInclude(uc => uc.User)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Company?> GetByNtnAsync(string ntn)
    {
        return await _context.Companies
            .FirstOrDefaultAsync(c => c.Ntn == ntn);
    }

    public async Task<List<Company>> GetByUserIdAsync(Guid userId)
    {
        return await _context.UserCompanies
            .Include(uc => uc.Company)
            .Where(uc => uc.UserId == userId && uc.IsActive)
            .Select(uc => uc.Company)
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task AddUserAsync(Guid companyId, Guid userId, UserRole role)
    {
        var existing = await _context.UserCompanies
            .FirstOrDefaultAsync(uc => uc.CompanyId == companyId && uc.UserId == userId);

        if (existing != null)
        {
            if (!existing.IsActive)
            {
                existing.IsActive = true;
                existing.Role = role;
                await _context.SaveChangesAsync();
                return;
            }
            throw new InvalidOperationException("User is already a member of this company");
        }

        var userCompany = new UserCompany
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            UserId = userId,
            Role = role,
            IsDefault = false,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.UserCompanies.Add(userCompany);
        await _context.SaveChangesAsync();

        _logger.LogInformation("User {UserId} added to company {CompanyId} with role {Role}",
            userId, companyId, role);
    }

    public async Task RemoveUserAsync(Guid companyId, Guid userId)
    {
        var userCompany = await _context.UserCompanies
            .FirstOrDefaultAsync(uc => uc.CompanyId == companyId && uc.UserId == userId)
            ?? throw new KeyNotFoundException("User company relationship not found");

        userCompany.IsActive = false;
        await _context.SaveChangesAsync();
    }

    public async Task UpdateUserRoleAsync(Guid companyId, Guid userId, UserRole newRole)
    {
        var userCompany = await _context.UserCompanies
            .FirstOrDefaultAsync(uc => uc.CompanyId == companyId && uc.UserId == userId)
            ?? throw new KeyNotFoundException("User company relationship not found");

        userCompany.Role = newRole;
        await _context.SaveChangesAsync();
    }

    public async Task<List<UserCompanyDto>> GetUsersAsync(Guid companyId)
    {
        return await _context.UserCompanies
            .Include(uc => uc.User)
            .Where(uc => uc.CompanyId == companyId && uc.IsActive)
            .Select(uc => new UserCompanyDto
            {
                UserId = uc.UserId,
                UserName = uc.User.UserName ?? "",
                Email = uc.User.Email ?? "",
                FullName = uc.User.FullName,
                Role = uc.Role,
                IsActive = uc.User.IsActive,
                JoinedAt = uc.CreatedAt
            })
            .OrderBy(u => u.FullName)
            .ToListAsync();
    }

    public async Task ConnectAccountingAsync(Guid companyId, AccountingConnectionDto dto)
    {
        var company = await _context.Companies.FindAsync(companyId)
            ?? throw new KeyNotFoundException($"Company not found: {companyId}");

        company.AccountingProvider = dto.Provider;
        company.EncryptedAccessToken = _protector.Protect(dto.AccessToken);
        company.EncryptedRefreshToken = !string.IsNullOrEmpty(dto.RefreshToken)
            ? _protector.Protect(dto.RefreshToken)
            : null;
        company.TokenExpiresAt = dto.ExpiresAt;
        company.ExternalCompanyId = dto.ExternalCompanyId;
        company.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Accounting system connected for company {CompanyId}: {Provider}",
            companyId, dto.Provider);
    }

    public async Task DisconnectAccountingAsync(Guid companyId)
    {
        var company = await _context.Companies.FindAsync(companyId)
            ?? throw new KeyNotFoundException($"Company not found: {companyId}");

        company.AccountingProvider = AccountingProvider.None;
        company.EncryptedAccessToken = null;
        company.EncryptedRefreshToken = null;
        company.TokenExpiresAt = null;
        company.ExternalCompanyId = null;
        company.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Accounting system disconnected for company {CompanyId}", companyId);
    }

    public async Task<bool> IsAccountingConnectedAsync(Guid companyId)
    {
        var company = await _context.Companies.FindAsync(companyId);
        return company != null &&
               company.AccountingProvider != AccountingProvider.None &&
               !string.IsNullOrEmpty(company.EncryptedAccessToken);
    }

    public async Task DeleteAsync(Guid id)
    {
        var company = await _context.Companies.FindAsync(id)
            ?? throw new KeyNotFoundException($"Company not found: {id}");

        // Soft delete
        company.IsActive = false;
        company.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Company deactivated: {CompanyId}", id);
    }
}
