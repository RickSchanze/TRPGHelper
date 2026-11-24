namespace TRPGHelper.Api.Contracts;

/// <summary>前端关键词弹窗用的词条。</summary>
public sealed record KeywordDto(
    string Key,
    string? Title,
    string Content,
    string? ImageUrl);

public sealed record AdminKeywordDto(
    Guid Id,
    string Key,
    string Category,
    string? Title,
    string Content,
    string? ImageUrl,
    int SortOrder,
    bool IsEnabled,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record SaveKeywordRequest(
    string? Key,
    string? Category,
    string? Title,
    string? Content,
    string? ImageUrl,
    int SortOrder,
    bool IsEnabled);
