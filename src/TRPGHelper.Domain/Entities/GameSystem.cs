namespace TRPGHelper.Domain.Entities;

/// <summary>
/// 跑团规则系统（例如 COC 7版、DND 5版），决定车卡时的属性模板。
/// </summary>
public class GameSystem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>稳定标识，例如 coc7 / dnd5e。</summary>
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>属性模板 JSON：数组，元素为 { key, label, formula }。</summary>
    public string AttributeTemplateJson { get; set; } = "[]";

    public bool IsBuiltin { get; set; }

    public int SortOrder { get; set; }
}
