namespace TRPGHelper.Domain.Entities;

/// <summary>
/// 车卡流程里一步的类型，决定前端渲染哪种面板。
/// 选规则书（DND / COC）不算流程步骤，它是流程的前置条件。
/// </summary>
public enum CreationStepKind
{
    /// <summary>选职业（第一阶段唯一实现了面板的类型）。</summary>
    Class = 0,

    /// <summary>选子职（例如圣武士的神圣誓约）。</summary>
    Subclass = 5,

    /// <summary>选种族。</summary>
    Race = 1,

    /// <summary>选亚种（高等精灵、山地矮人等）。</summary>
    Subrace = 6,

    /// <summary>掷属性 / 分配属性。</summary>
    Attributes = 2,

    /// <summary>起名字。</summary>
    Name = 3,

    /// <summary>确认卡面并提交。</summary>
    Summary = 4,

    /// <summary>决定背景：先是九宫格阵营，后面再补背景本身。</summary>
    Background = 7,

    /// <summary>选技能熟练项（职业选 N 项 + 背景固定项）。</summary>
    Skills = 8,

    /// <summary>选额外语言。</summary>
    Languages = 9,

    /// <summary>1 级职业特性选择（战斗风格 / 宿敌 / 自然探索者）。</summary>
    Features = 10,

    /// <summary>起始装备与武器选择。</summary>
    Equipment = 11,

    /// <summary>姓名 / 性别 / 年龄 / 外貌 / 个性。</summary>
    Description = 12,
}

/// <summary>
/// 车卡流程的一步。管理台可以配置步骤数量、顺序与标题，
/// 前端按 <see cref="Kind"/> 渲染对应面板。
/// </summary>
public class CreationStep
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>所属规则系统（GameSystems.Code），为空表示所有系统通用。</summary>
    public string? SystemCode { get; set; }

    public CreationStepKind Kind { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int SortOrder { get; set; }

    public bool IsEnabled { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
