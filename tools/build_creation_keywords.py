#!/usr/bin/env python3
"""补「技能与特性」步骤用到的词条：技能 / 怪物 / 地形三个新分类，以及规则、战斗分类的补充词条。

用法：
    python tools/build_creation_keywords.py           # 写入 data/*.xlsx（按 key 更新，缺的追加）
    python tools/build_creation_keywords.py --check   # 只校验现有文件与脚本内容是否一致

覆盖内容：
- 新分类：技能（009，17 条；隐匿复用规则分类的已有词条）、怪物（010，13 条）、
  地形（011，8 条）——分类编号已存在就沿用，不存在时按最大编号 +1 分配；
- 规则分类补充：技能、技能熟练、熟练加值、职业特性、职业、种族、背景；
- 战斗分类补充：巨武器战斗、保护（补全战士的战斗风格选项）；
- 车卡流程.xlsx 里 kind=skills 那一步的描述改成带 [关键词] 的版本。

改内容时优先改本文件里的常量再重跑，保证「脚本 → xlsx」可复现；
在管理台里直接改也行，但下次跑本脚本会以本文件为准（按 key 更新）。
依赖 openpyxl（见 README「开发命令」）。
"""

from __future__ import annotations

import argparse
import sys
from pathlib import Path

import openpyxl
from openpyxl.styles import Alignment

REPO_ROOT = Path(__file__).resolve().parent.parent
DATA_DIR = REPO_ROOT / "data"

CATEGORY_FILE = DATA_DIR / "关键词分类.xlsx"
STEP_FILE = DATA_DIR / "车卡流程.xlsx"
KEYWORD_HEADERS = ["id", "key", "分类", "title", "summary", "content", "imageUrl", "isEnabled"]
WRAP_COLUMNS = {"summary", "content"}

SKILLS_STEP_DESCRIPTION = (
    "[职业]按数量选[技能熟练]，[背景] / [种族]固定给的自动锁定；"
    "下面的 1 级[职业特性]（[战斗风格] / [宿敌] / [自然探索者]）也在这里选。"
)

# ---- 技能分类（隐匿已在规则分类里有词条，这里不重复） ----
SKILLS: dict[str, tuple[str, str]] = {
    "运动": (
        "力量技能：攀爬、跳跃、游泳与角力。",
        "隶属于[力量]的技能：攀爬、跳跃、游泳与角力。多数职业可自选，[背景]与[种族]也可能直接给出熟练。",
    ),
    "体操": ("敏捷技能：平衡、翻滚、挣脱擒抱。", "隶属于[敏捷]的技能：保持平衡、翻滚、从擒抱中挣脱。"),
    "巧手": ("敏捷技能：顺手牵羊、藏东西、手上功夫。", "隶属于[敏捷]的技能：扒窃、藏匿小物件、做精细的手上动作。"),
    "奥秘": ("智力技能：法术、魔法物品、位面知识。", "隶属于[智力]的技能：识别法术、魔法物品与位面传闻。"),
    "历史": ("智力技能：历史事件、王朝、战争。", "隶属于[智力]的技能：回忆历史事件、王朝更替与著名战争。"),
    "调查": ("智力技能：搜索线索、推理。", "隶属于[智力]的技能：搜索线索、检查现场、从细节里推理。"),
    "自然": ("智力技能：地形、动植物、天气。", "隶属于[智力]的技能：辨识地形、动植物与天气规律。"),
    "宗教": ("智力技能：神祇、仪式、圣徽。", "隶属于[智力]的技能：辨认神祇、仪式、圣徽与宗教传统。"),
    "驯兽": ("感知技能：安抚、训练、驾驭动物。", "隶属于[感知]的技能：安抚、训练与驾驭动物。"),
    "洞悉": ("感知技能：判断意图、识破谎言。", "隶属于[感知]的技能：判断他人意图、识破谎言。"),
    "医药": ("感知技能：稳定濒死、诊断。", "隶属于[感知]的技能：稳定濒死生物、诊断疾病与中毒。"),
    "察觉": (
        "感知技能：发现隐藏的事物。",
        "隶属于[感知]的技能：发现躲藏的生物、陷阱与不寻常的动静。[被动察觉]也基于它。",
    ),
    "生存": ("感知技能：追踪、觅食、辨向。", "隶属于[感知]的技能：追踪、觅食、辨向，以及在荒野中活下来。"),
    "欺瞒": ("魅力技能：说谎、伪装。", "隶属于[魅力]的技能：说谎、伪装、误导他人。"),
    "威吓": ("魅力技能：威胁、恐吓。", "隶属于[魅力]的技能：用威胁与气势逼迫对方让步。"),
    "表演": ("魅力技能：演奏、舞蹈、演戏。", "隶属于[魅力]的技能：演奏、舞蹈、演戏与讲段子。"),
    "说服": ("魅力技能：谈判、劝服。", "隶属于[魅力]的技能：谈判、劝服、用道理打动别人。"),
}

# ---- 怪物分类（游侠「宿敌」的选项） ----
MONSTERS: dict[str, tuple[str, str]] = {
    "异怪": (
        "来自异界的畸变生物，如夺心魔、眼魔。",
        "异怪是来自遥远异界或受扭曲魔法影响的生物，形貌违背常理，如夺心魔与眼魔。游侠的[宿敌]可以选它。",
    ),
    "野兽": ("自然界的普通动物，从老鼠到巨熊。", "野兽是自然界的普通动物：狼、熊、鹰、巨鼠之类。它们没有魔法，靠本能生存。"),
    "天界": ("与上层位面相连的善良生物。", "天界生物来自上层位面，形态常带光与羽翼，如天使与天马。"),
    "构装": ("由魔法驱动的构造体。", "构装体是被创造出来执行命令的魔法构造，如魔像与盾卫。"),
    "龙": ("古老的鳞甲生物，多能喷吐能量。", "龙是古老而强大的鳞甲生物，按龙种拥有不同的吐息与[抗性]。"),
    "精类": ("妖精荒野的居民。", "精类是妖精荒野的居民，如小妖精、树精与妖精龙，行为难以捉摸。"),
    "邪魔": ("下层位面的邪恶生物。", "邪魔来自下层位面，如魔鬼与恶魔，被负面能量驱使。"),
    "巨人": ("高大强壮的类人生物。", "巨人是体型远大于人类的类人生物，如食人魔、巨人与巨魔。"),
    "类地精": ("地精、大地精与熊地精。", "类地精是地精、大地精与熊地精这一类生物的总称，常成群活动。"),
    "怪物": ("不属于其它类别的奇异生物。", "「怪物」泛指难以归入其它类别的奇异生物，如双头巨人、魅影之类。"),
    "泥怪": ("无固定形态的软体生物。", "泥怪没有固定形态，靠吞噬有机物移动，如胶质方块与黑布丁。"),
    "植物": ("会动的植物或真菌。", "植物类生物是能自行移动或攻击的植物与真菌，如树人、尖叫蕈。"),
    "亡灵": ("被死亡魔法驱动的尸体或魂灵。", "亡灵是被死亡魔法驱动的尸体或魂灵，如骷髅、僵尸与幽魂。"),
}

# ---- 地形分类（游侠「自然探索者」的选项） ----
TERRAINS: dict[str, tuple[str, str]] = {
    "极地": ("冰雪覆盖的寒冷地带。", "极地常年冰封，风雪与低温本身就是敌人。"),
    "海岸": ("海洋与陆地相接的潮间带。", "海岸是海陆交界，潮汐、礁石与沙滩影响移动与追踪。"),
    "荒漠": ("干旱少雨的沙石地带。", "荒漠干旱缺水，昼夜温差大，沙暴会抹去痕迹。"),
    "森林": ("树木茂密的地带。", "森林树木茂密，视线受阻，适合伏击与隐蔽。"),
    "草原": ("开阔的草地与稀树平原。", "草原开阔，视野良好，也缺少掩体。"),
    "山地": ("起伏的岩石地形。", "山地崎岖陡峭，攀爬与辨向都更困难。"),
    "沼泽": ("泥泞湿滑的湿地。", "沼泽泥泞难行，蚊虫与暗坑随时可能出现。"),
    "幽暗地域": ("地下深处的黑暗世界。", "幽暗地域位于地下深处，终年无光，充满奇异生物与地下城。"),
}

# ---- 规则分类补充 ----
RULES: dict[str, tuple[str, str]] = {
    "技能": (
        "角色受训的 18 个领域，每项关联一项属性。",
        "技能是角色可以受训的 18 个领域，每项关联一项[属性]：力量（运动）、敏捷（体操、巧手、隐匿）、"
        "智力（奥秘、历史、调查、自然、宗教）、感知（驯兽、洞悉、医药、察觉、生存）、"
        "魅力（欺瞒、威吓、表演、说服）。进行[属性检定]时，若熟练该技能则加上[熟练加值]。",
    ),
    "技能熟练": (
        "对某项技能受训，检定加熟练加值。",
        "技能熟练代表你受过某项技能的训练。做该技能的[属性检定]时，可以加上[熟练加值]（1 级 +2）。"
        "车卡时职业按数量自选、[种族]与[背景]固定给的会自动锁定。",
    ),
    "熟练加值": (
        "随等级成长的加值：1 级 +2，5/9/13/17 级各 +1。",
        "熟练加值取决于角色等级：1-4 级 +2，5-8 级 +3，9-12 级 +4，13-16 级 +5，17 级以上 +6。"
        "攻击检定、[豁免]、熟练的技能检定都会加上它。",
    ),
    "职业特性": (
        "1 级职业给的特性，如战斗风格 / 宿敌 / 自然探索者。",
        "1 级[职业]特性：战士的[战斗风格]、游侠的[宿敌]与[自然探索者]等。"
        "部分特性需要在创建角色时选择具体选项，选好后会写进角色卡。",
    ),
    "职业": (
        "角色的行当，决定生命骰、擅长武器与主要属性。",
        "[职业]决定你的生命骰、护甲与武器熟练、主要属性，以及后续等级获得的特性。"
        "创建角色时先选职业，后面的[种族]、[背景]与[技能]选择都围绕它展开。",
    ),
    "种族": (
        "角色的血统，给出属性加值、速度与天生特质。",
        "[种族]给出[属性值加成]、[速度]与天生特质，部分种族还有[亚种]。"
        "它和[背景]一起决定了角色天生的[技能熟练]与[语言]。",
    ),
    "背景": (
        "角色出身的经历，给出技能、工具、语言与特性。",
        "[背景]是角色进入冒险前的经历，给出两项[技能熟练]、工具、额外[语言]、起始装备与一项背景特性。"
        "车卡时它和[种族]一起决定哪些技能自动锁定。",
    ),
}

# ---- 战斗分类补充 ----
COMBAT: dict[str, tuple[str, str]] = {
    "巨武器战斗": (
        "双手近战武器伤害掷出 1 或 2 可以重掷。",
        "战斗风格：使用双手或双手持握的近战武器时，伤害掷骰出现 1 或 2 可以重掷一次（必须使用新结果）。",
    ),
    "保护": (
        "持盾时用反应，让攻击 5 尺内友军的敌人有劣势。",
        "战斗风格：持盾时，若你看见的敌人攻击 5 尺内另一个生物，你可以用反应让那次攻击检定有劣势。",
    ),
}


def category_path(name: str) -> Path:
    return DATA_DIR / f"关键词.{name}.xlsx"


def lookup_category_code(path: Path, name: str) -> str | None:
    """只读查询分类编号，不修改文件。"""
    if not path.exists():
        return None

    sheet = openpyxl.load_workbook(path)["categories"]
    for row in sheet.iter_rows(min_row=2):
        code, category = row[0].value, row[1].value
        if code is not None and category == name:
            return str(code).strip()
    return None


def ensure_category(name: str) -> tuple[str, bool]:
    """返回分类编号；没有登记时按现有最大编号 +1 分配并写回。"""
    code = lookup_category_code(CATEGORY_FILE, name)
    if code is not None:
        return code, False

    workbook = openpyxl.load_workbook(CATEGORY_FILE)
    sheet = workbook["categories"]
    codes = [
        str(row[0].value).strip()
        for row in sheet.iter_rows(min_row=2)
        if row[0].value is not None and str(row[0].value).strip().isdigit()
    ]
    if not codes:
        raise SystemExit(f"{CATEGORY_FILE} 里没有任何分类，先补一条再跑。")

    next_code = f"{max(int(item) for item in codes) + 1:03d}"
    sheet.append([next_code, name])
    workbook.save(CATEGORY_FILE)
    return next_code, True


def next_keyword_id(rows: list[list[object]], code: str) -> int:
    next_id = 0
    for row in rows:
        value = str(row[0] or "")
        if value.startswith(code) and value[len(code):].isdigit():
            next_id = max(next_id, int(value[len(code):]))
    return next_id


def upsert_keywords(name: str, entries: dict[str, tuple[str, str]]) -> tuple[int, int]:
    """按 key 更新已有词条、追加缺少的词条；返回 (新增数, 更新数)。"""
    path = category_path(name)
    code, created = ensure_category(name)

    if created or not path.exists():
        workbook = openpyxl.Workbook()
        sheet = workbook.active
        sheet.title = "keywords"
        sheet.append(KEYWORD_HEADERS)
        rows: list[list[object]] = []
    else:
        workbook = openpyxl.load_workbook(path)
        sheet = workbook["keywords"]
        rows = [list(row) for row in sheet.iter_rows(min_row=2, values_only=True)]

    headers = [cell.value for cell in sheet[1]]
    ids_by_key = {str(row[1]): str(row[0]) for row in rows if row and row[1]}
    next_id = next_keyword_id(rows, code)
    added = updated = 0

    for key, (summary, content) in entries.items():
        values = {
            "id": ids_by_key.get(key),
            "key": key,
            "分类": name,
            "title": key,
            "summary": summary,
            "content": content,
            "imageUrl": None,
            "isEnabled": "是",
        }

        row_index = next((i for i, row in enumerate(rows) if row and str(row[1]) == key), None)
        if row_index is None:
            next_id += 1
            values["id"] = f"{code}{next_id:05d}"
            rows.append([values.get(header) for header in headers])
            added += 1
            continue

        current = rows[row_index]
        if str(current[4] or "") != summary or str(current[5] or "") != content:
            current[4] = summary
            current[5] = content
            updated += 1

    if created or added or updated:
        # 整表重写：保持表头，行按现有顺序 + 新增行
        for row in sheet.iter_rows(min_row=2):
            for cell in row:
                cell.value = None
        for index, row in enumerate(rows, start=2):
            for column, value in enumerate(row, start=1):
                sheet.cell(row=index, column=column, value=value)
        apply_layout(workbook, sheet)
        workbook.save(path)

    return added, updated


def apply_layout(workbook: openpyxl.Workbook, sheet: openpyxl.worksheet.worksheet.Worksheet) -> None:
    headers = [cell.value for cell in sheet[1]]
    widths = {"key": 14, "title": 14, "summary": 50, "content": 70}
    for column, header in enumerate(headers, start=1):
        letter = openpyxl.utils.get_column_letter(column)
        sheet.column_dimensions[letter].width = widths.get(header, 12)
        if header in WRAP_COLUMNS:
            for row in range(2, sheet.max_row + 1):
                sheet.cell(row=row, column=column).alignment = Alignment(wrap_text=True, vertical="top")


def update_step_description() -> bool:
    workbook = openpyxl.load_workbook(STEP_FILE)
    sheet = workbook["steps"]
    headers = [cell.value for cell in sheet[1]]
    kind_column = headers.index("kind") + 1
    description_column = headers.index("description") + 1

    changed = False
    for row in sheet.iter_rows(min_row=2):
        if row[kind_column - 1].value != "skills":
            continue
        cell = row[description_column - 1]
        if cell.value != SKILLS_STEP_DESCRIPTION:
            cell.value = SKILLS_STEP_DESCRIPTION
            changed = True

    if changed:
        workbook.save(STEP_FILE)
    return changed


def check() -> int:
    """校验现有 xlsx 是否与脚本一致；不一致时列出差异并返回 1。"""
    problems: list[str] = []
    all_entries = {"技能": SKILLS, "怪物": MONSTERS, "地形": TERRAINS, "规则": RULES, "战斗": COMBAT}

    for name, entries in all_entries.items():
        path = category_path(name)
        code = lookup_category_code(CATEGORY_FILE, name)
        if code is None:
            problems.append(f"{CATEGORY_FILE.name} 缺少分类「{name}」")
            continue
        if not path.exists():
            problems.append(f"缺少 {path.name}")
            continue

        workbook = openpyxl.load_workbook(path, data_only=True)
        rows = {str(row[1]): row for row in workbook["keywords"].iter_rows(min_row=2, values_only=True) if row and row[1]}
        for key, (summary, content) in entries.items():
            row = rows.get(key)
            if row is None:
                problems.append(f"{path.name} 缺少词条「{key}」")
                continue
            if not str(row[0] or "").startswith(code):
                problems.append(f"{path.name} 词条「{key}」的 id「{row[0]}」前缀不是 {code}")
            if row[4] != summary:
                problems.append(f"{path.name} 词条「{key}」的 summary 与脚本不一致")
            if row[5] != content:
                problems.append(f"{path.name} 词条「{key}」的 content 与脚本不一致")

    workbook = openpyxl.load_workbook(STEP_FILE, data_only=True)
    sheet = workbook["steps"]
    headers = [cell.value for cell in sheet[1]]
    kind_column = headers.index("kind")
    description_column = headers.index("description")
    matches = [
        row[description_column]
        for row in sheet.iter_rows(min_row=2, values_only=True)
        if row[kind_column] == "skills"
    ]
    if not matches or matches[0] != SKILLS_STEP_DESCRIPTION:
        problems.append(f"{STEP_FILE.name} 里 skills 步骤的描述与脚本不一致")

    for problem in problems:
        print(f"- {problem}")
    return 1 if problems else 0


def build() -> int:
    total_added = total_updated = 0
    for name, entries in [("技能", SKILLS), ("怪物", MONSTERS), ("地形", TERRAINS), ("规则", RULES), ("战斗", COMBAT)]:
        added, updated = upsert_keywords(name, entries)
        total_added += added
        total_updated += updated
        print(f"{name}：新增 {added} 条，更新 {updated} 条")

    if update_step_description():
        print(f"{STEP_FILE.name}：已更新 skills 步骤描述")
    else:
        print(f"{STEP_FILE.name}：skills 步骤描述已是最新")

    print(f"完成：新增 {total_added} 条，更新 {total_updated} 条")
    return 0


def main() -> int:
    parser = argparse.ArgumentParser(description="补技能 / 怪物 / 地形等关键词与技能步骤描述")
    parser.add_argument("--check", action="store_true", help="只校验，不写文件")
    args = parser.parse_args()
    return check() if args.check else build()


if __name__ == "__main__":
    sys.exit(main())
