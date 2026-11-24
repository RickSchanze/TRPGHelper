using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using TRPGHelper.Api.Contracts;
using TRPGHelper.Api.Services;
using TRPGHelper.Domain.Cards;
using TRPGHelper.Domain.Dice;
using TRPGHelper.Domain.Entities;
using TRPGHelper.Infrastructure.Content;
using TRPGHelper.Infrastructure.Data;

namespace TRPGHelper.Api.Endpoints;

/// <summary>
/// 管理台接口：规则系统、属性模板、生成器素材的增删改查。
/// 全部要求 Admin 角色（见 <see cref="PolicyName"/>）。
/// </summary>
public static class AdminEndpoints
{
    public const string PolicyName = "Admin";

    private const string DefaultNameTemplate = "{surname}{given}";

    private const long MaxUploadBytes = 5 * 1024 * 1024;

    private static readonly string[] AllowedImageExtensions = [".jpg", ".jpeg", ".png", ".webp", ".gif"];

    private static readonly Regex CodeRegex = new("^[a-z0-9][a-z0-9_-]{1,31}$", RegexOptions.Compiled);

    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin")
            .WithTags("Admin")
            .RequireAuthorization(PolicyName);

        group.MapGet("/overview", GetOverviewAsync)
            .WithName("AdminOverview")
            .WithSummary("管理台概览：系统 / 素材 / 用户 / 卡数量");

        group.MapGet("/systems", ListSystemsAsync)
            .WithName("AdminListSystems")
            .WithSummary("规则系统列表（含属性模板）");

        group.MapPost("/systems", CreateSystemAsync)
            .WithName("AdminCreateSystem")
            .WithSummary("新建规则系统");

        group.MapPut("/systems/{id:guid}", UpdateSystemAsync)
            .WithName("AdminUpdateSystem")
            .WithSummary("更新规则系统（含属性模板）");

        group.MapDelete("/systems/{id:guid}", DeleteSystemAsync)
            .WithName("AdminDeleteSystem")
            .WithSummary("删除规则系统（有卡引用时拒绝）");

        group.MapGet("/generator", ListEntriesAsync)
            .WithName("AdminListGeneratorEntries")
            .WithSummary("生成器素材列表（可按系统 / 类型 / 关键字 / 启停筛选）");

        group.MapPost("/generator", CreateEntryAsync)
            .WithName("AdminCreateGeneratorEntry")
            .WithSummary("新增生成器素材");

        group.MapPut("/generator/{id:guid}", UpdateEntryAsync)
            .WithName("AdminUpdateGeneratorEntry")
            .WithSummary("更新生成器素材");

        group.MapDelete("/generator/{id:guid}", DeleteEntryAsync)
            .WithName("AdminDeleteGeneratorEntry")
            .WithSummary("删除生成器素材");

        group.MapGet("/creation-steps", ListCreationStepsAsync)
            .WithName("AdminListCreationSteps")
            .WithSummary("车卡流程步骤列表");

        group.MapPost("/creation-steps", CreateCreationStepAsync)
            .WithName("AdminCreateCreationStep")
            .WithSummary("新增车卡流程步骤");

        group.MapPut("/creation-steps/{id:guid}", UpdateCreationStepAsync)
            .WithName("AdminUpdateCreationStep")
            .WithSummary("更新车卡流程步骤");

        group.MapDelete("/creation-steps/{id:guid}", DeleteCreationStepAsync)
            .WithName("AdminDeleteCreationStep")
            .WithSummary("删除车卡流程步骤");

        group.MapPost("/assets", UploadAssetAsync)
            .WithName("AdminUploadAsset")
            .WithSummary("上传图片（职业卡配图等），返回可访问地址")
            .DisableAntiforgery();

        group.MapGet("/keywords", ListKeywordsAsync)
            .WithName("AdminListKeywords")
            .WithSummary("关键词词条列表");

        group.MapGet("/keyword-categories", ListKeywordCategoriesAsync)
            .WithName("AdminListKeywordCategories")
            .WithSummary("关键词分类编号列表（分类编号 + 名称）");

        group.MapPost("/keywords", CreateKeywordAsync)
            .WithName("AdminCreateKeyword")
            .WithSummary("新增关键词词条");

        group.MapPut("/keywords/{id:guid}", UpdateKeywordAsync)
            .WithName("AdminUpdateKeyword")
            .WithSummary("更新关键词词条");

        group.MapDelete("/keywords/{id:guid}", DeleteKeywordAsync)
            .WithName("AdminDeleteKeyword")
            .WithSummary("删除关键词词条");

        group.MapGet("/classes/{classCode}/levels", GetClassLevelsAsync)
            .WithName("AdminGetClassLevels")
            .WithSummary("职业等级成长（含停用项）");

        group.MapPut("/classes/{classCode}/levels", SaveClassLevelsAsync)
            .WithName("AdminSaveClassLevels")
            .WithSummary("整份替换职业等级成长");

        group.MapPost("/content/export", ExportContentAsync)
            .WithName("AdminExportContent")
            .WithSummary("把当前内容导出成 data/ 下的人类可读 JSON");

        return app;
    }

    private static async Task<IResult> GetOverviewAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var byKind = await db.GeneratorEntries
            .AsNoTracking()
            .GroupBy(entry => entry.Kind)
            .Select(group => new { Kind = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);

        var counts = byKind.ToDictionary(item => item.Kind, item => item.Count);

        return Results.Ok(new AdminOverviewDto(
            await db.GameSystems.CountAsync(cancellationToken),
            await db.GeneratorEntries.CountAsync(cancellationToken),
            await db.GeneratorEntries.CountAsync(entry => entry.IsEnabled, cancellationToken),
            await db.Users.CountAsync(cancellationToken),
            await db.Users.CountAsync(user => user.Role == UserRoles.Admin, cancellationToken),
            await db.CharacterCards.CountAsync(cancellationToken),
            Enum.GetValues<GeneratorKind>()
                .Select(kind => new AdminKindCountDto(ToKindKey(kind), counts.GetValueOrDefault(kind)))
                .ToList(),
            DateTimeOffset.UtcNow));
    }

    private static async Task<IResult> ListSystemsAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var systems = await db.GameSystems
            .AsNoTracking()
            .OrderBy(system => system.SortOrder)
            .ThenBy(system => system.Name)
            .ToListAsync(cancellationToken);

        return Results.Ok(systems.Select(ToAdminSystemDto).ToList());
    }

    private static async Task<IResult> CreateSystemAsync(
        SaveSystemRequest request,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var error = ValidateSystem(request, out var normalized);
        if (error is not null || normalized is null)
        {
            return error!;
        }

        if (await db.GameSystems.AnyAsync(system => system.Code == normalized.Code, cancellationToken))
        {
            return Results.Conflict(new { message = $"规则系统 code「{normalized.Code}」已存在。" });
        }

        var system = new GameSystem
        {
            Code = normalized.Code,
            Name = normalized.Name,
            Description = normalized.Description,
            NameTemplate = normalized.NameTemplate,
            SortOrder = normalized.SortOrder,
            AttributeTemplateJson = AttributeTemplateParser.Serialize(normalized.Attributes),
            IsBuiltin = false,
        };

        db.GameSystems.Add(system);
        await db.SaveChangesAsync(cancellationToken);

        return Results.Created($"/api/admin/systems/{system.Id}", ToAdminSystemDto(system));
    }

    private static async Task<IResult> UpdateSystemAsync(
        Guid id,
        SaveSystemRequest request,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var system = await db.GameSystems.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (system is null)
        {
            return Results.NotFound();
        }

        var error = ValidateSystem(request, out var normalized);
        if (error is not null || normalized is null)
        {
            return error!;
        }

        if (!string.Equals(system.Code, normalized.Code, StringComparison.Ordinal))
        {
            if (await db.GameSystems.AnyAsync(item => item.Code == normalized.Code && item.Id != id, cancellationToken))
            {
                return Results.Conflict(new { message = $"规则系统 code「{normalized.Code}」已存在。" });
            }

            // code 是生成器素材的归属键，改的时候要一起搬过去。
            var oldCode = system.Code;
            await db.GeneratorEntries
                .Where(entry => entry.SystemCode == oldCode)
                .ExecuteUpdateAsync(setters => setters.SetProperty(entry => entry.SystemCode, normalized.Code), cancellationToken);
        }

        system.Code = normalized.Code;
        system.Name = normalized.Name;
        system.Description = normalized.Description;
        system.NameTemplate = normalized.NameTemplate;
        system.SortOrder = normalized.SortOrder;
        system.AttributeTemplateJson = AttributeTemplateParser.Serialize(normalized.Attributes);

        await db.SaveChangesAsync(cancellationToken);
        return Results.Ok(ToAdminSystemDto(system));
    }

    private static async Task<IResult> DeleteSystemAsync(Guid id, AppDbContext db, CancellationToken cancellationToken)
    {
        var system = await db.GameSystems.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (system is null)
        {
            return Results.NotFound();
        }

        var usedByCards = await db.CharacterCards.AnyAsync(card => card.SystemId == id, cancellationToken);
        if (usedByCards)
        {
            return Results.Conflict(new { message = "这个规则系统下还有卡，先转移或删除这些卡再删系统。" });
        }

        var removedEntries = await db.GeneratorEntries
            .Where(entry => entry.SystemCode == system.Code)
            .ExecuteDeleteAsync(cancellationToken);

        db.GameSystems.Remove(system);
        await db.SaveChangesAsync(cancellationToken);

        return Results.Ok(new { deleted = system.Id, removedGeneratorEntries = removedEntries });
    }

    private static async Task<IResult> ListEntriesAsync(
        AppDbContext db,
        string? systemCode,
        string? kind,
        string? keyword,
        bool? enabled,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken)
    {
        var query = db.GeneratorEntries.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(systemCode))
        {
            var code = systemCode.Trim().ToLowerInvariant();
            query = query.Where(entry => entry.SystemCode == code);
        }

        if (!string.IsNullOrWhiteSpace(kind))
        {
            if (!TryParseKind(kind, out var parsedKind))
            {
                return InvalidKind(kind);
            }

            query = query.Where(entry => entry.Kind == parsedKind);
        }

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var value = keyword.Trim();
            query = query.Where(entry => entry.Value.Contains(value) || entry.Category.Contains(value));
        }

        if (enabled is { } isEnabled)
        {
            query = query.Where(entry => entry.IsEnabled == isEnabled);
        }

        var total = await query.CountAsync(cancellationToken);
        var pageIndex = Math.Max(1, page ?? 1);
        var size = Math.Clamp(pageSize ?? 50, 1, 200);

        var items = await query
            .OrderBy(entry => entry.SystemCode)
            .ThenBy(entry => entry.Kind)
            .ThenBy(entry => entry.SortOrder)
            .ThenBy(entry => entry.Value)
            .Skip((pageIndex - 1) * size)
            .Take(size)
            .ToListAsync(cancellationToken);

        return Results.Ok(new PagedResult<AdminGeneratorEntryDto>(
            items.Select(ToDto).ToList(),
            total,
            pageIndex,
            size));
    }

    private static async Task<IResult> CreateEntryAsync(
        SaveGeneratorEntryRequest request,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var error = await ValidateEntryAsync(request, db, null, cancellationToken);
        if (error is not null)
        {
            return error;
        }

        var now = DateTime.UtcNow;
        var entry = new GeneratorEntry
        {
            SystemCode = NormalizeSystemCode(request.SystemCode),
            Kind = ParseKind(request.Kind),
            Category = request.Category!.Trim(),
            Value = request.Value!.Trim(),
            ExtraJson = NormalizeExtra(request.Extra),
            SortOrder = request.SortOrder,
            IsEnabled = request.IsEnabled,
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.GeneratorEntries.Add(entry);
        await db.SaveChangesAsync(cancellationToken);

        return Results.Created($"/api/admin/generator/{entry.Id}", ToDto(entry));
    }

    private static async Task<IResult> UpdateEntryAsync(
        Guid id,
        SaveGeneratorEntryRequest request,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var entry = await db.GeneratorEntries.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (entry is null)
        {
            return Results.NotFound();
        }

        var error = await ValidateEntryAsync(request, db, id, cancellationToken);
        if (error is not null)
        {
            return error;
        }

        entry.SystemCode = NormalizeSystemCode(request.SystemCode);
        entry.Kind = ParseKind(request.Kind);
        entry.Category = request.Category!.Trim();
        entry.Value = request.Value!.Trim();
        entry.ExtraJson = NormalizeExtra(request.Extra);
        entry.SortOrder = request.SortOrder;
        entry.IsEnabled = request.IsEnabled;
        entry.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        return Results.Ok(ToDto(entry));
    }

    private static async Task<IResult> DeleteEntryAsync(Guid id, AppDbContext db, CancellationToken cancellationToken)
    {
        var entry = await db.GeneratorEntries.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (entry is null)
        {
            return Results.NotFound();
        }

        db.GeneratorEntries.Remove(entry);
        await db.SaveChangesAsync(cancellationToken);

        return Results.Ok(new { deleted = id });
    }

    private static IResult? ValidateSystem(SaveSystemRequest request, out NormalizedSystem? normalized)
    {
        normalized = null;
        var errors = new Dictionary<string, string[]>();

        var code = string.IsNullOrWhiteSpace(request.Code) ? string.Empty : request.Code.Trim().ToLowerInvariant();
        if (!CodeRegex.IsMatch(code))
        {
            errors["code"] = ["code 只能是 2-32 位小写字母、数字、下划线或短横线，且以字母或数字开头。"];
        }

        var name = string.IsNullOrWhiteSpace(request.Name) ? string.Empty : request.Name.Trim();
        if (name.Length is 0 or > 128)
        {
            errors["name"] = ["名称不能为空，最多 128 个字符。"];
        }

        var nameTemplate = string.IsNullOrWhiteSpace(request.NameTemplate)
            ? DefaultNameTemplate
            : request.NameTemplate.Trim();
        if (nameTemplate.Length > 128)
        {
            errors["nameTemplate"] = ["姓名模板最多 128 个字符。"];
        }

        var attributes = new List<AttributeTemplate>();
        foreach (var attribute in request.Attributes ?? [])
        {
            var key = attribute.Key?.Trim() ?? string.Empty;
            var label = attribute.Label?.Trim() ?? string.Empty;
            var formula = attribute.Formula?.Trim() ?? string.Empty;

            if (key.Length == 0 || label.Length == 0 || formula.Length == 0)
            {
                errors["attributes"] = ["属性模板的 key / label / formula 都不能为空。"];
                break;
            }

            try
            {
                DiceRoller.Roll(formula);
            }
            catch (ArgumentException ex)
            {
                errors["attributes"] = [$"属性「{label}」的公式 `{formula}` 无法解析：{ex.Message}"];
                break;
            }

            attributes.Add(new AttributeTemplate(key, label, formula));
        }

        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        normalized = new NormalizedSystem(
            code,
            name,
            string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            nameTemplate,
            request.SortOrder,
            attributes);

        return null;
    }

    private static async Task<IResult?> ValidateEntryAsync(
        SaveGeneratorEntryRequest request,
        AppDbContext db,
        Guid? currentId,
        CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();

        if (!TryParseKind(request.Kind, out var kind))
        {
            return InvalidKind(request.Kind);
        }

        var category = request.Category?.Trim() ?? string.Empty;
        if (category.Length is 0 or > 32)
        {
            errors["category"] = ["分类不能为空，最多 32 个字符。"];
        }

        var value = request.Value?.Trim() ?? string.Empty;
        if (value.Length is 0 or > 256)
        {
            errors["value"] = ["内容不能为空，最多 256 个字符。"];
        }

        var systemCode = NormalizeSystemCode(request.SystemCode);
        if (systemCode is not null
            && !await db.GameSystems.AnyAsync(system => system.Code == systemCode, cancellationToken))
        {
            errors["systemCode"] = [$"规则系统「{systemCode}」不存在。"];
        }

        if (request.Extra is { ValueKind: not JsonValueKind.Object and not JsonValueKind.Null })
        {
            errors["extra"] = ["扩展字段必须是一个 JSON 对象。"];
        }

        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        // 同一系统、同一类型下不允许重复内容（姓名片段按分类区分）。
        var duplicate = await db.GeneratorEntries.AnyAsync(
            entry => entry.SystemCode == systemCode
                && entry.Kind == kind
                && entry.Category == category
                && entry.Value == value
                && (currentId == null || entry.Id != currentId),
            cancellationToken);

        if (duplicate)
        {
            return Results.Conflict(new { message = "同一系统 / 类型 / 分类下已经有相同内容了。" });
        }

        return null;
    }

    private static IResult InvalidKind(string? kind)
        => Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["kind"] = [$"不支持的类型「{kind}」，可用值：{string.Join(" / ", Enum.GetValues<GeneratorKind>().Select(ToKindKey))}。"],
        });

    private static bool TryParseKind(string? raw, out GeneratorKind kind)
    {
        kind = default;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        foreach (var candidate in Enum.GetValues<GeneratorKind>())
        {
            if (string.Equals(ToKindKey(candidate), raw.Trim(), StringComparison.OrdinalIgnoreCase)
                || string.Equals(candidate.ToString(), raw.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                kind = candidate;
                return true;
            }
        }

        return false;
    }

    private static GeneratorKind ParseKind(string? raw)
        => TryParseKind(raw, out var kind) ? kind : GeneratorKind.NamePart;

    private static string ToKindKey(GeneratorKind kind)
        => JsonNamingPolicy.CamelCase.ConvertName(kind.ToString());

    private static string? NormalizeSystemCode(string? systemCode)
        => string.IsNullOrWhiteSpace(systemCode) ? null : systemCode.Trim().ToLowerInvariant();

    private static string? NormalizeExtra(JsonElement? extra)
        => extra is { ValueKind: JsonValueKind.Object } value && value.EnumerateObject().Any()
            ? value.GetRawText()
            : null;

    private static AdminSystemDto ToAdminSystemDto(GameSystem system) => new(
        system.Id,
        system.Code,
        system.Name,
        system.Description,
        system.NameTemplate,
        system.IsBuiltin,
        system.SortOrder,
        AttributeTemplateParser.Parse(system.AttributeTemplateJson)
            .Select(template => new AttributeTemplateDto(template.Key, template.Label, template.Formula))
            .ToList());

    private static AdminGeneratorEntryDto ToDto(GeneratorEntry entry) => new(
        entry.Id,
        entry.SystemCode,
        ToKindKey(entry.Kind),
        entry.Category,
        entry.Value,
        GeneratorCatalogService.ParseExtra(entry.ExtraJson),
        entry.SortOrder,
        entry.IsEnabled,
        new DateTimeOffset(DateTime.SpecifyKind(entry.CreatedAt, DateTimeKind.Utc)),
        new DateTimeOffset(DateTime.SpecifyKind(entry.UpdatedAt, DateTimeKind.Utc)));

    private static async Task<IResult> ListCreationStepsAsync(
        AppDbContext db,
        string? systemCode,
        CancellationToken cancellationToken)
    {
        var query = db.CreationSteps.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(systemCode))
        {
            var code = systemCode.Trim().ToLowerInvariant();
            query = query.Where(step => step.SystemCode == code);
        }

        var steps = await query
            .OrderBy(step => step.SortOrder)
            .ThenBy(step => step.Title)
            .ToListAsync(cancellationToken);

        return Results.Ok(steps.Select(ToAdminStepDto).ToList());
    }

    private static async Task<IResult> CreateCreationStepAsync(
        SaveCreationStepRequest request,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var error = await ValidateCreationStepAsync(request, db, cancellationToken);
        if (error is not null)
        {
            return error;
        }

        var now = DateTime.UtcNow;
        var step = new CreationStep
        {
            SystemCode = NormalizeSystemCode(request.SystemCode),
            Kind = ParseStepKind(request.Kind),
            Title = request.Title!.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            SortOrder = request.SortOrder,
            IsEnabled = request.IsEnabled,
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.CreationSteps.Add(step);
        await db.SaveChangesAsync(cancellationToken);

        return Results.Created($"/api/admin/creation-steps/{step.Id}", ToAdminStepDto(step));
    }

    private static async Task<IResult> UpdateCreationStepAsync(
        Guid id,
        SaveCreationStepRequest request,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var step = await db.CreationSteps.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (step is null)
        {
            return Results.NotFound();
        }

        var error = await ValidateCreationStepAsync(request, db, cancellationToken);
        if (error is not null)
        {
            return error;
        }

        step.SystemCode = NormalizeSystemCode(request.SystemCode);
        step.Kind = ParseStepKind(request.Kind);
        step.Title = request.Title!.Trim();
        step.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        step.SortOrder = request.SortOrder;
        step.IsEnabled = request.IsEnabled;
        step.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        return Results.Ok(ToAdminStepDto(step));
    }

    private static async Task<IResult> DeleteCreationStepAsync(
        Guid id,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var step = await db.CreationSteps.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (step is null)
        {
            return Results.NotFound();
        }

        db.CreationSteps.Remove(step);
        await db.SaveChangesAsync(cancellationToken);

        return Results.Ok(new { deleted = id });
    }

    private static async Task<IResult> UploadAssetAsync(
        IFormFile? file,
        IWebHostEnvironment environment,
        HttpRequest request,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["file"] = ["没有收到文件，字段名应该是 file。"],
            });
        }

        if (file.Length > MaxUploadBytes)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["file"] = ["图片不能超过 5 MB。"],
            });
        }

        var extension = Path.GetExtension(file.FileName ?? string.Empty).ToLowerInvariant();
        if (!AllowedImageExtensions.Contains(extension))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["file"] = [$"只支持图片：{string.Join(" / ", AllowedImageExtensions)}。"],
            });
        }

        var uploadsDirectory = Path.Combine(environment.ContentRootPath, "App_Data", "uploads");
        Directory.CreateDirectory(uploadsDirectory);

        var fileName = $"{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}{extension}";
        await using (var stream = File.Create(Path.Combine(uploadsDirectory, fileName)))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        return Results.Ok(new UploadAssetResponse($"{request.Scheme}://{request.Host}/uploads/{fileName}"));
    }

    private static async Task<IResult?> ValidateCreationStepAsync(
        SaveCreationStepRequest request,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        if (!TryParseStepKind(request.Kind, out _))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["kind"] = [$"不支持的步骤类型「{request.Kind}」，可用值：{string.Join(" / ", Enum.GetValues<CreationStepKind>().Select(ToStepKey))}。"],
            });
        }

        var errors = new Dictionary<string, string[]>();
        var title = request.Title?.Trim() ?? string.Empty;
        if (title.Length is 0 or > 64)
        {
            errors["title"] = ["标题不能为空，最多 64 个字符。"];
        }

        if (request.Description?.Trim().Length > 512)
        {
            errors["description"] = ["说明最多 512 个字符。"];
        }

        var systemCode = NormalizeSystemCode(request.SystemCode);
        if (systemCode is not null
            && !await db.GameSystems.AnyAsync(system => system.Code == systemCode, cancellationToken))
        {
            errors["systemCode"] = [$"规则系统「{systemCode}」不存在。"];
        }

        return errors.Count > 0 ? Results.ValidationProblem(errors) : null;
    }

    private static AdminCreationStepDto ToAdminStepDto(CreationStep step) => new(
        step.Id,
        step.SystemCode,
        ToStepKey(step.Kind),
        step.Title,
        step.Description,
        step.SortOrder,
        step.IsEnabled,
        new DateTimeOffset(DateTime.SpecifyKind(step.CreatedAt, DateTimeKind.Utc)),
        new DateTimeOffset(DateTime.SpecifyKind(step.UpdatedAt, DateTimeKind.Utc)));

    private static string ToStepKey(CreationStepKind kind)
        => JsonNamingPolicy.CamelCase.ConvertName(kind.ToString());

    private static bool TryParseStepKind(string? raw, out CreationStepKind kind)
    {
        kind = default;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        foreach (var candidate in Enum.GetValues<CreationStepKind>())
        {
            if (string.Equals(ToStepKey(candidate), raw.Trim(), StringComparison.OrdinalIgnoreCase)
                || string.Equals(candidate.ToString(), raw.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                kind = candidate;
                return true;
            }
        }

        return false;
    }

    private static CreationStepKind ParseStepKind(string? raw)
        => TryParseStepKind(raw, out var kind) ? kind : CreationStepKind.Class;

    private static async Task<IResult> ListKeywordCategoriesAsync(
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var items = await db.KeywordCategories
            .AsNoTracking()
            .OrderBy(category => category.SortOrder)
            .ThenBy(category => category.Code)
            .Select(category => new { category.Code, category.Name })
            .ToListAsync(cancellationToken);

        return Results.Ok(items);
    }

    private static async Task<IResult> ListKeywordsAsync(
        AppDbContext db,
        string? keyword,
        CancellationToken cancellationToken)
    {
        var query = db.Keywords.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var value = keyword.Trim();
            query = query.Where(item => item.Key.Contains(value)
                || (item.Title != null && item.Title.Contains(value))
                || item.Content.Contains(value));
        }

        var items = await query
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.Key)
            .ToListAsync(cancellationToken);

        return Results.Ok(items.Select(ToAdminKeywordDto).ToList());
    }

    private static async Task<IResult> CreateKeywordAsync(
        SaveKeywordRequest request,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var error = await ValidateKeywordAsync(request, db, null, cancellationToken);
        if (error is not null)
        {
            return error;
        }

        var now = DateTime.UtcNow;
        var categoryCode = await ResolveKeywordCategoryCode(db, request.Category, cancellationToken);
        var keyword = new Keyword
        {
            Code = await NextKeywordCode(db, categoryCode, cancellationToken),
            Key = NormalizeKeywordKey(request.Key),
            Category = request.Category?.Trim() ?? string.Empty,
            Title = NormalizeOptional(request.Title),
            Summary = request.Summary!.Trim(),
            Content = request.Content!.Trim(),
            ImageUrl = NormalizeOptional(request.ImageUrl),
            SortOrder = request.SortOrder,
            IsEnabled = request.IsEnabled,
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.Keywords.Add(keyword);
        await db.SaveChangesAsync(cancellationToken);

        return Results.Created($"/api/admin/keywords/{keyword.Id}", ToAdminKeywordDto(keyword));
    }

    private static async Task<IResult> UpdateKeywordAsync(
        Guid id,
        SaveKeywordRequest request,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var keyword = await db.Keywords.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (keyword is null)
        {
            return Results.NotFound();
        }

        var error = await ValidateKeywordAsync(request, db, id, cancellationToken);
        if (error is not null)
        {
            return error;
        }

        keyword.Key = NormalizeKeywordKey(request.Key);
        var categoryCode = await ResolveKeywordCategoryCode(db, request.Category, cancellationToken);
        if (!keyword.Code.StartsWith(categoryCode, StringComparison.OrdinalIgnoreCase))
        {
            keyword.Code = await NextKeywordCode(db, categoryCode, cancellationToken);
        }
        keyword.Category = request.Category?.Trim() ?? string.Empty;
        keyword.Title = NormalizeOptional(request.Title);
        keyword.Summary = request.Summary!.Trim();
        keyword.Content = request.Content!.Trim();
        keyword.ImageUrl = NormalizeOptional(request.ImageUrl);
        keyword.SortOrder = request.SortOrder;
        keyword.IsEnabled = request.IsEnabled;
        keyword.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        return Results.Ok(ToAdminKeywordDto(keyword));
    }

    private static async Task<IResult> DeleteKeywordAsync(Guid id, AppDbContext db, CancellationToken cancellationToken)
    {
        var keyword = await db.Keywords.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (keyword is null)
        {
            return Results.NotFound();
        }

        db.Keywords.Remove(keyword);
        await db.SaveChangesAsync(cancellationToken);

        return Results.Ok(new { deleted = id });
    }

    private static async Task<IResult?> ValidateKeywordAsync(
        SaveKeywordRequest request,
        AppDbContext db,
        Guid? currentId,
        CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();

        var key = NormalizeKeywordKey(request.Key);
        if (key.Length is 0 or > 64)
        {
            errors["key"] = ["关键词不能为空，最多 64 个字符（不用写方括号）。"];
        }

        if ((request.Category?.Trim().Length ?? 0) > 32)
        {
            errors["category"] = ["分类最多 32 个字符。"];
        }

        if ((request.Title?.Trim().Length ?? 0) > 128)
        {
            errors["title"] = ["标题最多 128 个字符。"];
        }

        var content = request.Content?.Trim() ?? string.Empty;
        if (content.Length is 0 or > 4000)
        {
            errors["content"] = ["解释正文不能为空，最多 4000 个字符。"];
        }

        var summary = request.Summary?.Trim() ?? string.Empty;
        if (summary.Length is 0 or > 256)
        {
            errors["summary"] = ["简略描述不能为空，最多 256 个字符（一句话，不嵌关键词）。"];
        }

        if ((request.ImageUrl?.Trim().Length ?? 0) > 512)
        {
            errors["imageUrl"] = ["图片地址最多 512 个字符。"];
        }

        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var exists = await db.Keywords.AnyAsync(
            item => item.Key.ToLower() == key.ToLower() && (currentId == null || item.Id != currentId),
            cancellationToken);

        if (exists)
        {
            return Results.Conflict(new { message = $"关键词「{key}」已经存在了。" });
        }

        var category = (request.Category ?? string.Empty).Trim();
        if (category.Length == 0
            || !await db.KeywordCategories.AnyAsync(item => item.Name == category, cancellationToken))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["category"] = [$"分类「{category}」还没有编号，请先在 关键词分类.xlsx 里登记。"],
            });
        }

        return null;
    }

    /// <summary>分类编号：关键词 ID 的前三位。</summary>
    private static async Task<string> ResolveKeywordCategoryCode(
        AppDbContext db,
        string? categoryRaw,
        CancellationToken cancellationToken)
    {
        var name = (categoryRaw ?? string.Empty).Trim();
        var category = await db.KeywordCategories
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Name == name, cancellationToken);

        if (category is null)
        {
            throw new InvalidOperationException(
                $"分类「{name}」还没有编号，请先在 data/关键词分类.xlsx 里登记（例如 004 法术）。");
        }

        return category.Code;
    }

    /// <summary>取当前分类里下一个可用的五位序号。</summary>
    private static async Task<string> NextKeywordCode(
        AppDbContext db,
        string categoryCode,
        CancellationToken cancellationToken)
    {
        var codes = await db.Keywords
            .AsNoTracking()
            .Where(item => item.Code.StartsWith(categoryCode))
            .Select(item => item.Code)
            .ToListAsync(cancellationToken);

        var max = 0;
        foreach (var code in codes)
        {
            if (code.Length == 8 && int.TryParse(code.AsSpan(3), out var sequence))
            {
                max = Math.Max(max, sequence);
            }
        }

        return categoryCode + (max + 1).ToString("D5");
    }

    private static AdminKeywordDto ToAdminKeywordDto(Keyword keyword) => new(
        keyword.Id,
        keyword.Code,
        keyword.Key,
        keyword.Category,
        keyword.Title,
        keyword.Summary,
        keyword.Content,
        keyword.ImageUrl,
        keyword.SortOrder,
        keyword.IsEnabled,
        new DateTimeOffset(DateTime.SpecifyKind(keyword.CreatedAt, DateTimeKind.Utc)),
        new DateTimeOffset(DateTime.SpecifyKind(keyword.UpdatedAt, DateTimeKind.Utc)));

    private static string NormalizeKeywordKey(string? raw)
    {
        var value = (raw ?? string.Empty).Trim();
        if (value.StartsWith('['))
        {
            value = value[1..];
        }

        if (value.EndsWith(']'))
        {
            value = value[..^1];
        }

        return value.Trim();
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static async Task<IResult> GetClassLevelsAsync(
        string classCode,
        string? systemCode,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var code = NormalizeClassSystemCode(systemCode);
        var normalized = NormalizeClassCode(classCode);

        var features = await db.ClassLevelFeatures
            .AsNoTracking()
            .Where(feature => feature.SystemCode == code && feature.ClassCode == normalized)
            .OrderBy(feature => feature.Level)
            .ThenBy(feature => feature.SortOrder)
            .ToListAsync(cancellationToken);

        return Results.Ok(features.Select(ToAdminClassLevelDto).ToList());
    }

    private static async Task<IResult> SaveClassLevelsAsync(
        string classCode,
        SaveClassLevelsRequest request,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var code = NormalizeClassSystemCode(request.SystemCode);
        var normalized = NormalizeClassCode(classCode);

        var classEntry = await db.GeneratorEntries
            .AsNoTracking()
            .FirstOrDefaultAsync(
                entry => entry.SystemCode == code && entry.Kind == GeneratorKind.Class && entry.Category == normalized,
                cancellationToken);

        if (classEntry is null)
        {
            return Results.NotFound(new { message = $"找不到职业「{classCode}」。" });
        }

        var items = request.Items ?? [];
        if (items.Count > 200)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["items"] = ["一个职业最多 200 条等级记录。"],
            });
        }

        var errors = new Dictionary<string, string[]>();
        for (var index = 0; index < items.Count; index += 1)
        {
            var item = items[index];
            if (item.Level is < 1 or > 20)
            {
                errors[$"items[{index}].level"] = ["等级必须在 1-20 之间。"];
            }

            var title = item.Title?.Trim() ?? string.Empty;
            if (title.Length is 0 or > 64)
            {
                errors[$"items[{index}].title"] = ["特性名不能为空，最多 64 个字符。"];
            }

            var description = item.Description?.Trim() ?? string.Empty;
            if (description.Length is 0 or > 1024)
            {
                errors[$"items[{index}].description"] = ["说明不能为空，最多 1024 个字符。"];
            }
        }

        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var existing = await db.ClassLevelFeatures
            .Where(feature => feature.SystemCode == code && feature.ClassCode == normalized)
            .ToListAsync(cancellationToken);
        db.ClassLevelFeatures.RemoveRange(existing);

        var now = DateTime.UtcNow;
        foreach (var item in items)
        {
            db.ClassLevelFeatures.Add(new ClassLevelFeature
            {
                SystemCode = code,
                ClassCode = normalized,
                Level = item.Level,
                Title = item.Title!.Trim(),
                Description = item.Description!.Trim(),
                SortOrder = item.SortOrder,
                IsEnabled = item.IsEnabled,
                CreatedAt = now,
                UpdatedAt = now,
            });
        }

        await db.SaveChangesAsync(cancellationToken);

        var saved = await db.ClassLevelFeatures
            .AsNoTracking()
            .Where(feature => feature.SystemCode == code && feature.ClassCode == normalized)
            .OrderBy(feature => feature.Level)
            .ThenBy(feature => feature.SortOrder)
            .ToListAsync(cancellationToken);

        return Results.Ok(saved.Select(ToAdminClassLevelDto).ToList());
    }

    private static async Task<IResult> ExportContentAsync(
        AppDbContext db,
        IWebHostEnvironment environment,
        IConfiguration configuration,
        CancellationToken cancellationToken)
    {
        var dataPath = ContentPaths.FindRequired(configuration["Content:DataPath"], environment.ContentRootPath);
        var files = await ContentExporter.ExportAsync(db, dataPath, cancellationToken);
        return Results.Ok(new { dataPath, files });
    }

    private static AdminClassLevelFeatureDto ToAdminClassLevelDto(ClassLevelFeature feature) => new(
        feature.Id,
        feature.SystemCode,
        feature.ClassCode,
        feature.Level,
        feature.Title,
        feature.Description,
        feature.SortOrder,
        feature.IsEnabled,
        new DateTimeOffset(DateTime.SpecifyKind(feature.UpdatedAt, DateTimeKind.Utc)));

    private static string NormalizeClassSystemCode(string? systemCode)
        => string.IsNullOrWhiteSpace(systemCode) ? "dnd5e" : systemCode.Trim().ToLowerInvariant();

    private static string NormalizeClassCode(string? classCode)
        => (classCode ?? string.Empty).Trim().ToLowerInvariant();

    private sealed record NormalizedSystem(
        string Code,
        string Name,
        string? Description,
        string NameTemplate,
        int SortOrder,
        IReadOnlyList<AttributeTemplate> Attributes);
}
