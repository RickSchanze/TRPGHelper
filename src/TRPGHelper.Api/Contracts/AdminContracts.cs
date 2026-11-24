using System.Text.Json;

namespace TRPGHelper.Api.Contracts;

public sealed record AdminSystemDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    string NameTemplate,
    bool IsBuiltin,
    int SortOrder,
    IReadOnlyList<AttributeTemplateDto> Attributes);

public sealed record SaveSystemRequest(
    string? Code,
    string? Name,
    string? Description,
    string? NameTemplate,
    int SortOrder,
    IReadOnlyList<AttributeTemplateDto>? Attributes);

public sealed record AdminGeneratorEntryDto(
    Guid Id,
    string? SystemCode,
    string Kind,
    string Category,
    string Value,
    JsonElement? Extra,
    int SortOrder,
    bool IsEnabled,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record SaveGeneratorEntryRequest(
    string? SystemCode,
    string? Kind,
    string? Category,
    string? Value,
    JsonElement? Extra,
    int SortOrder,
    bool IsEnabled);

public sealed record AdminOverviewDto(
    int Systems,
    int GeneratorEntries,
    int EnabledGeneratorEntries,
    int Users,
    int Admins,
    int Cards,
    IReadOnlyList<AdminKindCountDto> EntriesByKind,
    DateTimeOffset GeneratedAt);

public sealed record AdminKindCountDto(string Kind, int Count);
