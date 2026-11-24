using System.Text.Json;
using TRPGHelper.Domain.Entities;

namespace TRPGHelper.Infrastructure.Data;

/// <summary>
/// 内置的生成器素材（姓名片段、职业、背景、职业卡、种族、灵感）。
/// 首次启动时写入数据库，之后由管理台维护——这里只是"出厂值"。
/// </summary>
public static class GeneratorSeedData
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private static readonly string[] CocSurnames =
    [
        "陈", "林", "王", "李", "张", "刘", "黄", "赵", "周", "吴",
        "徐", "孙", "马", "朱", "胡", "郭", "何", "高", "罗", "郑",
        "梁", "谢", "宋", "唐", "许", "韩", "冯", "邓", "曹", "彭",
        "曾", "萧", "田", "董", "袁", "潘", "于", "蒋", "蔡", "余",
    ];

    private static readonly string[] CocGivenNames =
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

    private static readonly string[] DndClassNames =
    [
        "战士", "法师", "盗贼", "牧师", "游侠", "圣武士", "德鲁伊", "吟游诗人",
        "术士", "邪术师", "武僧", "野蛮人",
    ];

    private static readonly string[] DndBackgrounds =
    [
        "士兵", "贵族", "平民", "罪犯", "学徒", "佣兵", "隐士", "水手", "艺人", "流浪者",
    ];

    public static IReadOnlyList<GeneratorEntry> Build()
    {
        var entries = new List<GeneratorEntry>();

        entries.AddRange(Names("coc7", "surname", CocSurnames));
        entries.AddRange(Names("coc7", "given", CocGivenNames));
        entries.AddRange(Names("dnd5e", "first", FantasyFirstNames));
        entries.AddRange(Names("dnd5e", "epithet", FantasyEpithets));

        entries.AddRange(Values("coc7", GeneratorKind.Occupation, "occupation", CocOccupations));
        entries.AddRange(Values("dnd5e", GeneratorKind.Occupation, "occupation", DndClassNames));
        entries.AddRange(Values("dnd5e", GeneratorKind.Background, "background", DndBackgrounds));

        entries.AddRange(Classes());
        entries.AddRange(Races());
        entries.AddRange(Inspirations("coc7", CocHooks));
        entries.AddRange(Inspirations("dnd5e", DndHooks));

        return entries;
    }

    private static readonly (string Code, string Name, string Description, string HitDie, string PrimaryAbility, string[] Features)[] ClassSeeds =
    [
        ("ranger", "游侠", "荒野的猎手，擅长弓箭、追踪与地形，带着动物伙伴在队伍外围游走。", "d10", "敏捷 / 感知",
        [
            "宿敌：选定一类敌人，追踪与回忆其情报时更占优势。",
            "自然探索者：在偏好地形中行军不会迷路，移动更高效。",
            "战斗风格：弓箭或双武器等专长任选。",
            "法术：从 1 级起获得施法能力，常用猎人印记。",
            "额外攻击：5 级起一回合可以攻击两次。",
        ]),
        ("fighter", "战士", "武器与护甲的大师，靠战术、韧性和稳定输出吃饭，队伍里最可靠的那把剑。", "d10", "力量 / 敏捷",
        [
            "战斗风格：防御、决斗、双手武器等专长任选。",
            "回气：短休恢复 1d10 + 等级的生命值。",
            "动作如潮：短休一次，额外获得一个动作。",
            "额外攻击：5 级起一回合可以攻击两次。",
            "不屈：豁免失败时可以选择重掷。",
        ]),
        ("monk", "武僧", "以气御身的近身武者，赤手空拳也能打出连击，速度与控制兼备。", "d8", "敏捷 / 感知",
        [
            "武艺：空手与武僧武器可用敏捷，伤害骰随等级提升。",
            "气：获得等于等级的元气点，用于疾风连击、闪避与疾走。",
            "无甲防御：未穿甲时 AC = 10 + 敏捷 + 感知。",
            "步伐如风：移动速度随等级提升，后期可在墙面行走。",
            "震慑拳：命中时消耗 1 点气，迫使目标进行体质豁免。",
        ]),
        ("druid", "德鲁伊", "聆听自然的施法者，能操控植物、召唤野兽，也能化身兽形冲锋。", "d8", "感知",
        [
            "德鲁伊法术：以感知准备法术，掌握戏法与自然法术。",
            "野性变身：2 级起可变形为野兽，可使用的形态随等级提升。",
            "德鲁伊语：掌握秘密语言，能留下只有同门看得懂的暗号。",
            "自然恢复：短休时恢复部分法术位。",
            "德鲁伊结社：2 级选择结社，获得额外能力与形态。",
        ]),
        ("paladin", "圣武士", "披甲执盾的圣战士，用誓言换取神术与光耀伤害，是队伍里最稳的定海神针。", "d10", "力量 / 魅力",
        [
            "神圣感知：能感应到附近的邪魔、不死生物与圣化之地。",
            "圣疗：治疗池为等级 × 5，可治伤或祛病解毒。",
            "战斗风格：防御、决斗、双手武器等专长任选。",
            "至圣斩：命中时消耗法术位，追加大量光耀伤害。",
            "守誓与光环：立下誓言后获得引导神力与护卫光环。",
        ]),
    ];

    private static readonly (string Code, string Name, string Description, string AbilityBonus, int Speed)[] RaceSeeds =
    [
        ("human", "人类", "适应力最强，属性全面发展。", "全属性 +1", 30),
        ("elf", "精灵", "敏捷而敏锐，拥有黑暗视觉与精类血统。", "敏捷 +2", 30),
        ("dwarf", "矮人", "坚韧的山地工匠，黑暗视觉与毒素抗性。", "体质 +2", 25),
        ("halfling", "半身人", "幸运的小个子，胆子却不小。", "敏捷 +2", 25),
        ("dragonborn", "龙裔", "龙血后裔，拥有吐息武器与元素抗性。", "力量 +2，魅力 +1", 30),
        ("gnome", "侏儒", "好奇心旺盛的发明家，擅长对抗魔法。", "智力 +2", 25),
        ("half-elf", "半精灵", "两个世界都懂一点，擅长社交且多才多艺。", "魅力 +2，另两项 +1", 30),
        ("half-orc", "半兽人", "强壮的混血儿，濒死时也能再撑一下。", "力量 +2，体质 +1", 30),
        ("tiefling", "提夫林", "带着魔裔血统，拥有火焰抗性与黑暗视觉。", "魅力 +2，智力 +1", 30),
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

    private static IEnumerable<GeneratorEntry> Names(string systemCode, string category, string[] values)
        => Values(systemCode, GeneratorKind.NamePart, category, values);

    private static IEnumerable<GeneratorEntry> Values(
        string systemCode,
        GeneratorKind kind,
        string category,
        string[] values)
        => values.Select((value, index) => new GeneratorEntry
        {
            SystemCode = systemCode,
            Kind = kind,
            Category = category,
            Value = value,
            SortOrder = (index + 1) * 10,
            IsEnabled = true,
        });

    private static IEnumerable<GeneratorEntry> Classes()
        => ClassSeeds.Select((seed, index) => new GeneratorEntry
        {
            SystemCode = "dnd5e",
            Kind = GeneratorKind.Class,
            Category = seed.Code,
            Value = seed.Name,
            ExtraJson = JsonSerializer.Serialize(new
            {
                description = seed.Description,
                hitDie = seed.HitDie,
                primaryAbility = seed.PrimaryAbility,
                features = seed.Features,
                // 默认指向前端 static 目录里的同名配图，管理台可以改成上传后的地址。
                imageUrl = $"/static/classes/{seed.Code}.jpg",
            }, JsonOptions),
            SortOrder = (index + 1) * 10,
            IsEnabled = true,
        });

    /// <summary>车卡流程的出厂步骤：第一阶段只有「选择职业」一步，其余由管理台按需添加。</summary>
    public static IEnumerable<CreationStep> BuildCreationSteps()
    {
        yield return new CreationStep
        {
            SystemCode = "dnd5e",
            Kind = CreationStepKind.Class,
            Title = "选择职业",
            Description = "先选一张职业卡，后面的步骤会围绕它展开。",
            SortOrder = 10,
            IsEnabled = true,
        };
    }

    /// <summary>
    /// 职业等级成长（DND 5E SRD 基础职业特性，1-20 级）。
    /// 按用户要求先不写副职（武术范型 / 结社 / 誓言 / 范型 / 传统），这些等级用「副职特性（暂未接入）」占位。
    /// </summary>
    public static IReadOnlyList<ClassLevelFeature> BuildClassLevels()
    {
        var list = new List<ClassLevelFeature>();

        void Add(string classCode, int level, string title, string description)
        {
            var used = list.Count(item => item.ClassCode == classCode && item.Level == level);
            list.Add(new ClassLevelFeature
            {
                SystemCode = "dnd5e",
                ClassCode = classCode,
                Level = level,
                Title = title,
                Description = description,
                SortOrder = level * 100 + used * 10,
                IsEnabled = true,
            });
        }

        const string Subclass = "副职特性（暂未接入）";
        const string SubclassNote = "这一级只有[副职]（范型 / 结社 / 誓言 / 传统）特性，等接入副职后再补。";

        // ---------------- 游侠 ranger ----------------
        Add("ranger", 1, "宿敌", "选定一类宿敌（异怪、野兽、龙、精类、邪魔、不死生物等），追踪与回忆其情报时获得优势。");
        Add("ranger", 1, "自然探索者", "选定一种偏好地形，在该地形中行军不减速、不会迷路，觅食可获得双倍食物。");
        Add("ranger", 2, "战斗风格", "从[弓箭术]、[防御]、[决斗]、[双武器战斗]中选择一项，获得对应加成。");
        Add("ranger", 2, "施法", "以[感知]为施法属性，凭[法术位]施放游侠法术。");
        Add("ranger", 3, "原始感知", "消耗一个法术位，感知 1 英里内特定类型生物的存在。");
        Add("ranger", 3, Subclass, SubclassNote);
        Add("ranger", 4, "属性值提升", "一项属性 +2，或两项各 +1（上限 20）。");
        Add("ranger", 5, "额外攻击", "使用攻击动作时可以攻击两次。");
        Add("ranger", 6, "宿敌与自然探索者强化", "再获得一种宿敌类型与一种偏好地形，并学会宿敌的语言。");
        Add("ranger", 7, Subclass, SubclassNote);
        Add("ranger", 8, "属性值提升", "一项属性 +2，或两项各 +1。");
        Add("ranger", 8, "穿林", "非魔法[困难地形]不额外消耗移动力，可穿过带刺植物而不受伤。");
        Add("ranger", 9, "法术位提升", "没有新增职业特性，[法术位]与已知法术按表提升。");
        Add("ranger", 10, "自然探索者强化", "再获得一种偏好地形。");
        Add("ranger", 10, "藏匿", "花 1 分钟制作伪装，静止不动时[隐匿]检定 +10。");
        Add("ranger", 11, Subclass, SubclassNote);
        Add("ranger", 12, "属性值提升", "一项属性 +2，或两项各 +1。");
        Add("ranger", 13, "法术位提升", "没有新增职业特性，[法术位]按表提升。");
        Add("ranger", 14, "宿敌强化", "再获得一种宿敌类型。");
        Add("ranger", 14, "消失", "可用附赠动作执行藏匿，且非魔法手段无法追踪你。");
        Add("ranger", 15, Subclass, SubclassNote);
        Add("ranger", 16, "属性值提升", "一项属性 +2，或两项各 +1。");
        Add("ranger", 17, "法术位提升", "没有新增职业特性，[法术位]按表提升。");
        Add("ranger", 18, "野性感知", "攻击看不见的生物不受劣势，并感知 30 尺内的隐形生物。");
        Add("ranger", 19, "属性值提升", "一项属性 +2，或两项各 +1。");
        Add("ranger", 20, "宿敌杀手", "每回合一次，对一个宿敌的攻击检定或伤害检定加上[感知]调整值。");

        // ---------------- 战士 fighter ----------------
        Add("fighter", 1, "战斗风格", "从[弓箭术]、[防御]、[决斗]、巨武器战斗、守护、[双武器战斗]中选择一项。");
        Add("fighter", 1, "回气", "附赠动作恢复 1d10 + 战士等级 的生命值，短休或长休后恢复。");
        Add("fighter", 2, "行动如潮", "一回合内额外获得一个动作，短休或长休后恢复。");
        Add("fighter", 3, Subclass, SubclassNote);
        Add("fighter", 4, "属性值提升", "一项属性 +2，或两项各 +1（上限 20）。");
        Add("fighter", 5, "额外攻击", "使用攻击动作时可以攻击两次。");
        Add("fighter", 6, "属性值提升", "一项属性 +2，或两项各 +1。");
        Add("fighter", 7, Subclass, SubclassNote);
        Add("fighter", 8, "属性值提升", "一项属性 +2，或两项各 +1。");
        Add("fighter", 9, "不屈", "豁免失败时可以重掷一次，长休后恢复。");
        Add("fighter", 10, Subclass, SubclassNote);
        Add("fighter", 11, "额外攻击（二）", "使用攻击动作时可以攻击三次。");
        Add("fighter", 12, "属性值提升", "一项属性 +2，或两项各 +1。");
        Add("fighter", 13, "不屈（两次）", "不屈可以使用两次。");
        Add("fighter", 14, "属性值提升", "一项属性 +2，或两项各 +1。");
        Add("fighter", 15, Subclass, SubclassNote);
        Add("fighter", 16, "属性值提升", "一项属性 +2，或两项各 +1。");
        Add("fighter", 17, "行动如潮（两次）", "行动如潮可以连续使用两次。");
        Add("fighter", 17, "不屈（三次）", "不屈可以使用三次。");
        Add("fighter", 18, Subclass, SubclassNote);
        Add("fighter", 19, "属性值提升", "一项属性 +2，或两项各 +1。");
        Add("fighter", 20, "额外攻击（三）", "使用攻击动作时可以攻击四次。");

        // ---------------- 武僧 monk ----------------
        Add("monk", 1, "无甲防御", "未穿甲且不持盾时，AC = 10 + [敏捷]调整值 + [感知]调整值。");
        Add("monk", 1, "武功", "空手与武僧武器可用[敏捷]，伤害骰从 1d4 起随等级提升，攻击后可附赠一次徒手打击。");
        Add("monk", 2, "气", "获得等于武僧等级的元气点，可驱动疾风连击、闪避与疾走。");
        Add("monk", 2, "无甲移动", "未穿甲时移动速度 +10 尺。");
        Add("monk", 3, "偏转飞弹", "用反应减少远程武器伤害，减到 0 时可以接住并掷回。");
        Add("monk", 3, Subclass, SubclassNote);
        Add("monk", 4, "属性值提升", "一项属性 +2，或两项各 +1。");
        Add("monk", 4, "轻身坠", "用反应减少坠落伤害，数值为武僧等级 × 5。");
        Add("monk", 5, "额外攻击", "使用攻击动作时可以攻击两次。");
        Add("monk", 5, "震慑拳", "命中时消耗 1 点气，目标[体质]豁免失败则被震慑。");
        Add("monk", 6, "气劲拳", "徒手打击视为魔法武器，用于克服非魔法抗性。");
        Add("monk", 6, Subclass, SubclassNote);
        Add("monk", 7, "闪避", "[敏捷]豁免成功时完全免伤，失败时只受一半伤害。");
        Add("monk", 7, "静心", "用动作结束自己身上的魅惑或恐惧。");
        Add("monk", 8, "属性值提升", "一项属性 +2，或两项各 +1。");
        Add("monk", 9, "无甲移动强化", "可沿垂直面或水面移动而不坠落。");
        Add("monk", 10, "净化", "免疫疾病与毒素。");
        Add("monk", 11, Subclass, SubclassNote);
        Add("monk", 12, "属性值提升", "一项属性 +2，或两项各 +1。");
        Add("monk", 13, "万语通", "理解所有语言，任何懂语言的生物也能听懂你说的话。");
        Add("monk", 14, "钻石魂", "熟练所有豁免；豁免失败时可花 1 点气重掷。");
        Add("monk", 15, "永恒之躯", "不再因年老衰弱，也不需要饮食。");
        Add("monk", 16, "属性值提升", "一项属性 +2，或两项各 +1。");
        Add("monk", 17, Subclass, SubclassNote);
        Add("monk", 18, "空之躯", "花 4 点气隐身 1 分钟并获得伤害抗性，也可花 8 点气施放星界投射。");
        Add("monk", 19, "属性值提升", "一项属性 +2，或两项各 +1。");
        Add("monk", 20, "完美自我", "战斗开始时若没有气，恢复 4 点气。");

        // ---------------- 德鲁伊 druid ----------------
        Add("druid", 1, "德鲁伊语", "掌握德鲁伊的秘密语言，可留下只有同门看得懂的暗号。");
        Add("druid", 1, "施法", "以[感知]为施法属性，准备并施放德鲁伊法术。");
        Add("druid", 2, "野性变身", "用动作变为野兽形态，每短休 2 次，可变形态随等级提升。");
        Add("druid", 2, Subclass, SubclassNote);
        Add("druid", 3, "法术位提升", "没有新增职业特性，[法术位]按表提升。");
        Add("druid", 4, "野性变身强化", "可变身挑战等级 1/2 的野兽。");
        Add("druid", 4, "属性值提升", "一项属性 +2，或两项各 +1。");
        Add("druid", 5, "法术位提升", "没有新增职业特性，[法术位]按表提升。");
        Add("druid", 6, Subclass, SubclassNote);
        Add("druid", 7, "法术位提升", "没有新增职业特性，[法术位]按表提升。");
        Add("druid", 8, "野性变身强化", "可变身挑战等级 1 的野兽。");
        Add("druid", 8, "属性值提升", "一项属性 +2，或两项各 +1。");
        Add("druid", 9, "法术位提升", "没有新增职业特性，[法术位]按表提升。");
        Add("druid", 10, Subclass, SubclassNote);
        Add("druid", 11, "法术位提升", "没有新增职业特性，[法术位]按表提升。");
        Add("druid", 12, "属性值提升", "一项属性 +2，或两项各 +1。");
        Add("druid", 13, "法术位提升", "没有新增职业特性，[法术位]按表提升。");
        Add("druid", 14, Subclass, SubclassNote);
        Add("druid", 15, "法术位提升", "没有新增职业特性，[法术位]按表提升。");
        Add("druid", 16, "属性值提升", "一项属性 +2，或两项各 +1。");
        Add("druid", 17, "法术位提升", "没有新增职业特性，[法术位]按表提升。");
        Add("druid", 18, "永恒之躯", "每过 10 年身体只衰老 1 年。");
        Add("druid", 18, "凶兽施法", "在野性变身的形态下也可以施放德鲁伊法术。");
        Add("druid", 19, "属性值提升", "一项属性 +2，或两项各 +1。");
        Add("druid", 20, "大德鲁伊", "野性变身次数无限，并可忽略无法术成分的施法材料。");

        // ---------------- 圣武士 paladin ----------------
        Add("paladin", 1, "神圣感知", "用动作感知 60 尺内的天界生物、邪魔、不死生物，以及圣化或亵渎之地。");
        Add("paladin", 1, "圣疗", "治疗池等于圣武士等级 × 5，也可消耗 5 点治疗一种疾病或毒素。");
        Add("paladin", 2, "战斗风格", "从[防御]、[决斗]、巨武器战斗、守护中选择一项。");
        Add("paladin", 2, "施法", "以[魅力]为施法属性，准备并施放圣武士法术。");
        Add("paladin", 2, "至圣斩", "近战命中时消耗[法术位]追加 2d8 光耀伤害，每高一环再 +1d8。");
        Add("paladin", 3, "神圣健康", "免疫疾病。");
        Add("paladin", 3, Subclass, SubclassNote);
        Add("paladin", 4, "属性值提升", "一项属性 +2，或两项各 +1。");
        Add("paladin", 5, "额外攻击", "使用攻击动作时可以攻击两次。");
        Add("paladin", 6, "守护光环", "10 尺内的友军在豁免上获得你的[魅力]加值。");
        Add("paladin", 7, Subclass, SubclassNote);
        Add("paladin", 8, "属性值提升", "一项属性 +2，或两项各 +1。");
        Add("paladin", 9, "法术位提升", "没有新增职业特性，[法术位]按表提升。");
        Add("paladin", 10, "勇气光环", "10 尺内的友军不会陷入恐惧。");
        Add("paladin", 11, "强化至圣斩", "近战武器命中时额外造成 1d8 光耀伤害。");
        Add("paladin", 12, "属性值提升", "一项属性 +2，或两项各 +1。");
        Add("paladin", 13, "法术位提升", "没有新增职业特性，[法术位]按表提升。");
        Add("paladin", 14, "净化之触", "用动作结束一个法术，可使用次数等于[魅力]调整值。");
        Add("paladin", 15, Subclass, SubclassNote);
        Add("paladin", 16, "属性值提升", "一项属性 +2，或两项各 +1。");
        Add("paladin", 17, "法术位提升", "没有新增职业特性，[法术位]按表提升。");
        Add("paladin", 18, "光环强化", "守护光环与勇气光环的范围从 10 尺扩大到 30 尺。");
        Add("paladin", 19, "属性值提升", "一项属性 +2，或两项各 +1。");
        Add("paladin", 20, Subclass, SubclassNote);

        return list;
    }

    /// <summary>
    /// 关键词的出厂词条：DND 六项属性 + 属性检定，互相引用，方便直接看到「点关键词弹解释、解释里还能再点」的效果。
    /// </summary>
    public static IReadOnlyList<Keyword> BuildKeywords()
    {
        var attributes = new (string Key, string Content)[]
        {
            ("力量", "衡量肌肉力量与负荷能力。影响近战攻击与伤害、力量豁免，也决定你能推动或举起多重的物体。"),
            ("敏捷", "衡量协调、平衡与反应速度。影响护甲等级、先攻、敏捷豁免，以及一切需要身手的动作。"),
            ("体质", "衡量耐力与健康。影响生命值上限、体质豁免，以及长时间行军与恶劣环境的耐受度。"),
            ("智力", "衡量记忆、推理与知识储备。影响调查、历史、奥秘这类知识检定。"),
            ("感知", "衡量专注、直觉与观察力。影响察觉、洞察、生存这类检定，也是牧师、德鲁伊的施法属性。"),
            ("魅力", "衡量存在感与社交影响力。影响说服、欺骗、威吓，也是术士、圣武士的施法属性。"),
        };

        var keywords = new List<Keyword>();
        var sort = 10;
        foreach (var (key, content) in attributes)
        {
            keywords.Add(new Keyword
            {
                Key = key,
                Title = key,
                Content = $"{content} 需要判定时参与[属性检定]。",
                SortOrder = sort,
                IsEnabled = true,
            });
            sort += 10;
        }

        keywords.Add(new Keyword
        {
            Key = "属性检定",
            Title = "属性检定",
            Content = "掷 d20 加上对应的属性调整值（必要时再加熟练加值），对抗 DM 设定的难度值。六项属性分别是 [力量]、[敏捷]、[体质]、[智力]、[感知]、[魅力]。",
            SortOrder = sort,
            IsEnabled = true,
        });
        sort += 10;

        var glossary = new (string Key, string Content)[]
        {
            ("弓箭术", "战斗风格选项：使用远程武器进行攻击时，攻击检定获得 +2 加值。"),
            ("防御", "战斗风格选项：穿着护甲时，护甲等级（AC）获得 +1 加值。"),
            ("决斗", "战斗风格选项：单手挥舞近战武器且另一只手不持武器时，该武器的伤害检定 +2。"),
            ("双武器战斗", "战斗风格选项：进行双武器战斗时，可以把属性调整值加到第二次攻击的伤害上。"),
            ("法术位", "施放法术要消耗的资源。法术位分环阶，施法者等级越高可用的环阶越高；完成一次长休后恢复全部已消耗的法术位。"),
            ("副职", "职业在特定等级选择的专精方向（游侠范型、武术范型、德鲁伊结社、神圣誓言、武僧传统）。本作暂未接入副职，等级表里对应等级只做占位。"),
        };

        foreach (var (key, content) in glossary)
        {
            keywords.Add(new Keyword
            {
                Key = key,
                Title = key,
                Content = content,
                SortOrder = sort,
                IsEnabled = true,
            });
            sort += 10;
        }

        return keywords;
    }

    private static IEnumerable<GeneratorEntry> Races()
        => RaceSeeds.Select((seed, index) => new GeneratorEntry
        {
            SystemCode = "dnd5e",
            Kind = GeneratorKind.Race,
            Category = seed.Code,
            Value = seed.Name,
            ExtraJson = JsonSerializer.Serialize(new
            {
                description = seed.Description,
                abilityBonus = seed.AbilityBonus,
                speed = seed.Speed,
            }, JsonOptions),
            SortOrder = (index + 1) * 10,
            IsEnabled = true,
        });

    private static IEnumerable<GeneratorEntry> Inspirations(string systemCode, (string Title, string Text)[] hooks)
        => hooks.Select((hook, index) => new GeneratorEntry
        {
            SystemCode = systemCode,
            Kind = GeneratorKind.Inspiration,
            Category = "hook",
            Value = hook.Title,
            ExtraJson = JsonSerializer.Serialize(new { text = hook.Text }, JsonOptions),
            SortOrder = (index + 1) * 10,
            IsEnabled = true,
        });
}
