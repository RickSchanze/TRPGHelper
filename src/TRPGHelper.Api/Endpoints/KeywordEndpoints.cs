using Microsoft.EntityFrameworkCore;
using TRPGHelper.Api.Contracts;
using TRPGHelper.Infrastructure.Data;

namespace TRPGHelper.Api.Endpoints;

/// <summary>
/// 关键词词条：前端文本里 [关键词] 被点击时，用这里的解释弹窗展示。
/// 没有对应词条的关键词在前端会原样显示 [关键词]。
/// </summary>
public static class KeywordEndpoints
{
    public static IEndpointRouteBuilder MapKeywordEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/keywords", async (AppDbContext db, CancellationToken cancellationToken) =>
            {
                var keywords = await db.Keywords
                    .AsNoTracking()
                    .Where(keyword => keyword.IsEnabled)
                    .OrderBy(keyword => keyword.SortOrder)
                    .ThenBy(keyword => keyword.Key)
                    .ToListAsync(cancellationToken);

                return Results.Ok(keywords
                    .Select(keyword => new KeywordDto(keyword.Key, keyword.Title, keyword.Content, keyword.ImageUrl))
                    .ToList());
            })
            .WithTags("Keywords")
            .WithSummary("关键词词条（启用中的，供前端弹窗）")
            .AllowAnonymous();

        return app;
    }
}
