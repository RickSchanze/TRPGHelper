using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using TRPGHelper.Api.Contracts;
using TRPGHelper.Api.Extensions;
using TRPGHelper.Domain.Cards;
using TRPGHelper.Domain.Entities;
using TRPGHelper.Domain.Enums;
using TRPGHelper.Infrastructure.Data;

namespace TRPGHelper.Api.Endpoints;

public static class CharacterEndpoints
{
    public static IEndpointRouteBuilder MapCharacterEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/characters")
            .WithTags("Characters")
            .RequireAuthorization();

        group.MapGet("", ListAsync)
            .WithName("ListCharacters")
            .WithSummary("卡库列表（状态 / 规则 / 关键词 / 收藏过滤，分页）");

        group.MapGet("/stats", GetStatsAsync)
            .WithName("CharacterStats")
            .WithSummary("卡库统计（总数、留存数、各状态数量）");

        group.MapPost("", CreateAsync)
            .WithName("CreateCharacter")
            .WithSummary("新建角色卡（不传属性时按规则自动掷一组）");

        group.MapGet("/{id:guid}", GetAsync)
            .WithName("GetCharacter")
            .WithSummary("角色卡详情（含操作记录与可切换状态）");

        group.MapPut("/{id:guid}", UpdateAsync)
            .WithName("UpdateCharacter")
            .WithSummary("更新角色卡资料 / 卡面");

        group.MapDelete("/{id:guid}", DeleteAsync)
            .WithName("DeleteCharacter")
            .WithSummary("删除角色卡");

        group.MapPost("/{id:guid}/status", ChangeStatusAsync)
            .WithName("ChangeCharacterStatus")
            .WithSummary("状态流转：留卡 / 取出 / 使用中 / 归档");

        group.MapPost("/{id:guid}/notes", AddNoteAsync)
            .WithName("AddCharacterNote")
            .WithSummary("给角色卡加一条备注");

        return app;
    }

    private static async Task<IResult> ListAsync(
        AppDbContext db,
        ClaimsPrincipal principal,
        string? status = null,
        string? systemCode = null,
        string? keyword = null,
        bool? favorite = null,
        string? sort = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var userId = principal.GetUserId();
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = db.CharacterCards
            .AsNoTracking()
            .Include(card => card.System)
            .Where(card => card.OwnerId == userId);

        if (!string.IsNullOrWhiteSpace(status) && !status.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            if (!CardStatusParser.TryParse(status, out var parsedStatus))
            {
                return Validation("status", "未知的卡状态。");
            }

            query = query.Where(card => card.Status == parsedStatus);
        }

        if (!string.IsNullOrWhiteSpace(systemCode))
        {
            var code = systemCode.Trim().ToLowerInvariant();
            query = query.Where(card => card.System.Code == code);
        }

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var pattern = $"%{keyword.Trim()}%";
            query = query.Where(card =>
                EF.Functions.Like(card.Name, pattern)
                || (card.Occupation != null && EF.Functions.Like(card.Occupation, pattern))
                || (card.Summary != null && EF.Functions.Like(card.Summary, pattern)));
        }

        if (favorite is true)
        {
            query = query.Where(card => card.IsFavorite);
        }

        query = sort?.Trim().ToLowerInvariant() switch
        {
            "created" or "createdat" => query.OrderByDescending(card => card.CreatedAt),
            "name" => query.OrderBy(card => card.Name),
            "retained" or "retainedat" => query.OrderByDescending(card => card.RetainedAt),
            _ => query.OrderByDescending(card => card.UpdatedAt),
        };

        var total = await query.CountAsync(cancellationToken);
        var cards = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return Results.Ok(new PagedResult<CharacterSummaryDto>(
            cards.Select(Mappings.ToSummary).ToList(),
            total,
            page,
            pageSize));
    }

    private static async Task<IResult> GetStatsAsync(
        AppDbContext db,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var userId = principal.GetUserId();
        var counts = await db.CharacterCards
            .Where(card => card.OwnerId == userId)
            .GroupBy(card => card.Status)
            .Select(group => new { group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);

        int CountOf(CardStatus status) => counts.FirstOrDefault(item => item.Key == status)?.Count ?? 0;

        var createdLast30Days = await db.CharacterCards
            .CountAsync(
                card => card.OwnerId == userId && card.CreatedAt >= DateTime.UtcNow.AddDays(-30),
                cancellationToken);

        return Results.Ok(new CharacterStatsDto(
            counts.Sum(item => item.Count),
            CountOf(CardStatus.Draft),
            CountOf(CardStatus.Ready),
            CountOf(CardStatus.InUse),
            CountOf(CardStatus.Retained),
            CountOf(CardStatus.Archived),
            createdLast30Days));
    }

    private static async Task<IResult> CreateAsync(
        CreateCharacterRequest request,
        AppDbContext db,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var userId = principal.GetUserId();
        var errors = ValidateName(request.Name);
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        if (request.Occupation?.Trim().Length > 40)
        {
            errors["occupation"] = ["职业最多 40 个字符。"];
        }

        if (request.ClassName?.Trim().Length > 40)
        {
            errors["className"] = ["职业最多 40 个字符。"];
        }

        if (request.SubclassName?.Trim().Length > 40)
        {
            errors["subclassName"] = ["子职最多 40 个字符。"];
        }

        if (request.RaceName?.Trim().Length > 40)
        {
            errors["raceName"] = ["种族最多 40 个字符。"];
        }

        if (request.SubraceName?.Trim().Length > 40)
        {
            errors["subraceName"] = ["亚种最多 40 个字符。"];
        }

        if (request.Summary?.Trim().Length > 500)
        {
            errors["summary"] = ["简介最多 500 个字符。"];
        }

        if (request.Age is < 0 or > 9999)
        {
            errors["age"] = ["年龄超出合理范围。"];
        }

        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var system = await GeneratorEndpoints.ResolveSystemAsync(
            db,
            request.SystemId,
            request.SystemCode,
            cancellationToken);

        if (system is null)
        {
            return Validation("system", "找不到对应的规则系统，请先选择规则系统。");
        }

        var initialStatus = CardStatus.Draft;
        if (!string.IsNullOrWhiteSpace(request.Status)
            && !CardStatusParser.TryParse(request.Status, out initialStatus))
        {
            return Validation("status", "未知的卡状态。");
        }

        if (initialStatus is not (CardStatus.Draft or CardStatus.Ready))
        {
            return Validation("status", "新建的卡只能是草稿或待用状态，之后再做流转。");
        }

        var sheet = request.Sheet is null ? new CharacterSheet() : Mappings.FromDto(request.Sheet);
        if (sheet.Attributes.Count == 0)
        {
            var templates = AttributeTemplateParser.Parse(system.AttributeTemplateJson);
            if (templates.Count > 0)
            {
                sheet.Attributes = AttributeGenerator.Generate(templates).ToList();
            }
        }

        var now = DateTime.UtcNow;
        var card = new CharacterCard
        {
            OwnerId = userId,
            SystemId = system.Id,
            System = system,
            Name = request.Name.Trim(),
            Occupation = NormalizeOptional(request.Occupation),
            ClassName = NormalizeOptional(request.ClassName),
            SubclassName = NormalizeOptional(request.SubclassName),
            RaceName = NormalizeOptional(request.RaceName),
            SubraceName = NormalizeOptional(request.SubraceName),
            Age = request.Age,
            Summary = NormalizeOptional(request.Summary),
            PortraitUrl = NormalizeOptional(request.PortraitUrl),
            Status = initialStatus,
            IsFavorite = request.IsFavorite ?? false,
            Sheet = sheet,
            CreatedAt = now,
            UpdatedAt = now,
        };

        card.Logs.Add(new CardLog
        {
            CharacterCardId = card.Id,
            Action = CardAction.Created,
            Note = $"在「{system.Name}」下创建角色卡",
            CreatedAt = now,
        });

        db.CharacterCards.Add(card);
        await db.SaveChangesAsync(cancellationToken);

        return Results.Created($"/api/characters/{card.Id}", Mappings.ToDetail(card, card.Logs));
    }

    private static async Task<IResult> GetAsync(
        Guid id,
        AppDbContext db,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var card = await LoadOwnedAsync(db, id, principal.GetUserId(), cancellationToken);
        if (card is null)
        {
            return Results.NotFound();
        }

        var logs = await LoadLogsAsync(db, card.Id, cancellationToken);
        return Results.Ok(Mappings.ToDetail(card, logs));
    }

    private static async Task<IResult> UpdateAsync(
        Guid id,
        UpdateCharacterRequest request,
        AppDbContext db,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var card = await LoadOwnedAsync(db, id, principal.GetUserId(), cancellationToken);
        if (card is null)
        {
            return Results.NotFound();
        }

        var errors = new Dictionary<string, string[]>();
        if (request.Name is not null)
        {
            errors = ValidateName(request.Name);
        }

        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var changed = false;
        var now = DateTime.UtcNow;

        if (request.Name is not null)
        {
            var name = request.Name.Trim();
            if (name != card.Name)
            {
                card.Name = name;
                changed = true;
            }
        }

        if (request.Occupation is not null)
        {
            var occupation = NormalizeOptional(request.Occupation);
            if (occupation?.Length > 40)
            {
                return Validation("occupation", "职业最多 40 个字符。");
            }

            if (occupation != card.Occupation)
            {
                card.Occupation = occupation;
                changed = true;
            }
        }

        if (request.ClassName is not null)
        {
            var className = NormalizeOptional(request.ClassName);
            if (className?.Length > 40)
            {
                return Validation("className", "职业最多 40 个字符。");
            }

            if (className != card.ClassName)
            {
                card.ClassName = className;
                changed = true;
            }
        }

        if (request.SubclassName is not null)
        {
            var subclassName = NormalizeOptional(request.SubclassName);
            if (subclassName?.Length > 40)
            {
                return Validation("subclassName", "子职最多 40 个字符。");
            }

            if (subclassName != card.SubclassName)
            {
                card.SubclassName = subclassName;
                changed = true;
            }
        }

        if (request.RaceName is not null)
        {
            var raceName = NormalizeOptional(request.RaceName);
            if (raceName?.Length > 40)
            {
                return Validation("raceName", "种族最多 40 个字符。");
            }

            if (raceName != card.RaceName)
            {
                card.RaceName = raceName;
                changed = true;
            }
        }

        if (request.SubraceName is not null)
        {
            var subraceName = NormalizeOptional(request.SubraceName);
            if (subraceName?.Length > 40)
            {
                return Validation("subraceName", "亚种最多 40 个字符。");
            }

            if (subraceName != card.SubraceName)
            {
                card.SubraceName = subraceName;
                changed = true;
            }
        }

        if (request.Age is not null && request.Age != card.Age)
        {
            if (request.Age is < 0 or > 9999)
            {
                return Validation("age", "年龄超出合理范围。");
            }

            card.Age = request.Age;
            changed = true;
        }

        if (request.Summary is not null)
        {
            var summary = NormalizeOptional(request.Summary);
            if (summary?.Length > 500)
            {
                return Validation("summary", "简介最多 500 个字符。");
            }

            if (summary != card.Summary)
            {
                card.Summary = summary;
                changed = true;
            }
        }

        if (request.PortraitUrl is not null)
        {
            var portrait = NormalizeOptional(request.PortraitUrl);
            if (portrait != card.PortraitUrl)
            {
                card.PortraitUrl = portrait;
                changed = true;
            }
        }

        if (request.IsFavorite is { } favorite && favorite != card.IsFavorite)
        {
            card.IsFavorite = favorite;
            changed = true;
        }

        if (request.Sheet is not null)
        {
            card.Sheet = Mappings.FromDto(request.Sheet);
            changed = true;
        }

        if (!changed)
        {
            return Results.Ok(Mappings.ToDetail(card, await LoadLogsAsync(db, card.Id, cancellationToken)));
        }

        card.UpdatedAt = now;
        db.CardLogs.Add(new CardLog
        {
            CharacterCardId = card.Id,
            Action = CardAction.Updated,
            Note = "更新角色卡资料",
            CreatedAt = now,
        });

        await db.SaveChangesAsync(cancellationToken);
        var logs = await LoadLogsAsync(db, card.Id, cancellationToken);
        return Results.Ok(Mappings.ToDetail(card, logs));
    }

    private static async Task<IResult> ChangeStatusAsync(
        Guid id,
        ChangeStatusRequest request,
        AppDbContext db,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        if (!CardStatusParser.TryParse(request.Status, out var target))
        {
            return Validation("status", "未知的卡状态。");
        }

        var card = await LoadOwnedAsync(db, id, principal.GetUserId(), cancellationToken);
        if (card is null)
        {
            return Results.NotFound();
        }

        if (!CardRules.CanTransition(card.Status, target))
        {
            return Results.Problem(
                $"不能从「{card.Status.ToLabel()}」切换到「{target.ToLabel()}」。",
                statusCode: StatusCodes.Status409Conflict);
        }

        var from = card.Status;
        var now = DateTime.UtcNow;

        card.Status = target;
        card.UpdatedAt = now;
        if (target == CardStatus.Retained)
        {
            card.RetainedAt = now;
        }
        else if (from == CardStatus.Retained)
        {
            card.RetainedAt = null;
        }

        var note = string.IsNullOrWhiteSpace(request.Note)
            ? $"{from.ToLabel()} → {target.ToLabel()}"
            : request.Note.Trim();

        db.CardLogs.Add(new CardLog
        {
            CharacterCardId = card.Id,
            Action = CardRules.ActionFor(from, target),
            Note = note.Length > 500 ? note[..500] : note,
            CreatedAt = now,
        });

        await db.SaveChangesAsync(cancellationToken);
        var logs = await LoadLogsAsync(db, card.Id, cancellationToken);
        return Results.Ok(Mappings.ToDetail(card, logs));
    }

    private static async Task<IResult> AddNoteAsync(
        Guid id,
        AddNoteRequest request,
        AppDbContext db,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Note))
        {
            return Validation("note", "备注不能为空。");
        }

        var card = await LoadOwnedAsync(db, id, principal.GetUserId(), cancellationToken);
        if (card is null)
        {
            return Results.NotFound();
        }

        var note = request.Note.Trim();
        if (note.Length > 500)
        {
            return Validation("note", "备注最多 500 个字符。");
        }

        var now = DateTime.UtcNow;
        db.CardLogs.Add(new CardLog
        {
            CharacterCardId = card.Id,
            Action = CardAction.Note,
            Note = note,
            CreatedAt = now,
        });

        await db.SaveChangesAsync(cancellationToken);
        var logs = await LoadLogsAsync(db, card.Id, cancellationToken);
        return Results.Ok(Mappings.ToDetail(card, logs));
    }

    private static async Task<IResult> DeleteAsync(
        Guid id,
        AppDbContext db,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var card = await db.CharacterCards
            .FirstOrDefaultAsync(card => card.Id == id && card.OwnerId == principal.GetUserId(), cancellationToken);

        if (card is null)
        {
            return Results.NotFound();
        }

        db.CharacterCards.Remove(card);
        await db.SaveChangesAsync(cancellationToken);
        return Results.NoContent();
    }

    private static Task<CharacterCard?> LoadOwnedAsync(
        AppDbContext db,
        Guid id,
        Guid ownerId,
        CancellationToken cancellationToken)
        => db.CharacterCards
            .Include(card => card.System)
            .FirstOrDefaultAsync(card => card.Id == id && card.OwnerId == ownerId, cancellationToken);

    private static Task<List<CardLog>> LoadLogsAsync(
        AppDbContext db,
        Guid characterId,
        CancellationToken cancellationToken)
        => db.CardLogs
            .AsNoTracking()
            .Where(log => log.CharacterCardId == characterId)
            .OrderByDescending(log => log.CreatedAt)
            .Take(100)
            .ToListAsync(cancellationToken);

    private static Dictionary<string, string[]> ValidateName(string? name)
    {
        var errors = new Dictionary<string, string[]>();
        var trimmed = name?.Trim() ?? string.Empty;

        if (trimmed.Length == 0)
        {
            errors["name"] = ["角色名不能为空。"];
        }
        else if (trimmed.Length > 40)
        {
            errors["name"] = ["角色名最多 40 个字符。"];
        }

        return errors;
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static IResult Validation(string field, string message)
        => Results.ValidationProblem(new Dictionary<string, string[]> { [field] = [message] });
}
