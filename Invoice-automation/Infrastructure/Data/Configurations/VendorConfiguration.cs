using InvoiceAutomation.Web.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvoiceAutomation.Web.Infrastructure.Data.Configurations;

public class VendorConfiguration : IEntityTypeConfiguration<Vendor>
{
    public void Configure(EntityTypeBuilder<Vendor> builder)
    {
        builder.ToTable("vendors");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(v => v.Ntn)
            .HasMaxLength(20);

        builder.Property(v => v.Strn)
            .HasMaxLength(20);

        builder.Property(v => v.Email)
            .HasMaxLength(255);

        builder.Property(v => v.Phone)
            .HasMaxLength(20);

        builder.Property(v => v.Address)
            .HasMaxLength(500);

        builder.Property(v => v.City)
            .HasMaxLength(100);

        builder.Property(v => v.ContactPerson)
            .HasMaxLength(200);

        // External account ID - NOT a foreign key
        builder.Property(v => v.DefaultExpenseAccountId)
            .HasMaxLength(100);

        builder.Property(v => v.ExternalVendorId)
            .HasMaxLength(100);

        builder.Property(v => v.PaymentTermsDays)
            .HasDefaultValue(30);

        builder.Property(v => v.IsActive)
            .HasDefaultValue(true);

        builder.Property(v => v.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

        // Relationships
        builder.HasOne(v => v.Company)
            .WithMany(c => c.Vendors)
            .HasForeignKey(v => v.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(v => new { v.CompanyId, v.Ntn })
            .IsUnique()
            .HasFilter(null); // For MySQL compatibility

        builder.HasIndex(v => v.CompanyId);
        builder.HasIndex(v => v.IsActive);
    }
}
