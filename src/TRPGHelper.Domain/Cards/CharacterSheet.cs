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

    /// <summary>背景名（与 <see cref="Background"/> 并存，车卡流程写入这里）。</summary>
    public string? BackgroundName { get; set; }

    public string? Gender { get; set; }

    public string? Height { get; set; }

    public string? Weight { get; set; }

    public string? Appearance { get; set; }

    public List<string> Languages { get; set; } = new();

    public SheetProficiencies Proficiencies { get; set; } = new();

    public List<SheetEquipmentItem> Equipment { get; set; } = new();

    public SheetCombat Combat { get; set; } = new();

    public SheetPersonality Personality { get; set; } = new();

    /// <summary>1 级职业特性选择，例如 fightingStyle = 防御。</summary>
    public Dictionary<string, string> FeatureChoices { get; set; } = new();
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

public sealed class SheetProficiencies
{
    public List<string> Armor { get; set; } = new();

    public List<string> Weapons { get; set; } = new();

    public List<string> Tools { get; set; } = new();
}

public sealed class SheetEquipmentItem
{
    public string Name { get; set; } = string.Empty;

    public int Quantity { get; set; } = 1;

    public string? Note { get; set; }
}

public sealed class SheetCombat
{
    public int HitPoints { get; set; }

    public int ArmorClass { get; set; }

    public int Initiative { get; set; }

    public int Speed { get; set; }

    public int ProficiencyBonus { get; set; } = 2;

    public int PassivePerception { get; set; }

    public Dictionary<string, int> SavingThrows { get; set; } = new();
}

public sealed class SheetPersonality
{
    public string? Trait { get; set; }

    public string? Ideal { get; set; }

    public string? Bond { get; set; }

    public string? Flaw { get; set; }
}
