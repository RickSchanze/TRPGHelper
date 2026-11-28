#!/usr/bin/env python3
"""生成 data/装备.xlsx 的 kits 工作表：每个职业 2～3 套起始包，车卡时玩家挑一套。

用法：
    python tools/build_equipment_kits.py           # 重写 kits 工作表（armor / weapons 不动）
    python tools/build_equipment_kits.py --check   # 只校验 kits 与脚本是否一致

列说明：
- kitId：套装的稳定 id（车卡草稿里记住玩家选的哪一套）；
- classId：引用 职业.xlsx 的 id；
- name：车上显示的套装名，例如「重装战士」；
- armorId / shield / weaponIds / defaultWeaponId / pack：这一套的装备，weaponIds 逗号分隔；
- 前端会让玩家在 weaponIds 里选一把作为主手，其余视为随身携带。

改套装时优先改本文件里的 KITS 再重跑；管理台不会编辑装备，直接改 xlsx 后重跑本脚本会被覆盖。
依赖 openpyxl（见 README「开发命令」）。
"""

from __future__ import annotations

import argparse
import sys
from pathlib import Path

import openpyxl

REPO_ROOT = Path(__file__).resolve().parent.parent
EQUIPMENT_FILE = REPO_ROOT / "data" / "装备.xlsx"
SHEET_NAME = "kits"
HEADERS = ["kitId", "classId", "systemId", "name", "armorId", "shield", "weaponIds", "defaultWeaponId", "pack"]

# ---- 每个职业的起始套装（参考 5E 玩家手册的起始装备选项，做成 2 套） ----
KITS: list[dict[str, object]] = [
    {
        "kitId": "fighter-heavy",
        "classId": "fighter",
        "name": "重装战士",
        "armorId": "chain",
        "shield": True,
        "weaponIds": ["longsword", "handcrossbow"],
        "defaultWeaponId": "longsword",
        "pack": "探索者包",
    },
    {
        "kitId": "fighter-archer",
        "classId": "fighter",
        "name": "弓手战士",
        "armorId": "leather",
        "shield": False,
        "weaponIds": ["longsword", "longbow"],
        "defaultWeaponId": "longbow",
        "pack": "探索者包",
    },
    {
        "kitId": "ranger-scout",
        "classId": "ranger",
        "name": "荒野游侠",
        "armorId": "scale",
        "shield": False,
        "weaponIds": ["shortsword", "longbow"],
        "defaultWeaponId": "longbow",
        "pack": "探索者包",
    },
    {
        "kitId": "ranger-duelist",
        "classId": "ranger",
        "name": "双刀游侠",
        "armorId": "leather",
        "shield": False,
        "weaponIds": ["shortsword", "dagger"],
        "defaultWeaponId": "shortsword",
        "pack": "探索者包",
    },
    {
        "kitId": "monk-blade",
        "classId": "monk",
        "name": "持剑武僧",
        "armorId": "",
        "shield": False,
        "weaponIds": ["shortsword", "dart"],
        "defaultWeaponId": "shortsword",
        "pack": "探索者包",
    },
    {
        "kitId": "monk-spear",
        "classId": "monk",
        "name": "持矛武僧",
        "armorId": "",
        "shield": False,
        "weaponIds": ["spear", "dart"],
        "defaultWeaponId": "spear",
        "pack": "探索者包",
    },
    {
        "kitId": "druid-scimitar",
        "classId": "druid",
        "name": "弯刀德鲁伊",
        "armorId": "leather",
        "shield": True,
        "weaponIds": ["scimitar", "javelin"],
        "defaultWeaponId": "scimitar",
        "pack": "探索者包",
    },
    {
        "kitId": "druid-spear",
        "classId": "druid",
        "name": "短矛德鲁伊",
        "armorId": "leather",
        "shield": True,
        "weaponIds": ["spear", "javelin"],
        "defaultWeaponId": "spear",
        "pack": "探索者包",
    },
    {
        "kitId": "paladin-shield",
        "classId": "paladin",
        "name": "剑盾圣武士",
        "armorId": "chain",
        "shield": True,
        "weaponIds": ["longsword", "javelin"],
        "defaultWeaponId": "longsword",
        "pack": "探索者包",
    },
    {
        "kitId": "paladin-two-weapons",
        "classId": "paladin",
        "name": "双武器圣武士",
        "armorId": "chain",
        "shield": False,
        "weaponIds": ["longsword", "spear"],
        "defaultWeaponId": "longsword",
        "pack": "探索者包",
    },
]


def kit_rows() -> list[list[object]]:
    return [
        [
            kit["kitId"],
            kit["classId"],
            "dnd5e",
            kit["name"],
            kit["armorId"],
            "是" if kit["shield"] else "否",
            ",".join(kit["weaponIds"]),
            kit["defaultWeaponId"],
            kit["pack"],
        ]
        for kit in KITS
    ]


def check() -> int:
    problems: list[str] = []
    if not EQUIPMENT_FILE.exists():
        problems.append(f"缺少 {EQUIPMENT_FILE.name}")
    else:
        workbook = openpyxl.load_workbook(EQUIPMENT_FILE, data_only=True)
        sheet = workbook[SHEET_NAME]
        actual_headers = [cell.value for cell in sheet[1]]
        if actual_headers != HEADERS:
            problems.append(f"{EQUIPMENT_FILE.name} / {SHEET_NAME} 表头与脚本不一致：{actual_headers}")
        else:
            actual = [
                [str(cell) if cell is not None else "" for cell in row]
                for row in sheet.iter_rows(min_row=2, values_only=True)
                if row and any(cell is not None for cell in row)
            ]
            expected = [[str(cell) for cell in row] for row in kit_rows()]
            if actual != expected:
                problems.append(f"{EQUIPMENT_FILE.name} / {SHEET_NAME} 与脚本不一致")

    for problem in problems:
        print(f"- {problem}")
    return 1 if problems else 0


def build() -> int:
    workbook = openpyxl.load_workbook(EQUIPMENT_FILE)
    index = workbook.sheetnames.index(SHEET_NAME) if SHEET_NAME in workbook.sheetnames else workbook.sheetnames.index("armor") + 1
    del workbook[SHEET_NAME]
    sheet = workbook.create_sheet(SHEET_NAME, index)
    sheet.append(HEADERS)
    for row in kit_rows():
        sheet.append(row)
    for column, width in {"A": 16, "B": 12, "C": 10, "D": 16, "E": 12, "F": 8, "G": 28, "H": 18, "I": 14}.items():
        sheet.column_dimensions[column].width = width
    workbook.save(EQUIPMENT_FILE)
    print(f"写入 {EQUIPMENT_FILE.relative_to(REPO_ROOT)} / {SHEET_NAME}（{len(KITS)} 套起始装备）")
    return 0


def main() -> int:
    parser = argparse.ArgumentParser(description="生成每个职业的起始装备套")
    parser.add_argument("--check", action="store_true", help="只校验，不写文件")
    args = parser.parse_args()
    return check() if args.check else build()


if __name__ == "__main__":
    sys.exit(main())
