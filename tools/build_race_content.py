#!/usr/bin/env python3
"""生成 DND 5E 九个核心种族的展示数据（data/种族.xlsx）与词条（data/关键词.种族.xlsx）。

用法：
    python tools/build_race_content.py          # 重新生成
    python tools/build_race_content.py --check  # 只校验现有文件与脚本内容是否一致

写法对齐职业卡：描述、属性加值、特质里都用 [关键词] 标出可点的词条，
trait 行按「[关键词]：说明」组织；方括号里的名字都必须登记成词条，
所以除了 9 个种族，还会补上 20 个特质词条（种族分类）和 速度 / 体型 / 年龄 / 语言
（规则分类）。内容取材：5E 不全书 · 玩家手册（2014）种族章节。
改内容时优先改本文件里的常量再重跑，保证「脚本 → xlsx」可复现；
管理台里直接改完再「导出内容」也能写回，但下次跑本脚本会被覆盖。
"""

from __future__ import annotations

import argparse
import sys
from pathlib import Path

import openpyxl

REPO_ROOT = Path(__file__).resolve().parent.parent
DATA_DIR = REPO_ROOT / "data"

CATEGORY_FILE = DATA_DIR / "关键词分类.xlsx"
KEYWORD_FILE = DATA_DIR / "关键词.种族.xlsx"
RULE_KEYWORD_FILE = DATA_DIR / "关键词.规则.xlsx"
RACE_FILE = DATA_DIR / "种族.xlsx"

CATEGORY_NAME = "种族"
RULE_CATEGORY_NAME = "规则"
KEYWORD_HEADERS = ["id", "key", "分类", "title", "summary", "content", "imageUrl", "isEnabled"]

# 可选规则：任意两项 +2 / +1
HOUSE_RULE = "可选：不使用本族的固定属性加值，改为自选两项属性分别 +2 / +1。"

# ---- 规则分类的补充词条（写进 关键词.规则.xlsx，只补不覆盖已有行） ----
GENERAL_RULES = {
    "速度": (
        "步行速度，单位尺；种族给出 25 或 30 尺。",
        "你的基础步行速度，单位是尺。种族会给出数值：矮人、半身人、侏儒 25 尺，"
        "多数种族 30 尺；部分特质（如矮人穿重甲）会说明速度如何受影响。",
    ),
    "体型": (
        "角色的体形档位：中型或小型。",
        "角色在规则里的体形档位。种族会指定你是中型还是小型：半身人与侏儒是小型，"
        "人类、精灵、矮人、龙裔、半精灵、半兽人、提夫林是中型。体型影响能使用的武器与可穿过的空间。",
    ),
    "年龄": (
        "种族的成年年龄与平均寿命。",
        "种族给出的成年年龄与寿命，用来描写角色年纪与阅历；不直接参与数值计算。"
        "例如精灵 100 岁成年、可活 750 岁，人类不到 20 岁成年、很少活过 100 岁。",
    ),
    "语言": (
        "角色能说、读、写的语言，通常是通用语加一门种族语。",
        "创建角色时获得的语言。多数种族给通用语加一门种族语（精灵语、矮人语、龙语、兽人语、"
        "炼狱语等），人类额外自选一门，半精灵再多一门自选语言。",
    ),
}

# ---- 种族分类的特质词条：卡面上的 [方括号] 都指向这里 ----
TRAITS = {
    "属性值加成": (
        "种族带来的属性加值；可选规则下可改为任意两项 +2 / +1。",
        "创建角色时由种族提供的属性加值。人类是全部六项各 +1；变体人类改为自选两项各 +1，"
        "并获得一项[技能熟练]与一项[专长]。若开启可选规则，任何种族都可改为自选两项属性分别 +2 / +1。",
    ),
    "敏锐感官": (
        "精灵的感官天赋：获得察觉技能熟练。",
        "精灵特质：你获得[察觉]技能的[技能熟练]。",
    ),
    "妖精血统": (
        "对抗魅惑的豁免有优势，魔法也无法使你睡眠。",
        "精灵与半精灵特质：你进行对抗[魅惑]的[豁免]时具有优势，并且不会因魔法效应而陷入睡眠。",
    ),
    "出神": (
        "精灵不用睡觉，4 小时冥想等于 8 小时睡眠。",
        "精灵特质：精灵不需要睡眠，需要休息时进入持续 4 小时的半清醒冥想（出神），期间保持意识；"
        "结束后获得人类睡 8 小时的全部益处。",
    ),
    "矮人体魄": (
        "对抗毒素的豁免有优势，并拥有毒素抗性。",
        "矮人特质：你进行对抗毒素的[豁免]时具有优势，并且拥有毒素伤害的[抗性]。",
    ),
    "矮人战斗训练": (
        "战斧、手斧、轻锤、战锤的熟练项。",
        "矮人特质：你具有战斧、手斧、轻锤和战锤的熟练项。",
    ),
    "石中精妙": (
        "石制品的历史检定可加双倍熟练。",
        "矮人特质：对石制品做智力（历史）[属性检定]时，视为具有[历史]技能的[技能熟练]，"
        "并加上双倍的[熟练加值]。",
    ),
    "幸运": (
        "攻击、属性检定、豁免掷出 1 时可以重掷。",
        "半身人特质：当攻击检定、[属性检定]或[豁免]掷出 1 时，你可以重掷一次，但必须使用重掷的结果。",
    ),
    "勇气": (
        "对抗恐慌的豁免有优势。",
        "半身人特质：你进行对抗[恐慌]的[豁免]时具有优势。",
    ),
    "半身人灵巧": (
        "可以穿过体型比你大的生物所在空间。",
        "半身人特质：你可以穿越任何[体型]比你大的生物所在空间。",
    ),
    "龙族血统": (
        "选择龙种，决定吐息形态与对应抗性。",
        "龙裔特质：选定一个龙种，据此决定[吐息武器]的形态、伤害类型，以及你拥有的伤害[抗性]。",
    ),
    "吐息武器": (
        "用动作喷吐能量，2d6 起，短休或长休后恢复。",
        "龙裔特质：用动作呼出破坏性能量，范围内生物做[豁免]，DC = 8 + 体质调整值 + 熟练加值；"
        "失败受 2d6 伤害（6/11/16 级升到 3d6/4d6/5d6），成功减半；使用后需短休或长休恢复。",
    ),
    "侏儒狡黠": (
        "对抗魔法的智力、感知、魅力豁免有优势。",
        "侏儒特质：你进行对抗魔法的智力、感知和魅力[豁免]时具有优势。",
    ),
    "多才多艺": (
        "获得两项自选技能的熟练项。",
        "半精灵特质：你获得两项自选[技能熟练]。",
    ),
    "凶恶": (
        "获得威吓技能的熟练项。",
        "半兽人特质：你获得[威吓]技能的[技能熟练]。",
    ),
    "坚韧不屈": (
        "生命值降到 0 时可改为 1，长休后恢复。",
        "半兽人特质：当你的[生命值]降至 0 而没有立即死亡时，可以改为降至 1。"
        "此特质生效后需完成长休才能再次使用。",
    ),
    "凶蛮攻击": (
        "近战重击时额外掷一个伤害骰。",
        "半兽人特质：使用近战武器攻击造成重击时，额外掷一个该武器的伤害骰，并加入重击的额外伤害。",
    ),
    "炎狱抗性": (
        "对火焰伤害具有抗性。",
        "提夫林特质：你对火焰伤害具有[抗性]。",
    ),
    "地狱遗赠": (
        "习得奇术，3 级炼狱叱喝、5 级黑暗术。",
        "提夫林特质：习得戏法奇术；3 级可用此特质免费施放一次 2 环炼狱叱喝，"
        "5 级可免费施放一次[黑暗术]；长休后恢复。施法属性为魅力。",
    ),
    "亚种": (
        "种族的细分，例如高等精灵、山地矮人。",
        "部分种族在种族之下还有亚种：精灵分高等 / 木 / 卓尔，矮人分丘陵 / 山地，"
        "半身人分轻足 / 敦实，侏儒分森林 / 岩石。亚种会替换或追加特质。",
    ),
}

# ---- 九个核心种族 ----
# description / abilityBonus / traits 都按职业卡的习惯用 [关键词]；traits 每行一条。
RACES = [
    {
        "key": "人类",
        "summary": "全属性 +1，速度 30 尺；适应力最强的年轻种族，多才多艺。",
        "description": "适应力最强的年轻种族，六项[属性值加成]全覆盖，什么都想试试。",
        "abilityBonus": "[属性值加成] 全属性 +1",
        "content": (
            "属性值：全部六项各 +1（[属性值加成]）。[年龄]：不到 20 岁成年，很少活过 100 岁。"
            "[体型]：中型，身高 5 到 6 尺以上。[速度]：30 尺。[语言]：通用语和一门自选语言。"
            "变体人类（可选规则）改为自选两项属性各 +1，并获得一项[技能熟练]与一项[专长]。"
            + HOUSE_RULE
        ),
        "traits": [
            "[属性值加成]：全部六项各 +1",
            "变体人类：自选两项属性各 +1，获得一项[技能熟练]和一项[专长]",
        ],
    },
    {
        "key": "精灵",
        "summary": "敏捷 +2，速度 30 尺；黑暗视觉与妖精血统，寿命可达 750 岁。",
        "description": "敏捷而敏锐，天生拥有[黑暗视觉]与[妖精血统]。",
        "abilityBonus": "[敏捷] +2",
        "content": (
            "[敏捷] +2。[年龄]：100 岁左右成年，寿命可达 750 岁。[体型]：中型。[速度]：30 尺。"
            "拥有[黑暗视觉] 60 尺；[敏锐感官]：察觉熟练；[妖精血统]：对抗[魅惑]有优势且不会因魔法睡眠；"
            "[出神]：4 小时冥想即可获得 8 小时睡眠的益处。"
            "[亚种]：高等精灵、木精灵、卓尔。" + HOUSE_RULE
        ),
        "traits": [
            "[黑暗视觉]：60 尺",
            "[敏锐感官]：察觉熟练",
            "[妖精血统]：对抗[魅惑]有优势",
            "[出神]：4 小时冥想",
            "[亚种]：高等 / 木 / 卓尔",
        ],
    },
    {
        "key": "矮人",
        "summary": "体质 +2，速度 25 尺；黑暗视觉、毒素抗性，穿重甲不减速。",
        "description": "坚韧的山地工匠，拥有[黑暗视觉]与毒素[抗性]。",
        "abilityBonus": "[体质] +2",
        "content": (
            "[体质] +2。[年龄]：50 岁才算成年，平均活 350 年。[体型]：中型，身高 4 到 5 尺。"
            "[速度]：25 尺，穿重甲也不会减速。拥有[黑暗视觉] 60 尺；"
            "[矮人体魄]：对抗毒素的[豁免]有优势，并拥有毒素[抗性]；"
            "[矮人战斗训练]：战斧、手斧、轻锤、战锤熟练；工匠工具熟练项；"
            "[石中精妙]：石制品历史检定加双倍熟练。[亚种]：丘陵矮人、山地矮人。" + HOUSE_RULE
        ),
        "traits": [
            "[黑暗视觉]：60 尺",
            "[矮人体魄]：毒素[抗性]",
            "[矮人战斗训练]：四种武器熟练",
            "工具熟练项：工匠工具 ×1",
            "[石中精妙]：石制品双倍熟练",
            "[亚种]：丘陵 / 山地",
        ],
    },
    {
        "key": "半身人",
        "summary": "敏捷 +2，速度 25 尺，小型体型；掷出 1 可以重掷的幸运儿。",
        "description": "幸运的小个子，胆子却不小，很难被[恐慌]击倒。",
        "abilityBonus": "[敏捷] +2",
        "content": (
            "[敏捷] +2。[年龄]：20 岁成年，一般活到 150 岁。[体型]：小型，约 3 尺高。[速度]：25 尺。"
            "[幸运]：攻击检定、[属性检定]、[豁免]掷出 1 时可以重掷一次；"
            "[勇气]：对抗[恐慌]有优势；[半身人灵巧]：可以穿过[体型]更大的生物所在空间。"
            "[亚种]：轻足半身人、敦实半身人。" + HOUSE_RULE
        ),
        "traits": [
            "[幸运]：掷出 1 可重掷",
            "[勇气]：对抗[恐慌]有优势",
            "[半身人灵巧]：穿过更大生物",
            "[亚种]：轻足 / 敦实",
        ],
    },
    {
        "key": "龙裔",
        "summary": "力量 +2、魅力 +1，速度 30 尺；吐息武器与对应伤害抗性。",
        "description": "龙血后裔，拥有[吐息武器]与对应伤害[抗性]。",
        "abilityBonus": "[力量] +2、[魅力] +1",
        "content": (
            "[力量] +2、[魅力] +1。[年龄]：15 岁成年，约活 80 年。[体型]：中型，身高 6 尺以上。"
            "[速度]：30 尺。[龙族血统]：选定一个龙种，决定吐息与[抗性]的类型；"
            "[吐息武器]：用动作喷吐，2d6 起，6/11/16 级提升，短休或长休后恢复；"
            "伤害[抗性]：与龙种对应的伤害类型。[语言]：通用语和龙语。" + HOUSE_RULE
        ),
        "traits": [
            "[龙族血统]：选择龙种",
            "[吐息武器]：2d6 起",
            "伤害[抗性]：对应龙种",
            "[语言]：通用语 + 龙语",
        ],
    },
    {
        "key": "侏儒",
        "summary": "智力 +2，速度 25 尺，小型体型；黑暗视觉，对抗魔法的豁免有优势。",
        "description": "好奇心旺盛的发明家，对抗魔法的[豁免]更有把握。",
        "abilityBonus": "[智力] +2",
        "content": (
            "[智力] +2。[年龄]：40 岁左右成人，可活 350 到 500 岁。[体型]：小型，3 到 4 尺。"
            "[速度]：25 尺。拥有[黑暗视觉] 60 尺；"
            "[侏儒狡黠]：对抗魔法的智力、感知、魅力[豁免]有优势。"
            "[亚种]：森林侏儒、岩石侏儒。" + HOUSE_RULE
        ),
        "traits": [
            "[黑暗视觉]：60 尺",
            "[侏儒狡黠]：对抗魔法[豁免]有优势",
            "[亚种]：森林 / 岩石",
        ],
    },
    {
        "key": "半精灵",
        "summary": "魅力 +2、另选两项 +1，速度 30 尺；黑暗视觉、妖精血统与两项技能熟练。",
        "description": "两个世界都懂一点，拥有[黑暗视觉]与[妖精血统]，还多才多艺。",
        "abilityBonus": "[魅力] +2，另选两项属性各 +1",
        "content": (
            "[魅力] +2，另选两项属性值各 +1。[年龄]：20 岁左右成年，通常能活到 180 岁以上。"
            "[体型]：中型。[速度]：30 尺。拥有[黑暗视觉] 60 尺；"
            "[妖精血统]：对抗[魅惑]的[豁免]有优势，魔法也无法使你陷入睡眠；"
            "[多才多艺]：两项自选[技能熟练]。[语言]：通用语、精灵语和一门自选语言。" + HOUSE_RULE
        ),
        "traits": [
            "[黑暗视觉]：60 尺",
            "[妖精血统]：对抗[魅惑]有优势",
            "[多才多艺]：两项[技能熟练]",
        ],
    },
    {
        "key": "半兽人",
        "summary": "力量 +2、体质 +1，速度 30 尺；黑暗视觉、坚韧不屈与凶蛮攻击。",
        "description": "强壮的混血儿，凭[坚韧不屈]在[生命值]见底时再撑一下。",
        "abilityBonus": "[力量] +2、[体质] +1",
        "content": (
            "[力量] +2、[体质] +1。[年龄]：14 岁左右成年，很少活过 75 岁。[体型]：中型。[速度]：30 尺。"
            "拥有[黑暗视觉] 60 尺；[凶恶]：威吓熟练；"
            "[坚韧不屈]：[生命值]降到 0 且没有立即死亡时，可以改为降到 1，长休后恢复；"
            "[凶蛮攻击]：近战武器重击时额外掷一个伤害骰。[语言]：通用语和兽人语。" + HOUSE_RULE
        ),
        "traits": [
            "[黑暗视觉]：60 尺",
            "[凶恶]：威吓熟练",
            "[坚韧不屈]：可改为 1 点[生命值]",
            "[凶蛮攻击]：重击额外伤害骰",
        ],
    },
    {
        "key": "提夫林",
        "summary": "智力 +1、魅力 +2，速度 30 尺；火焰抗性与地狱遗赠法术。",
        "description": "带着炼狱血统，拥有火焰[抗性]与[黑暗视觉]。",
        "abilityBonus": "[智力] +1、[魅力] +2",
        "content": (
            "[智力] +1、[魅力] +2。[年龄]：成年岁数与人类相仿，寿命略长。[体型]：中型。[速度]：30 尺。"
            "拥有[黑暗视觉] 60 尺；[炎狱抗性]：火焰[抗性]；"
            "[地狱遗赠]：习得戏法奇术，3 级可免费施放一次 2 环炼狱叱喝、"
            "5 级可免费施放一次[黑暗术]，长休后恢复，施法属性为魅力。[语言]：通用语和炼狱语。"
            + HOUSE_RULE
        ),
        "traits": [
            "[黑暗视觉]：60 尺",
            "[炎狱抗性]：火焰[抗性]",
            "[地狱遗赠]：奇术 / 炼狱叱喝 / [黑暗术]",
        ],
    },
]


SUBRACE_FILE = DATA_DIR / "亚种.xlsx"

# ---- 亚种：按种族分组，车卡流程「选择亚种」这一步用 ----
SUBRACES = [
    {"id": "high-elf", "raceId": "elf", "name": "高等精灵",
     "description": "聪慧而优雅，天生会一手奥术。",
     "traits": ["[属性值加成]：[智力] +1", "一项自选法师戏法（[智力] 施法）",
                "长剑、短弓、长弓、短剑熟练", "额外一门自选语言"]},
    {"id": "wood-elf", "raceId": "elf", "name": "木精灵",
     "description": "森林的守望者，隐匿与速度见长。",
     "traits": ["[属性值加成]：[感知] +1", "[速度]：35 尺", "自然隐匿：可在轻度遮蔽中躲藏"]},
    {"id": "drow", "raceId": "elf", "name": "卓尔",
     "description": "幽暗地域的黑暗精灵，天生带魔法。",
     "traits": ["[属性值加成]：[魅力] +1", "[黑暗视觉]：120 尺", "阳光下易感：直接阳光中视物不利",
                "戏法：舞光术", "3 级妖火、5 级[黑暗术]各一次（长休恢复）"]},
    {"id": "hill-dwarf", "raceId": "dwarf", "name": "丘陵矮人",
     "description": "感官敏锐、格外坚韧的矮人。",
     "traits": ["[属性值加成]：[感知] +1", "矮人坚韧：每级[生命值]上限 +1"]},
    {"id": "mountain-dwarf", "raceId": "dwarf", "name": "山地矮人",
     "description": "更强壮、披甲作战的矮人。",
     "traits": ["[属性值加成]：[力量] +2", "轻甲与中甲熟练"]},
    {"id": "lightfoot-halfling", "raceId": "halfling", "name": "轻足半身人",
     "description": "容易相处、天生会躲的游历者。",
     "traits": ["[属性值加成]：[魅力] +1", "天生隐匿：只需[体型]更大的生物遮蔽即可躲藏"]},
    {"id": "stout-halfling", "raceId": "halfling", "name": "敦实半身人",
     "description": "比同族更壮实，对毒素有抗性。",
     "traits": ["[属性值加成]：[体质] +1", "毒素韧性：对抗毒素的[豁免]有优势，并有毒素[抗性]"]},
    {"id": "forest-gnome", "raceId": "gnome", "name": "森林侏儒",
     "description": "隐居林间的幻术小师。",
     "traits": ["[属性值加成]：[敏捷] +1", "自然幻术：习得戏法[次级幻象]",
                "与小兽沟通：能和[体型]小型或更小的野兽简单交流"]},
    {"id": "rock-gnome", "raceId": "gnome", "name": "岩石侏儒",
     "description": "爱鼓捣机械与发明的工匠。",
     "traits": ["[属性值加成]：[体质] +1", "工匠巧手：可制作小型装置",
                "关于魔法或机械物品的历史[属性检定]加双倍熟练"]},
    {"id": "black", "raceId": "dragonborn", "name": "黑龙",
     "description": "强酸血脉的龙裔。",
     "traits": ["[吐息武器]：5×30 尺线状，2d6 强酸", "伤害[抗性]：强酸"]},
    {"id": "blue", "raceId": "dragonborn", "name": "蓝龙",
     "description": "闪电血脉的龙裔。",
     "traits": ["[吐息武器]：5×30 尺线状，2d6 闪电", "伤害[抗性]：闪电"]},
    {"id": "brass", "raceId": "dragonborn", "name": "黄铜龙",
     "description": "火焰血脉的龙裔。",
     "traits": ["[吐息武器]：5×30 尺线状，2d6 火焰", "伤害[抗性]：火焰"]},
    {"id": "bronze", "raceId": "dragonborn", "name": "青铜龙",
     "description": "闪电血脉的龙裔。",
     "traits": ["[吐息武器]：5×30 尺线状，2d6 闪电", "伤害[抗性]：闪电"]},
    {"id": "copper", "raceId": "dragonborn", "name": "赤铜龙",
     "description": "强酸血脉的龙裔。",
     "traits": ["[吐息武器]：5×30 尺线状，2d6 强酸", "伤害[抗性]：强酸"]},
    {"id": "gold", "raceId": "dragonborn", "name": "金龙",
     "description": "火焰血脉的龙裔。",
     "traits": ["[吐息武器]：15 尺锥形，2d6 火焰", "伤害[抗性]：火焰"]},
    {"id": "green", "raceId": "dragonborn", "name": "绿龙",
     "description": "毒素血脉的龙裔。",
     "traits": ["[吐息武器]：15 尺锥形，2d6 毒素", "伤害[抗性]：毒素"]},
    {"id": "red", "raceId": "dragonborn", "name": "红龙",
     "description": "火焰血脉的龙裔。",
     "traits": ["[吐息武器]：15 尺锥形，2d6 火焰", "伤害[抗性]：火焰"]},
    {"id": "silver", "raceId": "dragonborn", "name": "银龙",
     "description": "寒霜血脉的龙裔。",
     "traits": ["[吐息武器]：15 尺锥形，2d6 寒冷", "伤害[抗性]：寒冷"]},
    {"id": "white", "raceId": "dragonborn", "name": "白龙",
     "description": "寒霜血脉的龙裔。",
     "traits": ["[吐息武器]：15 尺锥形，2d6 寒冷", "伤害[抗性]：寒冷"]},
]

SUBRACE_HEADERS = ["systemId", "id", "raceId", "name", "description", "traits", "abilityBonuses"]
# 脚本只管这些列；abilityBonuses 等数据由内容维护，重跑时保留原值
SUBRACE_MANAGED = ["systemId", "id", "raceId", "name", "description", "traits"]


def subrace_values(item: dict) -> dict[str, str]:
    return {
        "systemId": "dnd5e",
        "id": item["id"],
        "raceId": item["raceId"],
        "name": item["name"],
        "description": item["description"],
        "traits": "\n".join(item["traits"]),
    }


def write_subraces() -> None:
    """按 id 更新亚种的展示字段，新增亚种追加；已有列（abilityBonuses 等）原样保留。"""
    if SUBRACE_FILE.exists():
        workbook = openpyxl.load_workbook(SUBRACE_FILE)
        sheet = workbook["subraces"]
    else:
        workbook = openpyxl.Workbook()
        sheet = workbook.active
        sheet.title = "subraces"
        sheet.append(SUBRACE_HEADERS)

    headers = [cell.value for cell in sheet[1]]
    for column_name in SUBRACE_HEADERS:
        if column_name not in headers:
            sheet.cell(row=1, column=len(headers) + 1, value=column_name)
            headers.append(column_name)

    rows = {str(row[1].value): row for row in sheet.iter_rows(min_row=2) if row[1].value}
    added = updated = 0

    for item in SUBRACES:
        values = subrace_values(item)
        if item["id"] in rows:
            row = rows[item["id"]]
            changed = False
            for column_name in SUBRACE_MANAGED:
                cell = row[headers.index(column_name)]
                if cell.value != values[column_name]:
                    cell.value = values[column_name]
                    changed = True
            updated += int(changed)
            continue

        values["abilityBonuses"] = item.get("abilityBonuses", "")
        sheet.append([values.get(header) for header in headers])
        added += 1

    if added or updated:
        workbook.save(SUBRACE_FILE)
    print(f"写入 {SUBRACE_FILE.relative_to(REPO_ROOT)}（新增 {added} 个、更新 {updated} 个亚种）")


def lookup_category_code(path: Path, name: str) -> str | None:
    """只读查询分类编号，不修改文件。"""
    sheet = openpyxl.load_workbook(path)["categories"]
    for row in sheet.iter_rows(min_row=2):
        code, category = row[0].value, row[1].value
        if code is not None and category == name:
            return str(code).strip()
    return None


def ensure_category(path: Path, name: str) -> tuple[str, bool]:
    """返回分类编号；没有登记时按现有最大编号 +1 分配并写回。"""
    code = lookup_category_code(path, name)
    if code is not None:
        return code, False

    workbook = openpyxl.load_workbook(path)
    sheet = workbook["categories"]
    codes = [
        str(row[0].value).strip()
        for row in sheet.iter_rows(min_row=2)
        if row[0].value is not None and str(row[0].value).strip().isdigit()
    ]
    if not codes:
        raise SystemExit(f"{path} 里没有任何分类，先补一条再跑。")

    next_code = f"{max(int(item) for item in codes) + 1:03d}"
    sheet.append([next_code, name])
    workbook.save(path)
    return next_code, True


def keyword_rows(code: str) -> list[list[object]]:
    entries: list[tuple[str, str, str]] = [(race["key"], race["summary"], race["content"]) for race in RACES]
    entries += [(key, summary, content) for key, (summary, content) in TRAITS.items()]

    return [
        [f"{code}{index:05d}", key, CATEGORY_NAME, key, summary, content, None, "是"]
        for index, (key, summary, content) in enumerate(entries, start=1)
    ]


def rule_rows(code: str) -> list[list[object]]:
    return [
        [f"{code}{index:05d}", key, RULE_CATEGORY_NAME, key, summary, content, None, "是"]
        for index, (key, (summary, content)) in enumerate(GENERAL_RULES.items(), start=1)
    ]


def race_values() -> dict[str, dict[str, str]]:
    return {
        race["key"]: {
            "description": race["description"],
            "abilityBonus": race["abilityBonus"],
            "traits": "\n".join(race["traits"]),
        }
        for race in RACES
    }


def append_rule_keywords() -> list[str]:
    """把 GENERAL_RULES 里缺的词条补进 关键词.规则.xlsx（已存在的跳过）。"""
    code = lookup_category_code(CATEGORY_FILE, RULE_CATEGORY_NAME)
    if code is None:
        raise SystemExit(f"{CATEGORY_FILE.name} 里没有「{RULE_CATEGORY_NAME}」分类。")

    workbook = openpyxl.load_workbook(RULE_KEYWORD_FILE)
    sheet = workbook["keywords"]
    headers = [cell.value for cell in sheet[1]]
    existing = {str(row[1].value) for row in sheet.iter_rows(min_row=2) if row[1].value}

    next_id = 0
    for row in sheet.iter_rows(min_row=2):
        value = str(row[0].value or "")
        if value.startswith(code) and value[len(code):].isdigit():
            next_id = max(next_id, int(value[len(code):]))

    added: list[str] = []
    for key, (summary, content) in GENERAL_RULES.items():
        if key in existing:
            continue
        next_id += 1
        values = {
            "id": f"{code}{next_id:05d}",
            "key": key,
            "分类": RULE_CATEGORY_NAME,
            "title": key,
            "summary": summary,
            "content": content,
            "imageUrl": None,
            "isEnabled": "是",
        }
        sheet.append([values.get(header) for header in headers])
        added.append(key)

    if added:
        workbook.save(RULE_KEYWORD_FILE)
    return added


def check() -> int:
    """校验现有 xlsx 是否与脚本一致；不一致时列出差异并返回 1。"""
    problems: list[str] = []

    code = lookup_category_code(CATEGORY_FILE, CATEGORY_NAME)
    if code is None:
        problems.append(f"{CATEGORY_FILE.name} 缺少分类「{CATEGORY_NAME}」")
        code = "006"

    expected_keywords = keyword_rows(code)
    if not KEYWORD_FILE.exists():
        problems.append(f"缺少 {KEYWORD_FILE.name}")
    else:
        workbook = openpyxl.load_workbook(KEYWORD_FILE, data_only=True)
        actual = [
            list(row)
            for row in workbook["keywords"].iter_rows(min_row=2, values_only=True)
            if row and row[0] is not None
        ]
        if len(actual) != len(expected_keywords):
            problems.append(f"{KEYWORD_FILE.name} 有 {len(actual)} 条，脚本生成 {len(expected_keywords)} 条")
        else:
            for expected, found in zip(expected_keywords, actual):
                if [str(cell) if cell is not None else None for cell in found] != [
                    str(cell) if cell is not None else None for cell in expected
                ]:
                    problems.append(f"词条「{expected[1]}」与脚本不一致")
                    break

    rule_code = lookup_category_code(CATEGORY_FILE, RULE_CATEGORY_NAME)
    if rule_code is None:
        problems.append(f"{CATEGORY_FILE.name} 缺少分类「{RULE_CATEGORY_NAME}」")
    elif RULE_KEYWORD_FILE.exists():
        workbook = openpyxl.load_workbook(RULE_KEYWORD_FILE, data_only=True)
        rows = {}
        for row in workbook["keywords"].iter_rows(min_row=2, values_only=True):
            if row and row[1]:
                rows[str(row[1])] = row
        for key, (summary, content) in GENERAL_RULES.items():
            found = rows.get(key)
            if found is None:
                problems.append(f"{RULE_KEYWORD_FILE.name} 缺少词条「{key}」")
                continue
            if str(found[4] or "") != summary or str(found[5] or "") != content:
                problems.append(f"{RULE_KEYWORD_FILE.name} 的词条「{key}」与脚本不一致")
    else:
        problems.append(f"缺少 {RULE_KEYWORD_FILE.name}")

    if not SUBRACE_FILE.exists():
        problems.append(f"缺少 {SUBRACE_FILE.name}")
    else:
        workbook = openpyxl.load_workbook(SUBRACE_FILE, data_only=True)
        sheet = workbook["subraces"]
        headers = [cell.value for cell in sheet[1]]
        missing_columns = [name for name in SUBRACE_HEADERS if name not in headers]
        for name in missing_columns:
            problems.append(f"{SUBRACE_FILE.name} 缺少 {name} 列")
        rows = {str(row[1]): row for row in sheet.iter_rows(min_row=2, values_only=True) if row and row[1]}
        for item in SUBRACES:
            row = rows.get(item["id"])
            if row is None:
                problems.append(f"{SUBRACE_FILE.name} 缺少亚种「{item['id']}」")
                continue
            for column_name, value in subrace_values(item).items():
                if column_name in missing_columns:
                    continue
                if str(row[headers.index(column_name)] or "") != value:
                    problems.append(f"{SUBRACE_FILE.name} 亚种「{item['id']}」的 {column_name} 与脚本不一致")

    if not RACE_FILE.exists():
        problems.append(f"缺少 {RACE_FILE.name}")
    else:
        workbook = openpyxl.load_workbook(RACE_FILE, data_only=True)
        sheet = workbook["races"]
        headers = [cell.value for cell in sheet[1]]
        for column_name in ("description", "abilityBonus", "traits"):
            if column_name not in headers:
                problems.append(f"{RACE_FILE.name} 缺少 {column_name} 列")
        if not any("列" in problem for problem in problems):
            expected = race_values()
            for row in sheet.iter_rows(min_row=2):
                name = row[2].value
                if name not in expected:
                    continue
                for column_name in ("description", "abilityBonus", "traits"):
                    actual = str(row[headers.index(column_name)].value or "")
                    if actual != expected[name][column_name]:
                        problems.append(f"种族「{name}」的 {column_name} 与脚本不一致")
                        break
                else:
                    continue
                break

    if problems:
        print("内容与脚本不一致：")
        for problem in problems:
            print(f"  - {problem}")
        return 1

    print(
        f"OK：{CATEGORY_NAME} 分类 {code}，{len(RACES)} 个种族 / {len(TRAITS)} 条特质词条，"
        f"规则分类 {rule_code} 补充 {len(GENERAL_RULES)} 条，全部与脚本一致。"
    )
    return 0


def build() -> int:
    code, created = ensure_category(CATEGORY_FILE, CATEGORY_NAME)
    if created:
        print(f"已在 {CATEGORY_FILE.name} 登记分类「{CATEGORY_NAME}」= {code}")

    workbook = openpyxl.Workbook()
    sheet = workbook.active
    sheet.title = "keywords"
    sheet.append(KEYWORD_HEADERS)
    for row in keyword_rows(code):
        sheet.append(row)
    for column, width in {"A": 11, "B": 14, "C": 8, "D": 14, "E": 46, "F": 80, "G": 16, "H": 9}.items():
        sheet.column_dimensions[column].width = width
    workbook.save(KEYWORD_FILE)
    print(
        f"写入 {KEYWORD_FILE.relative_to(REPO_ROOT)}"
        f"（{len(RACES)} 个种族 + {len(TRAITS)} 条特质词条，ID 前缀 {code}）"
    )

    added = append_rule_keywords()
    if added:
        print(f"补充 {RULE_KEYWORD_FILE.relative_to(REPO_ROOT)}：{'、'.join(added)}")

    workbook = openpyxl.load_workbook(RACE_FILE)
    sheet = workbook["races"]
    headers = [cell.value for cell in sheet[1]]
    for column_name, width in {"description": 48, "abilityBonus": 28, "traits": 48}.items():
        if column_name not in headers:
            sheet.cell(row=1, column=len(headers) + 1).value = column_name
            headers.append(column_name)
        sheet.column_dimensions[openpyxl.utils.get_column_letter(headers.index(column_name) + 1)].width = width

    expected = race_values()
    written = 0
    for row in sheet.iter_rows(min_row=2):
        name = row[2].value
        if name not in expected:
            continue
        for column_name, value in expected[name].items():
            sheet.cell(row=row[0].row, column=headers.index(column_name) + 1).value = value
        written += 1
    workbook.save(RACE_FILE)
    print(f"写入 {RACE_FILE.relative_to(REPO_ROOT)}（{written} 个种族的 description / abilityBonus / traits）")

    write_subraces()
    return 0


def main() -> int:
    parser = argparse.ArgumentParser(description="生成种族展示数据与种族词条")
    parser.add_argument("--check", action="store_true", help="只校验，不写文件；不一致时退出码为 1")
    args = parser.parse_args()

    if not DATA_DIR.is_dir():
        raise SystemExit(f"找不到内容目录：{DATA_DIR}")

    return check() if args.check else build()


if __name__ == "__main__":
    sys.exit(main())
