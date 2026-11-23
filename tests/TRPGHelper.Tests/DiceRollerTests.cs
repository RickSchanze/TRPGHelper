using TRPGHelper.Domain.Dice;

namespace TRPGHelper.Tests;

public class DiceRollerTests
{
    [Fact]
    public void Roll_SingleDie_StaysWithinRange()
    {
        var result = DiceRoller.Roll("d20");

        Assert.InRange(result.Total, 1, 20);
        var term = Assert.Single(result.Terms);
        Assert.Single(term.Rolls);
    }

    [Fact]
    public void Roll_KeepHighest_DropsLowestDice()
    {
        var result = DiceRoller.Roll("4d6kh3");

        var term = Assert.Single(result.Terms);
        Assert.Equal(4, term.Rolls.Count);
        Assert.Equal(3, term.KeptRolls.Count);
        Assert.InRange(result.Total, 3, 18);
        Assert.Equal(term.KeptRolls.Sum(), result.Total);
        Assert.Equal(term.Rolls.Sum() - term.Rolls.Min(), term.KeptRolls.Sum());
    }

    [Fact]
    public void Roll_WithMultiplier_ScalesSubtotal()
    {
        var result = DiceRoller.Roll("3d6*5");

        Assert.InRange(result.Total, 15, 90);
        Assert.Equal(0, result.Total % 5);
    }

    [Fact]
    public void Roll_CocSizeFormula_StaysWithinExpectedBand()
    {
        var result = DiceRoller.Roll("2d6*5+30");

        Assert.InRange(result.Total, 40, 90);
        Assert.Equal(2, result.Terms.Count);
    }

    [Fact]
    public void Roll_Subtraction_Works()
    {
        var result = DiceRoller.Roll("1d100-10");

        Assert.InRange(result.Total, -9, 90);
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("d")]
    [InlineData("10d6kh20")]
    [InlineData("0d6")]
    public void Roll_InvalidExpression_Throws(string expression)
    {
        Assert.ThrowsAny<ArgumentException>(() => DiceRoller.Roll(expression));
    }

    [Fact]
    public void Roll_WithSeed_IsDeterministic()
    {
        var first = DiceRoller.Roll("6d6", new Random(42));
        var second = DiceRoller.Roll("6d6", new Random(42));

        Assert.Equal(first.Total, second.Total);
        Assert.Equal(first.Terms[0].Rolls, second.Terms[0].Rolls);
    }
}
