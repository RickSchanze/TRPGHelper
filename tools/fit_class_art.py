#!/usr/bin/env python3
"""把生成出来的候选图裁成职业卡 / 子职卡要的 16:9 尺寸（1200×674）并转成 jpg。

用法：
    python tools/fit_class_art.py --src .tmp/art/paladin-final/电影剧照_20261128.png \
        --dst frontend/src/static/classes/paladin.jpg
    python tools/fit_class_art.py --src a.png --dst b.jpg --width 1200 --height 674 --quality 90

做法：先按目标比例居中裁切（不拉伸），再等比缩放到目标尺寸；默认输出 JPEG。
依赖 Pillow（README「开发命令」里列了 openpyxl，Pillow 同属图像依赖）。
"""

from __future__ import annotations

import argparse
import sys
from pathlib import Path

from PIL import Image

REPO_ROOT = Path(__file__).resolve().parent.parent
DEFAULT_WIDTH = 1200
DEFAULT_HEIGHT = 674


def center_crop_to_ratio(image: Image.Image, ratio: float) -> Image.Image:
    width, height = image.size
    current = width / height
    if abs(current - ratio) < 1e-4:
        return image

    if current > ratio:
        new_width = round(height * ratio)
        left = (width - new_width) // 2
        return image.crop((left, 0, left + new_width, height))

    new_height = round(width / ratio)
    top = (height - new_height) // 2
    return image.crop((0, top, width, top + new_height))


def resolve(path: str) -> Path:
    value = Path(path)
    return value if value.is_absolute() else REPO_ROOT / value


def main() -> int:
    parser = argparse.ArgumentParser(description="裁成 16:9 并转 jpg，供职业卡 / 子职卡使用")
    parser.add_argument("--src", required=True, help="源图（png / jpg 都行）")
    parser.add_argument("--dst", required=True, help="输出 jpg 路径，例如 frontend/src/static/classes/paladin.jpg")
    parser.add_argument("--width", type=int, default=DEFAULT_WIDTH)
    parser.add_argument("--height", type=int, default=DEFAULT_HEIGHT)
    parser.add_argument("--quality", type=int, default=90)
    args = parser.parse_args()

    source = resolve(args.src)
    if not source.exists():
        raise SystemExit(f"找不到源图：{source}")

    target = resolve(args.dst)
    target.parent.mkdir(parents=True, exist_ok=True)

    with Image.open(source) as image:
        image = image.convert("RGB")
        ratio = args.width / args.height
        image = center_crop_to_ratio(image, ratio)
        image = image.resize((args.width, args.height), Image.LANCZOS)
        image.save(target, "JPEG", quality=args.quality, optimize=True)

    print(f"{source} -> {target}（{args.width}x{args.height}）")
    return 0


if __name__ == "__main__":
    sys.exit(main())
