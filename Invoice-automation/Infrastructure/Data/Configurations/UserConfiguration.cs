using InvoiceAutomation.Web.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvoiceAutomation.Web.Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        // Identity columns - map to snake_case
        builder.Property(u => u.PasswordHash)
            .HasColumnName("password_hash");

        builder.Property(u => u.UserName)
            .HasColumnName("user_name");

        builder.Property(u => u.NormalizedUserName)
            .HasColumnName("normalized_user_name");

        builder.Property(u => u.NormalizedEmail)
            .HasColumnName("normalized_email");

        builder.Property(u => u.EmailConfirmed)
            .HasColumnName("email_confirmed");

        builder.Property(u => u.SecurityStamp)
            .HasColumnName("security_stamp");

        builder.Property(u => u.ConcurrencyStamp)
            .HasColumnName("concurrency_stamp");

        builder.Property(u => u.PhoneNumber)
            .HasColumnName("phone_number");

        builder.Property(u => u.PhoneNumberConfirmed)
            .HasColumnName("phone_number_confirmed");

        builder.Property(u => u.TwoFactorEnabled)
            .HasColumnName("two_factor_enabled");

        builder.Property(u => u.LockoutEnd)
            .HasColumnName("lockout_end");

        builder.Property(u => u.LockoutEnabled)
            .HasColumnName("lockout_enabled");

        builder.Property(u => u.AccessFailedCount)
            .HasColumnName("access_failed_count");

        // Custom columns
        builder.Property(u => u.FullName)
            .HasColumnName("full_name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(u => u.Phone)
            .HasMaxLength(20);

        builder.Property(u => u.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(u => u.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(u => u.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(u => u.LastLoginAt)
            .HasColumnName("last_login_at");

        // Indexes
        builder.HasIndex(u => u.Email)
            .IsUnique();

        builder.HasIndex(u => u.IsActive);
    }
}

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.Property(r => r.NormalizedName)
            .HasColumnName("normalized_name");

        builder.Property(r => r.ConcurrencyStamp)
            .HasColumnName("concurrency_stamp");

        builder.Property(r => r.Description)
            .HasMaxLength(500);

        builder.Property(r => r.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
    }
}
