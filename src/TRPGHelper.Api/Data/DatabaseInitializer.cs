using Microsoft.EntityFrameworkCore;
using TRPGHelper.Infrastructure.Data;

namespace TRPGHelper.Api.Data;

public static class DatabaseInitializer
{
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.Database.MigrateAsync();
        await DataSeeder.SeedAsync(db);
    }
}
