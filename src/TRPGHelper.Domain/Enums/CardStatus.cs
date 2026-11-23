namespace TRPGHelper.Domain.Enums;

/// <summary>
/// 一张角色卡（卡）在卡库中的生命周期状态。
/// </summary>
public enum CardStatus
{
    /// <summary>草稿：还在车（创建）的过程中，未定稿。</summary>
    Draft = 0,

    /// <summary>待用：已经车好，等待上桌。</summary>
    Ready = 1,

    /// <summary>使用中：正在跑团 / 正在用。</summary>
    InUse = 2,

    /// <summary>留存：留着的卡，想留、暂不打算用。</summary>
    Retained = 3,

    /// <summary>归档：暂时收起来，不占列表主视图。</summary>
    Archived = 4,
}

public static class CardStatusExtensions
{
    /// <summary>给界面展示用的中文名。</summary>
    public static string ToLabel(this CardStatus status) => status switch
    {
        CardStatus.Draft => "草稿",
        CardStatus.Ready => "待用",
        CardStatus.InUse => "使用中",
        CardStatus.Retained => "留存",
        CardStatus.Archived => "归档",
        _ => status.ToString(),
    };

    /// <summary>给接口使用的稳定 key（camelCase）。</summary>
    public static string ToKey(this CardStatus status) => status switch
    {
        CardStatus.Draft => "draft",
        CardStatus.Ready => "ready",
        CardStatus.InUse => "inUse",
        CardStatus.Retained => "retained",
        CardStatus.Archived => "archived",
        _ => status.ToString().ToLowerInvariant(),
    };

}

/// <summary>
/// 状态字符串解析。单独放一个类，避免和 Enum.TryParse 的静态成员解析冲突。
/// </summary>
public static class CardStatusParser
{
    public static bool TryParse(string? value, out CardStatus status)
    {
        status = CardStatus.Draft;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return Enum.TryParse(value.Trim(), ignoreCase: true, out status)
            && Enum.IsDefined(status);
    }

    public static CardStatus Parse(string value)
        => TryParse(value, out var status)
            ? status
            : throw new ArgumentException($"未知的卡状态：{value}。", nameof(value));
}
