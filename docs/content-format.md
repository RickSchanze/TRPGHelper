# 内容文件格式（data/*.xlsx）

内容以仓库根目录 `data/` 下的 xlsx 为准，后端启动时读进数据库（`ContentSynchronizer`）。
**文件名用中文，工作表名用英文**；跨表引用一律用 id。玩家数据（用户 / 角色卡 / 操作记录）不在这些文件里，只存在 SQLite。

通用规则：

- 第一行是表头，之后每行一条；**行顺序就是展示 / 抽取顺序**（加载时按 10、20、30… 生成排序值）。
- 空行会被忽略。
- `isEnabled` / `available` 列填 `是` / `否`（也接受 `true` / `false` / `1` / `0`，留空默认为「是」）。
- 关键词按分类拆成多个文件：`关键词.<分类>.xlsx`，分类可以带点（`关键词.法术.一环.xlsx` → `法术.一环`）。

## 规则系统.xlsx

### systems

| 列 | 必填 | 说明 |
| --- | --- | --- |
| `id` | 是 | 规则系统 id（同时也是 code），小写，例如 `coc7`、`dnd5e`；被其他表用 `systemId` 引用 |
| `name` | 是 | 展示名，例如「龙与地下城 5版」 |
| `description` | 否 | 一句话介绍 |
| `nameTemplate` | 否 | 姓名生成模板，用 `{分组}` 引用 `姓名片段.xlsx > names` 的 `category`，例如 `{surname}{given}`、`{first}·{epithet}`；留空默认 `{surname}{given}` |
| `iconUrl` | 否 | 规则书图标，例如 `/static/rulebooks/dnd5e.png` |
| `accent` | 否 | 图标底色 / 卡片主色，例如 `#5a1b1b` |
| `available` | 否 | 是否已开放（`否` 时前端显示「即将开放」且不可选），留空默认「是」 |

### attributes

| 列 | 必填 | 说明 |
| --- | --- | --- |
| `systemId` | 是 | 引用 `systems.id` |
| `key` | 是 | 属性键，例如 `STR` |
| `label` | 是 | 展示名，例如「力量」 |
| `formula` | 是 | 骰子表达式，例如 `3d6*5`、`4d6kh3`；加载时用骰子引擎校验，写错会启动失败并指出行号 |

## 姓名片段.xlsx（工作表 names）

| 列 | 必填 | 说明 |
| --- | --- | --- |
| `systemId` | 是 | 引用 `systems.id` |
| `category` | 是 | 片段分组，例如 `surname` / `given` / `first` / `epithet`，和系统 `nameTemplate` 里的占位符对应 |
| `value` | 是 | 片段本身 |

## 职业.xlsx（工作表 classes）

| 列 | 必填 | 说明 |
| --- | --- | --- |
| `id` | 是 | 职业 id（小写，例如 `ranger`），被 `职业等级.xlsx > classId` 引用 |
| `systemId` | 是 | 引用 `systems.id` |
| `name` | 是 | 职业名 |
| `description` | 否 | 职业卡描述（支持 `[关键词]`） |
| `hitDie` | 是 | 生命骰，例如 `d10` |
| `primaryAbility` | 是 | 主属性描述（支持 `[关键词]`） |
| `imageUrl` | 否 | 职业卡配图：`/static/classes/xxx.jpg` 或 `/uploads/xxx.png` |
| `features` | 否 | 职业卡上的特性列表，**一行一条**（Excel 单元格内 Alt+Enter 换行） |
| `accent` | 否 | 职业卡主色（没配图时用色块），例如 `#3f5a3a` |

## 子职.xlsx（工作表 subclasses）

车卡流程「选择子职」这一步的卡片数据，按职业分组。

| 列 | 必填 | 说明 |
| --- | --- | --- |
| `id` | 是 | 子职 id（小写，例如 `devotion`），同一职业内唯一 |
| `classId` | 是 | 所属职业 id，例如 `paladin`，会在 `职业.xlsx` 里校验 |
| `name` | 是 | 子职名，例如「奉献之誓」 |
| `description` | 否 | 一句话定位（支持 `[关键词]`） |
| `imageUrl` | 是 | 卡片配图，例如 `/static/subclasses/paladin-devotion.jpg`；没图会退化成色块，但正式内容要求配图 |
| `accent` | 否 | 没配图时的色块颜色 |
| `features` | 否 | 卡片上的特性列表，**一行一条**（Excel 单元格内 Alt+Enter 换行） |

配图提示词见 [image-prompts.md](image-prompts.md)。

## 随机职业.xlsx（工作表 occupations）

| 列 | 必填 | 说明 |
| --- | --- | --- |
| `systemId` | 是 | 引用 `systems.id` |
| `value` | 是 | 职业名 |

## 背景.xlsx（工作表 backgrounds）

| 列 | 必填 | 说明 |
| --- | --- | --- |
| `systemId` | 是 | 引用 `systems.id` |
| `value` | 是 | 背景名 |

职业生成器会把同一系统的 `随机职业.xlsx` 和 `背景.xlsx` 混在一起抽。

## 种族.xlsx（工作表 races）

| 列 | 必填 | 说明 |
| --- | --- | --- |
| `id` | 是 | 种族 id，例如 `elf` |
| `systemId` | 是 | 引用 `systems.id` |
| `name` / `description` | 是 / 否 | 展示名与描述 |
| `abilityBonus` | 是 | 属性加值，例如「敏捷 +2」 |
| `speed` | 否 | 速度（尺），留空按 30 |

## 职业灵感.xlsx（工作表 inspirations）

| 列 | 必填 | 说明 |
| --- | --- | --- |
| `systemId` | 否 | 引用 `systems.id`；留空表示通用 |
| `title` | 是 | 灵感标题 |
| `text` | 是 | 灵感正文（支持 `[关键词]`） |

## 车卡流程.xlsx（工作表 steps）

| 列 | 必填 | 说明 |
| --- | --- | --- |
| `systemId` | 否 | 引用 `systems.id`；留空表示通用 |
| `kind` | 是 | 步骤类型：`class` / `subclass` / `race` / `attributes` / `name` / `summary` |
| `title` | 是 | 步骤标题（stepper 上显示） |
| `description` | 否 | 步骤说明 |
| `isEnabled` | 否 | 是否启用 |

**选规则书（DND / COC）是流程前置条件，不写在这里。**

## 职业等级.xlsx（工作表 levels）

| 列 | 必填 | 说明 |
| --- | --- | --- |
| `classId` | 是 | 职业名（中文，例如 `游侠`）或职业 id（英文，例如 `ranger`）；两种都认，导出时统一写成中文职业名 |
| `level` | 是 | 1-20 |
| `title` | 是 | 特性名，例如「额外攻击」 |
| `description` | 是 | 特性说明（支持 `[关键词]`） |
| `isEnabled` | 否 | 是否启用 |

同一职业同一级可以有多行（例如 1 级同时有「战斗风格」和「回气」）。子职没有独立的表：在子职获得特性的等级（战士 3/7/10/15/18、游侠 3/7/11/15、武僧 3/6/11/17、德鲁伊 2/6/10/14、圣武士 3/7/15/20）写一行概述，正式的选子职步骤以后再加。

## 关键词.<分类>.xlsx（工作表 keywords）

文件按分类拆分，例如 `关键词.属性.xlsx`、`关键词.战斗.xlsx`、`关键词.法术.xlsx`。

| 列 | 必填 | 说明 |
| --- | --- | --- |
| `id` | 否 | 关键词 ID（8 位，前三位是分类编号）；留空时加载会按分类顺序自动补号，导出时写回 |
| `key` | 是 | 关键词本身（不含方括号，例如 `敏捷`），跨文件保持唯一（大小写不敏感） |
| `分类` | 否 | 分类名；留空时用文件名里的分类，例如 `关键词.战斗.xlsx` → `战斗` |
| `title` | 否 | 弹窗标题，留空用 `key` |
| `summary` | 是 | 简略描述：一句话结论，显示在弹窗标题下、词条列表与卡面提示；不写 `[方括号]`，最多 256 字 |
| `content` | 是 | 详细解释（支持再嵌 `[其它关键词]`，弹窗会一层层叠起来） |
| `imageUrl` | 否 | 弹窗配图 |
| `isEnabled` | 否 | 是否启用；没有启用的词条，前端会原样显示 `[关键词]` |

`summary` 和 `content` 分工：`summary` 回答「这是什么」，`content` 回答「具体怎么算」；`summary` 里不嵌关键词，避免弹窗后再套弹窗。

新增分类不需要改代码：新建 `关键词.<新分类>.xlsx`，表头保持一致即可。

## 加载流程

1. 启动时 `ContentPaths` 找到 `data/`（默认从内容根目录 / 程序目录向上找含 `规则系统.xlsx` 的目录；也可以用配置 `Content:DataPath` 指定）。
2. `ContentLoader` 读 xlsx 并校验：必填列、id 唯一、`systemId` / `classId` 引用存在、骰子公式可解析、等级在 1-20、关键词跨文件不重名。
3. `ContentSynchronizer` 把内容整份写进 SQLite：规则系统按 id upsert，其余内容表整份替换；玩家数据不动。
4. 导出时 `ContentExporter` 按分类重新生成关键词文件，其余文件用固定的中文文件名。
5. 任何一步出错都会在启动时报出「文件名 / 工作表 / 行号」，不会带着半份内容启动。
