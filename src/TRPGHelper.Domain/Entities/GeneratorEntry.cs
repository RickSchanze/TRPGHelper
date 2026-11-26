namespace TRPGHelper.Domain.Entities;

/// <summary>
/// 生成器素材类型。沿用最小集合，新增类型时记得同步管理台的标签映射。
/// </summary>
public enum GeneratorKind
{
    /// <summary>姓名片段，按 <see cref="GeneratorEntry.Category"/> 分组（surname / given / first / epithet）。</summary>
    NamePart = 0,

    /// <summary>职业，例如 COC 的私家侦探、DND 的法师。</summary>
    Occupation = 1,

    /// <summary>职业卡（DND 5E），扩展字段见 ExtraJson。</summary>
    Class = 2,

    /// <summary>种族卡（DND 5E），扩展字段见 ExtraJson。</summary>
    Race = 3,

    /// <summary>背景，例如士兵、贵族。</summary>
    Background = 4,

    /// <summary>车卡灵感（故事钩子），Value 是标题，ExtraJson.text 是正文。</summary>
    Inspiration = 5,

    /// <summary>
    /// 子职（DND 5E），Category 存所属职业 code，Value 是子职名，
    /// ExtraJson 存 description / imageUrl / accent / features。
    /// </summary>
    Subclass = 6,

    /// <summary>
    /// 亚种（DND 5E），Category 存所属种族 code，Value 是亚种名，
    /// ExtraJson 存 description / traits。
    /// </summary>
    Subrace = 7,

    /// <summary>
    /// 阵营（DND 5E 九宫格），Category 存阵营 code（如 lawful-good），
    /// Value 是阵营名，ExtraJson 存 iconUrl / accent。
    /// </summary>
    Alignment = 8,
}

/// <summary>
/// 辅助车卡素材库里的一条数据。所有内容都由管理台维护，不再硬编码在代码里。
/// </summary>
public class GeneratorEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>所属规则系统（GameSystems.Code），为空表示全局可用。</summary>
    public string? SystemCode { get; set; }

    public GeneratorKind Kind { get; set; }

    /// <summary>
    /// 子分组 / 稳定标识：
    /// NamePart 用来区分姓名片段类别，Class / Race 用来存职业、种族的 code。
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>展示文本：姓名片段、职业名、职业卡名、种族名、灵感标题。</summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>扩展字段 JSON，按 Kind 解释，例如职业卡的描述、生命骰、特性列表。</summary>
    public string? ExtraJson { get; set; }

    public int SortOrder { get; set; }

    public bool IsEnabled { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
