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

    /// <summary>
    /// 姓名生成模板，用 {category} 占位符引用 GeneratorEntries 里 NamePart 的分组，
    /// 例如 COC 的 "{surname}{given}"、DND 的 "{first}·{epithet}"。
    /// </summary>
    public string NameTemplate { get; set; } = "{surname}{given}";

    /// <summary>规则书图标地址（前端 static 资源或上传地址）。</summary>
    public string? IconUrl { get; set; }

    /// <summary>图标 / 卡片的主色，形如 #5a1b1b。</summary>
    public string? Accent { get; set; }

    /// <summary>是否已开放（false 时前端显示「即将开放」且不可选）。</summary>
    public bool IsAvailable { get; set; } = true;

    public bool IsBuiltin { get; set; }

    public int SortOrder { get; set; }
}
