using InvoiceAutomation.Web.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvoiceAutomation.Web.Infrastructure.Data.Configurations;

public class InvoiceApprovalConfiguration : IEntityTypeConfiguration<InvoiceApproval>
{
    public void Configure(EntityTypeBuilder<InvoiceApproval> builder)
    {
        builder.ToTable("invoice_approvals");

        builder.HasKey(ia => ia.Id);

        builder.Property(ia => ia.ApprovalLevel)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(ia => ia.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue("Pending");

        builder.Property(ia => ia.Comments)
            .HasMaxLength(2000);

        builder.Property(ia => ia.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

        // Relationships
        builder.HasOne(ia => ia.Invoice)
            .WithMany(i => i.Approvals)
            .HasForeignKey(ia => ia.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ia => ia.Approver)
            .WithMany(u => u.Approvals)
            .HasForeignKey(ia => ia.ApproverId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder.HasIndex(ia => ia.InvoiceId);
        builder.HasIndex(ia => ia.ApproverId);
        builder.HasIndex(ia => new { ia.InvoiceId, ia.ApprovalLevel });
    }
}
