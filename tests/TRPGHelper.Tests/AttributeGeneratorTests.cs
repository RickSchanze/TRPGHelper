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
}
