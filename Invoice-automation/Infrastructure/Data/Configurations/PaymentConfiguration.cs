using InvoiceAutomation.Web.Core.Entities;
using InvoiceAutomation.Web.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvoiceAutomation.Web.Infrastructure.Data.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payments");

        builder.HasKey(p => p.Id);

        // External account ID - NOT a foreign key
        builder.Property(p => p.PaymentAccountId)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(p => p.PaymentAccountName)
            .HasMaxLength(200);

        builder.Property(p => p.Amount)
            .HasPrecision(15, 2)
            .IsRequired();

        builder.Property(p => p.PaymentMethod)
            .HasMaxLength(100);

        builder.Property(p => p.ReferenceNumber)
            .HasMaxLength(100);

        builder.Property(p => p.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(PaymentStatus.Scheduled);

        builder.Property(p => p.ExternalRef)
            .HasMaxLength(100);

        builder.Property(p => p.JournalEntryRef)
            .HasMaxLength(100);

        builder.Property(p => p.FailureReason)
            .HasMaxLength(500);

        builder.Property(p => p.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

        // Relationships
        builder.HasOne(p => p.Invoice)
            .WithMany(i => i.Payments)
            .HasForeignKey(p => p.InvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.ExecutedBy)
            .WithMany(u => u.ExecutedPayments)
            .HasForeignKey(p => p.ExecutedById)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder.HasIndex(p => p.InvoiceId);
        builder.HasIndex(p => p.ExecutedById);
        builder.HasIndex(p => p.Status);
        builder.HasIndex(p => p.ScheduledDate);
    }
}
