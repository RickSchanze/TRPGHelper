using TRPGHelper.Domain.Cards;
using TRPGHelper.Domain.Enums;

namespace TRPGHelper.Domain.Entities;

/// <summary>
/// 角色卡（俗称“卡”）。一张卡属于一个玩家、一个规则系统，卡面数据以 JSON 保存。
/// </summary>
public class CharacterCard
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid OwnerId { get; set; }

    public User Owner { get; set; } = null!;

    public Guid SystemId { get; set; }

    public GameSystem System { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public string? Occupation { get; set; }

    /// <summary>规则系统里的职业，例如 DND 的「战士」。</summary>
    public string? ClassName { get; set; }

    /// <summary>子职，例如圣武士的「奉献之誓」。</summary>
    public string? SubclassName { get; set; }

    /// <summary>规则系统里的种族，例如 DND 的「半精灵」。</summary>
    public string? RaceName { get; set; }

    /// <summary>亚种，例如「高等精灵」「山地矮人」。</summary>
    public string? SubraceName { get; set; }

    public int? Age { get; set; }

    public string? Summary { get; set; }

    public string? PortraitUrl { get; set; }

    public CardStatus Status { get; set; } = CardStatus.Draft;

    public bool IsFavorite { get; set; }

    /// <summary>卡面全量数据：属性、技能、物品、背景等。</summary>
    public CharacterSheet Sheet { get; set; } = new();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? RetainedAt { get; set; }

    public ICollection<CardLog> Logs { get; set; } = new List<CardLog>();
}
