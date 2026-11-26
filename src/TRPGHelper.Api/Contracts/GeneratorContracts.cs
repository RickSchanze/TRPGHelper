namespace TRPGHelper.Api.Contracts;

public sealed record RollRequest(string Expression);

public sealed record RollTermDto(string Text, IReadOnlyList<int> Rolls, IReadOnlyList<int> KeptRolls, int Subtotal);

public sealed record RollResponse(string Expression, int Total, string Detail, IReadOnlyList<RollTermDto> Terms);

public sealed record GenerateAttributesRequest(string? SystemCode, Guid? SystemId, string? Method);

public sealed record GeneratedAttributeDto(string Key, string Label, int Value, string? Formula);

public sealed record GeneratedAttributesResponse(
    Guid SystemId,
    string SystemCode,
    string SystemName,
    string Method,
    IReadOnlyList<GeneratedAttributeDto> Attributes,
    int Total);

public sealed record InspirationDto(string Title, string Text);

public sealed record ClassOptionDto(
    string Code,
    string Name,
    string Description,
    string HitDie,
    string PrimaryAbility,
    string SavingThrows,
    string SpellcastingAbility,
    IReadOnlyList<string> Features,
    string? ImageUrl,
    string? Accent,
    int SkillChoiceCount,
    IReadOnlyList<string> SkillOptions,
    IReadOnlyList<string> ArmorProficiencies,
    IReadOnlyList<string> WeaponProficiencies);

public sealed record RaceOptionDto(
    string Code,
    string Name,
    string Description,
    string AbilityBonus,
    int Speed,
    IReadOnlyList<string> Traits,
    IReadOnlyList<AbilityBonusDto> AbilityBonuses,
    IReadOnlyList<AbilityChoiceDto> AbilityChoices,
    IReadOnlyList<string> Skills,
    int SkillChoiceCount,
    IReadOnlyList<string> Languages,
    int LanguageChoiceCount);

public sealed record SubclassOptionDto(
    string Code,
    string Name,
    string ClassId,
    string Description,
    IReadOnlyList<string> Features,
    string? ImageUrl,
    string? Accent);

public sealed record SubraceOptionDto(
    string Code,
    string Name,
    string RaceId,
    string Description,
    IReadOnlyList<string> Traits,
    IReadOnlyList<AbilityBonusDto> AbilityBonuses,
    IReadOnlyList<AbilityChoiceDto> AbilityChoices);

/// <summary>种族 / 亚种给的固定属性加值，例如「精灵：敏捷 +2」。</summary>
public sealed record AbilityBonusDto(string Key, string Label, int Value);

/// <summary>需要玩家自己选的属性加值，例如半精灵「任选两项 +1」。</summary>
public sealed record AbilityChoiceDto(string Id, string Label, int Count, int Value);

/// <summary>一次「决定属性」生成：基础值 + 掷骰明细（标准数组 / 手动时骰子为空）。</summary>
public sealed record AbilityScoreRollDto(
    string Key,
    string Label,
    int Base,
    string Formula,
    IReadOnlyList<int> Dice,
    IReadOnlyList<int> Kept);

public sealed record AbilityScoresRequest(string? SystemCode, string? Method);

public sealed record AbilityScoresResponse(
    string SystemCode,
    string Method,
    IReadOnlyList<AbilityScoreRollDto> Attributes);

/// <summary>18 项技能之一。</summary>
public sealed record SkillOptionDto(string Id, string Name, string Ability, string Description);

/// <summary>语言（标准语 / 异界语）。</summary>
public sealed record LanguageOptionDto(string Id, string Name, string Kind, string Description);

/// <summary>背景详情：技能、工具、语言、装备、背景特性。</summary>
public sealed record BackgroundDetailDto(
    string Id,
    string Name,
    string Description,
    IReadOnlyList<string> Skills,
    string Tools,
    int LanguageCount,
    string Equipment,
    string FeatureName,
    string FeatureText);

/// <summary>1 级职业特性选项（战斗风格 / 宿敌 / 自然探索者）。</summary>
public sealed record FeatureOptionDto(string ClassId, string OptionType, string Name, string Description);

/// <summary>护甲 / 盾。</summary>
public sealed record ArmorOptionDto(
    string Id,
    string Name,
    string Type,
    int BaseAc,
    int? DexCap,
    int? StrengthReq,
    bool StealthDisadvantage,
    string Description);

/// <summary>武器。</summary>
public sealed record WeaponOptionDto(
    string Id,
    string Name,
    string Damage,
    string DamageType,
    string Ability,
    bool Ranged,
    string Properties);

/// <summary>职业起始装备包。</summary>
public sealed record EquipmentKitDto(
    string ClassId,
    string ArmorId,
    bool Shield,
    IReadOnlyList<string> WeaponIds,
    string DefaultWeaponId,
    string Pack);

public sealed record EquipmentResponse(
    IReadOnlyList<ArmorOptionDto> Armor,
    IReadOnlyList<WeaponOptionDto> Weapons,
    EquipmentKitDto? Kit);

/// <summary>
/// 阵营（九宫格）。解释文字在关键词里，这里只给车卡流程渲染图标 + 名字要用的字段。
/// </summary>
public sealed record AlignmentOptionDto(
    string Code,
    string Name,
    string? IconUrl,
    string? Accent);
