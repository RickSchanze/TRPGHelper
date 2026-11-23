using Microsoft.EntityFrameworkCore;
using TRPGHelper.Infrastructure.Data;

namespace TRPGHelper.Api.Endpoints;

public static class SystemEndpoints
{
    public static IEndpointRouteBuilder MapSystemEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/systems", async (AppDbContext db, CancellationToken cancellationToken) =>
        {
            var systems = await db.GameSystems
                .AsNoTracking()
                .OrderBy(system => system.SortOrder)
                .ThenBy(system => system.Name)
                .ToListAsync(cancellationToken);

            return Results.Ok(systems.Select(Mappings.ToDto).ToList());
        })
        .WithTags("Systems")
        .WithSummary("规则系统列表（含属性模板）")
        .AllowAnonymous();

        return app;
    }
}
