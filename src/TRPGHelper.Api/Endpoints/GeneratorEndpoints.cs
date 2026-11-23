using Microsoft.EntityFrameworkCore;
using TRPGHelper.Api.Contracts;
using TRPGHelper.Api.Data;
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

        group.MapGet("/names", (string? systemCode, int? count) =>
                Results.Ok(GeneratorData.Names(systemCode, count ?? 5)))
            .WithSummary("随机姓名");

        group.MapGet("/occupations", (string? systemCode, int? count) =>
                Results.Ok(GeneratorData.Occupations(systemCode, count ?? 5)))
            .WithSummary("随机职业 / 职业与背景");

        group.MapGet("/inspirations", (string? systemCode, int? count) =>
            {
                var items = GeneratorData.Inspirations(systemCode, count ?? 3)
                    .Select(item => new InspirationDto(item.Title, item.Text))
                    .ToList();
                return Results.Ok(items);
            })
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
