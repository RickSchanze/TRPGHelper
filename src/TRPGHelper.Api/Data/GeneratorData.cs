using TRPGHelper.Api.Contracts;

namespace TRPGHelper.Api.Data;

/// <summary>
/// 辅助车卡的素材库：姓名、职业、灵感。首发用内置数据，后续可以搬到数据库 / 由用户自建。
/// </summary>
internal static class GeneratorData
{
    private static readonly string[] Surnames =
    [
        "陈", "林", "王", "李", "张", "刘", "黄", "赵", "周", "吴",
        "徐", "孙", "马", "朱", "胡", "郭", "何", "高", "罗", "郑",
        "梁", "谢", "宋", "唐", "许", "韩", "冯", "邓", "曹", "彭",
        "曾", "萧", "田", "董", "袁", "潘", "于", "蒋", "蔡", "余",
    ];

    private static readonly string[] GivenNames =
    [
        "静", "晓", "明", "子墨", "雨桐", "思远", "若曦", "一鸣", "嘉禾", "文博",
        "安然", "泽宇", "书瑶", "亦辰", "清和", "知夏", "望舒", "南星", "予安", "沐白",
        "立诚", "海生", "承志", "玉兰", "小满", "长风", "未晞", "秋实", "鹤鸣", "云舟",
    ];

    private static readonly string[] FantasyFirstNames =
    [
        "艾拉", "塞琳", "伊莱", "洛恩", "薇拉", "凯尔", "米娅", "亚瑟", "瑟拉", "雷恩",
        "诺拉", "达里尔", "伊莎", "奥兰", "卡莲", "提姆", "露娜", "艾德", "西格", "菲奥娜",
    ];

    private static readonly string[] FantasyEpithets =
    [
        "晨风", "夜歌", "铁誓", "银叶", "灰烬", "星语", "石心", "赤焰", "霜刃", "静水",
    ];

    private static readonly string[] CocOccupations =
    [
        "私家侦探", "记者", "医生", "大学教授", "古董商", "图书管理员", "警探", "精神科医生",
        "小说家", "画家", "牧师", "退役军人", "飞行员", "工程师", "律师", "会计师",
        "摄影师", "考古学家", "药剂师", "护士", "码头工人", "黑帮成员", "神秘学家", "探险家",
    ];

    private static readonly string[] DndClasses =
    [
        "战士", "法师", "盗贼", "牧师", "游侠", "圣武士", "德鲁伊", "吟游诗人",
        "术士", "邪术师", "武僧", "野蛮人",
    ];

    private static readonly string[] DndBackgrounds =
    [
        "士兵", "贵族", "平民", "罪犯", "学徒", "佣兵", "隐士", "水手", "艺人", "流浪者",
    ];

    private static readonly ClassOptionDto[] DndClassOptions =
    [
        new("barbarian", "野蛮人", "靠怒火冲锋的近战猛兽，生命值厚得离谱。", "d12", "力量"),
        new("bard", "吟游诗人", "用歌声和话术搅动战场，什么都会一点。", "d8", "魅力"),
        new("cleric", "牧师", "神祇的代行者，治疗与神术兼备。", "d8", "感知"),
        new("druid", "德鲁伊", "化身野兽、操控自然，荒野里最难缠的对手。", "d8", "感知"),
        new("fighter", "战士", "武器大师，稳定、可靠、正面能打。", "d10", "力量 / 敏捷"),
        new("monk", "武僧", "以气御身，赤手空拳也能打出连击。", "d8", "敏捷 / 感知"),
        new("paladin", "圣武士", "誓言与重甲，既能扛也能砍。", "d10", "力量 / 魅力"),
        new("ranger", "游侠", "荒野猎手，弓箭与追踪样样在行。", "d10", "敏捷 / 感知"),
        new("rogue", "盗贼", "潜行与偷袭的好手，正面不硬但很致命。", "d8", "敏捷"),
        new("sorcerer", "术士", "血脉里流淌着魔法，天生就会施法。", "d6", "魅力"),
        new("warlock", "邪术师", "与强大存在签下契约，借来力量。", "d8", "魅力"),
        new("wizard", "法师", "把魔法写进书里，准备得越充分越可怕。", "d6", "智力"),
    ];

    private static readonly RaceOptionDto[] DndRaceOptions =
    [
        new("human", "人类", "适应力最强，属性全面发展。", "全属性 +1", 30),
        new("elf", "精灵", "敏捷而敏锐，拥有黑暗视觉与精类血统。", "敏捷 +2", 30),
        new("dwarf", "矮人", "坚韧的山地工匠，黑暗视觉与毒素抗性。", "体质 +2", 25),
        new("halfling", "半身人", "幸运的小个子，胆子却不小。", "敏捷 +2", 25),
        new("dragonborn", "龙裔", "龙血后裔，拥有吐息武器与元素抗性。", "力量 +2，魅力 +1", 30),
        new("gnome", "侏儒", "好奇心旺盛的发明家，擅长对抗魔法。", "智力 +2", 25),
        new("half-elf", "半精灵", "两个世界都懂一点，擅长社交且多才多艺。", "魅力 +2，另两项 +1", 30),
        new("half-orc", "半兽人", "强壮的混血儿，濒死时也能再撑一下。", "力量 +2，体质 +1", 30),
        new("tiefling", "提夫林", "带着魔裔血统，拥有火焰抗性与黑暗视觉。", "魅力 +2，智力 +1", 30),
    ];

    private static readonly (string Title, string Text)[] CocHooks =
    [
        ("一封迟到的信", "信上写着三年前就该送达的地址，而收信人正是你。"),
        ("失踪的同事", "他最后一次出现是在城郊的旧疗养院，留下一串看不懂的坐标。"),
        ("旧宅的钥匙", "继承来的老宅里有一间上锁的房间，钥匙却挂在你的床头。"),
        ("午夜来电", "电话那头只有海浪声，以及一个反复念着你名字的嗓音。"),
        ("家族相册", "每张合影里都多出一个人，你从未见过他，但他一直在笑。"),
        ("博物馆的捐赠品", "那尊雕像被送来时，标签上写着你的出生日期。"),
        ("二手书店的日记", "日记最后一页的笔迹，和你的一模一样。"),
        ("雨夜的搭车人", "他浑身湿透，坚持要去一个地图上不存在的小镇。"),
        ("废弃疗养院", "院里的入住记录中，有一栏填着你的名字和空白的出院日期。"),
        ("姑妈的遗嘱", "遗产是一盒录音带，里面的人在尖叫，然后叫了你的名字。"),
        ("匿名包裹", "包裹里是一只旧怀表，指针停在某个你熟悉的时刻。"),
        ("老照片里的陌生人", "照片背面写着：别去那里。可你已经订好了车票。"),
    ];

    private static readonly (string Title, string Text)[] DndHooks =
    [
        ("雇佣兵契约", "一张没有署名的契约，报酬高得可疑，任务地点在一座刚消失的村庄。"),
        ("失落的矿坑", "矿工们挖穿了不该挖的东西，现在镇上的人开始做同一个梦。"),
        ("王都的密令", "密令只写了三个字：找到她。信封里是一缕银发。"),
        ("被诅咒的圣物", "圣物在夜里低语，愿意用它的人必须付出记忆作为代价。"),
        ("商队的请求", "商队首领愿意重金雇你护送，却坚持不让任何人打开第三辆马车。"),
        ("龙裔的预言", "老龙裔说你会死在同一把剑下，而那把剑此刻就在你背后。"),
        ("地下赌场", "赌场只收一种筹码：被遗忘的真名。"),
        ("森林低语", "森林在夜里会重复你说过的话，但总是慢半拍。"),
    ];

    public static IReadOnlyList<string> Names(string? systemCode, int count = 5, Random? random = null)
    {
        random ??= Random.Shared;
        count = Math.Clamp(count, 1, 20);
        var isFantasy = string.Equals(systemCode, "dnd5e", StringComparison.OrdinalIgnoreCase);
        var results = new List<string>(count);

        for (var attempt = 0; attempt < count * 30 && results.Count < count; attempt++)
        {
            var name = isFantasy
                ? $"{Pick(FantasyFirstNames, random)}·{Pick(FantasyEpithets, random)}"
                : $"{Pick(Surnames, random)}{Pick(GivenNames, random)}";

            if (!results.Contains(name))
            {
                results.Add(name);
            }
        }

        return results;
    }

    public static IReadOnlyList<string> Occupations(string? systemCode, int count = 5, Random? random = null)
    {
        random ??= Random.Shared;
        count = Math.Clamp(count, 1, 20);
        var isFantasy = string.Equals(systemCode, "dnd5e", StringComparison.OrdinalIgnoreCase);
        var pool = isFantasy ? DndClasses.Concat(DndBackgrounds).ToArray() : CocOccupations;

        return pool.OrderBy(_ => random.Next()).Take(count).ToList();
    }

    public static IReadOnlyList<ClassOptionDto> Classes(string? systemCode)
    {
        return IsDnd(systemCode) ? DndClassOptions : Array.Empty<ClassOptionDto>();
    }

    public static IReadOnlyList<RaceOptionDto> Races(string? systemCode)
    {
        return IsDnd(systemCode) ? DndRaceOptions : Array.Empty<RaceOptionDto>();
    }

    public static IReadOnlyList<(string Title, string Text)> Inspirations(string? systemCode, int count = 3, Random? random = null)
    {
        random ??= Random.Shared;
        count = Math.Clamp(count, 1, 10);
        var isFantasy = string.Equals(systemCode, "dnd5e", StringComparison.OrdinalIgnoreCase);
        var pool = isFantasy ? DndHooks : CocHooks;

        return pool.OrderBy(_ => random.Next()).Take(count).ToList();
    }

    private static bool IsDnd(string? systemCode)
        => string.Equals(systemCode, "dnd5e", StringComparison.OrdinalIgnoreCase);

    private static string Pick(string[] pool, Random random) => pool[random.Next(pool.Length)];
}
