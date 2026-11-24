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

    public const string OccupationsFile = "随机职业.xlsx";

    public const string RacesFile = "种族.xlsx";

    public const string BackgroundsFile = "背景.xlsx";

    public const string InspirationsFile = "职业灵感.xlsx";

    public const string FlowFile = "车卡流程.xlsx";

    public const string LevelsFile = "职业等级.xlsx";

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
