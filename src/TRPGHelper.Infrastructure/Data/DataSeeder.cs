using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using TRPGHelper.Domain.Cards;
using TRPGHelper.Domain.Entities;

namespace TRPGHelper.Infrastructure.Data;

/// <summary>
/// 内置数据（规则系统 + 生成器素材）的首次写入。属性模板里的 formula 使用 Domain 里的骰子表达式语法。
/// 写进数据库之后，内容以管理台为准，这里只在缺失时补种。
/// </summary>
public static class DataSeeder
{
    private static readonly Dictionary<string, string> BuiltinNameTemplates = new(StringComparer.OrdinalIgnoreCase)
    {
        ["coc7"] = "{surname}{given}",
        ["dnd5e"] = "{first}·{epithet}",
    };

    public static async Task SeedAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        var existingSystems = await db.GameSystems.ToListAsync(cancellationToken);
        var existingCodes = existingSystems.Select(system => system.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var missing = BuildSystems()
            .Where(system => !existingCodes.Contains(system.Code))
            .ToList();

        if (missing.Count > 0)
        {
            db.GameSystems.AddRange(missing);
        }

        // 老库里加列时拿到的是默认模板，按 code 纠正一次内置系统的姓名模板。
        foreach (var system in existingSystems)
        {
            if (BuiltinNameTemplates.TryGetValue(system.Code, out var template)
                && system.NameTemplate != template)
            {
                system.NameTemplate = template;
            }
        }

        var hasEntries = await db.GeneratorEntries.AnyAsync(cancellationToken);
        if (!hasEntries)
        {
            db.GeneratorEntries.AddRange(GeneratorSeedData.Build());
        }

        var hasSteps = await db.CreationSteps.AnyAsync(cancellationToken);
        if (!hasSteps)
        {
            db.CreationSteps.AddRange(GeneratorSeedData.BuildCreationSteps());
        }

        await BackfillClassImagesAsync(db, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// 老库里的职业卡是在「图片可配置」之前种下的，缺 imageUrl 就补一个默认图（前端 static 里的同名文件），
    /// 之后管理台改过就不会再被覆盖。
    /// </summary>
    private static async Task BackfillClassImagesAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var classes = await db.GeneratorEntries
            .Where(entry => entry.Kind == GeneratorKind.Class && entry.ExtraJson != null)
            .ToListAsync(cancellationToken);

        foreach (var entry in classes)
        {
            JsonObject? extra;
            try
            {
                extra = JsonNode.Parse(entry.ExtraJson!) as JsonObject;
            }
            catch (JsonException)
            {
                continue;
            }

            if (extra is null || extra.ContainsKey("imageUrl"))
            {
                continue;
            }

            extra["imageUrl"] = $"/static/classes/{entry.Category}.jpg";
            entry.ExtraJson = extra.ToJsonString();
        }
    }

    public static IEnumerable<GameSystem> BuildSystems()
    {
        yield return new GameSystem
        {
            Code = "coc7",
            Name = "克苏鲁的呼唤 7版",
            Description = "调查员风格，d100 检定，属性 3d6×5 起步。",
            SortOrder = 10,
            IsBuiltin = true,
            NameTemplate = "{surname}{given}",
            AttributeTemplateJson = AttributeTemplateParser.Serialize(
            [
                new("STR", "力量", "3d6*5"),
                new("CON", "体质", "3d6*5"),
                new("SIZ", "体型", "2d6*5+30"),
                new("DEX", "敏捷", "3d6*5"),
                new("APP", "外貌", "3d6*5"),
                new("INT", "智力", "2d6*5+30"),
                new("POW", "意志", "3d6*5"),
                new("EDU", "教育", "2d6*5+30"),
                new("Luck", "幸运", "3d6*5"),
            ]),
        };

        yield return new GameSystem
        {
            Code = "dnd5e",
            Name = "龙与地下城 5版",
            Description = "d20 系统，六维属性，4d6 取高 3。",
            SortOrder = 20,
            IsBuiltin = true,
            NameTemplate = "{first}·{epithet}",
            AttributeTemplateJson = AttributeTemplateParser.Serialize(
            [
                new("STR", "力量", "4d6kh3"),
                new("DEX", "敏捷", "4d6kh3"),
                new("CON", "体质", "4d6kh3"),
                new("INT", "智力", "4d6kh3"),
                new("WIS", "感知", "4d6kh3"),
                new("CHA", "魅力", "4d6kh3"),
            ]),
        };
    }
}
