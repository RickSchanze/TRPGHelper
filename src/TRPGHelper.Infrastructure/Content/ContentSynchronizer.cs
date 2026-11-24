using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TRPGHelper.Domain.Cards;
using TRPGHelper.Domain.Entities;
using TRPGHelper.Infrastructure.Data;

namespace TRPGHelper.Infrastructure.Content;

/// <summary>
/// 把 <see cref="ContentBundle"/> 写进数据库。内容表（规则系统 / 素材 / 流程 / 关键词 / 等级成长）
/// 以文件为准整份替换；玩家数据（用户、卡）不动。
/// 规则系统按 id 更新，且不会删除还有卡在用的系统。
/// </summary>
public static class ContentSynchronizer
{
    public static async Task SyncAsync(
        AppDbContext db,
        ContentBundle bundle,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        // ---------- 规则系统：按 id upsert ----------
        var existingSystems = await db.GameSystems.ToListAsync(cancellationToken);
        var usedSystemIds = await db.CharacterCards
            .Select(card => card.SystemId)
            .Distinct()
            .ToListAsync(cancellationToken);

        for (var index = 0; index < bundle.Systems.Count; index += 1)
        {
            var content = bundle.Systems[index];
            var entity = existingSystems.FirstOrDefault(system =>
                string.Equals(system.Code, content.Id, StringComparison.OrdinalIgnoreCase));

            if (entity is null)
            {
                entity = new GameSystem { Code = content.Id };
                db.GameSystems.Add(entity);
            }

            entity.Name = content.Name;
            entity.Description = content.Description;
            entity.NameTemplate = string.IsNullOrWhiteSpace(content.NameTemplate)
                ? "{surname}{given}"
                : content.NameTemplate;
            entity.IconUrl = content.IconUrl;
            entity.Accent = content.Accent;
            entity.IsAvailable = content.Available;
            entity.AttributeTemplateJson = AttributeTemplateParser.Serialize(
                bundle.Attributes
                    .Where(attribute => attribute.SystemId == content.Id)
                    .Select(attribute => new AttributeTemplate(attribute.Key, attribute.Label, attribute.Formula)));
            entity.SortOrder = (index + 1) * 10;
            entity.IsBuiltin = true;
        }

        var contentSystemIds = bundle.Systems
            .Select(system => system.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var removableSystems = existingSystems
            .Where(system => !contentSystemIds.Contains(system.Code) && !usedSystemIds.Contains(system.Id))
            .ToList();

        db.GameSystems.RemoveRange(removableSystems);

        // ---------- 生成器素材：整份替换 ----------
        db.GeneratorEntries.RemoveRange(await db.GeneratorEntries.ToListAsync(cancellationToken));

        foreach (var entry in bundle.GeneratorEntries)
        {
            db.GeneratorEntries.Add(new GeneratorEntry
            {
                SystemCode = entry.SystemId,
                Kind = ParseKind(entry.Kind),
                Category = entry.Category,
                Value = entry.Value,
                ExtraJson = entry.ExtraJson,
                SortOrder = entry.SortOrder,
                IsEnabled = entry.IsEnabled,
                CreatedAt = now,
                UpdatedAt = now,
            });
        }

        // ---------- 车卡流程：整份替换 ----------
        db.CreationSteps.RemoveRange(await db.CreationSteps.ToListAsync(cancellationToken));

        for (var index = 0; index < bundle.CreationSteps.Count; index += 1)
        {
            var step = bundle.CreationSteps[index];
            db.CreationSteps.Add(new CreationStep
            {
                SystemCode = step.SystemId,
                Kind = ParseStepKind(step.Kind),
                Title = step.Title,
                Description = step.Description,
                SortOrder = (index + 1) * 10,
                IsEnabled = step.IsEnabled,
                CreatedAt = now,
                UpdatedAt = now,
            });
        }

        // ---------- 关键词分类：整份替换 ----------
        db.KeywordCategories.RemoveRange(await db.KeywordCategories.ToListAsync(cancellationToken));

        for (var index = 0; index < bundle.KeywordCategories.Count; index += 1)
        {
            var category = bundle.KeywordCategories[index];
            db.KeywordCategories.Add(new KeywordCategory
            {
                Code = category.Code,
                Name = category.Name,
                SortOrder = (index + 1) * 10,
            });
        }

        // ---------- 关键词：整份替换 ----------
        db.Keywords.RemoveRange(await db.Keywords.ToListAsync(cancellationToken));

        for (var index = 0; index < bundle.Keywords.Count; index += 1)
        {
            var keyword = bundle.Keywords[index];
            db.Keywords.Add(new Keyword
            {
                Code = keyword.Code,
                Key = keyword.Key,
                Category = keyword.Category,
                Title = keyword.Title,
                Summary = keyword.Summary,
                Content = keyword.Content,
                ImageUrl = keyword.ImageUrl,
                SortOrder = (index + 1) * 10,
                IsEnabled = keyword.IsEnabled,
                CreatedAt = now,
                UpdatedAt = now,
            });
        }

        // ---------- 职业等级成长：整份替换 ----------
        var classSystemIds = bundle.GeneratorEntries
            .Where(entry => entry.Kind == "class" && entry.SystemId is not null)
            .GroupBy(entry => entry.Category, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.First().SystemId!,
                StringComparer.OrdinalIgnoreCase);

        db.ClassLevelFeatures.RemoveRange(await db.ClassLevelFeatures.ToListAsync(cancellationToken));

        foreach (var feature in bundle.ClassLevels)
        {
            if (!classSystemIds.TryGetValue(feature.ClassId, out var systemId))
            {
                throw new ContentException($"等级成长引用的职业「{feature.ClassId}」不存在（class-levels.xlsx）。");
            }

            db.ClassLevelFeatures.Add(new ClassLevelFeature
            {
                SystemCode = systemId,
                ClassCode = feature.ClassId,
                Level = feature.Level,
                Title = feature.Title,
                Description = feature.Description,
                SortOrder = feature.SortOrder,
                IsEnabled = feature.IsEnabled,
                CreatedAt = now,
                UpdatedAt = now,
            });
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static GeneratorKind ParseKind(string raw)
    {
        foreach (var kind in Enum.GetValues<GeneratorKind>())
        {
            if (string.Equals(JsonNamingPolicy.CamelCase.ConvertName(kind.ToString()), raw, StringComparison.OrdinalIgnoreCase)
                || string.Equals(kind.ToString(), raw, StringComparison.OrdinalIgnoreCase))
            {
                return kind;
            }
        }

        throw new ContentException($"无法识别的素材类型「{raw}」。");
    }

    private static CreationStepKind ParseStepKind(string raw)
    {
        foreach (var kind in Enum.GetValues<CreationStepKind>())
        {
            if (string.Equals(JsonNamingPolicy.CamelCase.ConvertName(kind.ToString()), raw, StringComparison.OrdinalIgnoreCase)
                || string.Equals(kind.ToString(), raw, StringComparison.OrdinalIgnoreCase))
            {
                return kind;
            }
        }

        throw new ContentException($"无法识别的步骤类型「{raw}」。");
    }
}
