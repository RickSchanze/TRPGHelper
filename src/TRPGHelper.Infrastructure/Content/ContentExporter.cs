using System.Text.Json;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using TRPGHelper.Domain.Cards;
using TRPGHelper.Domain.Entities;
using TRPGHelper.Infrastructure.Data;

namespace TRPGHelper.Infrastructure.Content;

/// <summary>
/// 把数据库里的内容导出成 <c>data/*.xlsx</c>（中文文件名，人类可读，可直接用 Excel 编辑）。
/// 关键词会按分类拆成「关键词.分类.xlsx」多个文件。
/// </summary>
public static class ContentExporter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static async Task<IReadOnlyList<string>> ExportAsync(
        AppDbContext db,
        string dataPath,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(dataPath);
        var written = new List<string>();

        // ---------- 规则系统 + 属性模板 ----------
        var systems = await db.GameSystems
            .AsNoTracking()
            .OrderBy(system => system.SortOrder)
            .ThenBy(system => system.Name)
            .ToListAsync(cancellationToken);

        var systemRows = systems
            .Select(system => new object?[]
            {
                system.Code,
                system.Name,
                system.Description,
                system.NameTemplate,
                system.IconUrl,
                system.Accent,
                system.IsAvailable,
            })
            .ToList();

        var attributeRows = systems
            .SelectMany(system => AttributeTemplateParser.Parse(system.AttributeTemplateJson)
                .Select(template => new object?[] { system.Code, template.Key, template.Label, template.Formula }))
            .ToList();

        using (var workbook = new XLWorkbook())
        {
            XlsxHelpers.WriteSheet(
                workbook,
                "systems",
                ["id", "name", "description", "nameTemplate", "iconUrl", "accent", "available"],
                systemRows,
                new Dictionary<string, double>
                {
                    ["id"] = 12,
                    ["name"] = 24,
                    ["description"] = 60,
                    ["nameTemplate"] = 24,
                    ["iconUrl"] = 34,
                    ["accent"] = 12,
                    ["available"] = 10,
                },
                new HashSet<string> { "description" });
            XlsxHelpers.WriteSheet(
                workbook,
                "attributes",
                ["systemId", "key", "label", "formula"],
                attributeRows,
                new Dictionary<string, double> { ["systemId"] = 12, ["key"] = 10, ["label"] = 12, ["formula"] = 16 });

            written.Add(Save(workbook, Path.Combine(dataPath, ContentPaths.SystemsFile)));
        }

        // ---------- 生成器素材 ----------
        var entries = await db.GeneratorEntries
            .AsNoTracking()
            .OrderBy(entry => entry.SystemCode)
            .ThenBy(entry => entry.SortOrder)
            .ThenBy(entry => entry.Value)
            .ToListAsync(cancellationToken);

        var nameRows = entries
            .Where(entry => entry.Kind == GeneratorKind.NamePart)
            .Select(entry => new object?[] { entry.SystemCode ?? "global", entry.Category, entry.Value })
            .ToList();

        var occupationRows = entries
            .Where(entry => entry.Kind == GeneratorKind.Occupation)
            .Select(entry => new object?[] { entry.SystemCode ?? "global", entry.Value })
            .ToList();

        var backgroundRows = entries
            .Where(entry => entry.Kind == GeneratorKind.Background)
            .Select(entry => new object?[] { entry.SystemCode ?? "global", entry.Value })
            .ToList();

        var classRows = entries
            .Where(entry => entry.Kind == GeneratorKind.Class)
            .Select(entry =>
            {
                var extra = ParseExtra(entry.ExtraJson);
                return new object?[]
                {
                    entry.Category,
                    entry.SystemCode ?? "dnd5e",
                    entry.Value,
                    ReadString(extra, "description") ?? string.Empty,
                    ReadString(extra, "hitDie") ?? string.Empty,
                    ReadString(extra, "primaryAbility") ?? string.Empty,
                    ReadString(extra, "imageUrl") ?? string.Empty,
                    string.Join('\n', ReadStringList(extra, "features")),
                    ReadString(extra, "accent") ?? string.Empty,
                    ReadString(extra, "savingThrows") ?? string.Empty,
                    ReadString(extra, "spellcastingAbility") ?? string.Empty,
                    ReadInt(extra, "skillChoiceCount") ?? 0,
                    string.Join(',', ReadStringList(extra, "skillOptions")),
                    string.Join(',', ReadStringList(extra, "armorProficiencies")),
                    string.Join(',', ReadStringList(extra, "weaponProficiencies")),
                };
            })
            .ToList();

        var subclassRows = entries
            .Where(entry => entry.Kind == GeneratorKind.Subclass)
            .Select(entry =>
            {
                var extra = ParseExtra(entry.ExtraJson);
                return new object?[]
                {
                    entry.SystemCode ?? "dnd5e",
                    ReadString(extra, "code") ?? entry.Category,
                    entry.Category,
                    entry.Value,
                    ReadString(extra, "description") ?? string.Empty,
                    ReadString(extra, "imageUrl") ?? string.Empty,
                    ReadString(extra, "accent") ?? string.Empty,
                    string.Join('\n', ReadStringList(extra, "features")),
                };
            })
            .ToList();

        var raceRows = entries
            .Where(entry => entry.Kind == GeneratorKind.Race)
            .Select(entry =>
            {
                var extra = ParseExtra(entry.ExtraJson);
                return new object?[]
                {
                    entry.Category,
                    entry.SystemCode ?? "dnd5e",
                    entry.Value,
                    ReadString(extra, "description") ?? string.Empty,
                    ReadString(extra, "abilityBonus") ?? string.Empty,
                    ReadInt(extra, "speed") ?? 30,
                    string.Join('\n', ReadStringList(extra, "traits")),
                    BuildAbilityBonusesText(extra),
                    string.Join(',', ReadStringList(extra, "skills")),
                    ReadInt(extra, "skillChoiceCount") ?? 0,
                    string.Join(',', ReadStringList(extra, "languages")),
                    ReadInt(extra, "languageChoiceCount") ?? 0,
                };
            })
            .ToList();

        var subraceRows = entries
            .Where(entry => entry.Kind == GeneratorKind.Subrace)
            .Select(entry =>
            {
                var extra = ParseExtra(entry.ExtraJson);
                return new object?[]
                {
                    entry.SystemCode ?? "dnd5e",
                    ReadString(extra, "code") ?? entry.Category,
                    entry.Category,
                    entry.Value,
                    ReadString(extra, "description") ?? string.Empty,
                    string.Join('\n', ReadStringList(extra, "traits")),
                    BuildAbilityBonusesText(extra),
                };
            })
            .ToList();

        var alignmentRows = entries
            .Where(entry => entry.Kind == GeneratorKind.Alignment)
            .Select(entry =>
            {
                var extra = ParseExtra(entry.ExtraJson);
                return new object?[]
                {
                    entry.SystemCode ?? "dnd5e",
                    ReadString(extra, "code") ?? entry.Category,
                    entry.Value,
                    ReadString(extra, "iconUrl") ?? string.Empty,
                    ReadString(extra, "accent") ?? string.Empty,
                };
            })
            .ToList();

        var skillRows = entries
            .Where(entry => entry.Kind == GeneratorKind.Skill)
            .Select(entry =>
            {
                var extra = ParseExtra(entry.ExtraJson);
                return new object?[]
                {
                    entry.Category,
                    entry.SystemCode ?? "dnd5e",
                    entry.Value,
                    ReadString(extra, "ability") ?? string.Empty,
                    ReadString(extra, "description") ?? string.Empty,
                };
            })
            .ToList();

        var languageRows = entries
            .Where(entry => entry.Kind == GeneratorKind.Language)
            .Select(entry =>
            {
                var extra = ParseExtra(entry.ExtraJson);
                return new object?[]
                {
                    entry.Category,
                    entry.SystemCode ?? "dnd5e",
                    entry.Value,
                    ReadString(extra, "kind") ?? string.Empty,
                    ReadString(extra, "description") ?? string.Empty,
                };
            })
            .ToList();

        var backgroundDetailRows = entries
            .Where(entry => entry.Kind == GeneratorKind.BackgroundDetail)
            .Select(entry =>
            {
                var extra = ParseExtra(entry.ExtraJson);
                return new object?[]
                {
                    entry.Category,
                    entry.SystemCode ?? "dnd5e",
                    entry.Value,
                    ReadString(extra, "description") ?? string.Empty,
                    string.Join(',', ReadStringList(extra, "skills")),
                    ReadString(extra, "tools") ?? string.Empty,
                    ReadInt(extra, "languageCount") ?? 0,
                    ReadString(extra, "equipment") ?? string.Empty,
                    ReadString(extra, "featureName") ?? string.Empty,
                    ReadString(extra, "featureText") ?? string.Empty,
                };
            })
            .ToList();

        var classOptionRows = entries
            .Where(entry => entry.Kind == GeneratorKind.ClassOption)
            .Select(entry =>
            {
                var extra = ParseExtra(entry.ExtraJson);
                return new object?[]
                {
                    entry.SystemCode ?? "dnd5e",
                    entry.Category,
                    ReadString(extra, "optionType") ?? string.Empty,
                    entry.Value,
                    ReadString(extra, "description") ?? string.Empty,
                };
            })
            .ToList();

        var armorRows = entries
            .Where(entry => entry.Kind == GeneratorKind.Armor)
            .Select(entry =>
            {
                var extra = ParseExtra(entry.ExtraJson);
                return new object?[]
                {
                    entry.Category,
                    entry.SystemCode ?? "dnd5e",
                    entry.Value,
                    ReadString(extra, "type") ?? string.Empty,
                    ReadInt(extra, "baseAc") ?? 0,
                    ReadInt(extra, "dexCap"),
                    ReadInt(extra, "strengthReq"),
                    ReadBool(extra, "stealthDisadvantage") ?? false,
                    ReadString(extra, "description") ?? string.Empty,
                };
            })
            .ToList();

        var weaponRows = entries
            .Where(entry => entry.Kind == GeneratorKind.Weapon)
            .Select(entry =>
            {
                var extra = ParseExtra(entry.ExtraJson);
                return new object?[]
                {
                    entry.Category,
                    entry.SystemCode ?? "dnd5e",
                    entry.Value,
                    ReadString(extra, "damage") ?? string.Empty,
                    ReadString(extra, "damageType") ?? string.Empty,
                    ReadString(extra, "ability") ?? string.Empty,
                    ReadBool(extra, "ranged") ?? false,
                    ReadString(extra, "properties") ?? string.Empty,
                };
            })
            .ToList();

        var kitRows = entries
            .Where(entry => entry.Kind == GeneratorKind.EquipmentKit)
            .Select(entry =>
            {
                var extra = ParseExtra(entry.ExtraJson);
                return new object?[]
                {
                    entry.Category,
                    entry.SystemCode ?? "dnd5e",
                    ReadString(extra, "armorId") ?? string.Empty,
                    ReadBool(extra, "shield") ?? false,
                    string.Join(',', ReadStringList(extra, "weaponIds")),
                    ReadString(extra, "defaultWeaponId") ?? string.Empty,
                    ReadString(extra, "pack") ?? string.Empty,
                };
            })
            .ToList();

        var inspirationRows = entries
            .Where(entry => entry.Kind == GeneratorKind.Inspiration)
            .Select(entry => new object?[]
            {
                entry.SystemCode ?? string.Empty,
                entry.Value,
                ReadString(ParseExtra(entry.ExtraJson), "text") ?? string.Empty,
            })
            .ToList();

        using (var workbook = new XLWorkbook())
        {
            XlsxHelpers.WriteSheet(workbook, "names", ["systemId", "category", "value"], nameRows,
                new Dictionary<string, double> { ["systemId"] = 12, ["category"] = 14, ["value"] = 24 });
            written.Add(Save(workbook, Path.Combine(dataPath, ContentPaths.NamesFile)));
        }

        using (var workbook = new XLWorkbook())
        {
            XlsxHelpers.WriteSheet(workbook, "classes",
                ["id", "systemId", "name", "description", "hitDie", "primaryAbility", "imageUrl", "features", "accent", "savingThrows", "spellcastingAbility", "skillChoiceCount", "skillOptions", "armorProficiencies", "weaponProficiencies"],
                classRows,
                new Dictionary<string, double>
                {
                    ["id"] = 12,
                    ["systemId"] = 12,
                    ["name"] = 12,
                    ["description"] = 56,
                    ["hitDie"] = 10,
                    ["primaryAbility"] = 18,
                    ["imageUrl"] = 30,
                    ["features"] = 64,
                    ["accent"] = 12,
                    ["savingThrows"] = 22,
                    ["spellcastingAbility"] = 22,
                    ["skillChoiceCount"] = 12,
                    ["skillOptions"] = 40,
                    ["armorProficiencies"] = 20,
                    ["weaponProficiencies"] = 22,
                },
                new HashSet<string> { "description", "features" });
            written.Add(Save(workbook, Path.Combine(dataPath, ContentPaths.ClassesFile)));
        }

        using (var workbook = new XLWorkbook())
        {
            XlsxHelpers.WriteSheet(workbook, "subclasses",
                ["systemId", "id", "classId", "name", "description", "imageUrl", "accent", "features"],
                subclassRows,
                new Dictionary<string, double>
                {
                    ["systemId"] = 12,
                    ["id"] = 14,
                    ["classId"] = 12,
                    ["name"] = 14,
                    ["description"] = 56,
                    ["imageUrl"] = 34,
                    ["accent"] = 12,
                    ["features"] = 64,
                },
                new HashSet<string> { "description", "features" });
            written.Add(Save(workbook, Path.Combine(dataPath, ContentPaths.SubclassesFile)));
        }

        using (var workbook = new XLWorkbook())
        {
            XlsxHelpers.WriteSheet(workbook, "races",
                ["id", "systemId", "name", "description", "abilityBonus", "speed", "traits", "abilityBonuses", "skills", "skillChoiceCount", "languages", "languageChoiceCount"],
                raceRows,
                new Dictionary<string, double>
                {
                    ["id"] = 12,
                    ["systemId"] = 12,
                    ["name"] = 12,
                    ["description"] = 48,
                    ["abilityBonus"] = 20,
                    ["speed"] = 8,
                    ["traits"] = 64,
                    ["abilityBonuses"] = 20,
                    ["skills"] = 16,
                    ["skillChoiceCount"] = 14,
                    ["languages"] = 18,
                    ["languageChoiceCount"] = 16,
                },
                new HashSet<string> { "description", "traits" });
            written.Add(Save(workbook, Path.Combine(dataPath, ContentPaths.RacesFile)));
        }

        using (var workbook = new XLWorkbook())
        {
            XlsxHelpers.WriteSheet(workbook, "subraces",
                ["systemId", "id", "raceId", "name", "description", "traits", "abilityBonuses"],
                subraceRows,
                new Dictionary<string, double>
                {
                    ["systemId"] = 12,
                    ["id"] = 14,
                    ["raceId"] = 12,
                    ["name"] = 16,
                    ["description"] = 56,
                    ["traits"] = 64,
                    ["abilityBonuses"] = 20,
                },
                new HashSet<string> { "description", "traits" });
            written.Add(Save(workbook, Path.Combine(dataPath, ContentPaths.SubracesFile)));
        }

        using (var workbook = new XLWorkbook())
        {
            XlsxHelpers.WriteSheet(workbook, "alignments",
                ["systemId", "id", "name", "iconUrl", "accent"],
                alignmentRows,
                new Dictionary<string, double>
                {
                    ["systemId"] = 12,
                    ["id"] = 18,
                    ["name"] = 14,
                    ["iconUrl"] = 34,
                    ["accent"] = 12,
                });
            written.Add(Save(workbook, Path.Combine(dataPath, ContentPaths.AlignmentsFile)));
        }

        using (var workbook = new XLWorkbook())
        {
            XlsxHelpers.WriteSheet(workbook, "skills",
                ["id", "systemId", "name", "ability", "description"],
                skillRows,
                new Dictionary<string, double> { ["id"] = 16, ["systemId"] = 10, ["name"] = 12, ["ability"] = 10, ["description"] = 44 },
                new HashSet<string> { "description" });
            written.Add(Save(workbook, Path.Combine(dataPath, ContentPaths.SkillsFile)));
        }

        using (var workbook = new XLWorkbook())
        {
            XlsxHelpers.WriteSheet(workbook, "languages",
                ["id", "systemId", "name", "kind", "description"],
                languageRows,
                new Dictionary<string, double> { ["id"] = 14, ["systemId"] = 10, ["name"] = 14, ["kind"] = 10, ["description"] = 30 });
            written.Add(Save(workbook, Path.Combine(dataPath, ContentPaths.LanguagesFile)));
        }

        using (var workbook = new XLWorkbook())
        {
            XlsxHelpers.WriteSheet(workbook, "backgrounds",
                ["id", "systemId", "name", "description", "skills", "tools", "languageCount", "equipment", "featureName", "featureText"],
                backgroundDetailRows,
                new Dictionary<string, double>
                {
                    ["id"] = 16,
                    ["systemId"] = 10,
                    ["name"] = 12,
                    ["description"] = 38,
                    ["skills"] = 16,
                    ["tools"] = 30,
                    ["languageCount"] = 12,
                    ["equipment"] = 56,
                    ["featureName"] = 14,
                    ["featureText"] = 50,
                },
                new HashSet<string> { "description", "equipment", "featureText" });
            written.Add(Save(workbook, Path.Combine(dataPath, ContentPaths.BackgroundDetailsFile)));
        }

        using (var workbook = new XLWorkbook())
        {
            XlsxHelpers.WriteSheet(workbook, "options",
                ["systemId", "classId", "optionType", "name", "description"],
                classOptionRows,
                new Dictionary<string, double> { ["systemId"] = 10, ["classId"] = 12, ["optionType"] = 16, ["name"] = 16, ["description"] = 50 },
                new HashSet<string> { "description" });
            written.Add(Save(workbook, Path.Combine(dataPath, ContentPaths.ClassOptionsFile)));
        }

        using (var workbook = new XLWorkbook())
        {
            XlsxHelpers.WriteSheet(workbook, "armor",
                ["id", "systemId", "name", "type", "baseAc", "dexCap", "strengthReq", "stealthDisadvantage", "description"],
                armorRows,
                new Dictionary<string, double>
                {
                    ["id"] = 14,
                    ["systemId"] = 10,
                    ["name"] = 12,
                    ["type"] = 10,
                    ["baseAc"] = 8,
                    ["dexCap"] = 8,
                    ["strengthReq"] = 10,
                    ["stealthDisadvantage"] = 16,
                    ["description"] = 34,
                });
            XlsxHelpers.WriteSheet(workbook, "weapons",
                ["id", "systemId", "name", "damage", "damageType", "ability", "ranged", "properties"],
                weaponRows,
                new Dictionary<string, double>
                {
                    ["id"] = 14,
                    ["systemId"] = 10,
                    ["name"] = 12,
                    ["damage"] = 8,
                    ["damageType"] = 10,
                    ["ability"] = 8,
                    ["ranged"] = 8,
                    ["properties"] = 36,
                });
            XlsxHelpers.WriteSheet(workbook, "kits",
                ["classId", "systemId", "armorId", "shield", "weaponIds", "defaultWeaponId", "pack"],
                kitRows,
                new Dictionary<string, double>
                {
                    ["classId"] = 12,
                    ["systemId"] = 10,
                    ["armorId"] = 12,
                    ["shield"] = 8,
                    ["weaponIds"] = 28,
                    ["defaultWeaponId"] = 16,
                    ["pack"] = 14,
                });
            written.Add(Save(workbook, Path.Combine(dataPath, ContentPaths.EquipmentFile)));
        }

        using (var workbook = new XLWorkbook())
        {
            XlsxHelpers.WriteSheet(workbook, "backgrounds", ["systemId", "value"], backgroundRows,
                new Dictionary<string, double> { ["systemId"] = 12, ["value"] = 24 });
            written.Add(Save(workbook, Path.Combine(dataPath, ContentPaths.BackgroundsFile)));
        }

        using (var workbook = new XLWorkbook())
        {
            XlsxHelpers.WriteSheet(workbook, "inspirations", ["systemId", "title", "text"], inspirationRows,
                new Dictionary<string, double> { ["systemId"] = 12, ["title"] = 24, ["text"] = 64 },
                new HashSet<string> { "text" });
            written.Add(Save(workbook, Path.Combine(dataPath, ContentPaths.InspirationsFile)));
        }

        using (var workbook = new XLWorkbook())
        {
            XlsxHelpers.WriteSheet(workbook, "occupations", ["systemId", "value"], occupationRows,
                new Dictionary<string, double> { ["systemId"] = 12, ["value"] = 24 });
            written.Add(Save(workbook, Path.Combine(dataPath, ContentPaths.OccupationsFile)));
        }

        // ---------- 车卡流程 ----------
        var steps = await db.CreationSteps
            .AsNoTracking()
            .OrderBy(step => step.SortOrder)
            .ThenBy(step => step.Title)
            .ToListAsync(cancellationToken);

        var stepRows = steps
            .Select(step => new object?[]
            {
                step.SystemCode ?? string.Empty,
                JsonKey(step.Kind),
                step.Title,
                step.Description ?? string.Empty,
                step.IsEnabled,
            })
            .ToList();

        using (var workbook = new XLWorkbook())
        {
            XlsxHelpers.WriteSheet(workbook, "steps", ["systemId", "kind", "title", "description", "isEnabled"], stepRows,
                new Dictionary<string, double> { ["systemId"] = 12, ["kind"] = 12, ["title"] = 18, ["description"] = 52, ["isEnabled"] = 9 },
                new HashSet<string> { "description" });
            written.Add(Save(workbook, Path.Combine(dataPath, ContentPaths.FlowFile)));
        }

        // ---------- 关键词分类编号 ----------
        var keywordCategories = await db.KeywordCategories
            .AsNoTracking()
            .OrderBy(category => category.SortOrder)
            .ThenBy(category => category.Code)
            .ToListAsync(cancellationToken);

        var categoryRows = keywordCategories
            .Select(category => new object?[] { category.Code, category.Name })
            .ToList();

        using (var workbook = new XLWorkbook())
        {
            XlsxHelpers.WriteSheet(workbook, "categories", ["id", "name"], categoryRows,
                new Dictionary<string, double> { ["id"] = 8, ["name"] = 16 });
            written.Add(Save(workbook, Path.Combine(dataPath, ContentPaths.KeywordCategoriesFile)));
        }

        // ---------- 关键词（按分类拆文件） ----------
        foreach (var existing in Directory.GetFiles(dataPath, $"{ContentPaths.KeywordFilePrefix}.*.xlsx"))
        {
            File.Delete(existing);
        }

        var keywords = await db.Keywords
            .AsNoTracking()
            .OrderBy(keyword => keyword.SortOrder)
            .ThenBy(keyword => keyword.Key)
            .ToListAsync(cancellationToken);

        foreach (var group in keywords.GroupBy(keyword => string.IsNullOrWhiteSpace(keyword.Category)
                     ? ContentPaths.Uncategorized
                     : keyword.Category.Trim()))
        {
            var rows = group
                .Select(keyword => new object?[]
                {
                    keyword.Code,
                    keyword.Key,
                    group.Key,
                    keyword.Title ?? string.Empty,
                    keyword.Summary,
                    keyword.Content,
                    keyword.ImageUrl ?? string.Empty,
                    keyword.IsEnabled,
                })
                .ToList();

            using var workbook = new XLWorkbook();
            XlsxHelpers.WriteSheet(workbook, "keywords",
                ["id", "key", "分类", "title", "summary", "content", "imageUrl", "isEnabled"],
                rows,
                new Dictionary<string, double>
                {
                    ["id"] = 10,
                    ["key"] = 14,
                    ["分类"] = 12,
                    ["title"] = 14,
                    ["summary"] = 40,
                    ["content"] = 70,
                    ["imageUrl"] = 28,
                    ["isEnabled"] = 9,
                },
                new HashSet<string> { "summary", "content" });

            written.Add(Save(workbook, Path.Combine(dataPath, $"{ContentPaths.KeywordFilePrefix}.{SanitizeFileName(group.Key)}.xlsx")));
        }

        // ---------- 职业等级成长 ----------
        var classNames = await db.GeneratorEntries
            .AsNoTracking()
            .Where(entry => entry.Kind == GeneratorKind.Class)
            .ToDictionaryAsync(entry => entry.Category, entry => entry.Value, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var levelFeatures = await db.ClassLevelFeatures
            .AsNoTracking()
            .OrderBy(feature => feature.ClassCode)
            .ThenBy(feature => feature.Level)
            .ThenBy(feature => feature.SortOrder)
            .ToListAsync(cancellationToken);

        var levelRows = levelFeatures
            .Select(feature => new object?[]
            {
                classNames.TryGetValue(feature.ClassCode, out var className) ? className : feature.ClassCode,
                feature.Level,
                feature.Title,
                feature.Description,
                feature.IsEnabled,
            })
            .ToList();

        using (var workbook = new XLWorkbook())
        {
            XlsxHelpers.WriteSheet(workbook, "levels", ["classId", "level", "title", "description", "isEnabled"], levelRows,
                new Dictionary<string, double> { ["classId"] = 12, ["level"] = 8, ["title"] = 22, ["description"] = 70, ["isEnabled"] = 9 },
                new HashSet<string> { "description" });
            written.Add(Save(workbook, Path.Combine(dataPath, ContentPaths.LevelsFile)));
        }

        return written;
    }

    private static string SanitizeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(value.Trim().Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray());
        return cleaned.Length == 0 ? ContentPaths.Uncategorized : cleaned;
    }

    private static string Save(XLWorkbook workbook, string path)
    {
        workbook.SaveAs(path);
        return path;
    }

    private static string JsonKey(Enum value)
        => JsonNamingPolicy.CamelCase.ConvertName(value.ToString());

    private static JsonElement? ParseExtra(string? json)
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

    private static string? ReadString(JsonElement? element, string property)
        => element is { ValueKind: JsonValueKind.Object } value
            && value.TryGetProperty(property, out var propertyValue)
            && propertyValue.ValueKind == JsonValueKind.String
                ? propertyValue.GetString()
                : null;

    private static bool? ReadBool(JsonElement? element, string property)
        => element is { ValueKind: JsonValueKind.Object } value
            && value.TryGetProperty(property, out var propertyValue)
            && propertyValue.ValueKind is JsonValueKind.True or JsonValueKind.False
                ? propertyValue.GetBoolean()
                : null;

    private static int? ReadInt(JsonElement? element, string property)
        => element is { ValueKind: JsonValueKind.Object } value
            && value.TryGetProperty(property, out var propertyValue)
            && propertyValue.ValueKind == JsonValueKind.Number
            && propertyValue.TryGetInt32(out var number)
                ? number
                : null;

    private static List<string> ReadStringList(JsonElement? element, string property)
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

    /// <summary>把种族 / 亚种的属性加值写回「敏捷+2，任选两项+1」这种人类可读文本。</summary>
    private static string BuildAbilityBonusesText(JsonElement? extra)
    {
        var parts = new List<string>();

        foreach (var item in ObjectItems(extra, "abilityBonuses"))
        {
            var label = ReadString(item, "label");
            var value = ReadInt(item, "value");
            if (!string.IsNullOrWhiteSpace(label) && value is not null)
            {
                parts.Add($"{label}{(value > 0 ? "+" : string.Empty)}{value}");
            }
        }

        foreach (var item in ObjectItems(extra, "abilityChoices"))
        {
            var label = ReadString(item, "label");
            if (!string.IsNullOrWhiteSpace(label))
            {
                parts.Add(label);
            }
        }

        return string.Join("，", parts);
    }

    private static IEnumerable<JsonElement> ObjectItems(JsonElement? element, string property)
        => element is { ValueKind: JsonValueKind.Object } value
            && value.TryGetProperty(property, out var propertyValue)
            && propertyValue.ValueKind == JsonValueKind.Array
                ? propertyValue.EnumerateArray().Where(item => item.ValueKind == JsonValueKind.Object)
                : Enumerable.Empty<JsonElement>();
}

