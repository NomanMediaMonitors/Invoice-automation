using InvoiceAutomation.Web.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvoiceAutomation.Web.Infrastructure.Data.Configurations;

public class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.ToTable("companies");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(c => c.Ntn)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(c => c.Strn)
            .HasMaxLength(20);

        builder.Property(c => c.Address)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(c => c.City)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(c => c.State)
            .HasMaxLength(100);

        builder.Property(c => c.Country)
            .HasMaxLength(100)
            .HasDefaultValue("Pakistan");

        builder.Property(c => c.PostalCode)
            .HasMaxLength(20);

        builder.Property(c => c.Email)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(c => c.Phone)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(c => c.Website)
            .HasMaxLength(255);

        builder.Property(c => c.LogoPath)
            .HasMaxLength(500);

        builder.Property(c => c.DefaultCurrency)
            .HasMaxLength(3)
            .HasDefaultValue("PKR");

        builder.Property(c => c.AccountingProvider)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(c => c.EncryptedAccessToken)
            .HasColumnType("TEXT");

        builder.Property(c => c.EncryptedRefreshToken)
            .HasColumnType("TEXT");

        builder.Property(c => c.ExternalCompanyId)
            .HasMaxLength(100);

        builder.Property(c => c.IsActive)
            .HasDefaultValue(true);

        builder.Property(c => c.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

        // Indexes
        builder.HasIndex(c => c.Ntn)
            .IsUnique();

        builder.HasIndex(c => c.IsActive);
    }
}
