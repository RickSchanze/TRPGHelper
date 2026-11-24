using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TRPGHelper.Api.Contracts;
using TRPGHelper.Api.Services;
using TRPGHelper.Infrastructure.Data;

namespace TRPGHelper.Api.Endpoints;

/// <summary>
/// 车卡流程：由管理台配置步骤数量和顺序，前端按步骤渲染。
/// 选规则书（DND / COC）是流程的前置条件，不算步骤，所以不在这里返回。
/// </summary>
public static class CreationFlowEndpoints
{
    public static IEndpointRouteBuilder MapCreationFlowEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/creation-flow", async (
                string? systemCode,
                AppDbContext db,
                GeneratorCatalogService catalog,
                CancellationToken cancellationToken) =>
            {
                var system = await catalog.ResolveSystemAsync(systemCode, cancellationToken);
                var steps = await db.CreationSteps
                    .AsNoTracking()
                    .Where(step => step.IsEnabled
                        && (step.SystemCode == null || (system != null && step.SystemCode == system.Code)))
                    .OrderBy(step => step.SortOrder)
                    .ThenBy(step => step.Title)
                    .ToListAsync(cancellationToken);

                return Results.Ok(new CreationFlowDto(
                    system?.Code,
                    system?.Name,
                    steps.Select(step => new CreationStepDto(
                        step.Id.ToString(),
                        JsonNamingPolicy.CamelCase.ConvertName(step.Kind.ToString()),
                        step.Title,
                        step.Description)).ToList()));
            })
            .WithTags("CreationFlow")
            .WithSummary("车卡流程配置（步骤列表，规则书选择不计入步骤）")
            .AllowAnonymous();

        return app;
    }
}
