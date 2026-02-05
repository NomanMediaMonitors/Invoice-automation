using InvoiceAutomation.Web.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvoiceAutomation.Web.Infrastructure.Data.Configurations;

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("invoices");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.InvoiceNumber)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(i => i.Subtotal)
            .HasPrecision(15, 2);

        builder.Property(i => i.TaxAmount)
            .HasPrecision(15, 2)
            .HasDefaultValue(0);

        builder.Property(i => i.TotalAmount)
            .HasPrecision(15, 2)
            .IsRequired();

        builder.Property(i => i.Currency)
            .HasMaxLength(3)
            .HasDefaultValue("PKR");

        builder.Property(i => i.Status)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(i => i.OriginalFilePath)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(i => i.OriginalFileName)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(i => i.ContentType)
            .HasMaxLength(100);

        builder.Property(i => i.OcrData)
            .HasColumnType("JSON");

        builder.Property(i => i.OcrConfidence)
            .HasPrecision(5, 2);

        builder.Property(i => i.Notes)
            .HasMaxLength(2000);

        builder.Property(i => i.ExternalRef)
            .HasMaxLength(100);

        builder.Property(i => i.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

        // Relationships
        builder.HasOne(i => i.Company)
            .WithMany(c => c.Invoices)
            .HasForeignKey(i => i.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Vendor)
            .WithMany(v => v.Invoices)
            .HasForeignKey(i => i.VendorId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(i => i.UploadedBy)
            .WithMany(u => u.UploadedInvoices)
            .HasForeignKey(i => i.UploadedById)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(i => new { i.CompanyId, i.Status });
        builder.HasIndex(i => new { i.CompanyId, i.InvoiceDate });
        builder.HasIndex(i => i.VendorId);
        builder.HasIndex(i => i.InvoiceNumber);
        builder.HasIndex(i => i.Status);
        builder.HasIndex(i => i.CreatedAt);
    }
}
