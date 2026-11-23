using Microsoft.EntityFrameworkCore;
using TRPGHelper.Infrastructure.Data;

namespace TRPGHelper.Api.Services;

internal static class UserStats
{
    public static async Task<(int Total, int Retained)> GetAsync(
        AppDbContext db,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var counts = await db.CharacterCards
            .Where(card => card.OwnerId == userId)
            .GroupBy(card => card.Status)
            .Select(group => new { group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);

        var total = counts.Sum(item => item.Count);
        var retained = counts
            .Where(item => item.Key == Domain.Enums.CardStatus.Retained)
            .Sum(item => item.Count);

        return (total, retained);
    }
}
