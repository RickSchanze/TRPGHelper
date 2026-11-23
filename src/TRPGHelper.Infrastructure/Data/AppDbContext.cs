using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using TRPGHelper.Domain.Cards;
using TRPGHelper.Domain.Entities;

namespace TRPGHelper.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public DbSet<User> Users => Set<User>();

    public DbSet<GameSystem> GameSystems => Set<GameSystem>();

    public DbSet<CharacterCard> CharacterCards => Set<CharacterCard>();

    public DbSet<CardLog> CardLogs => Set<CardLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(builder =>
        {
            builder.ToTable("Users");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.OpenId).HasMaxLength(64).IsRequired();
            builder.Property(x => x.UnionId).HasMaxLength(64);
            builder.Property(x => x.NickName).HasMaxLength(64).IsRequired();
            builder.Property(x => x.AvatarUrl).HasMaxLength(512);
            builder.HasIndex(x => x.OpenId).IsUnique();
        });

        modelBuilder.Entity<GameSystem>(builder =>
        {
            builder.ToTable("GameSystems");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Code).HasMaxLength(32).IsRequired();
            builder.Property(x => x.Name).HasMaxLength(128).IsRequired();
            builder.Property(x => x.Description).HasMaxLength(512);
            builder.HasIndex(x => x.Code).IsUnique();
        });

        modelBuilder.Entity<CharacterCard>(builder =>
        {
            builder.ToTable("CharacterCards");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Name).HasMaxLength(64).IsRequired();
            builder.Property(x => x.Occupation).HasMaxLength(64);
            builder.Property(x => x.ClassName).HasMaxLength(40);
            builder.Property(x => x.RaceName).HasMaxLength(40);
            builder.Property(x => x.Summary).HasMaxLength(512);
            builder.Property(x => x.PortraitUrl).HasMaxLength(512);
            builder.Property(x => x.Status).HasConversion<int>();

            var sheetComparer = new ValueComparer<CharacterSheet>(
                (left, right) => JsonSerializer.Serialize(left, JsonOptions) == JsonSerializer.Serialize(right, JsonOptions),
                value => JsonSerializer.Serialize(value, JsonOptions).GetHashCode(),
                value => JsonSerializer.Deserialize<CharacterSheet>(JsonSerializer.Serialize(value, JsonOptions), JsonOptions)!);

            builder.Property(x => x.Sheet)
                .HasConversion(
                    value => JsonSerializer.Serialize(value, JsonOptions),
                    json => JsonSerializer.Deserialize<CharacterSheet>(json, JsonOptions) ?? new CharacterSheet())
                .HasColumnType("TEXT")
                .Metadata.SetValueComparer(sheetComparer);

            builder.HasOne(x => x.Owner)
                .WithMany(x => x.Cards)
                .HasForeignKey(x => x.OwnerId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.System)
                .WithMany()
                .HasForeignKey(x => x.SystemId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.OwnerId, x.Status });
            builder.HasIndex(x => new { x.OwnerId, x.UpdatedAt });
            builder.HasIndex(x => x.RetainedAt);
        });

        modelBuilder.Entity<CardLog>(builder =>
        {
            builder.ToTable("CardLogs");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Action).HasConversion<int>();
            builder.Property(x => x.Note).HasMaxLength(512);

            builder.HasOne(x => x.CharacterCard)
                .WithMany(x => x.Logs)
                .HasForeignKey(x => x.CharacterCardId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.CharacterCardId);
        });
    }
}
