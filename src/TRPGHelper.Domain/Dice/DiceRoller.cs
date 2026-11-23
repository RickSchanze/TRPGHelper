using System.Text.RegularExpressions;

namespace TRPGHelper.Domain.Dice;

/// <summary>一次掷骰里的单个骰子项（可以带 kh/kl 与倍率）。</summary>
public sealed record RolledTerm(
    string Text,
    int Sign,
    int Count,
    int Sides,
    int? KeepHighest,
    int? KeepLowest,
    int Multiplier,
    IReadOnlyList<int> Rolls,
    IReadOnlyList<int> KeptRolls,
    int Subtotal);

public sealed record DiceResult(string Expression, int Total, IReadOnlyList<RolledTerm> Terms)
{
    /// <summary>给前端 / 日志直接展示的一句话结果。</summary>
    public string Detail => string.Join(
        " ",
        Terms.Select((t, index) =>
        {
            var sign = index == 0 ? (t.Sign < 0 ? "-" : string.Empty) : (t.Sign < 0 ? "- " : "+ ");
            return $"{sign}{t.Text}[{string.Join(",", t.Rolls)}]{(t.Multiplier > 1 ? $"*{t.Multiplier}" : string.Empty)}";
        })) + $" = {Total}";
}

/// <summary>
/// 轻量骰子引擎。支持：d20、2d6、4d6kh3（留高3）、2d6kl1（留低1）、3d6*5、2d6*5+30、1d100-10。
/// </summary>
public static class DiceRoller
{
    private const int MaxDice = 100;
    private const int MaxSides = 1000;

    private static readonly Regex TermRegex = new(
        @"^\s*(?:(\d+)\s*)?d\s*(\d+)\s*(?:(kh|kl)\s*(\d+))?\s*(?:\*\s*(\d+))?\s*$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex ConstantRegex = new(
        @"^\s*(\d+)\s*(?:\*\s*(\d+))?\s*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex SignedTokenRegex = new(
        @"([+-]?)\s*([^+-]+)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static DiceResult Roll(string expression, Random? random = null)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            throw new ArgumentException("骰子表达式不能为空。", nameof(expression));
        }

        var trimmed = expression.Trim();
        if (trimmed.Length > 100)
        {
            throw new ArgumentException("骰子表达式过长。", nameof(expression));
        }

        random ??= Random.Shared;

        var matches = SignedTokenRegex.Matches(trimmed);
        if (matches.Count == 0 || matches.Sum(m => m.Length) != trimmed.Length)
        {
            throw new ArgumentException($"无法解析的骰子表达式：{expression}。", nameof(expression));
        }

        var terms = new List<RolledTerm>(matches.Count);
        var total = 0;

        foreach (Match match in matches)
        {
            var sign = match.Groups[1].Value == "-" ? -1 : 1;
            var body = match.Groups[2].Value.Trim();
            var term = EvaluateTerm(body, sign, random);
            terms.Add(term);
            total += term.Sign * term.Subtotal;
        }

        return new DiceResult(trimmed, total, terms);
    }

    private static RolledTerm EvaluateTerm(string body, int sign, Random random)
    {
        var diceMatch = TermRegex.Match(body);
        if (diceMatch.Success)
        {
            var count = diceMatch.Groups[1].Success ? int.Parse(diceMatch.Groups[1].Value) : 1;
            var sides = int.Parse(diceMatch.Groups[2].Value);
            var keep = diceMatch.Groups[3].Success ? diceMatch.Groups[3].Value.ToLowerInvariant() : null;
            var keepCount = diceMatch.Groups[4].Success ? int.Parse(diceMatch.Groups[4].Value) : 0;
            var multiplier = diceMatch.Groups[5].Success ? int.Parse(diceMatch.Groups[5].Value) : 1;

            if (count is < 1 or > MaxDice)
            {
                throw new ArgumentException($"一次最多掷 {MaxDice} 个骰子（收到 {count} 个）。", nameof(body));
            }

            if (sides is < 1 or > MaxSides)
            {
                throw new ArgumentException($"骰子面数需在 1~{MaxSides} 之间（收到 {sides} 面）。", nameof(body));
            }

            if (keep is not null && (keepCount < 1 || keepCount > count))
            {
                throw new ArgumentException($"保留数量必须介于 1 和骰子数 {count} 之间。", nameof(body));
            }

            var rolls = new List<int>(count);
            for (var i = 0; i < count; i++)
            {
                rolls.Add(random.Next(1, sides + 1));
            }

            var ordered = keep == "kh"
                ? rolls.OrderByDescending(v => v).Take(keepCount)
                : keep == "kl"
                    ? rolls.OrderBy(v => v).Take(keepCount)
                    : rolls;

            var kept = ordered.ToList();
            var subtotal = kept.Sum() * multiplier;

            return new RolledTerm(
                body,
                sign,
                count,
                sides,
                keep == "kh" ? keepCount : null,
                keep == "kl" ? keepCount : null,
                multiplier,
                rolls,
                kept,
                subtotal);
        }

        var constantMatch = ConstantRegex.Match(body);
        if (constantMatch.Success)
        {
            var value = int.Parse(constantMatch.Groups[1].Value);
            var multiplier = constantMatch.Groups[2].Success ? int.Parse(constantMatch.Groups[2].Value) : 1;
            var subtotal = value * multiplier;

            return new RolledTerm(body, sign, 0, 0, null, null, multiplier, [value], [value], subtotal);
        }

        throw new ArgumentException($"无法解析的骰子项：{body}。", nameof(body));
    }
}
