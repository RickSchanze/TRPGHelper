namespace TRPGHelper.Domain.Entities;

/// <summary>
/// 用户角色。目前只有「普通玩家」和「管理员」，管理台接口统一要求 admin 角色。
/// </summary>
public static class UserRoles
{
    public const string User = "user";

    public const string Admin = "admin";

    public static bool IsAdmin(string? role)
        => string.Equals(role, Admin, StringComparison.OrdinalIgnoreCase);
}
