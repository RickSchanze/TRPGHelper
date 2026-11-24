using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using TRPGHelper.Api.Contracts;
using TRPGHelper.Domain.Entities;
using TRPGHelper.Infrastructure.Data;

namespace TRPGHelper.Api.Services;

/// <summary>
/// 生成器素材的读取入口：全部数据来自数据库（GeneratorEntries），
/// 管理台改完立即可见，代码里不再有硬编码素材。
/// </summary>
public sealed class GeneratorCatalogService(AppDbContext db)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private static readonly Regex TokenRegex = new(@"\{([a-zA-Z][a-zA-Z0-9_-]*)\}", RegexOptions.Compiled);

    private const string DefaultNameTemplate = "{surname}{given}";

    public async Task<GameSystem?> ResolveSystemAsync(string? systemCode, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(systemCode))
        {
            var code = systemCode.Trim().ToLowerInvariant();
            return await db.GameSystems
                .AsNoTracking()
                .FirstOrDefaultAsync(system => system.Code == code, cancellationToken);
        }

        return await db.GameSystems
            .AsNoTracking()
            .OrderBy(system => system.SortOrder)
            .ThenBy(system => system.Name)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> NamesAsync(
        string? systemCode,
        int count,
        Random? random,
        CancellationToken cancellationToken)
    {
        var system = await ResolveSystemAsync(systemCode, cancellationToken);
        if (system is null)
        {
            return [];
        }

        count = Math.Clamp(count, 1, 20);
        random ??= Random.Shared;

        var entries = await EntriesAsync(system.Code, [GeneratorKind.NamePart], cancellationToken);
        var pools = entries
            .GroupBy(entry => entry.Category, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.Select(entry => entry.Value).Where(value => !string.IsNullOrWhiteSpace(value)).ToList(),
                StringComparer.OrdinalIgnoreCase);

        var template = string.IsNullOrWhiteSpace(system.NameTemplate) ? DefaultNameTemplate : system.NameTemplate;
        var results = new List<string>(count);

        for (var attempt = 0; attempt < count * 40 && results.Count < count; attempt++)
        {
            var missing = false;
            var name = TokenRegex.Replace(template, match =>
            {
                if (!pools.TryGetValue(match.Groups[1].Value, out var pool) || pool.Count == 0)
                {
                    missing = true;
                    return string.Empty;
                }

                return pool[random.Next(pool.Count)];
            });

            if (missing || string.IsNullOrWhiteSpace(name))
            {
                break;
            }

            if (!results.Contains(name))
            {
                results.Add(name);
            }
        }

        return results;
    }

    public async Task<IReadOnlyList<string>> OccupationsAsync(
        string? systemCode,
        int count,
        Random? random,
        CancellationToken cancellationToken)
    {
        var system = await ResolveSystemAsync(systemCode, cancellationToken);
        if (system is null)
        {
            return [];
        }

        count = Math.Clamp(count, 1, 20);
        random ??= Random.Shared;

        var entries = await EntriesAsync(system.Code, [GeneratorKind.Occupation, GeneratorKind.Background], cancellationToken);
        return entries
            .Select(entry => entry.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(_ => random.Next())
            .Take(count)
            .ToList();
    }

    public async Task<IReadOnlyList<ClassOptionDto>> ClassesAsync(string? systemCode, CancellationToken cancellationToken)
    {
        var system = await ResolveSystemAsync(systemCode, cancellationToken);
        if (system is null)
        {
            return [];
        }

        var entries = await EntriesAsync(system.Code, [GeneratorKind.Class], cancellationToken);
        return entries.Select(ToClassOption).ToList();
    }

    public async Task<IReadOnlyList<RaceOptionDto>> RacesAsync(string? systemCode, CancellationToken cancellationToken)
    {
        var system = await ResolveSystemAsync(systemCode, cancellationToken);
        if (system is null)
        {
            return [];
        }

        var entries = await EntriesAsync(system.Code, [GeneratorKind.Race], cancellationToken);
        return entries.Select(ToRaceOption).ToList();
    }

    public async Task<IReadOnlyList<InspirationDto>> InspirationsAsync(
        string? systemCode,
        int count,
        Random? random,
        CancellationToken cancellationToken)
    {
        var system = await ResolveSystemAsync(systemCode, cancellationToken);
        if (system is null)
        {
            return [];
        }

        count = Math.Clamp(count, 1, 10);
        random ??= Random.Shared;

        var entries = await EntriesAsync(system.Code, [GeneratorKind.Inspiration], cancellationToken);
        return entries
            .OrderBy(_ => random.Next())
            .Take(count)
            .Select(entry => new InspirationDto(entry.Value, ReadString(ParseExtra(entry.ExtraJson), "text") ?? string.Empty))
            .ToList();
    }

    private Task<List<GeneratorEntry>> EntriesAsync(
        string systemCode,
        GeneratorKind[] kinds,
        CancellationToken cancellationToken)
        => db.GeneratorEntries
            .AsNoTracking()
            .Where(entry => entry.IsEnabled
                && (entry.SystemCode == null || entry.SystemCode == systemCode)
                && kinds.Contains(entry.Kind))
            .OrderBy(entry => entry.SortOrder)
            .ThenBy(entry => entry.Value)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<SubclassOptionDto>> SubclassesAsync(
        string? systemCode,
        string? classId,
        CancellationToken cancellationToken)
    {
        var system = await ResolveSystemAsync(systemCode, cancellationToken);
        if (system is null)
        {
            return [];
        }

        var entries = await EntriesAsync(system.Code, [GeneratorKind.Subclass], cancellationToken);
        var code = classId?.Trim().ToLowerInvariant();
        if (!string.IsNullOrEmpty(code))
        {
            entries = entries.Where(entry => entry.Category == code).ToList();
        }

        return entries.Select(ToSubclassOption).ToList();
    }

    internal static SubclassOptionDto ToSubclassOption(GeneratorEntry entry)
    {
        var extra = ParseExtra(entry.ExtraJson);
        return new SubclassOptionDto(
            ReadString(extra, "code") ?? entry.Value,
            entry.Value,
            entry.Category,
            ReadString(extra, "description") ?? string.Empty,
            ReadStringList(extra, "features"),
            ReadString(extra, "imageUrl"),
            ReadString(extra, "accent"));
    }

    internal static ClassOptionDto ToClassOption(GeneratorEntry entry)
    {
        var extra = ParseExtra(entry.ExtraJson);
        return new ClassOptionDto(
            entry.Category,
            entry.Value,
            ReadString(extra, "description") ?? string.Empty,
            ReadString(extra, "hitDie") ?? string.Empty,
            ReadString(extra, "primaryAbility") ?? string.Empty,
            ReadStringList(extra, "features"),
            ReadString(extra, "imageUrl"),
            ReadString(extra, "accent"));
    }

    internal static RaceOptionDto ToRaceOption(GeneratorEntry entry)
    {
        var extra = ParseExtra(entry.ExtraJson);
        return new RaceOptionDto(
            entry.Category,
            entry.Value,
            ReadString(extra, "description") ?? string.Empty,
            ReadString(extra, "abilityBonus") ?? string.Empty,
            ReadInt(extra, "speed") ?? 30);
    }

    internal static JsonElement? ParseExtra(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.Clone();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    internal static string? ReadString(JsonElement? element, string property)
        => element is { ValueKind: JsonValueKind.Object } value
            && value.TryGetProperty(property, out var propertyValue)
            && propertyValue.ValueKind == JsonValueKind.String
                ? propertyValue.GetString()
                : null;

    internal static int? ReadInt(JsonElement? element, string property)
        => element is { ValueKind: JsonValueKind.Object } value
            && value.TryGetProperty(property, out var propertyValue)
            && propertyValue.ValueKind == JsonValueKind.Number
            && propertyValue.TryGetInt32(out var number)
                ? number
                : null;

    internal static IReadOnlyList<string> ReadStringList(JsonElement? element, string property)
        => element is { ValueKind: JsonValueKind.Object } value
            && value.TryGetProperty(property, out var propertyValue)
            && propertyValue.ValueKind == JsonValueKind.Array
                ? propertyValue.EnumerateArray()
                    .Where(item => item.ValueKind == JsonValueKind.String)
                    .Select(item => item.GetString())
                    .Where(text => !string.IsNullOrWhiteSpace(text))
                    .Select(text => text!)
                    .ToList()
                : [];

    internal static string SerializeExtra(JsonElement? element)
        => element is null ? "{}" : element.Value.GetRawText();

    internal static JsonSerializerOptions SerializerOptions => JsonOptions;
}
