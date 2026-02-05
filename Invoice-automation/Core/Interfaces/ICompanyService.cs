using InvoiceAutomation.Web.Core.DTOs;
using InvoiceAutomation.Web.Core.Entities;
using InvoiceAutomation.Web.Core.Enums;

namespace InvoiceAutomation.Web.Core.Interfaces;

/// <summary>
/// Interface for company operations
/// </summary>
public interface ICompanyService
{
    /// <summary>
    /// Register a new company
    /// </summary>
    Task<Company> CreateAsync(CompanyCreateDto dto, Guid creatorUserId);

    /// <summary>
    /// Update company details
    /// </summary>
    Task<Company> UpdateAsync(Guid id, CompanyUpdateDto dto);

    /// <summary>
    /// Get company by ID
    /// </summary>
    Task<Company?> GetByIdAsync(Guid id);

    /// <summary>
    /// Get company by NTN
    /// </summary>
    Task<Company?> GetByNtnAsync(string ntn);

    /// <summary>
    /// Get all companies for a user
    /// </summary>
    Task<List<Company>> GetByUserIdAsync(Guid userId);

    /// <summary>
    /// Add a user to a company
    /// </summary>
    Task AddUserAsync(Guid companyId, Guid userId, UserRole role);

    /// <summary>
    /// Remove a user from a company
    /// </summary>
    Task RemoveUserAsync(Guid companyId, Guid userId);

    /// <summary>
    /// Update user's role in a company
    /// </summary>
    Task UpdateUserRoleAsync(Guid companyId, Guid userId, UserRole newRole);

    /// <summary>
    /// Get users in a company
    /// </summary>
    Task<List<UserCompanyDto>> GetUsersAsync(Guid companyId);

    /// <summary>
    /// Connect to accounting system (save OAuth tokens)
    /// </summary>
    Task ConnectAccountingAsync(Guid companyId, AccountingConnectionDto dto);

    /// <summary>
    /// Disconnect accounting system
    /// </summary>
    Task DisconnectAccountingAsync(Guid companyId);

    /// <summary>
    /// Check if accounting is connected
    /// </summary>
    Task<bool> IsAccountingConnectedAsync(Guid companyId);

    /// <summary>
    /// Delete (deactivate) a company
    /// </summary>
    Task DeleteAsync(Guid id);
}
