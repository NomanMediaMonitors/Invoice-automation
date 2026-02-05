using System.Text.Json;
using InvoiceAutomation.Web.Core.Entities;
using InvoiceAutomation.Web.Core.Interfaces;
using InvoiceAutomation.Web.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InvoiceAutomation.Web.Core.Services;

/// <summary>
/// Audit logging service
/// </summary>
public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AuditService> _logger;

    public AuditService(
        ApplicationDbContext context,
        IHttpContextAccessor httpContextAccessor,
        ILogger<AuditService> logger)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task LogAsync(AuditLog log)
    {
        // Add request context
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            log.IpAddress ??= httpContext.Connection.RemoteIpAddress?.ToString();
            log.UserAgent ??= httpContext.Request.Headers.UserAgent.ToString();
        }

        log.Id = Guid.NewGuid();
        log.CreatedAt = DateTime.UtcNow;

        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    public async Task LogAsync<T>(Guid? userId, Guid? companyId, string action, T? oldValue, T? newValue, Guid entityId)
        where T : class
    {
        var log = new AuditLog
        {
            UserId = userId,
            CompanyId = companyId,
            EntityType = typeof(T).Name,
            EntityId = entityId,
            Action = action,
            OldValues = oldValue != null ? JsonSerializer.Serialize(oldValue) : null,
            NewValues = newValue != null ? JsonSerializer.Serialize(newValue) : null
        };

        await LogAsync(log);
    }

    public async Task<List<AuditLog>> GetEntityHistoryAsync(string entityType, Guid entityId)
    {
        return await _context.AuditLogs
            .Include(a => a.User)
            .Where(a => a.EntityType == entityType && a.EntityId == entityId)
            .OrderByDescending(a => a.CreatedAt)
            .Take(100)
            .ToListAsync();
    }

    public async Task<List<AuditLog>> GetCompanyHistoryAsync(Guid companyId, DateTime? from = null, DateTime? to = null)
    {
        var query = _context.AuditLogs
            .Include(a => a.User)
            .Where(a => a.CompanyId == companyId);

        if (from.HasValue)
            query = query.Where(a => a.CreatedAt >= from);

        if (to.HasValue)
            query = query.Where(a => a.CreatedAt <= to);

        return await query
            .OrderByDescending(a => a.CreatedAt)
            .Take(500)
            .ToListAsync();
    }

    public async Task<List<AuditLog>> GetUserHistoryAsync(Guid userId, DateTime? from = null, DateTime? to = null)
    {
        var query = _context.AuditLogs
            .Where(a => a.UserId == userId);

        if (from.HasValue)
            query = query.Where(a => a.CreatedAt >= from);

        if (to.HasValue)
            query = query.Where(a => a.CreatedAt <= to);

        return await query
            .OrderByDescending(a => a.CreatedAt)
            .Take(500)
            .ToListAsync();
    }
}
