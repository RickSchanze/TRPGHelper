namespace TRPGHelper.Domain.Cards;

/// <summary>
/// 卡面数据。故意保持“松散”结构：不同规则系统的字段差异很大，先存 JSON，
/// 需要时再逐步升级成强类型字段。
/// </summary>
public sealed class CharacterSheet
{
    public List<SheetAttribute> Attributes { get; set; } = new();

    public List<SheetSkill> Skills { get; set; } = new();

    public List<string> Items { get; set; } = new();

    public string? Background { get; set; }

    public string? Notes { get; set; }
}

public sealed class SheetAttribute
{
    public string Key { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    public int Value { get; set; }

    /// <summary>生成这个属性用到的骰子表达式，便于复现 / 展示。</summary>
    public string? Formula { get; set; }
}

public sealed class SheetSkill
{
    public string Name { get; set; } = string.Empty;

    public int Value { get; set; }

    public string? Note { get; set; }
}
