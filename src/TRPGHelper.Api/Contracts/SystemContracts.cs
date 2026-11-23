namespace TRPGHelper.Api.Contracts;

public sealed record AttributeTemplateDto(string Key, string Label, string Formula);

public sealed record GameSystemDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    IReadOnlyList<AttributeTemplateDto> Attributes);
