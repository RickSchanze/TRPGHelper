namespace TRPGHelper.Api.Contracts;

/// <summary>某个职业在某一级获得的一条特性。</summary>
public sealed record ClassLevelFeatureDto(int Level, string Title, string Description);

public sealed record ClassLevelsDto(
    string SystemCode,
    string ClassCode,
    string ClassName,
    IReadOnlyList<ClassLevelFeatureDto> Features);

public sealed record AdminClassLevelFeatureDto(
    Guid Id,
    string SystemCode,
    string ClassCode,
    int Level,
    string Title,
    string Description,
    int SortOrder,
    bool IsEnabled,
    DateTimeOffset UpdatedAt);

public sealed record SaveClassLevelItem(
    int Level,
    string? Title,
    string? Description,
    int SortOrder,
    bool IsEnabled);

public sealed record SaveClassLevelsRequest(
    string? SystemCode,
    IReadOnlyList<SaveClassLevelItem>? Items);
