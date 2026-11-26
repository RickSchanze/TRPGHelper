using System.Text.Json;
using ClosedXML.Excel;
using TRPGHelper.Domain.Dice;
using TRPGHelper.Domain.Entities;

namespace TRPGHelper.Infrastructure.Content;

/// <summary>
/// 读取 <c>data/</c> 下的中文命名 xlsx 内容配置：
/// 规则系统.xlsx / 姓名片段.xlsx / 职业.xlsx / 种族.xlsx / 背景.xlsx / 职业灵感.xlsx /
/// 车卡流程.xlsx / 职业等级.xlsx / 关键词.&lt;分类&gt;.xlsx。
/// 跨表引用用 id；出错直接抛 <see cref="ContentException"/>（带文件名 / 工作表 / 行号）。
/// </summary>
public static class ContentLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static ContentBundle Load(string dataPath)
    {
        var systems = LoadSystems(dataPath, out var attributes);

        var entries = new List<GeneratorEntryContent>();
        LoadNames(dataPath, systems, entries);
        LoadSimpleValues(dataPath, ContentPaths.OccupationsFile, "occupations", GeneratorKind.Occupation, systems, entries);
        LoadSimpleValues(dataPath, ContentPaths.BackgroundsFile, "backgrounds", GeneratorKind.Background, systems, entries);
        LoadClasses(dataPath, systems, entries);
        LoadSubclasses(dataPath, systems, entries);
        LoadRaces(dataPath, systems, entries);
        LoadSubraces(dataPath, systems, entries);
        LoadAlignments(dataPath, systems, entries);
        LoadInspirations(dataPath, systems, entries);

        var steps = LoadSteps(dataPath, systems);
        var keywordCategories = LoadKeywordCategories(dataPath);
        var keywords = LoadKeywords(dataPath, keywordCategories);
        var classLevels = LoadClassLevels(dataPath, entries);

        return new ContentBundle
        {
            Systems = systems,
            Attributes = attributes,
            GeneratorEntries = entries,
            CreationSteps = steps,
            KeywordCategories = keywordCategories,
            Keywords = keywords,
            ClassLevels = classLevels,
        };
    }

    /* ---------------- 规则系统 ---------------- */

    private static List<ContentSystem> LoadSystems(string dataPath, out List<ContentAttribute> attributes)
    {
        var path = Path.Combine(dataPath, ContentPaths.SystemsFile);
        var systems = new List<ContentSystem>();
        attributes = [];

        using var workbook = OpenWorkbook(path);

        foreach (var row in XlsxHelpers.ReadRows(
                     XlsxHelpers.RequireSheet(workbook, path, "systems"),
                     path,
                     ["id", "name", "description", "nameTemplate", "iconUrl", "accent", "available"]))
        {
            var id = row.Required("id", path).ToLowerInvariant();
            if (systems.Any(system => system.Id == id))
            {
                row.Fail(path, $"系统 id「{id}」重复。");
            }

            var description = row["description"].Trim();
            var template = row["nameTemplate"].Trim();
            var iconUrl = row["iconUrl"].Trim();
            var accent = row["accent"].Trim();

            systems.Add(new ContentSystem(
                id,
                row.Required("name", path),
                description.Length == 0 ? null : description,
                template.Length == 0 ? "{surname}{given}" : template,
                iconUrl.Length == 0 ? null : iconUrl,
                accent.Length == 0 ? null : accent,
                row.Bool("available")));
        }

        foreach (var row in XlsxHelpers.ReadRows(
                     XlsxHelpers.RequireSheet(workbook, path, "attributes"),
                     path,
                     ["systemId", "key", "label", "formula"]))
        {
            var systemId = row.Required("systemId", path).ToLowerInvariant();
            if (systems.All(system => system.Id != systemId))
            {
                row.Fail(path, $"systemId「{systemId}」在 systems 工作表里不存在。");
            }

            var key = row.Required("key", path);
            var label = row.Required("label", path);
            var formula = row.Required("formula", path);

            try
            {
                DiceRoller.Roll(formula);
            }
            catch (ArgumentException ex)
            {
                row.Fail(path, $"属性「{label}」的公式 {formula} 无法解析：{ex.Message}");
            }

            attributes.Add(new ContentAttribute(systemId, key, label, formula));
        }

        if (systems.Count == 0)
        {
            throw new ContentException($"{ContentPaths.SystemsFile} 里没有任何规则系统。");
        }

        return systems;
    }

    /* ---------------- 姓名片段 ---------------- */

    private static void LoadNames(
        string dataPath,
        IReadOnlyList<ContentSystem> systems,
        List<GeneratorEntryContent> entries)
    {
        var path = Path.Combine(dataPath, ContentPaths.NamesFile);
        using var workbook = OpenWorkbook(path);
        var counters = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in XlsxHelpers.ReadRows(
                     XlsxHelpers.RequireSheet(workbook, path, "names"),
                     path,
                     ["systemId", "category", "value"]))
        {
            var systemId = RequireSystem(row, systems, path);
            var category = row.Required("category", path);
            var counterKey = $"{systemId}#{category}";
            counters.TryGetValue(counterKey, out var index);
            counters[counterKey] = index + 1;

            entries.Add(new GeneratorEntryContent(
                systemId,
                JsonKey(GeneratorKind.NamePart),
                category,
                row.Required("value", path),
                null,
                (index + 1) * 10));
        }
    }

    /* ---------------- 背景 / 职业 / 种族 / 灵感 ---------------- */

    private static void LoadSimpleValues(
        string dataPath,
        string fileName,
        string sheetName,
        GeneratorKind kind,
        IReadOnlyList<ContentSystem> systems,
        List<GeneratorEntryContent> entries)
    {
        var path = Path.Combine(dataPath, fileName);
        using var workbook = OpenWorkbook(path);
        var counters = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in XlsxHelpers.ReadRows(
                     XlsxHelpers.RequireSheet(workbook, path, sheetName),
                     path,
                     ["systemId", "value"]))
        {
            var systemId = RequireSystem(row, systems, path);
            counters.TryGetValue(systemId, out var index);
            counters[systemId] = index + 1;

            entries.Add(new GeneratorEntryContent(
                systemId,
                JsonKey(kind),
                JsonKey(kind),
                row.Required("value", path),
                null,
                (index + 1) * 10));
        }
    }

    private static void LoadClasses(
        string dataPath,
        IReadOnlyList<ContentSystem> systems,
        List<GeneratorEntryContent> entries)
    {
        var path = Path.Combine(dataPath, ContentPaths.ClassesFile);
        using var workbook = OpenWorkbook(path);
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var index = 0;

        foreach (var row in XlsxHelpers.ReadRows(
                     XlsxHelpers.RequireSheet(workbook, path, "classes"),
                     path,
                     ["id", "systemId", "name", "description", "hitDie", "primaryAbility", "imageUrl", "features", "accent", "savingThrows", "spellcastingAbility"]))
        {
            var id = row.Required("id", path).ToLowerInvariant();
            if (!ids.Add(id))
            {
                row.Fail(path, $"职业 id「{id}」重复。");
            }

            var systemId = RequireSystem(row, systems, path);
            var features = row["features"]
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();
            var imageUrl = row["imageUrl"].Trim();
            var accent = row["accent"].Trim();

            entries.Add(new GeneratorEntryContent(
                systemId,
                JsonKey(GeneratorKind.Class),
                id,
                row.Required("name", path),
                JsonSerializer.Serialize(new
                {
                    description = row["description"].Trim(),
                    hitDie = row.Required("hitDie", path),
                    primaryAbility = row.Required("primaryAbility", path),
                    features,
                    imageUrl = imageUrl.Length == 0 ? null : imageUrl,
                    accent = accent.Length == 0 ? null : accent,
                    savingThrows = row["savingThrows"].Trim(),
                    spellcastingAbility = row["spellcastingAbility"].Trim(),
                }, JsonOptions),
                (index + 1) * 10));
            index += 1;
        }
    }

    private static void LoadSubclasses(
        string dataPath,
        IReadOnlyList<ContentSystem> systems,
        List<GeneratorEntryContent> entries)
    {
        var path = Path.Combine(dataPath, ContentPaths.SubclassesFile);
        if (!File.Exists(path))
        {
            return;
        }

        using var workbook = OpenWorkbook(path);
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var classCodes = entries
            .Where(entry => entry.Kind == JsonKey(GeneratorKind.Class))
            .Select(entry => entry.Category)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var index = 0;

        foreach (var row in XlsxHelpers.ReadRows(
                     XlsxHelpers.RequireSheet(workbook, path, "subclasses"),
                     path,
                     ["systemId", "id", "classId", "name", "description", "imageUrl", "accent", "features"]))
        {
            var id = row.Required("id", path).ToLowerInvariant();
            if (!ids.Add(id))
            {
                row.Fail(path, $"子职 id「{id}」重复。");
            }

            var systemId = RequireSystem(row, systems, path);
            var classId = row.Required("classId", path).ToLowerInvariant();
            if (!classCodes.Contains(classId))
            {
                row.Fail(path, $"子职「{id}」的 classId「{classId}」在 {ContentPaths.ClassesFile} 里不存在。");
            }

            var features = row["features"]
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();
            var imageUrl = row["imageUrl"].Trim();
            var accent = row["accent"].Trim();

            entries.Add(new GeneratorEntryContent(
                systemId,
                JsonKey(GeneratorKind.Subclass),
                classId,
                row.Required("name", path),
                JsonSerializer.Serialize(new
                {
                    code = id,
                    description = row["description"].Trim(),
                    features,
                    imageUrl = imageUrl.Length == 0 ? null : imageUrl,
                    accent = accent.Length == 0 ? null : accent,
                }, JsonOptions),
                (index + 1) * 10));
            index += 1;
        }
    }

    private static void LoadRaces(
        string dataPath,
        IReadOnlyList<ContentSystem> systems,
        List<GeneratorEntryContent> entries)
    {
        var path = Path.Combine(dataPath, ContentPaths.RacesFile);
        using var workbook = OpenWorkbook(path);
        var index = 0;

        foreach (var row in XlsxHelpers.ReadRows(
                     XlsxHelpers.RequireSheet(workbook, path, "races"),
                     path,
                     ["id", "systemId", "name", "description", "abilityBonus", "speed", "traits"]))
        {
            var id = row.Required("id", path).ToLowerInvariant();
            var systemId = RequireSystem(row, systems, path);
            var traits = row["traits"]
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();

            entries.Add(new GeneratorEntryContent(
                systemId,
                JsonKey(GeneratorKind.Race),
                id,
                row.Required("name", path),
                JsonSerializer.Serialize(new
                {
                    description = row["description"].Trim(),
                    abilityBonus = row["abilityBonus"].Trim(),
                    speed = row.Int("speed", 30),
                    traits,
                }, JsonOptions),
                (index + 1) * 10));
            index += 1;
        }
    }

    private static void LoadSubraces(
        string dataPath,
        IReadOnlyList<ContentSystem> systems,
        List<GeneratorEntryContent> entries)
    {
        var path = Path.Combine(dataPath, ContentPaths.SubracesFile);
        if (!File.Exists(path))
        {
            return;
        }

        using var workbook = OpenWorkbook(path);
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var raceCodes = entries
            .Where(entry => entry.Kind == JsonKey(GeneratorKind.Race))
            .Select(entry => entry.Category)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var index = 0;

        foreach (var row in XlsxHelpers.ReadRows(
                     XlsxHelpers.RequireSheet(workbook, path, "subraces"),
                     path,
                     ["systemId", "id", "raceId", "name", "description", "traits"]))
        {
            var id = row.Required("id", path).ToLowerInvariant();
            if (!ids.Add(id))
            {
                row.Fail(path, $"亚种 id「{id}」重复。");
            }

            var systemId = RequireSystem(row, systems, path);
            var raceId = row.Required("raceId", path).ToLowerInvariant();
            if (!raceCodes.Contains(raceId))
            {
                row.Fail(path, $"亚种「{id}」的 raceId「{raceId}」在 {ContentPaths.RacesFile} 里不存在。");
            }

            var traits = row["traits"]
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();

            entries.Add(new GeneratorEntryContent(
                systemId,
                JsonKey(GeneratorKind.Subrace),
                raceId,
                row.Required("name", path),
                JsonSerializer.Serialize(new
                {
                    code = id,
                    description = row["description"].Trim(),
                    traits,
                }, JsonOptions),
                (index + 1) * 10));
            index += 1;
        }
    }

    /// <summary>
    /// 阵营（九宫格）。Category 存 code，ExtraJson 存 iconUrl / accent，
    /// 具体解释放在关键词里（[守序善良] 之类可以直接点开）。
    /// </summary>
    private static void LoadAlignments(
        string dataPath,
        IReadOnlyList<ContentSystem> systems,
        List<GeneratorEntryContent> entries)
    {
        var path = Path.Combine(dataPath, ContentPaths.AlignmentsFile);
        if (!File.Exists(path))
        {
            return;
        }

        using var workbook = OpenWorkbook(path);
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var index = 0;

        foreach (var row in XlsxHelpers.ReadRows(
                     XlsxHelpers.RequireSheet(workbook, path, "alignments"),
                     path,
                     ["systemId", "id", "name", "iconUrl", "accent"]))
        {
            var id = row.Required("id", path).ToLowerInvariant();
            if (!ids.Add(id))
            {
                row.Fail(path, $"阵营 id「{id}」重复。");
            }

            var systemId = RequireSystem(row, systems, path);
            var iconUrl = row["iconUrl"].Trim();
            var accent = row["accent"].Trim();

            entries.Add(new GeneratorEntryContent(
                systemId,
                JsonKey(GeneratorKind.Alignment),
                id,
                row.Required("name", path),
                JsonSerializer.Serialize(new
                {
                    code = id,
                    iconUrl = iconUrl.Length == 0 ? null : iconUrl,
                    accent = accent.Length == 0 ? null : accent,
                }, JsonOptions),
                (index + 1) * 10));
            index += 1;
        }
    }

    private static void LoadInspirations(
        string dataPath,
        IReadOnlyList<ContentSystem> systems,
        List<GeneratorEntryContent> entries)
    {
        var path = Path.Combine(dataPath, ContentPaths.InspirationsFile);
        using var workbook = OpenWorkbook(path);
        var counters = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in XlsxHelpers.ReadRows(
                     XlsxHelpers.RequireSheet(workbook, path, "inspirations"),
                     path,
                     ["systemId", "title", "text"]))
        {
            var systemId = row["systemId"].Trim().ToLowerInvariant();
            if (systemId.Length > 0 && systems.All(system => system.Id != systemId))
            {
                row.Fail(path, $"systemId「{systemId}」在 {ContentPaths.SystemsFile} 里不存在。");
            }

            var key = systemId.Length == 0 ? "global" : systemId;
            counters.TryGetValue(key, out var index);
            counters[key] = index + 1;

            entries.Add(new GeneratorEntryContent(
                systemId.Length == 0 ? null : systemId,
                JsonKey(GeneratorKind.Inspiration),
                "hook",
                row.Required("title", path),
                JsonSerializer.Serialize(new { text = row.Required("text", path) }, JsonOptions),
                (index + 1) * 10));
        }
    }

    /* ---------------- 车卡流程 ---------------- */

    private static List<ContentCreationStep> LoadSteps(string dataPath, IReadOnlyList<ContentSystem> systems)
    {
        var path = Path.Combine(dataPath, ContentPaths.FlowFile);
        using var workbook = OpenWorkbook(path);
        var steps = new List<ContentCreationStep>();

        foreach (var row in XlsxHelpers.ReadRows(
                     XlsxHelpers.RequireSheet(workbook, path, "steps"),
                     path,
                     ["systemId", "kind", "title", "description", "isEnabled"]))
        {
            var systemId = row["systemId"].Trim().ToLowerInvariant();
            if (systemId.Length > 0 && systems.All(system => system.Id != systemId))
            {
                row.Fail(path, $"systemId「{systemId}」在 {ContentPaths.SystemsFile} 里不存在。");
            }

            var kind = ParseStepKind(row.Required("kind", path));
            if (kind is null)
            {
                row.Fail(path, $"步骤类型「{row["kind"]}」不认识，可用：class / subclass / race / subrace / background / attributes / name / summary。");
            }

            steps.Add(new ContentCreationStep(
                systemId.Length == 0 ? null : systemId,
                JsonKey(kind!.Value),
                row.Required("title", path),
                row["description"].Trim(),
                row.Bool("isEnabled")));
        }

        return steps;
    }

    /* ---------------- 关键词分类与关键词 ---------------- */

    private static List<ContentKeywordCategory> LoadKeywordCategories(string dataPath)
    {
        var path = Path.Combine(dataPath, ContentPaths.KeywordCategoriesFile);
        using var workbook = OpenWorkbook(path);
        var categories = new List<ContentKeywordCategory>();
        var codes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in XlsxHelpers.ReadRows(
                     XlsxHelpers.RequireSheet(workbook, path, "categories"),
                     path,
                     ["id", "name"]))
        {
            var code = row.Required("id", path);
            if (!int.TryParse(code, out _) || code.Length != 3)
            {
                row.Fail(path, $"分类编号「{code}」必须是三位数字，例如 001。");
            }

            if (!codes.Add(code))
            {
                row.Fail(path, $"分类编号「{code}」重复。");
            }

            var name = row.Required("name", path);
            if (!names.Add(name))
            {
                row.Fail(path, $"分类名称「{name}」重复。");
            }

            categories.Add(new ContentKeywordCategory(code, name));
        }

        if (categories.Count == 0)
        {
            throw new ContentException($"{ContentPaths.KeywordCategoriesFile} 里没有任何关键词分类。");
        }

        return categories;
    }

    private static List<ContentKeyword> LoadKeywords(
        string dataPath,
        IReadOnlyList<ContentKeywordCategory> categories)
    {
        var files = Directory
            .GetFiles(dataPath, $"{ContentPaths.KeywordFilePrefix}.*.xlsx")
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (files.Count == 0)
        {
            throw new ContentException(
                $"data/ 下没有关键词文件，命名规则是「{ContentPaths.KeywordFilePrefix}.分类.xlsx」，例如 关键词.属性.xlsx。");
        }

        var codeByName = categories.ToDictionary(category => category.Name, category => category.Code, StringComparer.OrdinalIgnoreCase);
        var raw = new List<(string Path, string Category, string Key, string Id, string Title, string Summary, string Content, string? ImageUrl, bool IsEnabled)>();

        foreach (var path in files)
        {
            var fileCategory = ReadCategoryFromFileName(path);

            using var workbook = OpenWorkbook(path);
            foreach (var row in XlsxHelpers.ReadRows(
                         XlsxHelpers.RequireSheet(workbook, path, "keywords"),
                         path,
                         ["key", "title", "summary", "content", "imageUrl", "isEnabled"]))
            {
                var category = row["分类"].Trim();
                if (category.Length == 0)
                {
                    category = fileCategory;
                }

                if (!codeByName.ContainsKey(category))
                {
                    row.Fail(path, $"分类「{category}」没有在 {ContentPaths.KeywordCategoriesFile} 里登记。");
                }

                var summary = row.Required("summary", path);
                if (summary.Length > 256)
                {
                    row.Fail(path, $"关键词「{row["key"].Trim()}」的简略描述超过 256 字。");
                }

                if (summary.Contains('[') || summary.Contains(']'))
                {
                    row.Fail(path, $"关键词「{row["key"].Trim()}」的简略描述里不能写 [方括号]，需要展开的内容放进详细描述。");
                }

                raw.Add((
                    path,
                    category,
                    row.Required("key", path),
                    row["id"].Trim(),
                    row["title"].Trim(),
                    summary,
                    row.Required("content", path),
                    row["imageUrl"].Trim(),
                    row.Bool("isEnabled")));
            }
        }

        // 先收集显式 ID，再给没写 ID 的词条按分类顺序补号，避免和已有编号撞车。
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in raw.Where(item => item.Id.Length > 0))
        {
            if (!used.Add(item.Id))
            {
                throw new ContentException($"关键词 ID「{item.Id}」重复（{Path.GetFileName(item.Path)}）。");
            }
        }

        var counters = categories.ToDictionary(category => category.Code, _ => 0, StringComparer.OrdinalIgnoreCase);
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var keywords = new List<ContentKeyword>();

        foreach (var item in raw)
        {
            if (!keys.Add(item.Key))
            {
                throw new ContentException($"关键词「{item.Key}」重复（同分类文件之间也不能重名）。");
            }

            var categoryCode = codeByName[item.Category];
            var code = item.Id;

            if (code.Length == 0)
            {
                do
                {
                    counters[categoryCode] += 1;
                    code = categoryCode + counters[categoryCode].ToString("D5");
                }
                while (used.Contains(code));

                used.Add(code);
            }
            else
            {
                if (code.Length != 8 || !code.StartsWith(categoryCode, StringComparison.OrdinalIgnoreCase) || !int.TryParse(code, out _))
                {
                    throw new ContentException(
                        $"关键词「{item.Key}」的 ID「{code}」不符合规则：应为 8 位数字，且前三位是分类编号 {categoryCode}（例如 {categoryCode}00001）。");
                }
            }

            keywords.Add(new ContentKeyword(
                code,
                item.Key,
                item.Category,
                item.Title.Length == 0 ? item.Key : item.Title,
                item.Summary,
                item.Content,
                item.ImageUrl.Length == 0 ? null : item.ImageUrl,
                item.IsEnabled));
        }

        return keywords;
    }

    private static string ReadCategoryFromFileName(string path)
    {
        var name = Path.GetFileNameWithoutExtension(path);
        var prefix = ContentPaths.KeywordFilePrefix + ".";
        if (!name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        var category = name[prefix.Length..];
        return string.Equals(category, "未分类", StringComparison.OrdinalIgnoreCase) ? string.Empty : category;
    }

    /* ---------------- 职业等级成长 ---------------- */

    private static List<ContentLevelFeature> LoadClassLevels(
        string dataPath,
        IReadOnlyList<GeneratorEntryContent> entries)
    {
        var path = Path.Combine(dataPath, ContentPaths.LevelsFile);
        using var workbook = OpenWorkbook(path);

        var classEntries = entries
            .Where(entry => entry.Kind == JsonKey(GeneratorKind.Class))
            .ToList();

        var classByCode = classEntries.ToDictionary(
            entry => entry.Category,
            entry => entry.Category,
            StringComparer.OrdinalIgnoreCase);

        var classByName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in classEntries)
        {
            classByName.TryAdd(entry.Value, entry.Category);
        }

        var levels = new List<ContentLevelFeature>();
        var counters = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in XlsxHelpers.ReadRows(
                     XlsxHelpers.RequireSheet(workbook, path, "levels"),
                     path,
                     ["classId", "level", "title", "description", "isEnabled"]))
        {
            var rawClassId = row.Required("classId", path).Trim();
            var classId = classByCode.TryGetValue(rawClassId, out var code)
                ? code
                : classByName.TryGetValue(rawClassId, out var byName)
                    ? byName
                    : null;

            if (classId is null)
            {
                row.Fail(path, $"classId「{rawClassId}」既不是 {ContentPaths.ClassesFile} 里的职业 id，也不是职业名。");
            }

            var level = row.Int("level");
            if (level is < 1 or > 20)
            {
                row.Fail(path, "level 必须在 1-20 之间。");
            }

            var counterKey = $"{classId}#{level}";
            counters.TryGetValue(counterKey, out var index);
            counters[counterKey] = index + 1;

            levels.Add(new ContentLevelFeature(
                classId,
                level,
                row.Required("title", path),
                row.Required("description", path),
                row.Bool("isEnabled"),
                level * 100 + index * 10));
        }

        return levels;
    }

    /* ---------------- 小工具 ---------------- */

    private static XLWorkbook OpenWorkbook(string path)
    {
        if (!File.Exists(path))
        {
            throw new ContentException($"缺少内容文件：{path}");
        }

        return new XLWorkbook(path);
    }

    private static string RequireSystem(SheetRow row, IReadOnlyList<ContentSystem> systems, string path)
    {
        var systemId = row.Required("systemId", path).ToLowerInvariant();
        if (systems.All(system => system.Id != systemId))
        {
            row.Fail(path, $"systemId「{systemId}」在 {ContentPaths.SystemsFile} 里不存在。");
        }

        return systemId;
    }

    private static string JsonKey(Enum value)
        => JsonNamingPolicy.CamelCase.ConvertName(value.ToString());

    private static CreationStepKind? ParseStepKind(string raw)
    {
        foreach (var kind in Enum.GetValues<CreationStepKind>())
        {
            if (string.Equals(JsonKey(kind), raw, StringComparison.OrdinalIgnoreCase)
                || string.Equals(kind.ToString(), raw, StringComparison.OrdinalIgnoreCase))
            {
                return kind;
            }
        }

        return null;
    }
}
