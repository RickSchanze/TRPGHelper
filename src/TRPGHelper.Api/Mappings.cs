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
        sheet.Notes,
        sheet.BackgroundName,
        sheet.Gender,
        sheet.Height,
        sheet.Weight,
        sheet.Appearance,
        sheet.Languages.ToList(),
        new SheetProficienciesDto(
            sheet.Proficiencies.Armor.ToList(),
            sheet.Proficiencies.Weapons.ToList(),
            sheet.Proficiencies.Tools.ToList()),
        sheet.Equipment.Select(item => new SheetEquipmentItemDto(item.Name, item.Quantity, item.Note)).ToList(),
        new SheetCombatDto(
            sheet.Combat.HitPoints,
            sheet.Combat.ArmorClass,
            sheet.Combat.Initiative,
            sheet.Combat.Speed,
            sheet.Combat.ProficiencyBonus,
            sheet.Combat.PassivePerception,
            new Dictionary<string, int>(sheet.Combat.SavingThrows)),
        new SheetPersonalityDto(
            sheet.Personality.Trait,
            sheet.Personality.Ideal,
            sheet.Personality.Bond,
            sheet.Personality.Flaw),
        new Dictionary<string, string>(sheet.FeatureChoices));

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
        BackgroundName = dto.BackgroundName,
        Notes = dto.Notes,
        Gender = dto.Gender,
        Height = dto.Height,
        Weight = dto.Weight,
        Appearance = dto.Appearance,
        Languages = dto.Languages?.Where(l => !string.IsNullOrWhiteSpace(l)).Select(l => l.Trim()).ToList() ?? [],
        Proficiencies = new SheetProficiencies
        {
            Armor = dto.Proficiencies?.Armor?.Where(i => !string.IsNullOrWhiteSpace(i)).Select(i => i.Trim()).ToList() ?? [],
            Weapons = dto.Proficiencies?.Weapons?.Where(i => !string.IsNullOrWhiteSpace(i)).Select(i => i.Trim()).ToList() ?? [],
            Tools = dto.Proficiencies?.Tools?.Where(i => !string.IsNullOrWhiteSpace(i)).Select(i => i.Trim()).ToList() ?? [],
        },
        Equipment = dto.Equipment?
            .Where(item => !string.IsNullOrWhiteSpace(item.Name))
            .Select(item => new SheetEquipmentItem
            {
                Name = item.Name.Trim(),
                Quantity = item.Quantity <= 0 ? 1 : item.Quantity,
                Note = item.Note,
            })
            .ToList() ?? [],
        Combat = dto.Combat is null
            ? new SheetCombat()
            : new SheetCombat
            {
                HitPoints = dto.Combat.HitPoints,
                ArmorClass = dto.Combat.ArmorClass,
                Initiative = dto.Combat.Initiative,
                Speed = dto.Combat.Speed,
                ProficiencyBonus = dto.Combat.ProficiencyBonus,
                PassivePerception = dto.Combat.PassivePerception,
                SavingThrows = dto.Combat.SavingThrows is null
                    ? new Dictionary<string, int>()
                    : new Dictionary<string, int>(dto.Combat.SavingThrows),
            },
        Personality = dto.Personality is null
            ? new SheetPersonality()
            : new SheetPersonality
            {
                Trait = dto.Personality.Trait,
                Ideal = dto.Personality.Ideal,
                Bond = dto.Personality.Bond,
                Flaw = dto.Personality.Flaw,
            },
        FeatureChoices = dto.FeatureChoices is null
            ? new Dictionary<string, string>()
            : new Dictionary<string, string>(dto.FeatureChoices),
    };

    public static CharacterSummaryDto ToSummary(CharacterCard card) => new(
        card.Id,
        card.Name,
        card.Occupation,
        card.ClassName,
        card.SubclassName,
        card.RaceName,
        card.SubraceName,
        card.AlignmentName,
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
            card.AlignmentName,
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
