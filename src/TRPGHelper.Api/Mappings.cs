using TRPGHelper.Api.Contracts;
using TRPGHelper.Domain.Cards;
using TRPGHelper.Domain.Entities;
using TRPGHelper.Domain.Enums;

namespace TRPGHelper.Api;

internal static class Mappings
{
    public static UserDto ToUserDto(User user, int totalCards, int retainedCards) => new(
        user.Id,
        user.NickName,
        user.AvatarUrl,
        string.IsNullOrWhiteSpace(user.Role) ? UserRoles.User : user.Role,
        AsOffset(user.CreatedAt),
        AsOffset(user.LastLoginAt),
        totalCards,
        retainedCards);

    public static GameSystemDto ToDto(GameSystem system) => new(
        system.Id,
        system.Code,
        system.Name,
        system.Description,
        system.IconUrl,
        system.Accent,
        system.IsAvailable,
        AttributeTemplateParser.Parse(system.AttributeTemplateJson)
            .Select(t => new AttributeTemplateDto(t.Key, t.Label, t.Formula))
            .ToList());

    public static CharacterSheetDto ToDto(CharacterSheet sheet) => new(
        sheet.Attributes.Select(a => new SheetAttributeDto(a.Key, a.Label, a.Value, a.Formula)).ToList(),
        sheet.Skills.Select(s => new SheetSkillDto(s.Name, s.Value, s.Note)).ToList(),
        sheet.Items.ToList(),
        sheet.Background,
        sheet.Notes);

    public static CharacterSheet FromDto(CharacterSheetDto dto) => new()
    {
        Attributes = dto.Attributes?
            .Select(a => new SheetAttribute
            {
                Key = (a.Key ?? string.Empty).Trim(),
                Label = (a.Label ?? string.Empty).Trim(),
                Value = a.Value,
                Formula = a.Formula,
            })
            .ToList() ?? [],
        Skills = dto.Skills?
            .Select(s => new SheetSkill
            {
                Name = (s.Name ?? string.Empty).Trim(),
                Value = s.Value,
                Note = s.Note,
            })
            .ToList() ?? [],
        Items = dto.Items?.Where(i => !string.IsNullOrWhiteSpace(i)).Select(i => i.Trim()).ToList() ?? [],
        Background = dto.Background,
        Notes = dto.Notes,
    };

    public static CharacterSummaryDto ToSummary(CharacterCard card) => new(
        card.Id,
        card.Name,
        card.Occupation,
        card.ClassName,
        card.SubclassName,
        card.RaceName,
        card.SubraceName,
        card.Age,
        card.Summary,
        card.PortraitUrl,
        card.SystemId,
        card.System?.Code ?? string.Empty,
        card.System?.Name ?? string.Empty,
        card.Status.ToKey(),
        card.Status.ToLabel(),
        card.IsFavorite,
        AsOffset(card.CreatedAt),
        AsOffset(card.UpdatedAt),
        AsOffset(card.RetainedAt));

    public static CharacterDetailDto ToDetail(CharacterCard card, IEnumerable<CardLog> logs)
    {
        var next = CardRules.NextStatuses(card.Status);

        return new CharacterDetailDto(
            card.Id,
            card.Name,
            card.Occupation,
            card.ClassName,
            card.SubclassName,
            card.RaceName,
            card.SubraceName,
            card.Age,
            card.Summary,
            card.PortraitUrl,
            card.SystemId,
            card.System?.Code ?? string.Empty,
            card.System?.Name ?? string.Empty,
            card.Status.ToKey(),
            card.Status.ToLabel(),
            card.IsFavorite,
            AsOffset(card.CreatedAt),
            AsOffset(card.UpdatedAt),
            AsOffset(card.RetainedAt),
            ToDto(card.Sheet),
            logs.Select(ToLogDto).ToList(),
            next.Select(s => s.ToKey()).ToList(),
            next.Select(s => s.ToLabel()).ToList());
    }

    public static CardLogDto ToLogDto(CardLog log) => new(
        log.Id,
        log.Action.ToKey(),
        log.Action.ToLabel(),
        log.Note,
        AsOffset(log.CreatedAt));

    /// <summary>数据库存 UTC DateTime，接口统一输出带时区的 DateTimeOffset。</summary>
    private static DateTimeOffset AsOffset(DateTime value)
        => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));

    private static DateTimeOffset? AsOffset(DateTime? value)
        => value is null ? null : AsOffset(value.Value);
}
