using TRPGHelper.Domain.Enums;

namespace TRPGHelper.Domain.Cards;

/// <summary>
/// 卡状态流转规则。
/// 草稿 → 待用 →（使用中 / 留存）→ 归档；留存可以随时取出，归档也可以重新启用。
/// </summary>
public static class CardRules
{
    private static readonly Dictionary<CardStatus, CardStatus[]> Allowed = new()
    {
        [CardStatus.Draft] = [CardStatus.Ready, CardStatus.Archived],
        [CardStatus.Ready] = [CardStatus.Draft, CardStatus.InUse, CardStatus.Retained, CardStatus.Archived],
        [CardStatus.InUse] = [CardStatus.Ready, CardStatus.Retained, CardStatus.Archived],
        [CardStatus.Retained] = [CardStatus.Ready, CardStatus.InUse, CardStatus.Archived],
        [CardStatus.Archived] = [CardStatus.Ready, CardStatus.Retained],
    };

    public static IReadOnlyList<CardStatus> NextStatuses(CardStatus from)
        => Allowed.TryGetValue(from, out var next) ? next : Array.Empty<CardStatus>();

    public static bool CanTransition(CardStatus from, CardStatus to)
        => from != to && NextStatuses(from).Contains(to);

    /// <summary>根据流转方向决定写哪种操作日志。</summary>
    public static CardAction ActionFor(CardStatus from, CardStatus to) => to switch
    {
        CardStatus.Retained => CardAction.Retained,
        CardStatus.Archived => CardAction.Archived,
        _ when from == CardStatus.Retained => CardAction.Released,
        _ => CardAction.StatusChanged,
    };
}
