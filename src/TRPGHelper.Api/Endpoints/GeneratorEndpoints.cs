using Microsoft.EntityFrameworkCore;
using TRPGHelper.Api.Contracts;
using TRPGHelper.Api.Services;
using TRPGHelper.Domain.Cards;
using TRPGHelper.Domain.Dice;
using TRPGHelper.Infrastructure.Data;

namespace TRPGHelper.Api.Endpoints;

public static class GeneratorEndpoints
{
    public static IEndpointRouteBuilder MapGeneratorEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/generator")
            .WithTags("Generator")
            .AllowAnonymous();

        group.MapPost("/roll", RollDice)
            .WithSummary("掷骰（支持 4d6kh3 / 3d6*5 / 2d6*5+30 等表达式）");

        group.MapPost("/attributes", GenerateAttributes)
            .WithSummary("按规则系统生成一组初始属性（辅助车卡）");

        group.MapGet("/names", async (string? systemCode, int? count, GeneratorCatalogService catalog, CancellationToken cancellationToken) =>
                Results.Ok(await catalog.NamesAsync(systemCode, count ?? 5, null, cancellationToken)))
            .WithSummary("随机姓名（素材来自管理台配置）");

        group.MapGet("/occupations", async (string? systemCode, int? count, GeneratorCatalogService catalog, CancellationToken cancellationToken) =>
                Results.Ok(await catalog.OccupationsAsync(systemCode, count ?? 5, null, cancellationToken)))
            .WithSummary("随机职业 / 职业与背景");

        group.MapGet("/classes", async (string? systemCode, GeneratorCatalogService catalog, CancellationToken cancellationToken) =>
                Results.Ok(await catalog.ClassesAsync(systemCode, cancellationToken)))
            .WithSummary("职业列表（管理台可维护）");

        group.MapGet("/subclasses", async (string? systemCode, string? classId, GeneratorCatalogService catalog, CancellationToken cancellationToken) =>
                Results.Ok(await catalog.SubclassesAsync(systemCode, classId, cancellationToken)))
            .WithSummary("子职列表（按职业过滤）");

        group.MapGet("/races", async (string? systemCode, GeneratorCatalogService catalog, CancellationToken cancellationToken) =>
                Results.Ok(await catalog.RacesAsync(systemCode, cancellationToken)))
            .WithSummary("种族列表（管理台可维护）");

        group.MapGet("/subraces", async (string? systemCode, string? raceId, GeneratorCatalogService catalog, CancellationToken cancellationToken) =>
                Results.Ok(await catalog.SubracesAsync(systemCode, raceId, cancellationToken)))
            .WithSummary("亚种列表（按种族过滤）");

        group.MapGet("/alignments", async (string? systemCode, GeneratorCatalogService catalog, CancellationToken cancellationToken) =>
                Results.Ok(await catalog.AlignmentsAsync(systemCode, cancellationToken)))
            .WithSummary("阵营列表（九宫格，带图标）");

        group.MapGet("/inspirations", async (string? systemCode, int? count, GeneratorCatalogService catalog, CancellationToken cancellationToken) =>
                Results.Ok(await catalog.InspirationsAsync(systemCode, count ?? 3, null, cancellationToken)))
            .WithSummary("车卡灵感（故事钩子）");

        return app;
    }

    private static IResult RollDice(RollRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Expression))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["expression"] = ["骰子表达式不能为空，例如 d20 或 4d6kh3。"],
            });
        }

        try
        {
            var result = DiceRoller.Roll(request.Expression);
            return Results.Ok(new RollResponse(
                result.Expression,
                result.Total,
                result.Detail,
                result.Terms.Select(term => new RollTermDto(
                    term.Text,
                    term.Rolls,
                    term.KeptRolls,
                    term.Subtotal)).ToList()));
        }
        catch (ArgumentException ex)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["expression"] = [ex.Message],
            });
        }
    }

    private static async Task<IResult> GenerateAttributes(
        GenerateAttributesRequest request,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var system = await ResolveSystemAsync(db, request.SystemId, request.SystemCode, cancellationToken);
        if (system is null)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["system"] = ["找不到对应的规则系统。"],
            });
        }

        var templates = AttributeTemplateParser.Parse(system.AttributeTemplateJson);
        if (templates.Count == 0)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["system"] = ["这个规则系统没有配置属性模板。"],
            });
        }

        try
        {
            var method = string.IsNullOrWhiteSpace(request.Method) ? AttributeGenerator.RollMethod : request.Method;
            var attributes = AttributeGenerator.Generate(templates, method)
                .Select(attribute => new GeneratedAttributeDto(
                    attribute.Key,
                    attribute.Label,
                    attribute.Value,
                    attribute.Formula))
                .ToList();

            return Results.Ok(new GeneratedAttributesResponse(
                system.Id,
                system.Code,
                system.Name,
                method,
                attributes,
                attributes.Sum(attribute => attribute.Value)));
        }
        catch (ArgumentException ex)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["method"] = [ex.Message],
            });
        }
    }

    internal static async Task<Domain.Entities.GameSystem?> ResolveSystemAsync(
        AppDbContext db,
        Guid? systemId,
        string? systemCode,
        CancellationToken cancellationToken)
    {
        if (systemId is { } id)
        {
            return await db.GameSystems.FirstOrDefaultAsync(system => system.Id == id, cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(systemCode))
        {
            var code = systemCode.Trim().ToLowerInvariant();
            return await db.GameSystems.FirstOrDefaultAsync(system => system.Code == code, cancellationToken);
        }

        return null;
    }
}
