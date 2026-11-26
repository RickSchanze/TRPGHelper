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

    public async Task<IReadOnlyList<SubraceOptionDto>> SubracesAsync(
        string? systemCode,
        string? raceId,
        CancellationToken cancellationToken)
    {
        var system = await ResolveSystemAsync(systemCode, cancellationToken);
        if (system is null)
        {
            return [];
        }

        var entries = await EntriesAsync(system.Code, [GeneratorKind.Subrace], cancellationToken);
        var code = raceId?.Trim().ToLowerInvariant();
        if (!string.IsNullOrEmpty(code))
        {
            entries = entries.Where(entry => entry.Category == code).ToList();
        }

        return entries.Select(ToSubraceOption).ToList();
    }

    internal static SubraceOptionDto ToSubraceOption(GeneratorEntry entry)
    {
        var extra = ParseExtra(entry.ExtraJson);
        return new SubraceOptionDto(
            ReadString(extra, "code") ?? entry.Value,
            entry.Value,
            entry.Category,
            ReadString(extra, "description") ?? string.Empty,
            ReadStringList(extra, "traits"),
            ReadAbilityBonuses(extra),
            ReadAbilityChoices(extra));
    }

    public async Task<IReadOnlyList<AlignmentOptionDto>> AlignmentsAsync(
        string? systemCode,
        CancellationToken cancellationToken)
    {
        var system = await ResolveSystemAsync(systemCode, cancellationToken);
        if (system is null)
        {
            return [];
        }

        var entries = await EntriesAsync(system.Code, [GeneratorKind.Alignment], cancellationToken);
        return entries.Select(ToAlignmentOption).ToList();
    }

    internal static AlignmentOptionDto ToAlignmentOption(GeneratorEntry entry)
    {
        var extra = ParseExtra(entry.ExtraJson);
        return new AlignmentOptionDto(
            ReadString(extra, "code") ?? entry.Category,
            entry.Value,
            ReadString(extra, "iconUrl"),
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
            ReadString(extra, "savingThrows") ?? string.Empty,
            ReadString(extra, "spellcastingAbility") ?? string.Empty,
            ReadStringList(extra, "features"),
            ReadString(extra, "imageUrl"),
            ReadString(extra, "accent"),
            ReadInt(extra, "skillChoiceCount") ?? 0,
            ReadStringList(extra, "skillOptions"),
            ReadStringList(extra, "armorProficiencies"),
            ReadStringList(extra, "weaponProficiencies"));
    }

    public async Task<IReadOnlyList<SkillOptionDto>> SkillsAsync(string? systemCode, CancellationToken cancellationToken)
    {
        var system = await ResolveSystemAsync(systemCode, cancellationToken);
        if (system is null)
        {
            return [];
        }

        var entries = await EntriesAsync(system.Code, [GeneratorKind.Skill], cancellationToken);
        return entries.Select(entry => new SkillOptionDto(
            entry.Category,
            entry.Value,
            ReadString(ParseExtra(entry.ExtraJson), "ability") ?? string.Empty,
            ReadString(ParseExtra(entry.ExtraJson), "description") ?? string.Empty)).ToList();
    }

    public async Task<IReadOnlyList<LanguageOptionDto>> LanguagesAsync(string? systemCode, CancellationToken cancellationToken)
    {
        var system = await ResolveSystemAsync(systemCode, cancellationToken);
        if (system is null)
        {
            return [];
        }

        var entries = await EntriesAsync(system.Code, [GeneratorKind.Language], cancellationToken);
        return entries.Select(entry =>
        {
            var extra = ParseExtra(entry.ExtraJson);
            return new LanguageOptionDto(
                entry.Category,
                entry.Value,
                ReadString(extra, "kind") ?? string.Empty,
                ReadString(extra, "description") ?? string.Empty);
        }).ToList();
    }

    public async Task<IReadOnlyList<BackgroundDetailDto>> BackgroundsAsync(string? systemCode, CancellationToken cancellationToken)
    {
        var system = await ResolveSystemAsync(systemCode, cancellationToken);
        if (system is null)
        {
            return [];
        }

        var entries = await EntriesAsync(system.Code, [GeneratorKind.BackgroundDetail], cancellationToken);
        return entries.Select(entry =>
        {
            var extra = ParseExtra(entry.ExtraJson);
            return new BackgroundDetailDto(
                entry.Category,
                entry.Value,
                ReadString(extra, "description") ?? string.Empty,
                ReadStringList(extra, "skills"),
                ReadString(extra, "tools") ?? string.Empty,
                ReadInt(extra, "languageCount") ?? 0,
                ReadString(extra, "equipment") ?? string.Empty,
                ReadString(extra, "featureName") ?? string.Empty,
                ReadString(extra, "featureText") ?? string.Empty);
        }).ToList();
    }

    public async Task<IReadOnlyList<FeatureOptionDto>> ClassOptionsAsync(
        string? systemCode,
        string? classId,
        CancellationToken cancellationToken)
    {
        var system = await ResolveSystemAsync(systemCode, cancellationToken);
        if (system is null)
        {
            return [];
        }

        var entries = await EntriesAsync(system.Code, [GeneratorKind.ClassOption], cancellationToken);
        if (!string.IsNullOrWhiteSpace(classId))
        {
            var code = classId.Trim().ToLowerInvariant();
            entries = entries.Where(entry => entry.Category == code).ToList();
        }

        return entries.Select(entry =>
        {
            var extra = ParseExtra(entry.ExtraJson);
            return new FeatureOptionDto(
                entry.Category,
                ReadString(extra, "optionType") ?? string.Empty,
                entry.Value,
                ReadString(extra, "description") ?? string.Empty);
        }).ToList();
    }

    public async Task<EquipmentResponse> EquipmentAsync(
        string? systemCode,
        string? classId,
        CancellationToken cancellationToken)
    {
        var system = await ResolveSystemAsync(systemCode, cancellationToken);
        if (system is null)
        {
            return new EquipmentResponse([], [], null);
        }

        var entries = await EntriesAsync(
            system.Code,
            [GeneratorKind.Armor, GeneratorKind.Weapon, GeneratorKind.EquipmentKit],
            cancellationToken);

        var armor = entries
            .Where(entry => entry.Kind == GeneratorKind.Armor)
            .Select(entry =>
            {
                var extra = ParseExtra(entry.ExtraJson);
                return new ArmorOptionDto(
                    entry.Category,
                    entry.Value,
                    ReadString(extra, "type") ?? string.Empty,
                    ReadInt(extra, "baseAc") ?? 0,
                    ReadInt(extra, "dexCap"),
                    ReadInt(extra, "strengthReq"),
                    ReadBool(extra, "stealthDisadvantage") ?? false,
                    ReadString(extra, "description") ?? string.Empty);
            })
            .ToList();

        var weapons = entries
            .Where(entry => entry.Kind == GeneratorKind.Weapon)
            .Select(entry =>
            {
                var extra = ParseExtra(entry.ExtraJson);
                return new WeaponOptionDto(
                    entry.Category,
                    entry.Value,
                    ReadString(extra, "damage") ?? string.Empty,
                    ReadString(extra, "damageType") ?? string.Empty,
                    ReadString(extra, "ability") ?? string.Empty,
                    ReadBool(extra, "ranged") ?? false,
                    ReadString(extra, "properties") ?? string.Empty);
            })
            .ToList();

        var code = classId?.Trim().ToLowerInvariant();
        var kitEntry = entries
            .Where(entry => entry.Kind == GeneratorKind.EquipmentKit)
            .FirstOrDefault(entry => code is not null && entry.Category == code);

        EquipmentKitDto? kit = null;
        if (kitEntry is not null)
        {
            var extra = ParseExtra(kitEntry.ExtraJson);
            kit = new EquipmentKitDto(
                kitEntry.Category,
                ReadString(extra, "armorId") ?? string.Empty,
                ReadBool(extra, "shield") ?? false,
                ReadStringList(extra, "weaponIds"),
                ReadString(extra, "defaultWeaponId") ?? string.Empty,
                ReadString(extra, "pack") ?? string.Empty);
        }

        return new EquipmentResponse(armor, weapons, kit);
    }

    internal static bool? ReadBool(JsonElement? element, string property)
        => element is { ValueKind: JsonValueKind.Object } value
            && value.TryGetProperty(property, out var propertyValue)
            && propertyValue.ValueKind is JsonValueKind.True or JsonValueKind.False
                ? propertyValue.GetBoolean()
                : null;

    internal static RaceOptionDto ToRaceOption(GeneratorEntry entry)
    {
        var extra = ParseExtra(entry.ExtraJson);
        return new RaceOptionDto(
            entry.Category,
            entry.Value,
            ReadString(extra, "description") ?? string.Empty,
            ReadString(extra, "abilityBonus") ?? string.Empty,
            ReadInt(extra, "speed") ?? 30,
            ReadStringList(extra, "traits"),
            ReadAbilityBonuses(extra),
            ReadAbilityChoices(extra),
            ReadStringList(extra, "skills"),
            ReadInt(extra, "skillChoiceCount") ?? 0,
            ReadStringList(extra, "languages"),
            ReadInt(extra, "languageChoiceCount") ?? 0);
    }

    internal static IReadOnlyList<AbilityBonusDto> ReadAbilityBonuses(JsonElement? extra)
        => ObjectItems(extra, "abilityBonuses")
            .Select(item => new AbilityBonusDto(
                ReadString(item, "key") ?? string.Empty,
                ReadString(item, "label") ?? string.Empty,
                ReadInt(item, "value") ?? 0))
            .Where(bonus => bonus.Key.Length > 0 && bonus.Value != 0)
            .ToList();

    internal static IReadOnlyList<AbilityChoiceDto> ReadAbilityChoices(JsonElement? extra)
        => ObjectItems(extra, "abilityChoices")
            .Select(item => new AbilityChoiceDto(
                ReadString(item, "id") ?? "choice",
                ReadString(item, "label") ?? "任选",
                ReadInt(item, "count") ?? 0,
                ReadInt(item, "value") ?? 0))
            .Where(choice => choice.Count > 0 && choice.Value != 0)
            .ToList();

    private static IEnumerable<JsonElement> ObjectItems(JsonElement? element, string property)
        => element is { ValueKind: JsonValueKind.Object } value
            && value.TryGetProperty(property, out var propertyValue)
            && propertyValue.ValueKind == JsonValueKind.Array
                ? propertyValue.EnumerateArray().Where(item => item.ValueKind == JsonValueKind.Object)
                : Enumerable.Empty<JsonElement>();

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
