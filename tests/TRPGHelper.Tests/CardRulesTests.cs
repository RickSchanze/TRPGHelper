using TRPGHelper.Domain.Cards;
using TRPGHelper.Domain.Enums;

namespace TRPGHelper.Tests;

public class CardRulesTests
{
    [Theory]
    [InlineData(CardStatus.Draft, CardStatus.Ready, true)]
    [InlineData(CardStatus.Ready, CardStatus.Retained, true)]
    [InlineData(CardStatus.Ready, CardStatus.InUse, true)]
    [InlineData(CardStatus.InUse, CardStatus.Retained, true)]
    [InlineData(CardStatus.Retained, CardStatus.Ready, true)]
    [InlineData(CardStatus.Archived, CardStatus.Ready, true)]
    [InlineData(CardStatus.Draft, CardStatus.InUse, false)]
    [InlineData(CardStatus.Ready, CardStatus.Draft, true)]
    [InlineData(CardStatus.Ready, CardStatus.Ready, false)]
    public void CanTransition_FollowsLifecycle(CardStatus from, CardStatus to, bool expected)
    {
        Assert.Equal(expected, CardRules.CanTransition(from, to));
    }

    [Fact]
    public void NextStatuses_ExposesAllowedTargets()
    {
        var next = CardRules.NextStatuses(CardStatus.Ready);

        Assert.Contains(CardStatus.Retained, next);
        Assert.Contains(CardStatus.InUse, next);
        Assert.DoesNotContain(CardStatus.Ready, next);
    }

    [Theory]
    [InlineData(CardStatus.Ready, CardStatus.Retained, CardAction.Retained)]
    [InlineData(CardStatus.Retained, CardStatus.Ready, CardAction.Released)]
    [InlineData(CardStatus.InUse, CardStatus.Archived, CardAction.Archived)]
    [InlineData(CardStatus.Draft, CardStatus.Ready, CardAction.StatusChanged)]
    public void ActionFor_MapsIntendedAction(CardStatus from, CardStatus to, CardAction expected)
    {
        Assert.Equal(expected, CardRules.ActionFor(from, to));
    }

    [Fact]
    public void StatusKeys_AreStableForApi()
    {
        Assert.Equal("inUse", CardStatus.InUse.ToKey());
        Assert.Equal("留存", CardStatus.Retained.ToLabel());
        Assert.True(CardStatusParser.TryParse("inUse", out var parsed));
        Assert.Equal(CardStatus.InUse, parsed);
    }
}
