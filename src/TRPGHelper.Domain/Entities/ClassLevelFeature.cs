namespace TRPGHelper.Domain.Entities;

/// <summary>
/// 职业的等级成长：某个职业在第 N 级获得什么（按 DND 规则逐级列）。
/// 同等级可以有多条（例如 1 级同时获得「战斗风格」和「回气」）。
/// 说明文本支持关键词（[关键词]）。
/// </summary>
public class ClassLevelFeature
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>所属规则系统（GameSystems.Code），目前只有 dnd5e。</summary>
    public string SystemCode { get; set; } = "dnd5e";

    /// <summary>所属职业的稳定 code，对应 GeneratorEntries(Kind=Class).Category。</summary>
    public string ClassCode { get; set; } = string.Empty;

    /// <summary>等级 1-20。</summary>
    public int Level { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public bool IsEnabled { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
