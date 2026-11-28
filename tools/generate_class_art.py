#!/usr/bin/env python3
"""用本地 ComfyUI 给职业卡生成多套候选图：二次元（Illustrious XL）和写实（Z-Image Turbo）都有。

用法：
    python tools/generate_class_art.py --class paladin                      # 二次元，全部画风
    python tools/generate_class_art.py --class paladin --styles 水彩,厚涂
    python tools/generate_class_art.py --class paladin --engine zimage      # 写实，全部风格
    python tools/generate_class_art.py --class paladin --engine zimage --styles 电影剧照,暗黑油画

前提：ComfyUI 已经在本机 8188 端口跑着（D:\\AI\\ComfyUI 的「启动 ComfyUI.bat」）。
- illustrious：Illustrious-XL-v2.0-stable + models/loras 里的风格 LoRA；
- zimage：z_image_turbo_int8_convrot + qwen_3_4b_fp8_mixed，自然语言提示词，写实向。

输出：
    .tmp/art/<职业>[-<tag>]/<风格>.png   —— 每张候选图
    .tmp/art/<职业>[-<tag>]/_sheet.png   —— 带标签的总览图
    .tmp/art/<职业>[-<tag>]/meta.json    —— 提示词 / 种子 / 参数，方便复现

挑中以后把图复制成 frontend/src/static/classes/<职业>.jpg（16:9），
再确认 data/职业.xlsx 的 imageUrl 指到它即可。
"""

from __future__ import annotations

import argparse
import io
import json
import os
import shutil
import sys
import time
import urllib.parse
import urllib.request
import uuid
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parent.parent

COMFY = "http://127.0.0.1:8188"
COMFY_INPUT = Path(r"D:\AI\ComfyUI\ComfyUI\input")
CHECKPOINT = "Illustrious-XL-v2.0-stable.safetensors"
ZIMAGE_UNET = "z_image_turbo_int8_convrot.safetensors"
ZIMAGE_CLIP = "qwen_3_4b_fp8_mixed.safetensors"
ZIMAGE_VAE = "z_image_ae.safetensors"
ZIMAGE_STEPS = 8
ZIMAGE_CFG = 1.0

DEFAULT_SIZE = (1344, 768)  # 16:9 附近，和职业卡配图比例一致
DEFAULT_SEED = 20261128
NEGATIVE = (
    "lowres, worst quality, low quality, bad anatomy, bad hands, missing fingers, "
    "extra fingers, extra limbs, text, watermark, signature, logo, blurry, cropped, "
    "out of frame, deformed armor, multiple views, greyscale"
)
ZIMAGE_NEGATIVE = (
    "blurry, low quality, watermark, text, deformed, extra fingers, bad hands, "
    "cartoon, anime, cgi, doll face, child, plastic skin"
)

# 职业提示词：同一个主体，换画风只换 LoRA，方便横向对比
CLASS_PROMPTS: dict[str, str] = {
    "paladin": (
        "1boy, solo, full body, wide shot, paladin, knight, adult male, male focus, "
        "silver plate armor, ornate armor, gold trim, white cape, short hair, blue eyes, "
        "serious expression, armored greaves, armored boots, standing, holding longsword, "
        "glowing sword, holding shield, sun emblem, holy light, divine radiance, sunbeams, "
        "ruined battlefield, dawn sky, smoke, sparks, fantasy, cinematic lighting, "
        "dramatic composition, masterpiece, best quality, highly detailed armor"
    ),
    "fighter": (
        "1boy, solo, full body, wide shot, warrior, knight, steel plate armor, scarred armor, "
        "red cape, short hair, determined expression, standing, holding greatsword, "
        "battlefield, banners, smoke, sparks, fantasy, cinematic lighting, "
        "masterpiece, best quality, highly detailed armor"
    ),
    "ranger": (
        "1boy, solo, full body, wide shot, ranger, archer, leather armor, green hooded cloak, "
        "longbow, quiver, forest, mist, sunlight through leaves, fantasy, cinematic lighting, "
        "masterpiece, best quality, highly detailed"
    ),
    "monk": (
        "1boy, solo, full body, wide shot, monk, martial artist, simple robes, bare arms, "
        "martial arts stance, temple courtyard, falling leaves, fantasy, cinematic lighting, "
        "masterpiece, best quality, highly detailed"
    ),
    "druid": (
        "1girl, solo, full body, wide shot, druid, nature priestess, bark armor, moss, antlers, "
        "staff, glowing runes, ancient forest, fireflies, fantasy, cinematic lighting, "
        "masterpiece, best quality, highly detailed"
    ),
}

# 画风：从 风格分类.md 的八个聚类里各挑一个代表 LoRA，外加无水印基线。
# prompt 是追加在职业提示词后面的画风词，lora_strength 控制该风格 LoRA 的强度。
STYLES: list[dict[str, object]] = [
    {"label": "原味基线", "lora": None, "prompt": ""},
    {
        "label": "硬边赛璐璐",
        "lora": "AnimagineXLV3_Style_Difference_EdgeEmphasys.safetensors",
        "prompt": "cel shading, flat color, crisp lineart, anime screencap",
        "lora_strength": 1.0,
    },
    {
        "label": "明快立体",
        "lora": "CoolFlatColor.safetensors",
        "prompt": "strong highlights, 3d shading, glossy, saturated",
    },
    {
        "label": "浓彩奇幻",
        "lora": "ag31-fuzichoco_1-000032.safetensors",
        "prompt": "vibrant colors, colorful, rich detail, fantasy illustration",
    },
    {
        "label": "柔光水彩",
        "lora": "LightWatercolor.safetensors",
        "prompt": "watercolor, soft lighting, pastel colors, paint wash, soft edges",
    },
    {
        "label": "厚涂梦幻",
        "lora": "Impasto.safetensors",
        "prompt": "impasto, thick brushstrokes, oil painting, painterly texture",
    },
    {
        "label": "清爽薄涂",
        "lora": "delta_style_koni-000022.safetensors",
        "prompt": "flat color, clean lineart, minimal shading, cool palette",
    },
    {
        "label": "冷调通透",
        "lora": "kohaku_delta_style_ba_v1-000012.safetensors",
        "prompt": "cool tones, muted colors, atmospheric, blue palette, transparent light",
    },
    {
        "label": "传统媒介",
        "lora": "traditional_media_a31_xl-6.safetensors",
        "prompt": "traditional media, oil painting, canvas texture, classical painting",
    },
    {
        "label": "暗黑奇幻",
        "lora": None,
        "prompt": "dark fantasy, desaturated, gritty, dramatic shadows, oil painting, cinematic",
    },
]
LORA_STRENGTH = 0.9

# 写实向（Z-Image Turbo）：自然语言提示词，同一主体、同一 seed，只换光线 / 质感 / 场景
ZIMAGE_PROMPTS: dict[str, str] = {
    "paladin": (
        "A cinematic portrait of a paladin knight in his thirties, short brown hair and a short "
        "beard, wearing ornate silver plate armor with gold trim and a white cape, standing on the "
        "steps of a ruined cathedral at dawn. He holds a longsword with a faint golden glow and a "
        "kite shield bearing a sun emblem. Warm divine light breaks through storm clouds, embers and "
        "dust drift in the air. Realistic skin and brushed metal, weathered face, ultra detailed, "
        "dramatic lighting, epic fantasy photography."
    ),
    "fighter": (
        "A cinematic film still of a warrior in scarred steel plate armor and a dark red cape, caught "
        "mid-swing with a longsword on a smoke-filled battlefield, sparks and ash in the air, broken "
        "shields and tattered banners behind him, low camera angle, dynamic action pose, realistic "
        "metal and skin, ultra detailed, dramatic lighting."
    ),
    "fighter-champion": (
        "A cinematic film still of a champion fighter in polished steel plate armor, raising a "
        "greatsword overhead in a torch-lit arena, crowd silhouettes and banners behind him, "
        "victorious and confident, dynamic heroic pose, realistic textures, ultra detailed."
    ),
    "fighter-battle-master": (
        "A cinematic film still of a battle master fighter in worn plate armor with a shield and "
        "longsword, directing his troops on a smoky battlefield, pointing his sword forward, soldiers "
        "and banners in the background, tactical confidence, dynamic pose, realistic textures, ultra "
        "detailed."
    ),
    "fighter-eldritch-knight": (
        "A cinematic film still of an eldritch knight in dark steel plate armor, a longsword wreathed "
        "in blue arcane runes, glowing sigils orbiting his free hand, night battlefield with faint "
        "magic mist, dynamic combat pose, realistic textures, ultra detailed."
    ),
    "ranger": (
        "A cinematic film still of a ranger in weathered leather armor and a dark green hooded cloak, "
        "crouched on a fallen log in a misty ancient forest, drawing a longbow with an arrow nocked, "
        "a quiver of arrows on his back, shafts of morning sunlight cutting through the canopy, mist "
        "and drifting leaves, dynamic pose, low camera angle, realistic skin and textures, ultra "
        "detailed, dramatic lighting."
    ),
    "ranger-action2": (
        "A cinematic film still of a ranger in weathered leather armor and a dark green hooded cloak, "
        "walking forward through a misty ancient forest, bow in his left hand and an arrow in his right, "
        "caught mid-stride and glancing to the side, a quiver on his back, shafts of sunlight cutting "
        "through the pines, drifting mist and leaves, dynamic pose, low camera angle, realistic skin "
        "and textures, ultra detailed, dramatic lighting."
    ),
    "ranger-hunter": (
        "A cinematic film still of a hunter ranger in leather armor and a hood, crouched behind a "
        "fallen log at the edge of a misty forest clearing, drawing a longbow and aiming into the fog, "
        "quiver of arrows on his back, shafts of dawn light through the pines, focused expression, "
        "dynamic tension in the pose, realistic textures, ultra detailed."
    ),
    "ranger-beast-master": (
        "A cinematic film still of a young ranger in dark leather armor and a green hood, striding "
        "through a misty pine forest beside a large grey wolf, bow in one hand and quiver on his back, "
        "the wolf at his side with ears alert, both moving together as a team, dynamic action pose, "
        "realistic fur and leather textures, ultra detailed."
    ),
    "monk": (
        "A cinematic film still of a monk in simple saffron robes, caught mid-kick in a temple courtyard "
        "at dawn, bare arms wrapped in cloth, prayer beads swinging, stone lions and incense smoke "
        "behind, dynamic martial arts pose, low camera angle, realistic skin and fabric, ultra detailed."
    ),
    "monk-open-hand": (
        "A cinematic film still of a monk of the Open Hand in simple robes, striking with an open palm "
        "in a torch-lit monastery hall, a visible shockwave of air, focused expression, dynamic combat "
        "pose, realistic skin and fabric, ultra detailed."
    ),
    "monk-shadow": (
        "A cinematic film still of a shadow monk in dark wrapped robes, crouched on a tiled rooftop under "
        "moonlight, face partly hidden by a hood, a dagger in one hand, mist and lantern glow below, "
        "dynamic stealth pose, realistic textures, ultra detailed."
    ),
    "monk-four-elements": (
        "A cinematic film still of a monk of the Four Elements in simple robes, surrounded by swirling "
        "spheres of fire, water, earth and air, hands weaving a kata, glowing elemental runes, dynamic "
        "pose, realistic textures, ultra detailed."
    ),
    "druid": (
        "A cinematic film still of a druid in bark-and-leaf armor woven with moss, holding a gnarled "
        "staff, standing in an ancient forest clearing as vines and roots rise around her, green-gold "
        "sunlight through the canopy, fireflies and drifting spores, dynamic casting pose, realistic "
        "textures, ultra detailed."
    ),
    "druid-land": (
        "A cinematic film still of a druid of the land in layered robes and a mantle of leaves, standing "
        "on a rocky cliff above a misty valley, staff planted in the ground, glowing green runes tracing "
        "the stone, wind and birds, realistic textures, ultra detailed."
    ),
    "druid-moon": (
        "A cinematic film still of a druid of the moon half-transformed under a full moon in a dark "
        "forest, fur and claws emerging from her arms, tattered robes, glowing yellow eyes, mist and "
        "moonlight, dynamic pose, realistic textures, ultra detailed."
    ),
    # ---- 圣武士子职（职业卡的「详情 / 选择子职」用） ----
    "paladin-devotion": (
        "A cinematic film still of a paladin of devotion in polished silver plate armor with gold "
        "filigree and a snow-white tabard, kneeling on the steps of a sunlit cathedral, sword point "
        "down before a sunburst emblem, warm golden light streaming through stained glass, serene and "
        "resolute expression, realistic skin, weathered face, ultra detailed."
    ),
    "paladin-ancients": (
        "A cinematic film still of an elf paladin of the ancients in bark-and-bronze armor woven with "
        "moss, a pair of curved antlers on the helmet, standing among giant tree roots in an ancient "
        "forest, holding a vine-wrapped longsword, green-gold sunbeams through the canopy, fireflies "
        "and drifting leaves, mystical and serene, realistic textures, ultra detailed."
    ),
    "paladin-vengeance": (
        "A cinematic film still of a battle-scarred paladin of vengeance in blackened plate armor and "
        "a tattered dark cape, standing in a burning village at night, gripping a notched greatsword, "
        "embers and ash swirling in the air, cold rage in his eyes, dramatic backlight, gritty "
        "realism, ultra detailed."
    ),
    # ---- 动态姿势版（职业图 / 古贤之誓用） ----
    "paladin-action": (
        "A cinematic film still of a paladin knight in his thirties, short brown hair and a short "
        "beard, wearing ornate silver plate armor with gold trim and a flowing white cape, caught "
        "mid-stride as he strides out of a ruined cathedral gate at dawn, raising his longsword toward "
        "the sky, a kite shield with a sun emblem on his left arm, cape billowing, warm divine light "
        "breaking through storm clouds, embers and dust in the air, low camera angle, dynamic heroic "
        "pose, realistic skin and brushed metal, ultra detailed, dramatic lighting."
    ),
    "paladin-ancients-action": (
        "A cinematic film still of an elf paladin of the ancients in bark-and-bronze armor woven with "
        "moss, curved antlers on the helmet, lunging forward through an ancient forest while swinging a "
        "vine-wrapped longsword, leaves and fireflies swirling around him, green-gold sunbeams through "
        "the canopy, dynamic action pose, low camera angle, realistic textures, ultra detailed."
    ),
}

REALISTIC_STYLES: list[dict[str, object]] = [
    {
        "label": "电影剧照",
        "prompt": (
            "Photographed as a cinematic film still, 35mm lens, shallow depth of field, "
            "natural film grain, realistic skin texture, dramatic golden backlight, "
            "high production value, sharp focus on the face."
        ),
    },
    {"label": "暗黑油画", "prompt": "Classical oil painting, chiaroscuro lighting, desaturated palette, dark fantasy, visible brushwork."},
    {"label": "圣光圣像", "prompt": "Religious iconography, radiant golden halo of light, stained glass colors, solemn and reverent atmosphere."},
    {"label": "史诗战场", "prompt": "Gritty battlefield realism, smoke, ash and embers, low camera angle, rain-soaked ground, epic scale."},
    {"label": "冷调雨夜", "prompt": "Cold blue tones, wet armor reflecting torchlight, rain and fog, grim and quiet mood, realistic detail."},
    {"label": "暖阳晨光", "prompt": "Warm golden morning light, soft haze, flowers on cracked stone, hopeful heroic tone, realistic detail."},
    {"label": "黑白胶片", "prompt": "Black and white medium format photography, strong contrast, film grain, timeless heroic portrait."},
    {"label": "古典壁画", "prompt": "Renaissance fresco painting style, marble and gold leaf, solemn composition, aged texture."},
]


def post(path: str, payload: dict) -> dict:
    data = json.dumps(payload).encode("utf-8")
    req = urllib.request.Request(COMFY + path, data=data, headers={"Content-Type": "application/json"})
    with urllib.request.urlopen(req, timeout=120) as response:
        return json.loads(response.read().decode("utf-8"))


def get(path: str) -> dict:
    with urllib.request.urlopen(COMFY + path, timeout=120) as response:
        return json.loads(response.read().decode("utf-8"))


def wait_ready(timeout: float = 120) -> None:
    deadline = time.time() + timeout
    while time.time() < deadline:
        try:
            get("/system_stats")
            return
        except Exception:
            time.sleep(2)
    raise SystemExit(f"连不上 ComfyUI（{COMFY}），先启动它再跑。")


def build_graph(
    prompt: str,
    negative: str,
    lora: str | None,
    lora_strength: float,
    seed: int,
    steps: int,
    cfg: float,
    size: tuple[int, int],
    prefix: str,
) -> dict:
    graph: dict[str, object] = {
        "1": {"class_type": "CheckpointLoaderSimple", "inputs": {"ckpt_name": CHECKPOINT}},
    }
    model_src: list[object] = ["1", 0]
    clip_src: list[object] = ["1", 1]

    if lora:
        graph["2"] = {
            "class_type": "LoraLoader",
            "inputs": {
                "model": ["1", 0],
                "clip": ["1", 1],
                "lora_name": lora,
                "strength_model": lora_strength,
                "strength_clip": lora_strength,
            },
        }
        model_src, clip_src = ["2", 0], ["2", 1]

    graph.update(
        {
            "3": {"class_type": "CLIPTextEncode", "inputs": {"clip": clip_src, "text": prompt}},
            "4": {"class_type": "CLIPTextEncode", "inputs": {"clip": clip_src, "text": negative}},
            "5": {
                "class_type": "EmptyLatentImage",
                "inputs": {"width": size[0], "height": size[1], "batch_size": 1},
            },
            "6": {
                "class_type": "KSampler",
                "inputs": {
                    "model": model_src,
                    "positive": ["3", 0],
                    "negative": ["4", 0],
                    "latent_image": ["5", 0],
                    "seed": seed,
                    "steps": steps,
                    "cfg": cfg,
                    "sampler_name": "euler_ancestral",
                    "scheduler": "normal",
                    "denoise": 1.0,
                },
            },
            "7": {"class_type": "VAEDecode", "inputs": {"samples": ["6", 0], "vae": ["1", 2]}},
            "8": {"class_type": "SaveImage", "inputs": {"images": ["7", 0], "filename_prefix": prefix}},
        }
    )
    return graph


def build_zimage_graph(
    prompt: str,
    negative: str,
    seed: int,
    size: tuple[int, int],
    prefix: str,
    init_image: str | None = None,
    denoise: float = 1.0,
) -> dict:
    """Z-Image Turbo：UNET + Qwen3 文本编码 + 专用 VAE，8 步、CFG 1.0。

    init_image 不为空时走图生图（VAEEncode + denoise < 1），用同一张底图保证角色形象一致。
    """
    graph = {
        "1": {
            "class_type": "UNETLoader",
            "inputs": {"unet_name": ZIMAGE_UNET, "weight_dtype": "default"},
        },
        "2": {"class_type": "ModelSamplingAuraFlow", "inputs": {"model": ["1", 0], "shift": 3}},
        "3": {
            "class_type": "CLIPLoader",
            "inputs": {"clip_name": ZIMAGE_CLIP, "type": "lumina2", "device": "default"},
        },
        "4": {"class_type": "VAELoader", "inputs": {"vae_name": ZIMAGE_VAE}},
        "5": {"class_type": "CLIPTextEncode", "inputs": {"clip": ["3", 0], "text": prompt}},
        "6": {"class_type": "CLIPTextEncode", "inputs": {"clip": ["3", 0], "text": negative}},
        "8": {
            "class_type": "KSampler",
            "inputs": {
                "model": ["2", 0],
                "positive": ["5", 0],
                "negative": ["6", 0],
                "seed": seed,
                "steps": ZIMAGE_STEPS,
                "cfg": ZIMAGE_CFG,
                "sampler_name": "res_multistep",
                "scheduler": "simple",
                "denoise": denoise,
            },
        },
        "9": {"class_type": "VAEDecode", "inputs": {"samples": ["8", 0], "vae": ["4", 0]}},
        "10": {"class_type": "SaveImage", "inputs": {"images": ["9", 0], "filename_prefix": prefix}},
    }

    if init_image:
        graph["7"] = {"class_type": "LoadImage", "inputs": {"image": init_image}}
        graph["11"] = {
            "class_type": "VAEEncode",
            "inputs": {"pixels": ["7", 0], "vae": ["4", 0]},
        }
        graph["8"]["inputs"]["latent_image"] = ["11", 0]
    else:
        graph["7"] = {
            "class_type": "EmptySD3LatentImage",
            "inputs": {"width": size[0], "height": size[1], "batch_size": 1},
        }
        graph["8"]["inputs"]["latent_image"] = ["7", 0]

    return graph


def generate(graph: dict, timeout: float = 900) -> bytes:
    client_id = str(uuid.uuid4())
    prompt_id = post("/prompt", {"prompt": graph, "client_id": client_id})["prompt_id"]

    started = time.time()
    while time.time() - started < timeout:
        time.sleep(2)
        history = get(f"/history/{prompt_id}")
        if prompt_id not in history:
            continue

        entry = history[prompt_id]
        status = entry.get("status", {}).get("status_str")
        images = [image for node in entry.get("outputs", {}).values() for image in node.get("images", [])]
        if status != "success" or not images:
            message = entry.get("status", {}).get("messages", [])
            raise RuntimeError(f"出图失败（{status}）：{message}")

        image = images[0]
        query = urllib.parse.urlencode(
            {"filename": image["filename"], "subfolder": image.get("subfolder", ""), "type": image.get("type", "output")}
        )
        with urllib.request.urlopen(f"{COMFY}/view?{query}", timeout=120) as response:
            return response.read()

    raise TimeoutError(f"等待出图超时（{timeout:.0f}s）")


def make_sheet(records: list[dict[str, object]], sheet_path: Path, subtitle: str = "") -> None:
    from PIL import Image, ImageDraw, ImageFont

    font_path = Path(r"C:\Windows\Fonts\msyh.ttc")
    label_font = ImageFont.truetype(str(font_path), 20) if font_path.exists() else ImageFont.load_default()
    header_font = ImageFont.truetype(str(font_path), 26) if font_path.exists() else ImageFont.load_default()

    cols = 3
    thumb_w, thumb_h = 400, 229
    pad, label_h, header = 12, 34, 54
    rows = (len(records) + cols - 1) // cols
    width = cols * thumb_w + (cols + 1) * pad
    height = header + rows * (thumb_h + label_h + pad) + pad

    sheet = Image.new("RGB", (width, height), (24, 24, 28))
    draw = ImageDraw.Draw(sheet)
    draw.text(
        (pad, 16),
        "TRPG 职业卡候选%s  %d styles  seed=%s"
        % (f" · {subtitle}" if subtitle else "", len(records), records[0].get("seed") if records else ""),
        fill=(235, 235, 240),
        font=header_font,
    )

    for index, record in enumerate(records):
        row, col = divmod(index, cols)
        x = pad + col * (thumb_w + pad)
        y = header + row * (thumb_h + label_h + pad)
        try:
            with Image.open(record["path"]) as image:
                image = image.convert("RGB")
                image = image.resize((thumb_w, thumb_h))
                sheet.paste(image, (x, y))
        except Exception:
            draw.text((x + 6, y + 6), "ERR", fill=(255, 90, 90), font=label_font)
        draw.text((x, y + thumb_h + 8), str(record["label"]), fill=(220, 220, 230), font=label_font)

    sheet_path.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(sheet_path)


def parse_size(raw: str) -> tuple[int, int]:
    try:
        width, height = (int(part) for part in raw.lower().split("x"))
    except ValueError as error:
        raise SystemExit(f"--size 要写成 宽x高，例如 1344x768（收到 {raw}）") from error
    return width, height


def resolve_path(raw: str) -> Path:
    value = Path(raw)
    return value if value.is_absolute() else REPO_ROOT / value


def main() -> int:
    parser = argparse.ArgumentParser(description="用本地 ComfyUI 生成职业卡候选图（二次元 / 写实）")
    parser.add_argument("--class", dest="class_id", required=True, help="职业 id，例如 paladin")
    parser.add_argument("--engine", choices=["illustrious", "zimage"], default="illustrious",
                        help="illustrious=二次元（LoRA 画风），zimage=写实（自然语言风格）")
    parser.add_argument("--styles", default="", help="逗号分隔的风格标签，默认全部")
    parser.add_argument("--seed", type=int, default=DEFAULT_SEED)
    parser.add_argument("--count", type=int, default=1, help="每个风格抽几张（seed 从 --seed 开始依次 +1）")
    parser.add_argument("--steps", type=int, default=0, help="采样步数，默认二次元 30 / 写实 8")
    parser.add_argument("--cfg", type=float, default=0.0, help="CFG，默认二次元 5.0 / 写实 1.0")
    parser.add_argument("--size", default="%dx%d" % DEFAULT_SIZE, help="宽x高，默认 1344x768")
    parser.add_argument("--tag", default="", help="输出目录后缀，例如 realistic -> .tmp/art/<class>-realistic")
    parser.add_argument("--out", default="", help="输出目录，默认 .tmp/art/<class>[-tag]")
    parser.add_argument("--prompt", default="", help="覆盖提示词（默认用脚本里的职业提示词）")
    parser.add_argument("--init", default="", help="图生图底图（写实引擎用），用同一张底图保证角色形象一致")
    parser.add_argument("--denoise", type=float, default=0.0, help="图生图重绘强度，默认 0.55；1.0 等于文生图")
    parser.add_argument("--comfy-input", default=str(COMFY_INPUT), help="ComfyUI 的 input 目录，图生图底图会复制进去")
    args = parser.parse_args()

    realistic = args.engine == "zimage"
    prompt = args.prompt or (ZIMAGE_PROMPTS if realistic else CLASS_PROMPTS).get(args.class_id)
    if not prompt:
        source = "ZIMAGE_PROMPTS" if realistic else "CLASS_PROMPTS"
        raise SystemExit(f"没有「{args.class_id}」的提示词：先在 {source} 里补一条，或用 --prompt 传。")

    style_pool = REALISTIC_STYLES if realistic else STYLES
    wanted = {item.strip() for item in args.styles.split(",") if item.strip()}
    styles = [style for style in style_pool if not wanted or str(style["label"]) in wanted]
    if not styles:
        raise SystemExit(f"没有匹配的风格：{args.styles}；可选 {[s['label'] for s in style_pool]}")

    steps = args.steps or (ZIMAGE_STEPS if realistic else 30)
    cfg = args.cfg or (ZIMAGE_CFG if realistic else 5.0)
    size = parse_size(args.size)
    dir_name = args.class_id + (f"-{args.tag}" if args.tag else "")
    out_dir = Path(args.out) if args.out else REPO_ROOT / ".tmp" / "art" / dir_name
    out_dir.mkdir(parents=True, exist_ok=True)

    # 图生图：把底图复制进 ComfyUI 的 input 目录，换一个唯一文件名避免缓存
    init_name: str | None = None
    denoise = args.denoise or 0.55
    if args.init:
        init_path = resolve_path(args.init)
        if not init_path.exists():
            raise SystemExit(f"找不到底图：{init_path}")
        if not realistic:
            raise SystemExit("--init 目前只支持 --engine zimage（写实引擎）。")
        comfy_input = Path(args.comfy_input)
        comfy_input.mkdir(parents=True, exist_ok=True)
        init_name = f"trpg_init_{uuid.uuid4().hex[:8]}{init_path.suffix.lower()}"
        shutil.copyfile(init_path, comfy_input / init_name)
        print(f"图生图底图：{init_path.name}（denoise={denoise}）")

    wait_ready()
    seeds = [args.seed + offset for offset in range(max(1, args.count))]
    records: list[dict[str, object]] = []
    done = 0
    total = len(styles) * len(seeds)

    for style in styles:
        lora = style.get("lora")
        strength = float(style.get("lora_strength", LORA_STRENGTH))
        style_prompt = str(style.get("prompt", "")).strip()
        full_prompt = (
            f"{prompt} {style_prompt}".strip() if realistic else f"{prompt}, {style_prompt}".strip(", ")
        )

        for seed in seeds:
            done += 1
            label = str(style["label"]) if len(seeds) == 1 else f"{style['label']}_{seed}"
            print(f"[{done}/{total}] {label}" + (f"  LoRA={lora}" if lora else ""), end="  ", flush=True)

            graph = (
                build_zimage_graph(
                    prompt=full_prompt,
                    negative=ZIMAGE_NEGATIVE,
                    seed=seed,
                    size=size,
                    prefix=f"trpg_{args.class_id}_{done:02d}",
                    init_image=init_name,
                    denoise=denoise if init_name else 1.0,
                )
                if realistic
                else build_graph(
                    prompt=full_prompt,
                    negative=NEGATIVE,
                    lora=str(lora) if lora else None,
                    lora_strength=strength,
                    seed=seed,
                    steps=steps,
                    cfg=cfg,
                    size=size,
                    prefix=f"trpg_{args.class_id}_{done:02d}",
                )
            )
            started = time.time()
            try:
                data = generate(graph)
            except Exception as error:  # 单张失败不影响其它风格
                print(f"失败：{error}")
                continue

            file_path = out_dir / f"{label}.png"
            file_path.write_bytes(data)
            records.append(
                {
                    "label": label,
                    "engine": args.engine,
                    "lora": lora,
                    "lora_strength": strength if lora else None,
                    "prompt": full_prompt,
                    "path": str(file_path),
                    "seed": seed,
                    "size": list(size),
                    "steps": steps,
                    "cfg": cfg,
                    "init": init_name,
                    "denoise": denoise if init_name else 1.0,
                }
            )
            print(f"{time.time() - started:.1f}s -> {file_path.name}", flush=True)

    if not records:
        print("一张都没出成。")
        return 1

    sheet_path = out_dir / "_sheet.png"
    make_sheet(records, sheet_path, subtitle="写实 · Z-Image" if realistic else "二次元 · Illustrious")
    (out_dir / "meta.json").write_text(
        json.dumps(
            {
                "class": args.class_id,
                "engine": args.engine,
                "model": ZIMAGE_UNET if realistic else CHECKPOINT,
                "prompt": prompt,
                "negative": ZIMAGE_NEGATIVE if realistic else NEGATIVE,
                "size": list(size),
                "seed": args.seed,
                "steps": steps,
                "cfg": cfg,
                "images": records,
            },
            ensure_ascii=False,
            indent=2,
        ),
        encoding="utf-8",
    )

    print("总览图:", sheet_path)
    print("目录:", out_dir)
    return 0


if __name__ == "__main__":
    sys.exit(main())
