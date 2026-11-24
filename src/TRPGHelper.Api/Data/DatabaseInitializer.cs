using Microsoft.EntityFrameworkCore;
using TRPGHelper.Infrastructure.Content;
using TRPGHelper.Infrastructure.Data;

namespace TRPGHelper.Api.Data;

public static class DatabaseInitializer
{
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        await db.Database.MigrateAsync();

        // 内容以 data/*.xlsx 为准：启动时整份同步进数据库，玩家数据（用户 / 卡）不受影响。
        var dataPath = ContentPaths.FindRequired(configuration["Content:DataPath"], app.Environment.ContentRootPath);
        var bundle = ContentLoader.Load(dataPath);
        await ContentSynchronizer.SyncAsync(db, bundle);
    }
}
