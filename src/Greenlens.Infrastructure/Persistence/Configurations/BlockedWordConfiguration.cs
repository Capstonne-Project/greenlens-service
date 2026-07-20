using Greenlens.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Greenlens.Infrastructure.Persistence.Configurations;

internal sealed class BlockedWordConfiguration : IEntityTypeConfiguration<BlockedWord>
{
    public void Configure(EntityTypeBuilder<BlockedWord> builder)
    {
        builder.ToTable("blocked_words");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Word).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Note).HasMaxLength(500);
        builder.Property(x => x.IsActive).HasDefaultValue(true);

        // Case-insensitive uniqueness enforced by storing normalized lowercase Word
        builder.HasIndex(x => x.Word)
            .IsUnique()
            .HasDatabaseName("ix_blocked_words_word");

        builder.HasIndex(x => x.IsActive)
            .HasDatabaseName("ix_blocked_words_is_active");

        var now = new DateTime(2026, 7, 15, 0, 0, 0, DateTimeKind.Utc);
        builder.HasData(
            Seed(new Guid("b1000001-0000-0000-0000-000000000001"), "địt", now),
            Seed(new Guid("b1000001-0000-0000-0000-000000000002"), "đụ", now),
            Seed(new Guid("b1000001-0000-0000-0000-000000000003"), "lồn", now),
            Seed(new Guid("b1000001-0000-0000-0000-000000000004"), "cặc", now),
            Seed(new Guid("b1000001-0000-0000-0000-000000000005"), "đéo", now),
            Seed(new Guid("b1000001-0000-0000-0000-000000000006"), "vcl", now),
            Seed(new Guid("b1000001-0000-0000-0000-000000000007"), "vl", now),
            Seed(new Guid("b1000001-0000-0000-0000-000000000008"), "fuck", now),
            Seed(new Guid("b1000001-0000-0000-0000-000000000009"), "shit", now),
            Seed(new Guid("b1000001-0000-0000-0000-00000000000a"), "bitch", now));
    }

    private static object Seed(Guid id, string word, DateTime now) => new
    {
        Id = id,
        Word = word,
        Note = (string?)null,
        IsActive = true,
        CreatedAt = now,
        UpdatedAt = (DateTime?)null
    };
}
