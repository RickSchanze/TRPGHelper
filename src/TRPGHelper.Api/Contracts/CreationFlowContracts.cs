namespace TRPGHelper.Api.Contracts;

public sealed record CreationStepDto(string Id, string Kind, string Title, string? Description);

public sealed record CreationFlowDto(
    string? SystemCode,
    string? SystemName,
    IReadOnlyList<CreationStepDto> Steps);

public sealed record AdminCreationStepDto(
    Guid Id,
    string? SystemCode,
    string Kind,
    string Title,
    string? Description,
    int SortOrder,
    bool IsEnabled,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record SaveCreationStepRequest(
    string? SystemCode,
    string? Kind,
    string? Title,
    string? Description,
    int SortOrder,
    bool IsEnabled);

public sealed record UploadAssetResponse(string Url);
