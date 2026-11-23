namespace TRPGHelper.Api.Contracts;

public sealed record SheetAttributeDto(string Key, string Label, int Value, string? Formula);

public sealed record SheetSkillDto(string Name, int Value, string? Note);

public sealed record CharacterSheetDto(
    List<SheetAttributeDto>? Attributes,
    List<SheetSkillDto>? Skills,
    List<string>? Items,
    string? Background,
    string? Notes);

public sealed record CharacterSummaryDto(
    Guid Id,
    string Name,
    string? Occupation,
    string? ClassName,
    string? RaceName,
    int? Age,
    string? Summary,
    string? PortraitUrl,
    Guid SystemId,
    string SystemCode,
    string SystemName,
    string Status,
    string StatusLabel,
    bool IsFavorite,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? RetainedAt);

public sealed record CardLogDto(Guid Id, string Action, string ActionLabel, string? Note, DateTimeOffset CreatedAt);

public sealed record CharacterDetailDto(
    Guid Id,
    string Name,
    string? Occupation,
    string? ClassName,
    string? RaceName,
    int? Age,
    string? Summary,
    string? PortraitUrl,
    Guid SystemId,
    string SystemCode,
    string SystemName,
    string Status,
    string StatusLabel,
    bool IsFavorite,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? RetainedAt,
    CharacterSheetDto Sheet,
    IReadOnlyList<CardLogDto> Logs,
    IReadOnlyList<string> NextStatuses,
    IReadOnlyList<string> NextStatusLabels);

public sealed record CreateCharacterRequest(
    Guid? SystemId,
    string? SystemCode,
    string Name,
    string? Occupation,
    string? ClassName,
    string? RaceName,
    int? Age,
    string? Summary,
    string? PortraitUrl,
    CharacterSheetDto? Sheet,
    bool? IsFavorite,
    string? Status);

public sealed record UpdateCharacterRequest(
    string? Name,
    string? Occupation,
    string? ClassName,
    string? RaceName,
    int? Age,
    string? Summary,
    string? PortraitUrl,
    CharacterSheetDto? Sheet,
    bool? IsFavorite);

public sealed record ChangeStatusRequest(string Status, string? Note);

public sealed record AddNoteRequest(string Note);

public sealed record CharacterStatsDto(
    int Total,
    int Draft,
    int Ready,
    int InUse,
    int Retained,
    int Archived,
    int CreatedLast30Days);
