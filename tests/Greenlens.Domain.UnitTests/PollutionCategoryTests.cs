using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;

namespace Greenlens.Domain.UnitTests;

public sealed class PollutionCategoryTests
{
    [Fact]
    public void Create_ShouldSetDefaults()
    {
        var cat = PollutionCategory.Create("TRASH", "Rác thải", "Trash", "https://icon/trash.png");

        Assert.Equal("TRASH", cat.Code);
        Assert.Equal("Rác thải", cat.NameVi);
        Assert.Equal("Trash", cat.NameEn);
        Assert.Equal("https://icon/trash.png", cat.IconUrl);
        Assert.True(cat.IsActive);
    }

    [Fact]
    public void Deactivate_ShouldSetInactive()
    {
        var cat = PollutionCategory.Create("NOISE", "Tiếng ồn", "Noise");

        cat.Deactivate();

        Assert.False(cat.IsActive);
    }

    [Fact]
    public void Activate_AfterDeactivate_ShouldReactivate()
    {
        var cat = PollutionCategory.Create("NOISE", "Tiếng ồn", "Noise");
        cat.Deactivate();

        cat.Activate();

        Assert.True(cat.IsActive);
    }

    [Fact]
    public void Update_ShouldChangeFields()
    {
        var cat = PollutionCategory.Create("TRASH", "Rác", "Trash");

        cat.Update("Rác thải sinh hoạt", "Household Trash", "https://new-icon.png");

        Assert.Equal("Rác thải sinh hoạt", cat.NameVi);
        Assert.Equal("Household Trash", cat.NameEn);
        Assert.Equal("https://new-icon.png", cat.IconUrl);
    }
}
