namespace TRPGHelper.Infrastructure.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "TRPGHelper";

    public string Audience { get; set; } = "TRPGHelper.Client";

    public string Key { get; set; } = string.Empty;

    /// <summary>访问令牌有效期（分钟），默认 7 天。</summary>
    public int AccessTokenMinutes { get; set; } = 60 * 24 * 7;

    public bool IsValid => !string.IsNullOrWhiteSpace(Key) && Key.Length >= 32;
}

public sealed class WeChatOptions
{
    public const string SectionName = "WeChat";

    public string AppId { get; set; } = string.Empty;

    public string AppSecret { get; set; } = string.Empty;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(AppId) && !string.IsNullOrWhiteSpace(AppSecret);
}

public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    /// <summary>仅本地开发用：跳过微信登录，直接用昵称换 token。</summary>
    public bool EnableDevLogin { get; set; }
}

public sealed class AdminOptions
{
    public const string SectionName = "Admin";

    /// <summary>本地开发登录的账号是否直接获得管理员角色（仅 Development 生效时建议开启）。</summary>
    public bool DevLoginIsAdmin { get; set; } = true;

    /// <summary>登录时自动提升为管理员的 openid 白名单，用来给首个管理员开门。</summary>
    public string[] BootstrapOpenIds { get; set; } = [];
}
