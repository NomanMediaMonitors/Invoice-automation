using InvoiceAutomation.Web.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvoiceAutomation.Web.Infrastructure.Data.Configurations;

public class UserCompanyConfiguration : IEntityTypeConfiguration<UserCompany>
{
    public void Configure(EntityTypeBuilder<UserCompany> builder)
    {
        builder.ToTable("user_companies");

        builder.HasKey(uc => uc.Id);

        builder.Property(uc => uc.Role)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(uc => uc.IsActive)
            .HasDefaultValue(true);

        builder.Property(uc => uc.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

        // Relationships
        builder.HasOne(uc => uc.User)
            .WithMany(u => u.UserCompanies)
            .HasForeignKey(uc => uc.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(uc => uc.Company)
            .WithMany(c => c.UserCompanies)
            .HasForeignKey(uc => uc.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(uc => new { uc.UserId, uc.CompanyId })
            .IsUnique();

        builder.HasIndex(uc => uc.UserId);
        builder.HasIndex(uc => uc.CompanyId);
    }
}
