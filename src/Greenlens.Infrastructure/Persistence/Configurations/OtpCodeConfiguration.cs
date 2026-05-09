using Greenlens.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Greenlens.Infrastructure.Persistence.Configurations;

internal sealed class OtpCodeConfiguration : IEntityTypeConfiguration<OtpCode>
{
    public void Configure(EntityTypeBuilder<OtpCode> builder)
    {
        builder.ToTable("otp_codes");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Email).IsRequired().HasMaxLength(256);
        builder.Property(o => o.CodeHash).IsRequired().HasMaxLength(512);
        builder.Property(o => o.Purpose).HasConversion<string>().HasMaxLength(50);

        builder.HasIndex(o => new { o.Email, o.Purpose, o.ExpiresAt });
    }
}
