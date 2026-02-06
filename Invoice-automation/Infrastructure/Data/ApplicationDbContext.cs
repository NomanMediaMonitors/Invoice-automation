using InvoiceAutomation.Web.Core.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace InvoiceAutomation.Web.Infrastructure.Data;

/// <summary>
/// Main application database context
/// </summary>
public class ApplicationDbContext : IdentityDbContext<User, Role, Guid>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Company> Companies => Set<Company>();
    public DbSet<UserCompany> UserCompanies => Set<UserCompany>();
    public DbSet<Vendor> Vendors => Set<Vendor>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();
    public DbSet<InvoiceApproval> InvoiceApprovals => Set<InvoiceApproval>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Convert all column names to snake_case
        foreach (var entity in builder.Model.GetEntityTypes())
        {
            foreach (var property in entity.GetProperties())
            {
                var name = property.Name;
                var snakeCase = string.Concat(
                    name.Select((c, i) =>
                        i > 0 && char.IsUpper(c) ? "_" + c : c.ToString()
                    )
                ).ToLower();

                property.SetColumnName(snakeCase);
            }
        }

        // Identity table names
        builder.Entity<User>().ToTable("users");
        builder.Entity<Role>().ToTable("roles");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserRole<Guid>>().ToTable("user_roles");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserClaim<Guid>>().ToTable("user_claims");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserLogin<Guid>>().ToTable("user_logins");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserToken<Guid>>().ToTable("user_tokens");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityRoleClaim<Guid>>().ToTable("role_claims");

        // Apply snake_case naming convention to all columns
        builder.ApplySnakeCaseNamingConvention();
    }

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            // Set UpdatedAt for entities that have it
            var updatedAtProperty = entry.Entity.GetType().GetProperty("UpdatedAt");
            if (updatedAtProperty != null && updatedAtProperty.PropertyType == typeof(DateTime?))
            {
                updatedAtProperty.SetValue(entry.Entity, DateTime.UtcNow);
            }
        }
    }
}
