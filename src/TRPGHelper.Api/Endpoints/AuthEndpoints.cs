using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TRPGHelper.Api.Contracts;
using TRPGHelper.Api.Extensions;
using TRPGHelper.Api.Services;
using TRPGHelper.Domain.Entities;
using TRPGHelper.Infrastructure.Auth;
using TRPGHelper.Infrastructure.Data;

namespace TRPGHelper.Api.Endpoints;

public static class AuthEndpoints
{
    private const string DevOpenId = "dev:local";

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var anonymous = app.MapGroup("/api/auth")
            .WithTags("Auth")
            .AllowAnonymous();

        anonymous.MapPost("/wechat/login", LoginWithWeChatAsync)
            .WithName("LoginWithWeChat")
            .WithSummary("微信小程序登录（code2session 换 token）");

        anonymous.MapPost("/dev-login", LoginAsDevAsync)
            .WithName("DevLogin")
            .WithSummary("本地开发登录（仅当 Auth:EnableDevLogin 开启时可用）");

        var authorized = app.MapGroup("/api/auth")
            .WithTags("Auth")
            .RequireAuthorization();

        authorized.MapGet("/me", GetMeAsync)
            .WithName("GetCurrentUser")
            .WithSummary("当前用户资料");

        authorized.MapPut("/me", UpdateMeAsync)
            .WithName("UpdateCurrentUser")
            .WithSummary("更新昵称 / 头像");

        return app;
    }

    private static async Task<IResult> LoginWithWeChatAsync(
        WeChatLoginRequest request,
        AppDbContext db,
        WeChatAuthClient weChat,
        JwtTokenService jwt,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["code"] = ["缺少微信登录 code。"],
            });
        }

        WeChatSession session;
        try
        {
            session = await weChat.Code2SessionAsync(request.Code.Trim(), cancellationToken);
        }
        catch (WeChatNotConfiguredException ex)
        {
            return Results.Problem(ex.Message, statusCode: StatusCodes.Status503ServiceUnavailable);
        }
        catch (WeChatAuthException ex)
        {
            return Results.Problem(ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
        catch (HttpRequestException)
        {
            return Results.Problem("暂时连不上微信服务器，请稍后再试。", statusCode: StatusCodes.Status502BadGateway);
        }

        var now = DateTime.UtcNow;
        var user = await db.Users.FirstOrDefaultAsync(u => u.OpenId == session.OpenId, cancellationToken);
        if (user is null)
        {
            user = new User
            {
                OpenId = session.OpenId,
                UnionId = session.UnionId,
                NickName = NormalizeNickName(request.NickName) ?? "微信调查员",
                AvatarUrl = request.AvatarUrl,
                CreatedAt = now,
                LastLoginAt = now,
            };
            db.Users.Add(user);
        }
        else
        {
            user.LastLoginAt = now;
            if (!string.IsNullOrWhiteSpace(session.UnionId))
            {
                user.UnionId = session.UnionId;
            }

            var nickName = NormalizeNickName(request.NickName);
            if (nickName is not null)
            {
                user.NickName = nickName;
            }

            if (!string.IsNullOrWhiteSpace(request.AvatarUrl))
            {
                user.AvatarUrl = request.AvatarUrl;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        return await BuildLoginResponseAsync(user, db, jwt, cancellationToken);
    }

    private static async Task<IResult> LoginAsDevAsync(
        DevLoginRequest request,
        AppDbContext db,
        JwtTokenService jwt,
        IOptions<AuthOptions> authOptions,
        CancellationToken cancellationToken)
    {
        if (!authOptions.Value.EnableDevLogin)
        {
            return Results.Problem(
                "开发登录未开启（Auth:EnableDevLogin）。生产环境请使用微信登录。",
                statusCode: StatusCodes.Status403Forbidden);
        }

        var now = DateTime.UtcNow;
        var nickName = NormalizeNickName(request.NickName);
        var user = await db.Users.FirstOrDefaultAsync(u => u.OpenId == DevOpenId, cancellationToken);
        if (user is null)
        {
            user = new User
            {
                OpenId = DevOpenId,
                NickName = nickName ?? "本地调查员",
                CreatedAt = now,
                LastLoginAt = now,
            };
            db.Users.Add(user);
        }
        else
        {
            user.LastLoginAt = now;
            if (nickName is not null)
            {
                user.NickName = nickName;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        return await BuildLoginResponseAsync(user, db, jwt, cancellationToken);
    }

    private static async Task<IResult> GetMeAsync(
        AppDbContext db,
        System.Security.Claims.ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var user = await db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == principal.GetUserId(), cancellationToken);

        if (user is null)
        {
            return Results.NotFound();
        }

        var (total, retained) = await UserStats.GetAsync(db, user.Id, cancellationToken);
        return Results.Ok(Mappings.ToUserDto(user, total, retained));
    }

    private static async Task<IResult> UpdateMeAsync(
        UpdateProfileRequest request,
        AppDbContext db,
        System.Security.Claims.ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == principal.GetUserId(), cancellationToken);
        if (user is null)
        {
            return Results.NotFound();
        }

        var nickName = NormalizeNickName(request.NickName);
        if (nickName is null)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["nickName"] = ["昵称不能为空，最多 32 个字符。"],
            });
        }

        user.NickName = nickName;
        if (request.AvatarUrl is not null)
        {
            user.AvatarUrl = string.IsNullOrWhiteSpace(request.AvatarUrl) ? null : request.AvatarUrl.Trim();
        }

        await db.SaveChangesAsync(cancellationToken);

        var (total, retained) = await UserStats.GetAsync(db, user.Id, cancellationToken);
        return Results.Ok(Mappings.ToUserDto(user, total, retained));
    }

    private static async Task<IResult> BuildLoginResponseAsync(
        User user,
        AppDbContext db,
        JwtTokenService jwt,
        CancellationToken cancellationToken)
    {
        var (total, retained) = await UserStats.GetAsync(db, user.Id, cancellationToken);
        var (token, expiresAt) = jwt.CreateToken(user);
        return Results.Ok(new LoginResponse(token, expiresAt, Mappings.ToUserDto(user, total, retained)));
    }

    private static string? NormalizeNickName(string? nickName)
    {
        if (string.IsNullOrWhiteSpace(nickName))
        {
            return null;
        }

        var trimmed = nickName.Trim();
        return trimmed.Length <= 32 ? trimmed : null;
    }
}
