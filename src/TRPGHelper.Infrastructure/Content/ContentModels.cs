namespace TRPGHelper.Infrastructure.Content;

/// <summary>
/// `data/*.xlsx` 里的人类可读内容定义。表头就是这些字段名，
/// 跨表引用一律用 id（systems.id / classes.id / keywords.key 等）。
/// </summary>
public sealed record ContentAttribute(string SystemId, string Key, string Label, string Formula);

public sealed record ContentSystem(
    string Id,
    string Name,
    string? Description,
    string NameTemplate,
    string? IconUrl = null,
    string? Accent = null,
    bool Available = true);

public sealed record ContentClass(
    string Id,
    string SystemId,
    string Name,
    string Description,
    string HitDie,
    string PrimaryAbility,
    string? ImageUrl,
    IReadOnlyList<string> Features,
    string? Accent = null);

public sealed record ContentRace(
    string Id,
    string SystemId,
    string Name,
    string Description,
    string AbilityBonus,
    int Speed);

public sealed record ContentInspiration(
    string? SystemId,
    string Title,
    string Text);

public sealed record ContentCreationStep(
    string? SystemId,
    string Kind,
    string Title,
    string? Description,
    bool IsEnabled = true);

public sealed record ContentKeywordCategory(
    string Code,
    string Name);

public sealed record ContentKeyword(
    string Code,
    string Key,
    string Category,
    string? Title,
    string Content,
    string? ImageUrl,
    bool IsEnabled = true);

public sealed record ContentLevelFeature(
    string ClassId,
    int Level,
    string Title,
    string Description,
    bool IsEnabled = true,
    int SortOrder = 0);

/// <summary>把文件里分散的素材摊平成统一的写入单元。</summary>
public sealed record GeneratorEntryContent(
    string? SystemId,
    string Kind,
    string Category,
    string Value,
    string? ExtraJson,
    int SortOrder,
    bool IsEnabled = true);

public sealed class ContentBundle
{
    public required IReadOnlyList<ContentSystem> Systems { get; init; }

    public required IReadOnlyList<ContentAttribute> Attributes { get; init; }

    public required IReadOnlyList<GeneratorEntryContent> GeneratorEntries { get; init; }

    public required IReadOnlyList<ContentCreationStep> CreationSteps { get; init; }

    public required IReadOnlyList<ContentKeywordCategory> KeywordCategories { get; init; }

    public required IReadOnlyList<ContentKeyword> Keywords { get; init; }

    public required IReadOnlyList<ContentLevelFeature> ClassLevels { get; init; }
}

/// <summary>内容文件读不出来 / 写错了时抛这个，启动阶段直接把原因打出来。</summary>
public sealed class ContentException(string message) : Exception(message);
