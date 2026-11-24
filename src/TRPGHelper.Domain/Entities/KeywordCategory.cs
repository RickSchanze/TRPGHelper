namespace TRPGHelper.Domain.Entities;

/// <summary>
/// 关键词分类。Code 是三位数字编号（例如 001），关键词的完整 ID = 分类编号 + 五位序号。
/// </summary>
public class KeywordCategory
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>三位数字分类编号，例如 001 / 002。</summary>
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int SortOrder { get; set; }
}
