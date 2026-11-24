using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TRPGHelper.Api.Services;
using TRPGHelper.Domain.Entities;
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
        await DataSeeder.SeedAsync(_db);

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
        await DataSeeder.SeedAsync(_db);

        var steps = await _db.CreationSteps.OrderBy(step => step.SortOrder).ToListAsync();

        var step = Assert.Single(steps);
        Assert.Equal("dnd5e", step.SystemCode);
        Assert.Equal(CreationStepKind.Class, step.Kind);
        Assert.Equal("选择职业", step.Title);
        Assert.True(step.IsEnabled);
    }

    [Fact]
    public async Task Seed_creates_keyword_entries_with_cross_references()
    {
        await DataSeeder.SeedAsync(_db);

        var keywords = await _db.Keywords.OrderBy(keyword => keyword.SortOrder).ToListAsync();

        Assert.Equal(13, keywords.Count);
        Assert.Contains(keywords, keyword => keyword.Key == "敏捷");
        Assert.Contains(keywords, keyword => keyword.Key == "属性检定" && keyword.Content.Contains("[力量]"));
        foreach (var key in new[] { "弓箭术", "防御", "决斗", "双武器战斗", "法术位", "副职" })
        {
            Assert.Contains(keywords, keyword => keyword.Key == key);
        }

        Assert.All(keywords, keyword => Assert.True(keyword.IsEnabled));
    }

    [Fact]
    public async Task Seed_creates_class_level_progression_covering_levels_1_to_20()
    {
        await DataSeeder.SeedAsync(_db);

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
    public async Task Seed_backfills_class_images_for_old_entries()
    {
        _db.GameSystems.Add(new GameSystem { Code = "dnd5e", Name = "龙与地下城 5版" });
        _db.GeneratorEntries.Add(new GeneratorEntry
        {
            SystemCode = "dnd5e",
            Kind = GeneratorKind.Class,
            Category = "ranger",
            Value = "游侠",
            ExtraJson = """{"description":"荒野的猎手","hitDie":"d10"}""",
        });
        await _db.SaveChangesAsync();

        await DataSeeder.SeedAsync(_db);

        var entry = await _db.GeneratorEntries.SingleAsync(item => item.Category == "ranger");
        Assert.Contains("/static/classes/ranger.jpg", entry.ExtraJson);
    }

    [Fact]
    public async Task Seed_is_idempotent()
    {
        await DataSeeder.SeedAsync(_db);
        var firstCount = await _db.GeneratorEntries.CountAsync();

        await DataSeeder.SeedAsync(_db);

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
