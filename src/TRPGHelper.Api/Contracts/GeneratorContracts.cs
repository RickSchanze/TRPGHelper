namespace TRPGHelper.Api.Contracts;

public sealed record RollRequest(string Expression);

public sealed record RollTermDto(string Text, IReadOnlyList<int> Rolls, IReadOnlyList<int> KeptRolls, int Subtotal);

public sealed record RollResponse(string Expression, int Total, string Detail, IReadOnlyList<RollTermDto> Terms);

public sealed record GenerateAttributesRequest(string? SystemCode, Guid? SystemId, string? Method);

public sealed record GeneratedAttributeDto(string Key, string Label, int Value, string? Formula);

public sealed record GeneratedAttributesResponse(
    Guid SystemId,
    string SystemCode,
    string SystemName,
    string Method,
    IReadOnlyList<GeneratedAttributeDto> Attributes,
    int Total);

public sealed record InspirationDto(string Title, string Text);

public sealed record ClassOptionDto(
    string Code,
    string Name,
    string Description,
    string HitDie,
    string PrimaryAbility,
    string SavingThrows,
    string SpellcastingAbility,
    IReadOnlyList<string> Features,
    string? ImageUrl,
    string? Accent);

public sealed record RaceOptionDto(
    string Code,
    string Name,
    string Description,
    string AbilityBonus,
    int Speed,
    IReadOnlyList<string> Traits);

public sealed record SubclassOptionDto(
    string Code,
    string Name,
    string ClassId,
    string Description,
    IReadOnlyList<string> Features,
    string? ImageUrl,
    string? Accent);

public sealed record SubraceOptionDto(
    string Code,
    string Name,
    string RaceId,
    string Description,
    IReadOnlyList<string> Traits);
