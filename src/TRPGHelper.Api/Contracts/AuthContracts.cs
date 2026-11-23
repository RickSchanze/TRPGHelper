namespace TRPGHelper.Api.Contracts;

public sealed record UserDto(
    Guid Id,
    string NickName,
    string? AvatarUrl,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastLoginAt,
    int TotalCards,
    int RetainedCards);

public sealed record WeChatLoginRequest(string Code, string? NickName, string? AvatarUrl);

public sealed record DevLoginRequest(string? NickName);

public sealed record LoginResponse(string Token, DateTimeOffset ExpiresAt, UserDto User);

public sealed record UpdateProfileRequest(string? NickName, string? AvatarUrl);
