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

    public DbSet<GeneratorEntry> GeneratorEntries => Set<GeneratorEntry>();

    public DbSet<CreationStep> CreationSteps => Set<CreationStep>();

    public DbSet<Keyword> Keywords => Set<Keyword>();

    public DbSet<KeywordCategory> KeywordCategories => Set<KeywordCategory>();

    public DbSet<ClassLevelFeature> ClassLevelFeatures => Set<ClassLevelFeature>();

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
            builder.Property(x => x.Role).HasMaxLength(16).HasDefaultValue(UserRoles.User).IsRequired();
            builder.HasIndex(x => x.OpenId).IsUnique();
        });

        modelBuilder.Entity<GameSystem>(builder =>
        {
            builder.ToTable("GameSystems");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Code).HasMaxLength(32).IsRequired();
            builder.Property(x => x.Name).HasMaxLength(128).IsRequired();
            builder.Property(x => x.Description).HasMaxLength(512);
            builder.Property(x => x.NameTemplate).HasMaxLength(128).HasDefaultValue("{surname}{given}").IsRequired();
            builder.Property(x => x.IconUrl).HasMaxLength(512);
            builder.Property(x => x.Accent).HasMaxLength(32);
            builder.Property(x => x.IsAvailable).HasDefaultValue(true);
            builder.HasIndex(x => x.Code).IsUnique();
        });

        modelBuilder.Entity<GeneratorEntry>(builder =>
        {
            builder.ToTable("GeneratorEntries");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.SystemCode).HasMaxLength(32);
            builder.Property(x => x.Kind).HasConversion<string>().HasMaxLength(32);
            builder.Property(x => x.Category).HasMaxLength(32).IsRequired();
            builder.Property(x => x.Value).HasMaxLength(256).IsRequired();
            builder.Property(x => x.ExtraJson).HasColumnType("TEXT");
            builder.HasIndex(x => new { x.SystemCode, x.Kind, x.IsEnabled });
            builder.HasIndex(x => new { x.Kind, x.Category });
        });

        modelBuilder.Entity<CreationStep>(builder =>
        {
            builder.ToTable("CreationSteps");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.SystemCode).HasMaxLength(32);
            builder.Property(x => x.Kind).HasConversion<string>().HasMaxLength(32);
            builder.Property(x => x.Title).HasMaxLength(64).IsRequired();
            builder.Property(x => x.Description).HasMaxLength(512);
            builder.HasIndex(x => new { x.SystemCode, x.IsEnabled, x.SortOrder });
        });

        modelBuilder.Entity<Keyword>(builder =>
        {
            builder.ToTable("Keywords");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Code).HasMaxLength(16).HasDefaultValue(string.Empty).IsRequired();
            builder.Property(x => x.Key).HasMaxLength(64).IsRequired();
            builder.Property(x => x.Title).HasMaxLength(128);
            builder.Property(x => x.Content).HasMaxLength(4000).IsRequired();
            builder.Property(x => x.ImageUrl).HasMaxLength(512);
            builder.HasIndex(x => x.Key).IsUnique();
            builder.HasIndex(x => x.IsEnabled);
        });

        modelBuilder.Entity<KeywordCategory>(builder =>
        {
            builder.ToTable("KeywordCategories");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Code).HasMaxLength(8).IsRequired();
            builder.Property(x => x.Name).HasMaxLength(32).IsRequired();
            builder.HasIndex(x => x.Code).IsUnique();
            builder.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<ClassLevelFeature>(builder =>
        {
            builder.ToTable("ClassLevelFeatures");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.SystemCode).HasMaxLength(32).IsRequired();
            builder.Property(x => x.ClassCode).HasMaxLength(32).IsRequired();
            builder.Property(x => x.Title).HasMaxLength(64).IsRequired();
            builder.Property(x => x.Description).HasMaxLength(1024).IsRequired();
            builder.HasIndex(x => new { x.SystemCode, x.ClassCode, x.Level, x.SortOrder });
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
