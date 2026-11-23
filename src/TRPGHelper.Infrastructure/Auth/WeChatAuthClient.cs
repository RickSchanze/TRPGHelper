using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace TRPGHelper.Infrastructure.Auth;

public sealed record WeChatSession(string OpenId, string? UnionId, string? SessionKey);

public sealed class WeChatNotConfiguredException()
    : Exception("微信登录尚未配置，请在 appsettings 中填写 WeChat:AppId 与 WeChat:AppSecret。");

public sealed class WeChatAuthException(int errorCode, string message)
    : Exception($"微信登录失败（{errorCode}）：{message}")
{
    public int ErrorCode { get; } = errorCode;
}

/// <summary>调用微信 jscode2session 换取 openid。</summary>
public sealed class WeChatAuthClient(HttpClient httpClient, IOptions<WeChatOptions> options)
{
    public async Task<WeChatSession> Code2SessionAsync(string code, CancellationToken cancellationToken = default)
    {
        var weChat = options.Value;
        if (!weChat.IsConfigured)
        {
            throw new WeChatNotConfiguredException();
        }

        var url = "sns/jscode2session"
            + $"?appid={Uri.EscapeDataString(weChat.AppId)}"
            + $"&secret={Uri.EscapeDataString(weChat.AppSecret)}"
            + $"&js_code={Uri.EscapeDataString(code)}"
            + "&grant_type=authorization_code";

        using var response = await httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<Code2SessionResponse>(cancellationToken);
        if (payload is null || string.IsNullOrWhiteSpace(payload.OpenId))
        {
            throw new WeChatAuthException(payload?.ErrorCode ?? -1, payload?.ErrorMessage ?? "未知错误");
        }

        return new WeChatSession(payload.OpenId, payload.UnionId, payload.SessionKey);
    }

    private sealed class Code2SessionResponse
    {
        [JsonPropertyName("openid")]
        public string? OpenId { get; set; }

        [JsonPropertyName("unionid")]
        public string? UnionId { get; set; }

        [JsonPropertyName("session_key")]
        public string? SessionKey { get; set; }

        [JsonPropertyName("errcode")]
        public int ErrorCode { get; set; }

        [JsonPropertyName("errmsg")]
        public string? ErrorMessage { get; set; }
    }
}
