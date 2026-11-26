namespace TRPGHelper.Infrastructure.Content;

/// <summary>
/// 定位仓库里的 `data/` 内容目录，并集中管理内容文件名（文件名用中文，工作表名保持英文）。
/// 默认从内容根目录 / 程序目录逐级往上找，也可以用配置项 <c>Content:DataPath</c> 显式指定。
/// </summary>
public static class ContentPaths
{
    /// <summary>用来判断「这个目录确实是内容目录」的标记文件。</summary>
    public const string MarkerFile = SystemsFile;

    public const string SystemsFile = "规则系统.xlsx";

    public const string NamesFile = "姓名片段.xlsx";

    public const string ClassesFile = "职业.xlsx";

    /// <summary>子职（战士范型 / 游侠范型 / 武僧宗派 / 德鲁伊结社 / 圣武士誓约）。</summary>
    public const string SubclassesFile = "子职.xlsx";

    public const string OccupationsFile = "随机职业.xlsx";

    public const string RacesFile = "种族.xlsx";

    /// <summary>亚种（高等精灵 / 山地矮人 / 轻足半身人等），按种族分组。</summary>
    public const string SubracesFile = "亚种.xlsx";

    public const string BackgroundsFile = "背景.xlsx";

    /// <summary>阵营（九宫格：守序善良 … 混乱邪恶），带图标给车卡流程用。</summary>
    public const string AlignmentsFile = "阵营.xlsx";

    /// <summary>18 项技能（关联属性、说明）。</summary>
    public const string SkillsFile = "技能.xlsx";

    /// <summary>语言（标准语 / 异界语）。</summary>
    public const string LanguagesFile = "语言.xlsx";

    /// <summary>背景详情：技能、工具、语言、装备、背景特性。</summary>
    public const string BackgroundDetailsFile = "背景详情.xlsx";

    /// <summary>1 级职业特性选项（战斗风格 / 宿敌 / 自然探索者）。</summary>
    public const string ClassOptionsFile = "职业选项.xlsx";

    /// <summary>护甲 / 武器 / 职业起始装备包。</summary>
    public const string EquipmentFile = "装备.xlsx";

    public const string InspirationsFile = "职业灵感.xlsx";

    public const string FlowFile = "车卡流程.xlsx";

    public const string LevelsFile = "职业等级.xlsx";

    /// <summary>关键词分类编号表：三位分类号 + 名称，关键词 ID = 分类号 + 五位序号。</summary>
    public const string KeywordCategoriesFile = "关键词分类.xlsx";

    /// <summary>关键词按分类拆成多个文件，命名规则：关键词.分类.xlsx（分类可以带点，例如 法术.一环）。</summary>
    public const string KeywordFilePrefix = "关键词";

    /// <summary>没有分类的关键词文件 / 工作表里的兜底分类名。</summary>
    public const string Uncategorized = "未分类";

    public static string? Resolve(string? configuredPath, string contentRootPath)
    {
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            var candidate = Path.IsPathRooted(configuredPath)
                ? configuredPath
                : Path.GetFullPath(Path.Combine(contentRootPath, configuredPath));

            return HasMarker(candidate) ? candidate : null;
        }

        foreach (var start in new[] { contentRootPath, AppContext.BaseDirectory })
        {
            var found = FindUpwards(start);
            if (found is not null)
            {
                return found;
            }
        }

        return null;
    }

    public static string FindRequired(string? configuredPath, string contentRootPath)
        => Resolve(configuredPath, contentRootPath)
            ?? throw new ContentException(
                $"找不到内容目录 data/（需要包含 {MarkerFile}）。请确认仓库根目录有 data/，或用配置项 Content:DataPath 指定路径。");

    private static string? FindUpwards(string startPath)
    {
        var directory = new DirectoryInfo(Path.GetFullPath(startPath));

        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "data");
            if (HasMarker(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        return null;
    }

    private static bool HasMarker(string directory)
        => Directory.Exists(directory) && File.Exists(Path.Combine(directory, MarkerFile));
}
