using Greenlens.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Greenlens.Infrastructure.Persistence.Configurations;

internal sealed class WasteTagConfiguration : IEntityTypeConfiguration<WasteTag>
{
    public void Configure(EntityTypeBuilder<WasteTag> builder)
    {
        builder.ToTable("waste_tags");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Code).IsRequired().HasMaxLength(50);
        builder.HasIndex(t => t.Code).IsUnique();

        builder.Property(t => t.NameVi).IsRequired().HasMaxLength(100);
        builder.Property(t => t.NameEn).IsRequired().HasMaxLength(100);
        builder.Property(t => t.IconUrl).HasMaxLength(500);
        builder.Property(t => t.Description).HasMaxLength(500);
        builder.Property(t => t.DisplayOrder).HasDefaultValue(0);
        builder.Property(t => t.IsActive).HasDefaultValue(true);

        // ── Seed 12 standard waste tags ──
        builder.HasData(
            Seed("HOUSEHOLD",      "Rác sinh hoạt",        "Household Waste",      1,  "Túi nilon, quần áo cũ, rác hỗn hợp gia đình"),
            Seed("FOOD_ORGANIC",   "Thực phẩm & Hữu cơ",   "Food & Organic",       2,  "Thức ăn thừa, rau củ hỏng, bã trà, rác vườn"),
            Seed("RECYCLABLE",     "Tái chế",              "Recyclable",           3,  "Chai PET, lon nhôm, carton, giấy, thủy tinh"),
            Seed("MEDICAL",        "Rác y tế",             "Medical Waste",        4,  "Khẩu trang, kim tiêm, băng gạc, thuốc hết hạn"),
            Seed("ELECTRONIC",     "Rác điện tử",          "Electronic Waste",     5,  "Điện thoại, dây cáp, bảng mạch, TV, máy tính"),
            Seed("HAZARDOUS",      "Nguy hại",             "Hazardous",            6,  "Pin, bình hóa chất, sơn, dầu nhớt, thuốc trừ sâu"),
            Seed("CONSTRUCTION",   "Phế thải xây dựng",     "Construction Debris",  7,  "Gạch, xi măng, tấm lợp, ống nước, sắt thép"),
            Seed("BULKY",          "Đồ cồng kềnh",        "Bulky Items",          8,  "Nệm, ghế sofa, tủ lạnh, bàn ghế, máy giặt"),
            Seed("TIRE",           "Lốp xe",              "Tires",                9,  "Lốp xe máy, ô tô, xe tải bị vứt bỏ"),
            Seed("ANIMAL_CARCASS", "Xác động vật",         "Animal Carcass",       10, "Chuột, chó mèo bị xe cán, gia cầm chết"),
            Seed("TEXTILE",        "Vải, quần áo",         "Textile",              11, "Quần áo cũ, vải vụn, thảm, rèm cửa"),
            Seed("VEGETATION",     "Cây cỏ, lá",          "Yard/Vegetation",      12, "Cành cây, gốc cây, cỏ, lá khô số lượng lớn")
        );
    }

    /// <summary>Deterministic GUID for stable seed data across environments.</summary>
    private static object Seed(string code, string nameVi, string nameEn, int order, string desc) => new
    {
        Id = DeterministicGuid(code),
        Code = code,
        NameVi = nameVi,
        NameEn = nameEn,
        IconUrl = (string?)null,
        Description = desc,
        DisplayOrder = order,
        IsActive = true,
        CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
    };

    private static Guid DeterministicGuid(string input)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes("WasteTag:" + input));
        var g = bytes[..16];
        g[6] = (byte)((g[6] & 0x0F) | 0x50);
        g[8] = (byte)((g[8] & 0x3F) | 0x80);
        return new Guid(g);
    }
}
