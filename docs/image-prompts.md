# 配图提示词（豆包 / 即梦）

> 2026-11-28：圣武士四张图用本地 Z-Image Turbo（「电影剧照」写实风格）重做，覆盖 `static/classes/paladin.jpg` 与
> `static/subclasses/paladin-*.jpg`。职业图与古贤之誓用「动态姿势」提示词（`paladin-action`、`paladin-ancients-action`，
> seed 20261129 / 20261130），奉献、复仇沿用初版（seed 20261128）。提示词与参数在 `.tmp/art/*/meta.json`（可复现），
> 工具见 `tools/generate_class_art.py`（支持 `--engine zimage`、`--init/--denoise` 图生图）与 `tools/fit_class_art.py`
> （16:9 裁切 + jpg）。下面保留最早的豆包提示词作为备用。
>
> 2026-11-28（游侠）：职业图 `static/classes/ranger-forest.jpg`（走路持弓姿势，seed 20261129）、猎人 `ranger-hunter.jpg`
> （seed 20261131）、驯兽师 `ranger-beast-master.jpg`（seed 20261132），同样是电影剧照风格；子职内容按 5E 不全书
> 写进 `data/子职.xlsx`（猎人 3/7/11/15 级分别选猎物 / 防守战术 / 多重攻击 / 高效猎人防御；驯兽师为游侠伴侣 / 特殊训练 /
> 兽性狂怒 / 法术共享）。职业图特意换了文件名，避免浏览器拿旧缓存。
>
> 2026-11-28（战士）：职业图 `static/classes/fighter-warrior.jpg`（挥剑冲锋，seed 20261128）、勇士 `fighter-champion.jpg`
> （seed 20261132）、战斗大师 `fighter-battle-master.jpg`（seed 20261129）、奥法骑士
> `fighter-eldritch-knight-arcane.jpg`（seed 20261131）；子职内容同样写进 `data/子职.xlsx`，职业图与奥法骑士图都换了文件名避免缓存。
>
> 2026-11-28（武僧）：职业图 `static/classes/monk-warrior.jpg`（腾空踢腿，seed 20261128）、散打宗 `monk-open-hand.jpg`
> （seed 20261129）、暗影宗 `monk-shadow.jpg`（seed 20261131）、四象宗 `monk-four-elements.jpg`（seed 20261132）；
> 三个宗派按 5E 不全书 3/6/11/17 级写进 `data/子职.xlsx`。
>
> 2026-11-28（德鲁伊）：职业图 `static/classes/druid-circle.jpg`（seed 20261128）、大地结社 `druid-circle-land.jpg`
> （seed 20261128）、月亮结社 `druid-circle-moon.jpg`（seed 20261132）；结社内容按 5E 不全书 2/3/6/10/14 级写进
> `data/子职.xlsx`，职业图换新文件名避免缓存。

圣武士三个子职的卡片图，生成后放进 `frontend/src/static/subclasses/`：

| 文件 | 子职 |
| --- | --- |
| `paladin-devotion.jpg` | 奉献之誓 |
| `paladin-ancients.jpg` | 古贤之誓 |
| `paladin-vengeance.jpg` | 复仇之誓 |

统一要求：**16:9**（1200×674 以上，能出 2K 更好），暗黑奇幻写实厚涂、电影感，和 `static/classes/*.jpg` 同一套观感；三等分构图、角色偏中，不要文字/边框/logo。

## 奉献之誓 → paladin-devotion.jpg

```
16:9 电影感数字绘画，暗黑奇幻写实厚涂风格，高细节。
破晓时分，一名圣武士站在倒塌教堂前的石阶上，银色重甲镶金边，胸甲刻着展开双翼的天使纹章；左手高举圆盾，盾面的太阳圣徽迸发金色光芒，驱散周围的灰烬与黑雾；右手持长剑，剑身缠绕金色圣光；白色披风被风掀起，下摆沾着尘土与火星。
身后是跪地祈祷的村民与燃烧后余烟未散的废墟，远处晨光从云缝中倾泻下来；主角面部坚毅平静，目光望向远方。
色调：暗棕、冷灰与金色高光；光影强烈，空气中可见尘埃颗粒，史诗感。
```

## 古贤之誓 → paladin-ancients.jpg

```
16:9 电影感数字绘画，暗黑奇幻写实厚涂风格，高细节。
幽深古老的森林里，一名精灵圣武士站在盘根错节的巨树根系之间；护甲由深绿树皮、青铜与苔藓构成，头盔是一对弯曲的鹿角；左手按在发光的古树树干上，右手持一把缠满藤蔓与白花的长剑；披风上绣着藤蔓与鹿角纹样。
周围漂浮着萤火与飘落的树叶，一道斜射的林间光柱照亮半身，背景深处隐约有石阵与蔓生的发光符文；气氛宁静、古老、充满生机。
色调：灰绿、苔藓绿与暖金色光斑；湿润的空气感，景深虚化。
```

## 复仇之誓 → paladin-vengeance.jpg

```
16:9 电影感数字绘画，暗黑奇幻写实厚涂风格，高细节。
夜色中燃烧的村庄，一名黑甲圣武士站在余烬与浓烟之间；铠甲布满划痕与凹痕，边缘被熏黑，披风焦黑破损；右手持一把带缺口的巨剑，剑锋映着远处的火光，左手攥紧一枚失去光泽的圣徽。
火星在空气中飞舞，背景是倒塌的钟楼与燃烧的房屋，隐约有逃散的人影；主角微微低头，只露出半张脸，眼神冷硬、压抑着怒火。
色调：暗红、炭黑与炽橙高光；强逆光，烟尘弥漫，肃杀感。
```

## 负向提示词（模型支持就填）

```
文字, 英文, 字幕, 水印, logo, 边框, 拼贴, 现代服装, 枪械, 卡通, 二次元, 低分辨率, 模糊, 比例错误, 多余手指, 盔甲穿模, 血腥
```

## 小提示

- 豆包出的图右下角可能带「豆包AI生成」水印，裁成 16:9 时切掉底部即可。
- 三张最好用同一轮对话连续生成，画风更统一；不满意就只改局部描述（光、色调、姿态），别把整段重写。
- 生成后同名覆盖，前端子职卡会自动换成大图；不想用色块兜底就把 `子职.xlsx` 的 `imageUrl` 指向新文件。

## 本地 ComfyUI 生图（职业卡候选）

本机 `D:\AI\ComfyUI` 装了两套模型，可以直接出图、多风格对比：

- **二次元**：Illustrious-XL v2 + `models/loras` 里的一批画风 LoRA
- **写实**：Z-Image Turbo（`z_image_turbo_int8_convrot` + `qwen_3_4b_fp8_mixed`），自然语言提示词

```powershell
# 先启动 ComfyUI（黑窗口保持开着），再跑生成脚本
python tools/generate_class_art.py --class paladin                        # 二次元全部画风
python tools/generate_class_art.py --class paladin --styles 柔光水彩,厚涂梦幻 --seed 123
python tools/generate_class_art.py --class paladin --engine zimage --tag realistic   # 写实全部风格
python tools/generate_class_art.py --class paladin --engine zimage --styles 电影剧照,圣光圣像
```

- 输出在 `.tmp/art/<职业>[-<tag>]/`：每张候选 `<风格>.png` + `_sheet.png` 总览图 + `meta.json`（提示词 / seed / 参数，可复现）
- 二次元画风按 `D:\AI\ComfyUI\风格分类.md` 的聚类挑代表 LoRA；写实风格是提示词里的光线 / 质感 / 场景方向（电影剧照、暗黑油画、圣光圣像、史诗战场、冷调雨夜、暖阳晨光、黑白胶片、古典壁画）
- 提示词在脚本的 `CLASS_PROMPTS`（二次元）和 `ZIMAGE_PROMPTS`（写实）里，加职业各补一条；挑中后把图复制成 `frontend/src/static/classes/<职业>.jpg`，前端会自动用上
