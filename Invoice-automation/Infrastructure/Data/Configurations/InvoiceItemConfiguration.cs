using InvoiceAutomation.Web.Core.Entities;
using InvoiceAutomation.Web.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CoreMatchType = InvoiceAutomation.Web.Core.Enums.MatchType;

namespace InvoiceAutomation.Web.Infrastructure.Data.Configurations;

public class InvoiceItemConfiguration : IEntityTypeConfiguration<InvoiceItem>
{
    public void Configure(EntityTypeBuilder<InvoiceItem> builder)
    {
        builder.ToTable("invoice_items");

        builder.HasKey(ii => ii.Id);

        // External account ID - NOT a foreign key
        builder.Property(ii => ii.ExpenseAccountId)
            .HasMaxLength(100);

        builder.Property(ii => ii.Description)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(ii => ii.Quantity)
            .HasPrecision(10, 3)
            .HasDefaultValue(1);

        builder.Property(ii => ii.Unit)
            .HasMaxLength(50);

        builder.Property(ii => ii.UnitPrice)
            .HasPrecision(15, 2);

        builder.Property(ii => ii.TaxAmount)
            .HasPrecision(15, 2)
            .HasDefaultValue(0);

        builder.Property(ii => ii.Amount)
            .HasPrecision(15, 2)
            .IsRequired();

        builder.Property(ii => ii.MatchType)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(CoreMatchType.Manual);

        builder.Property(ii => ii.MatchConfidence)
            .HasPrecision(5, 2);

        // Relationships
        builder.HasOne(ii => ii.Invoice)
            .WithMany(i => i.Items)
            .HasForeignKey(ii => ii.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(ii => ii.InvoiceId);
    }
}
