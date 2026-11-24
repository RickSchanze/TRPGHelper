namespace TRPGHelper.Domain.Entities;

/// <summary>
/// 关键词词条：前端文本里出现 [关键词] 时，点击可以弹出它的解释。
/// 解释文本本身也可以带 [关键词]，支持一层层往下点。
/// </summary>
public class Keyword
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>关键词本身，不含方括号，例如「敏捷」。不区分大小写唯一。</summary>
    /// <summary>关键词编号：三位分类号 + 五位序号，例如 00200001。</summary>
    public string Code { get; set; } = string.Empty;

    public string Key { get; set; } = string.Empty;

    /// <summary>弹窗标题，为空时用 Key。</summary>
    /// <summary>分类（例如 属性 / 规则 / 战斗），用于把词条分到不同的 xlsx 文件里。</summary>
    public string Category { get; set; } = string.Empty;

    public string? Title { get; set; }

    /// <summary>简略描述：一句话结论，用于弹窗标题下、词条列表与卡面提示；不嵌 [关键词]。</summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>解释正文，可以包含 [其它关键词]。</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>配图地址（可以是相对路径或上传后的 /uploads 地址）。</summary>
    public string? ImageUrl { get; set; }

    public int SortOrder { get; set; }

    public bool IsEnabled { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
