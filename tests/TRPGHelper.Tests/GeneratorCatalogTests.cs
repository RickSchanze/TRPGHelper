using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TRPGHelper.Api.Services;
using TRPGHelper.Domain.Entities;
using TRPGHelper.Infrastructure.Content;
using TRPGHelper.Infrastructure.Data;

namespace TRPGHelper.Tests;

/// <summary>
/// 生成器素材已经是「数据库里的配置」，这里验证读取逻辑与出厂数据都符合预期。
/// </summary>
public sealed class GeneratorCatalogTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;

    private AppDbContext _db = null!;

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new AppDbContext(options);
        await _db.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        await _connection.DisposeAsync();
    }

    [Fact]
    public async Task Seed_creates_builtin_systems_and_generator_entries()
    {
        await SyncContentAsync();

        var systems = await _db.GameSystems.OrderBy(system => system.SortOrder).ToListAsync();
        Assert.Equal(2, systems.Count);
        Assert.Equal("coc7", systems[0].Code);
        Assert.Equal("{surname}{given}", systems[0].NameTemplate);
        Assert.Equal("dnd5e", systems[1].Code);
        Assert.Equal("{first}·{epithet}", systems[1].NameTemplate);

        Assert.True(await _db.GeneratorEntries.CountAsync() >= 100);
        Assert.True(await _db.GeneratorEntries.AnyAsync(entry => entry.Kind == GeneratorKind.Class));
        Assert.True(await _db.GeneratorEntries.AnyAsync(entry => entry.Kind == GeneratorKind.Race));
    }

    [Fact]
    public async Task Seed_creates_a_default_creation_flow_without_rulebook_step()
    {
        await SyncContentAsync();

        var steps = await _db.CreationSteps.OrderBy(step => step.SortOrder).ToListAsync();

        Assert.Equal(11, steps.Count);

        var first = steps[0];
        Assert.Equal("dnd5e", first.SystemCode);
        Assert.Equal(CreationStepKind.Class, first.Kind);
        Assert.Equal("选择职业", first.Title);
        Assert.True(first.IsEnabled);

        var second = steps[1];
        Assert.Equal(CreationStepKind.Subclass, second.Kind);
        Assert.Equal("选择子职", second.Title);
        Assert.True(second.IsEnabled);

        var third = steps[2];
        Assert.Equal(CreationStepKind.Race, third.Kind);
        Assert.Equal("选择种族", third.Title);
        Assert.True(third.IsEnabled);

        var fourth = steps[3];
        Assert.Equal(CreationStepKind.Subrace, fourth.Kind);
        Assert.Equal("选择亚种", fourth.Title);
        Assert.True(fourth.IsEnabled);

        var fifth = steps[4];
        Assert.Equal(CreationStepKind.Background, fifth.Kind);
        Assert.Equal("决定背景", fifth.Title);
        Assert.True(fifth.IsEnabled);

        var sixth = steps[5];
        Assert.Equal(CreationStepKind.Attributes, sixth.Kind);
        Assert.Equal("决定属性", sixth.Title);
        Assert.True(sixth.IsEnabled);

        Assert.Equal(CreationStepKind.Skills, steps[6].Kind);
        Assert.Equal("选择技能", steps[6].Title);
        Assert.Equal(CreationStepKind.Languages, steps[7].Kind);
        Assert.Equal("选择语言", steps[7].Title);
        Assert.Equal(CreationStepKind.Features, steps[8].Kind);
        Assert.Equal("职业特性", steps[8].Title);
        Assert.Equal(CreationStepKind.Equipment, steps[9].Kind);
        Assert.Equal("起始装备", steps[9].Title);
        Assert.Equal(CreationStepKind.Description, steps[10].Kind);
        Assert.Equal("描述角色", steps[10].Title);
    }

    [Fact]
    public async Task Seed_creates_keyword_entries_with_cross_references()
    {
        await SyncContentAsync();

        var keywords = await _db.Keywords.OrderBy(keyword => keyword.SortOrder).ToListAsync();

        Assert.True(keywords.Count >= 150);
        Assert.All(keywords, keyword =>
        {
            Assert.Equal(8, keyword.Code.Length);
            Assert.Matches("^[0-9]{8}$", keyword.Code);
            Assert.False(string.IsNullOrWhiteSpace(keyword.Summary));
            Assert.DoesNotContain("[", keyword.Summary);
            Assert.DoesNotContain("]", keyword.Summary);
        });
        Assert.Equal(keywords.Count, keywords.Select(keyword => keyword.Code).Distinct().Count());

        var categoryCodes = new Dictionary<string, string>
        {
            ["法术"] = "001",
            ["职业"] = "002",
            ["属性"] = "003",
            ["规则"] = "004",
            ["战斗"] = "005",
            ["种族"] = "006",
            ["阵营"] = "007",
        };
        Assert.All(keywords, keyword => Assert.StartsWith(categoryCodes[keyword.Category], keyword.Code));
        Assert.Contains(keywords, keyword => keyword.Key == "敏捷");
        Assert.Contains(keywords, keyword => keyword.Key == "属性检定" && keyword.Content.Contains("[力量]"));
        foreach (var key in new[] { "箭术", "防御", "对决", "双武器战斗", "法术位", "子职", "奉献之誓", "防护善恶" })
        {
            Assert.Contains(keywords, keyword => keyword.Key == key);
        }

        // 阵营：九宫格九个 + 两条坐标轴上的五个词，前端点名字就能看解释
        foreach (var key in new[]
                 {
                     "守序善良", "中立善良", "混乱善良", "守序中立", "绝对中立",
                     "混乱中立", "守序邪恶", "中立邪恶", "混乱邪恶",
                     "守序", "混乱", "善良", "邪恶", "中立",
                 })
        {
            Assert.Contains(keywords, keyword => keyword.Key == key && keyword.Category == "阵营");
        }

        Assert.All(keywords, keyword => Assert.True(keyword.IsEnabled));
    }

    [Fact]
    public async Task Seed_creates_class_level_progression_covering_levels_1_to_20()
    {
        await SyncContentAsync();

        var rows = await _db.ClassLevelFeatures.ToListAsync();

        Assert.All(rows, row => Assert.True(row.IsEnabled));

        foreach (var classCode in new[] { "ranger", "fighter", "monk", "druid", "paladin" })
        {
            var levels = rows
                .Where(row => row.ClassCode == classCode)
                .Select(row => row.Level)
                .Distinct()
                .OrderBy(level => level)
                .ToList();

            Assert.Equal(Enumerable.Range(1, 20), levels);
        }

        var rangerLevelOne = rows.Where(row => row.ClassCode == "ranger" && row.Level == 1).ToList();
        Assert.Equal(2, rangerLevelOne.Count);
        Assert.Contains(rangerLevelOne, row => row.Title == "宿敌");
    }

    [Fact]
    public async Task Content_files_are_loaded_from_the_repo_data_folder()
    {
        var dataPath = ContentPaths.Resolve(null, AppContext.BaseDirectory);

        Assert.NotNull(dataPath);
        Assert.True(File.Exists(Path.Combine(dataPath!, ContentPaths.SystemsFile)));
        Assert.True(File.Exists(Path.Combine(dataPath!, ContentPaths.ClassesFile)));
        Assert.True(File.Exists(Path.Combine(dataPath!, ContentPaths.LevelsFile)));
        Assert.True(File.Exists(Path.Combine(dataPath!, ContentPaths.KeywordCategoriesFile)));
        Assert.NotEmpty(Directory.GetFiles(dataPath!, $"{ContentPaths.KeywordFilePrefix}.*.xlsx"));
    }

    private async Task SyncContentAsync()
    {
        var dataPath = ContentPaths.FindRequired(null, AppContext.BaseDirectory);
        var bundle = ContentLoader.Load(dataPath);
        await ContentSynchronizer.SyncAsync(_db, bundle);
    }

    [Fact]
    public async Task Alignments_carry_icon_and_accent()
    {
        await SyncContentAsync();

        var catalog = new GeneratorCatalogService(_db);
        var list = await catalog.AlignmentsAsync("dnd5e", CancellationToken.None);

        Assert.Equal(9, list.Count);

        var lawfulGood = Assert.Single(list, item => item.Code == "lawful-good");
        Assert.Equal("守序善良", lawfulGood.Name);
        Assert.Equal("/static/alignments/lawful-good.svg", lawfulGood.IconUrl);
        Assert.False(string.IsNullOrWhiteSpace(lawfulGood.Accent));

        // 九宫格按「守序 / 中立 / 混乱」三行排列，读起来是 3×3
        Assert.Equal(
            new[] { "lawful-good", "neutral-good", "chaotic-good" },
            list.Skip(0).Take(3).Select(item => item.Code));
        Assert.Equal("chaotic-evil", list[^1].Code);
    }

    [Fact]
    public async Task Race_and_subrace_ability_bonuses_are_structured()
    {
        await SyncContentAsync();

        var catalog = new GeneratorCatalogService(_db);

        var races = await catalog.RacesAsync("dnd5e", CancellationToken.None);
        var elf = Assert.Single(races, race => race.Code == "elf");
        Assert.Contains(elf.AbilityBonuses, bonus => bonus.Key == "DEX" && bonus.Label == "敏捷" && bonus.Value == 2);
        Assert.Empty(elf.AbilityChoices);

        var human = Assert.Single(races, race => race.Code == "human");
        Assert.Equal(6, human.AbilityBonuses.Count);
        Assert.All(human.AbilityBonuses, bonus => Assert.Equal(1, bonus.Value));

        var halfElf = Assert.Single(races, race => race.Code == "half-elf");
        Assert.Contains(halfElf.AbilityBonuses, bonus => bonus.Key == "CHA" && bonus.Value == 2);
        var choice = Assert.Single(halfElf.AbilityChoices);
        Assert.Equal(2, choice.Count);
        Assert.Equal(1, choice.Value);

        var subraces = await catalog.SubracesAsync("dnd5e", "elf", CancellationToken.None);
        var highElf = Assert.Single(subraces, subrace => subrace.Code == "high-elf");
        Assert.Contains(highElf.AbilityBonuses, bonus => bonus.Key == "INT" && bonus.Value == 1);

        var dragonSubraces = await catalog.SubracesAsync("dnd5e", "dragonborn", CancellationToken.None);
        Assert.All(dragonSubraces, subrace => Assert.Empty(subrace.AbilityBonuses));
    }

    [Fact]
    public async Task Seed_is_idempotent()
    {
        await SyncContentAsync();
        var firstCount = await _db.GeneratorEntries.CountAsync();

        await SyncContentAsync();

        Assert.Equal(2, await _db.GameSystems.CountAsync());
        Assert.Equal(firstCount, await _db.GeneratorEntries.CountAsync());
    }

    [Fact]
    public async Task Names_follow_the_system_template()
    {
        _db.GameSystems.Add(new GameSystem
        {
            Code = "dnd5e",
            Name = "龙与地下城 5版",
            NameTemplate = "{first}·{epithet}",
        });
        _db.GeneratorEntries.AddRange(
            new GeneratorEntry { SystemCode = "dnd5e", Kind = GeneratorKind.NamePart, Category = "first", Value = "艾拉" },
            new GeneratorEntry { SystemCode = "dnd5e", Kind = GeneratorKind.NamePart, Category = "epithet", Value = "晨风" });
        await _db.SaveChangesAsync();

        var catalog = new GeneratorCatalogService(_db);
        var names = await catalog.NamesAsync("dnd5e", 3, new Random(42), CancellationToken.None);

        Assert.NotEmpty(names);
        Assert.All(names, name => Assert.Equal("艾拉·晨风", name));
    }

    [Fact]
    public async Task Occupations_mix_occupation_and_background_entries()
    {
        _db.GameSystems.Add(new GameSystem { Code = "dnd5e", Name = "龙与地下城 5版" });
        _db.GeneratorEntries.AddRange(
            new GeneratorEntry { SystemCode = "dnd5e", Kind = GeneratorKind.Occupation, Category = "occupation", Value = "法师" },
            new GeneratorEntry { SystemCode = "dnd5e", Kind = GeneratorKind.Background, Category = "background", Value = "士兵" },
            new GeneratorEntry { SystemCode = "coc7", Kind = GeneratorKind.Occupation, Category = "occupation", Value = "记者" });
        await _db.SaveChangesAsync();

        var catalog = new GeneratorCatalogService(_db);
        var occupations = await catalog.OccupationsAsync("dnd5e", 5, new Random(1), CancellationToken.None);

        Assert.Equal(2, occupations.Count);
        Assert.Contains("法师", occupations);
        Assert.Contains("士兵", occupations);
    }

    [Fact]
    public async Task Disabled_entries_are_ignored()
    {
        _db.GameSystems.Add(new GameSystem { Code = "dnd5e", Name = "龙与地下城 5版" });
        _db.GeneratorEntries.AddRange(
            new GeneratorEntry { SystemCode = "dnd5e", Kind = GeneratorKind.Race, Category = "elf", Value = "精灵" },
            new GeneratorEntry { SystemCode = "dnd5e", Kind = GeneratorKind.Race, Category = "orc", Value = "兽人", IsEnabled = false });
        await _db.SaveChangesAsync();

        var catalog = new GeneratorCatalogService(_db);
        var races = await catalog.RacesAsync("dnd5e", CancellationToken.None);

        Assert.Single(races);
        Assert.Equal("精灵", races[0].Name);
    }

    [Fact]
    public async Task Class_and_race_extra_fields_are_mapped()
    {
        _db.GameSystems.Add(new GameSystem { Code = "dnd5e", Name = "龙与地下城 5版" });
        _db.GeneratorEntries.AddRange(
            new GeneratorEntry
            {
                SystemCode = "dnd5e",
                Kind = GeneratorKind.Class,
                Category = "ranger",
                Value = "游侠",
                ExtraJson = """
                    {"description":"荒野的猎手","hitDie":"d10","primaryAbility":"敏捷 / 感知","features":["宿敌","战斗风格"],"imageUrl":"/static/classes/ranger.jpg"}
                    """,
            },
            new GeneratorEntry
            {
                SystemCode = "dnd5e",
                Kind = GeneratorKind.Race,
                Category = "dwarf",
                Value = "矮人",
                ExtraJson = """
                    {"description":"山地工匠","abilityBonus":"体质 +2","speed":25}
                    """,
            });
        await _db.SaveChangesAsync();

        var catalog = new GeneratorCatalogService(_db);

        var classes = await catalog.ClassesAsync("dnd5e", CancellationToken.None);
        var ranger = Assert.Single(classes);
        Assert.Equal("ranger", ranger.Code);
        Assert.Equal("游侠", ranger.Name);
        Assert.Equal("d10", ranger.HitDie);
        Assert.Equal("敏捷 / 感知", ranger.PrimaryAbility);
        Assert.Equal(new[] { "宿敌", "战斗风格" }, ranger.Features);
        Assert.Equal("/static/classes/ranger.jpg", ranger.ImageUrl);

        var races = await catalog.RacesAsync("dnd5e", CancellationToken.None);
        var dwarf = Assert.Single(races);
        Assert.Equal("矮人", dwarf.Name);
        Assert.Equal("体质 +2", dwarf.AbilityBonus);
        Assert.Equal(25, dwarf.Speed);
    }
}
