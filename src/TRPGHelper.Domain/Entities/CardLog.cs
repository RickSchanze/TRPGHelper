using TRPGHelper.Domain.Enums;

namespace TRPGHelper.Domain.Entities;

/// <summary>
/// 一张卡的操作记录。用它串起“什么时候车了什么、什么时候留了哪张卡”。
/// </summary>
public class CardLog
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid CharacterCardId { get; set; }

    public CharacterCard CharacterCard { get; set; } = null!;

    public CardAction Action { get; set; }

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
