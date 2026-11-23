using Microsoft.EntityFrameworkCore;
using TRPGHelper.Domain.Cards;
using TRPGHelper.Domain.Entities;

namespace TRPGHelper.Infrastructure.Data;

/// <summary>内置规则系统。属性模板里的 formula 使用 Domain 里的骰子表达式语法。</summary>
public static class DataSeeder
{
    public static async Task SeedAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        var existingCodes = await db.GameSystems
            .Select(s => s.Code)
            .ToListAsync(cancellationToken);

        var missing = BuildSystems()
            .Where(system => !existingCodes.Contains(system.Code))
            .ToList();

        if (missing.Count == 0)
        {
            return;
        }

        db.GameSystems.AddRange(missing);
        await db.SaveChangesAsync(cancellationToken);
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
