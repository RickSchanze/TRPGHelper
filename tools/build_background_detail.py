#!/usr/bin/env python3
"""给 data/背景详情.xlsx 补「详细介绍」列（detail）：卡片上仍用一句话 description，点「详情」显示 detail。

用法：
    python tools/build_background_detail.py           # 写入 data/背景详情.xlsx（缺列就补在 description 后面）
    python tools/build_background_detail.py --check   # 只校验现有文件与脚本内容是否一致

改文案时优先改本文件里的 DETAILS 再重跑，保证「脚本 → xlsx」可复现；
管理台导出的文件不会被本脚本覆盖，除非再跑一次（按背景 id 更新）。
依赖 openpyxl（见 README「开发命令」）。
"""

from __future__ import annotations

import argparse
import sys
from pathlib import Path

import openpyxl
from openpyxl.styles import Alignment

REPO_ROOT = Path(__file__).resolve().parent.parent
DETAIL_FILE = REPO_ROOT / "data" / "背景详情.xlsx"
SHEET_NAME = "backgrounds"
DETAIL_COLUMN = "detail"
DETAIL_HEADER_AFTER = "description"
DETAIL_WIDTH = 72

# ---- 背景 id -> 详细介绍（3～4 句，卡片上的一句话 description 保持不动） ----
DETAILS: dict[str, str] = {
    "acolyte": "你自幼在神庙中长大，祷词、仪式和经文是你最早学会的东西。你曾为一位神祇服务，熟悉圣徽、祭服、焚香与祈祷的规矩，"
    "也见过信徒最虔诚与最脆弱的一面。离开神庙并不等于背弃信仰——只要遇到供奉同一位神祇的圣所，"
    "你仍被当作自己人，可以得到食宿、治疗与消息。旅途中，你习惯在固定的时刻祷告，也习惯用经文安抚惊慌的同伴。",
    "criminal": "你在城市的暗巷里学会了走路、说话和闭嘴。偷窃、讹诈、押货、赌局或销赃，你至少沾过一样，"
    "也至少被一个帮派记住了名字。你知道该找谁出手，知道哪条街巡得最松，也知道什么时候该把脸藏起来。"
    "你未必乐于作恶，但你比谁都清楚：所谓规矩，从来只对守规矩的人生效。",
    "folk-hero": "你原本只是镇上的农夫、牧人或铁匠，直到某一天，一件大事把你推到了人群前面——也许你挡下了一头野兽，"
    "也许你揭穿了地主的把戏。乡亲们把这件事传了一遍又一遍，你走到哪里都有人端出麦酒、硬塞给你一块面包。"
    "你熟悉土地、牲口和收成，也懂得如何让散沙一样的人心聚拢起来。这份朴实的声望，有时比刀剑更能打开一扇门。",
    "entertainer": "你靠表演为生：唱歌、跳舞、演奏、说书，或者讲些让人脸红的故事。舞台教会你如何抓住满堂目光，"
    "也教会你如何在喝倒彩时保持微笑。旅店、剧场、节庆与宴会上总能听到你的名字，各色人物你都打过交道。"
    "你随身带着乐器与戏服，必要时，一副好嗓子和一张厚脸皮就是最好的通行证。",
    "guild-artisan": "你在公会里熬过了学徒期，把一门手艺练到能吃饭的地步。公会的规矩、行话、行情与人情，你门儿清；"
    "而公会也为你提供住处、原料、销路和政治上的靠山。你习惯用双手解决问题，也习惯被同行称作师傅。"
    "出门远行时别忘了带上那封介绍信——对陌生人来说，它比黄金更能证明你是个正经手艺人。",
    "hermit": "你曾在荒野中独居多年，远离人群与喧嚣，只为想明白心里的一个问题。苦修、冥想与长久的孤独让你窥见了一个秘密——"
    "也许是关于世界的真相，也许是某位神祇的意图。带着这份秘密回到人间并不容易：你不太擅长寒暄，"
    "却比大多数人更清楚什么值得追求。这个秘密会在适当的时候派上用场，至于代价，你早已有数。",
    "noble": "你出身名门，从小有人替你系鞋带、替你背族谱。财富、头衔和扈从是家常便饭，你也学会了在觥筹交错间分辨谁是朋友、"
    "谁是匕首。你的姓氏是一张通行证，也是一面靶子——有人因你的出身卑躬屈膝，也有人因此想除掉你。"
    "你也许厌倦了这一切，但贵族的手段，你一样也没落下。",
    "outlander": "你在文明的边界之外长大，靠打猎、采集与迁徙活命。你知道哪片林子有鹿、哪条河能喝、什么样的天气必须躲进岩洞，"
    "也知道哪些东西绝不该碰。城市对你来说太吵、太挤，规矩多得让人喘不过气；但只要脚下还有土地，"
    "你就能养活自己和同伴。你对野外的熟悉程度，是任何地图都比不上的。",
    "sage": "你把大半生花在书堆、卷轴和实验室里，读过的典籍比大多数人见过的都多。你熟稔历史、秘闻与失传的语言，"
    "知道该去哪里查资料、该向哪位学究请教。有一个缠绕你多年的问题驱使你上路——或许答案就藏在某个传说、"
    "某座废墟或某本禁书里。人们带着疑问来找你，而你总能给出至少三个听起来都很有道理的说法。",
    "soldier": "你曾在军队中服役，经历过枯燥的训练、漫长的行军和真正的战斗。军衔、队列与命令早已刻进你的本能，"
    "你也清楚恐惧会怎样吞掉一个没受过训练的人。你认识一些旧部，需要时能借到装备、马匹或通行许可；"
    "而你身上的伤疤本身就是一份履历。战争也许结束了，但战争教会你的东西还跟着你。",
    "urchin": "你在城市的缝隙里长大：屋檐、下水道、废弃的宅子和集市的后巷都是你的地盘。你靠乞讨、跑腿和顺手牵羊活到今天，"
    "比任何人都清楚这座城的暗道与捷径。你没有家世可以炫耀，却有一群同样在街头讨生活的伙伴，消息传得比告示还快。"
    "别小看一个流浪儿——他知道的秘密，往往比贵族还多。",
    "sailor": "你曾在船上讨生活：拉帆、值夜、挨鞭子，也看惯了风暴与一望无际的海平线。你知道如何读云、辨星、系绳结，"
    "也知道港口里哪家酒馆能打听到可靠的船期。你的老伙计散落在各条航线上，只要开口，总有人愿意捎你一程、分你一碗热汤。"
    "陆地让你有些不太自在，但船上的人情你一直带在身上。",
    "charlatan": "你靠一张嘴、一副好皮囊和一个编得滴水不漏的假身份过活。你扮过贵族、商人、祭司和落魄学者，"
    "每个身份都配着相应的口音、行头和故事。你擅长察言观色，也擅长在谎言被戳穿之前抽身。"
    "你并不总是坏人——只是比起真相，你更相信一个讲得漂亮的故事。",
}


def column_index(headers: list[str], name: str) -> int | None:
    for index, header in enumerate(headers, start=1):
        if str(header).strip() == name:
            return index
    return None


def open_sheet():
    workbook = openpyxl.load_workbook(DETAIL_FILE)
    sheet = workbook[SHEET_NAME]
    headers = [cell.value for cell in sheet[1]]
    return workbook, sheet, headers


def ensure_detail_column(workbook, sheet, headers: list[str]) -> tuple[int, bool]:
    """detail 列不存在时插在 description 后面，返回 (1 基列号, 是否新插入)。"""
    index = column_index(headers, DETAIL_COLUMN)
    if index is not None:
        return index, False

    anchor = column_index(headers, DETAIL_HEADER_AFTER)
    index = (anchor or len(headers)) + 1
    sheet.insert_cols(index)
    sheet.cell(row=1, column=index, value=DETAIL_COLUMN)
    sheet.column_dimensions[openpyxl.utils.get_column_letter(index)].width = DETAIL_WIDTH
    return index, True


def build() -> int:
    workbook, sheet, headers = open_sheet()
    index, inserted = ensure_detail_column(workbook, sheet, headers)

    updated = added = 0
    for row in sheet.iter_rows(min_row=2):
        background_id = str(row[0].value or "").strip()
        detail = DETAILS.get(background_id)
        if detail is None:
            continue
        cell = row[index - 1]
        if cell.value in (None, ""):
            added += 1
        elif cell.value != detail:
            updated += 1
        cell.value = detail
        cell.alignment = Alignment(wrap_text=True, vertical="top")

    missing = sorted(set(DETAILS) - {str(row[0].value or "").strip() for row in sheet.iter_rows(min_row=2)})
    if inserted or added or updated:
        workbook.save(DETAIL_FILE)

    print(
        f"{DETAIL_FILE.name}：新增列 {int(inserted)}，写入 {added} 条，更新 {updated} 条"
        + ("（无改动）" if not (inserted or added or updated) else "")
    )
    if missing:
        print(f"提示：脚本里有但表里没有的背景 id：{'、'.join(missing)}")
    return 0


def check() -> int:
    problems: list[str] = []

    workbook = openpyxl.load_workbook(DETAIL_FILE, data_only=True)
    sheet = workbook[SHEET_NAME]
    headers = [cell.value for cell in sheet[1]]
    index = column_index(headers, DETAIL_COLUMN)
    if index is None:
        problems.append(f"{DETAIL_FILE.name} 缺少列「{DETAIL_COLUMN}」")
    else:
        rows = {
            str(row[0]).strip(): row[index - 1]
            for row in sheet.iter_rows(min_row=2, values_only=True)
            if row and row[0] is not None
        }
        for background_id, detail in DETAILS.items():
            if rows.get(background_id) != detail:
                problems.append(f"{DETAIL_FILE.name} 背景「{background_id}」的详细介绍与脚本不一致")

    for problem in problems:
        print(f"- {problem}")
    return 1 if problems else 0


def main() -> int:
    parser = argparse.ArgumentParser(description="补背景详细介绍一下（detail 列）")
    parser.add_argument("--check", action="store_true", help="只校验，不写文件")
    args = parser.parse_args()
    return check() if args.check else build()


if __name__ == "__main__":
    sys.exit(main())
