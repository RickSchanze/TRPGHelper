using Microsoft.EntityFrameworkCore;
using TRPGHelper.Api.Contracts;
using TRPGHelper.Domain.Entities;
using TRPGHelper.Infrastructure.Data;

namespace TRPGHelper.Api.Endpoints;

/// <summary>
/// 职业详情：某个职业 1-20 级分别获得什么。
/// 数据来自 ClassLevelFeatures（管理台可维护），副职特性暂未接入。
/// </summary>
public static class ClassLevelEndpoints
{
    private const string DefaultSystemCode = "dnd5e";

    public static IEndpointRouteBuilder MapClassLevelEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/classes/{classCode}/levels", async (
                string classCode,
                string? systemCode,
                AppDbContext db,
                CancellationToken cancellationToken) =>
            {
                var code = NormalizeSystemCode(systemCode);
                var normalizedClassCode = classCode.Trim().ToLowerInvariant();

                var classEntry = await db.GeneratorEntries
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        entry => entry.SystemCode == code
                            && entry.Kind == GeneratorKind.Class
                            && entry.Category == normalizedClassCode,
                        cancellationToken);

                if (classEntry is null)
                {
                    return Results.NotFound(new { message = $"找不到职业「{classCode}」。" });
                }

                var features = await db.ClassLevelFeatures
                    .AsNoTracking()
                    .Where(feature => feature.SystemCode == code
                        && feature.ClassCode == normalizedClassCode
                        && feature.IsEnabled)
                    .OrderBy(feature => feature.Level)
                    .ThenBy(feature => feature.SortOrder)
                    .ToListAsync(cancellationToken);

                return Results.Ok(new ClassLevelsDto(
                    code,
                    normalizedClassCode,
                    classEntry.Value,
                    features.Select(ToDto).ToList()));
            })
            .WithTags("Classes")
            .WithSummary("职业等级成长（1-20 级，管理台可配置）")
            .AllowAnonymous();

        return app;
    }

    private static string NormalizeSystemCode(string? systemCode)
        => string.IsNullOrWhiteSpace(systemCode) ? DefaultSystemCode : systemCode.Trim().ToLowerInvariant();

    private static ClassLevelFeatureDto ToDto(ClassLevelFeature feature)
        => new(feature.Level, feature.Title, feature.Description);
}
