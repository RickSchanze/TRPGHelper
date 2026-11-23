using System.Text.Json;

namespace TRPGHelper.Domain.Cards;

/// <summary>
/// 某个规则系统的属性模板项。
/// </summary>
public sealed record AttributeTemplate(string Key, string Label, string Formula);

public static class AttributeTemplateParser
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static IReadOnlyList<AttributeTemplate> Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return Array.Empty<AttributeTemplate>();
        }

        try
        {
            List<AttributeTemplate>? parsed = JsonSerializer.Deserialize<List<AttributeTemplate>>(json, JsonOptions);
            return parsed ?? [];
        }
        catch (JsonException)
        {
            return Array.Empty<AttributeTemplate>();
        }
    }

    public static string Serialize(IEnumerable<AttributeTemplate> templates)
        => JsonSerializer.Serialize(templates, JsonOptions);
}
