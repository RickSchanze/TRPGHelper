using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TRPGHelper.Infrastructure.Auth;
using TRPGHelper.Infrastructure.Data;

namespace TRPGHelper.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        string contentRootPath)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<WeChatOptions>(configuration.GetSection(WeChatOptions.SectionName));
        services.Configure<AuthOptions>(configuration.GetSection(AuthOptions.SectionName));

        var connectionString = ResolveConnectionString(
            configuration.GetConnectionString("Default"),
            contentRootPath,
            "trpghelper.db");

        services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));

        services.AddHttpClient<WeChatAuthClient>(client =>
        {
            client.BaseAddress = new Uri("https://api.weixin.qq.com/");
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        services.AddSingleton<JwtTokenService>();

        return services;
    }

    /// <summary>
    /// 把 "Data Source=App_Data/xxx.db" 这种相对路径按内容根目录解析成绝对路径，
    /// 避免 `dotnet run` 之外的启动方式把数据库建到奇怪的位置。
    /// </summary>
    private static string ResolveConnectionString(string? connectionString, string contentRootPath, string defaultFileName)
    {
        var builder = string.IsNullOrWhiteSpace(connectionString)
            ? new SqliteConnectionStringBuilder { DataSource = Path.Combine(contentRootPath, "App_Data", defaultFileName) }
            : new SqliteConnectionStringBuilder(connectionString);

        if (!string.IsNullOrWhiteSpace(builder.DataSource) && !Path.IsPathRooted(builder.DataSource))
        {
            builder.DataSource = Path.GetFullPath(Path.Combine(contentRootPath, builder.DataSource));
        }

        if (!string.IsNullOrWhiteSpace(builder.DataSource) && builder.DataSource != ":memory:")
        {
            var directory = Path.GetDirectoryName(Path.GetFullPath(builder.DataSource));
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        return builder.ToString();
    }
}
