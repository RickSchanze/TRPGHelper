using TRPGHelper.Domain.Dice;

namespace TRPGHelper.Domain.Cards;

/// <summary>
/// 车卡时用来生成初始属性：掷骰法，或标准数组法。
/// </summary>
public static class AttributeGenerator
{
    public const string RollMethod = "roll";
    public const string StandardMethod = "standard";

    private static readonly int[] StandardArray = [15, 14, 13, 12, 10, 8];

    public static IReadOnlyList<string> SupportedMethods { get; } = [RollMethod, StandardMethod];

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
