using TRPGHelper.Domain.Cards;

namespace TRPGHelper.Tests;

public class AttributeGeneratorTests
{
    private static readonly AttributeTemplate[] Coc7Templates =
    [
        new("STR", "力量", "3d6*5"),
        new("CON", "体质", "3d6*5"),
        new("SIZ", "体型", "2d6*5+30"),
        new("DEX", "敏捷", "3d6*5"),
        new("APP", "外貌", "3d6*5"),
        new("INT", "智力", "2d6*5+30"),
        new("POW", "意志", "3d6*5"),
        new("EDU", "教育", "2d6*5+30"),
        new("Luck", "幸运", "3d6*5"),
    ];

    [Fact]
    public void Generate_RollMethod_ProducesOneValuePerAttribute()
    {
        var attributes = AttributeGenerator.Generate(Coc7Templates, "roll", new Random(7));

        Assert.Equal(Coc7Templates.Length, attributes.Count);
        Assert.All(attributes, attribute => Assert.NotEmpty(attribute.Key));

        var size = attributes.Single(attribute => attribute.Key == "SIZ");
        Assert.InRange(size.Value, 40, 90);
    }

    [Fact]
    public void Generate_StandardMethod_UsesStandardArray()
    {
        var attributes = AttributeGenerator.Generate(
            Coc7Templates.Take(6).ToArray(),
            "standard");

        Assert.Equal([15, 14, 13, 12, 10, 8], attributes.Select(attribute => attribute.Value));
    }

    [Fact]
    public void Generate_UnknownMethod_Throws()
    {
        Assert.Throws<ArgumentException>(() => AttributeGenerator.Generate(Coc7Templates, "banana"));
    }

    [Fact]
    public void Generate_EmptyTemplate_ReturnsEmpty()
    {
        Assert.Empty(AttributeGenerator.Generate([]));
    }

    private static readonly AttributeTemplate[] Dnd5eTemplates =
    [
        new("STR", "力量", "4d6kh3"),
        new("DEX", "敏捷", "4d6kh3"),
        new("CON", "体质", "4d6kh3"),
        new("INT", "智力", "4d6kh3"),
        new("WIS", "感知", "4d6kh3"),
        new("CHA", "魅力", "4d6kh3"),
    ];

    [Fact]
    public void GenerateDetailed_RollMethod_KeepsDiceDetail()
    {
        var rolls = AttributeGenerator.GenerateDetailed(Dnd5eTemplates, "roll", new Random(11));

        Assert.Equal(6, rolls.Count);
        Assert.All(rolls, roll =>
        {
            Assert.Equal(4, roll.Dice.Count);
            Assert.Equal(3, roll.Kept.Count);
            Assert.Equal(roll.Kept.Sum(), roll.Base);
            Assert.Equal("4d6kh3", roll.Formula);
        });
    }

    [Fact]
    public void GenerateDetailed_StandardMethod_ReturnsStandardArrayInOrder()
    {
        var rolls = AttributeGenerator.GenerateDetailed(Dnd5eTemplates, "standard");

        Assert.Equal([15, 14, 13, 12, 10, 8], rolls.Select(roll => roll.Base));
        Assert.All(rolls, roll =>
        {
            Assert.Empty(roll.Dice);
            Assert.Equal("标准数组", roll.Formula);
        });
    }

    [Fact]
    public void GenerateDetailed_ManualMethod_StartsFromTen()
    {
        var rolls = AttributeGenerator.GenerateDetailed(Dnd5eTemplates, "manual");

        Assert.All(rolls, roll =>
        {
            Assert.Equal(10, roll.Base);
            Assert.Equal("手动填写", roll.Formula);
        });
    }
}
