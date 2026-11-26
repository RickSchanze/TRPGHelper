using TRPGHelper.Domain.Dice;

namespace TRPGHelper.Domain.Cards;

/// <summary>
/// 车卡时用来生成初始属性：掷骰法、标准数组法，或手动填写（全 10 起手）。
/// </summary>
public static class AttributeGenerator
{
    public const string RollMethod = "roll";
    public const string StandardMethod = "standard";
    public const string ManualMethod = "manual";

    private static readonly int[] StandardArray = [15, 14, 13, 12, 10, 8];

    public static IReadOnlyList<string> SupportedMethods { get; } = [RollMethod, StandardMethod, ManualMethod];

    public static IReadOnlyList<SheetAttribute> Generate(
        IEnumerable<AttributeTemplate> templates,
        string? method = null,
        Random? random = null)
    {
        ArgumentNullException.ThrowIfNull(templates);

        var list = templates.ToList();
        if (list.Count == 0)
        {
            return Array.Empty<SheetAttribute>();
        }

        var normalized = string.IsNullOrWhiteSpace(method) ? RollMethod : method.Trim().ToLowerInvariant();
        random ??= Random.Shared;

        return normalized switch
        {
            RollMethod => list.Select(t => RollOne(t, random)).ToList(),
            StandardMethod => list
                .Select((t, index) => index < StandardArray.Length
                    ? new SheetAttribute { Key = t.Key, Label = t.Label, Value = StandardArray[index], Formula = "标准数组" }
                    : RollOne(t, random))
                .ToList(),
            ManualMethod => list
                .Select(t => new SheetAttribute { Key = t.Key, Label = t.Label, Value = 10, Formula = "手动填写" })
                .ToList(),
            _ => throw new ArgumentException(
                $"不支持的车卡方式：{method}。可用：{string.Join(" / ", SupportedMethods)}。",
                nameof(method)),
        };
    }

    /// <summary>
    /// 「决定属性」面板用：除了基础值，还带掷骰明细（骰面与保留的骰子），
    /// 前端点开每个属性时用它说明「这个数是怎么来的」。
    /// </summary>
    public static IReadOnlyList<AbilityScoreRoll> GenerateDetailed(
        IEnumerable<AttributeTemplate> templates,
        string? method = null,
        Random? random = null)
    {
        ArgumentNullException.ThrowIfNull(templates);

        var list = templates.ToList();
        if (list.Count == 0)
        {
            return Array.Empty<AbilityScoreRoll>();
        }

        var normalized = string.IsNullOrWhiteSpace(method) ? RollMethod : method.Trim().ToLowerInvariant();
        random ??= Random.Shared;

        return normalized switch
        {
            RollMethod => list
                .Select(template =>
                {
                    var result = DiceRoller.Roll(template.Formula, random);
                    var term = result.Terms.FirstOrDefault();
                    return new AbilityScoreRoll(
                        template.Key,
                        template.Label,
                        result.Total,
                        template.Formula,
                        term?.Rolls ?? [],
                        term?.KeptRolls ?? []);
                })
                .ToList(),
            StandardMethod => list
                .Select((template, index) => new AbilityScoreRoll(
                    template.Key,
                    template.Label,
                    index < StandardArray.Length ? StandardArray[index] : 10,
                    "标准数组",
                    [],
                    []))
                .ToList(),
            ManualMethod => list
                .Select(template => new AbilityScoreRoll(template.Key, template.Label, 10, "手动填写", [], []))
                .ToList(),
            _ => throw new ArgumentException(
                $"不支持的车卡方式：{method}。可用：{string.Join(" / ", SupportedMethods)}。",
                nameof(method)),
        };
    }

    private static SheetAttribute RollOne(AttributeTemplate template, Random random)
    {
        var result = DiceRoller.Roll(template.Formula, random);
        return new SheetAttribute
        {
            Key = template.Key,
            Label = template.Label,
            Value = result.Total,
            Formula = template.Formula,
        };
    }
}

/// <summary>一次生成得到的属性基础值 + 掷骰明细。</summary>
public sealed record AbilityScoreRoll(
    string Key,
    string Label,
    int Base,
    string Formula,
    IReadOnlyList<int> Dice,
    IReadOnlyList<int> Kept);
