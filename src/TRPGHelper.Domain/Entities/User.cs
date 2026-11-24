namespace TRPGHelper.Domain.Entities;

/// <summary>
/// 玩家。目前以微信小程序的 openid 作为唯一身份，开发环境也支持免微信登录。
/// </summary>
public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string OpenId { get; set; } = string.Empty;

    public string? UnionId { get; set; }

    public string NickName { get; set; } = "调查员";

    public string? AvatarUrl { get; set; }

    /// <summary>角色：user / admin。管理员由配置项或管理台授予。</summary>
    public string Role { get; set; } = UserRoles.User;

    /// <summary>UTC 时间，SQLite 原生支持排序与范围比较。</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime LastLoginAt { get; set; } = DateTime.UtcNow;

    public ICollection<CharacterCard> Cards { get; set; } = new List<CharacterCard>();
}
