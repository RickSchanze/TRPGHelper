# 文档

- 产品/接口说明见根目录 [README.md](../README.md)
- 数据库结构由 EF Core 迁移维护：`src/TRPGHelper.Infrastructure/Data/Migrations`
- 领域规则：
  - 骰子表达式解析：`src/TRPGHelper.Domain/Dice/DiceRoller.cs`
  - 卡状态流转：`src/TRPGHelper.Domain/Cards/CardRules.cs`
  - 属性生成：`src/TRPGHelper.Domain/Cards/AttributeGenerator.cs`

## 可配置内容（管理台）

内容型配置全部落在 SQLite，由 `admin/` 管理台维护，代码里不再硬编码素材。

| 表 / 代码 | 内容 |
| --- | --- |
| `GameSystems` | 规则系统、属性模板 JSON、姓名模板 `{category}` |
| `GeneratorEntries` | 生成器素材：`namePart` / `occupation` / `background` / `class` / `race` / `inspiration` |
| `CreationSteps` | 车卡流程步骤（`Kind` 决定前端渲染哪种面板；规则书选择不在其中） |
| `Keywords` | 关键词词条：点击 `[关键词]` 弹出解释，解释正文可再嵌关键词 |
| `GeneratorSeedData.cs` | 出厂素材与默认流程（首次启动写入，之后以管理台为准） |
| `GeneratorCatalogService.cs` | 公共接口读取素材、按姓名模板拼名字 |
| `AdminEndpoints.cs` | `/api/admin/*` 增删改查 + 图片上传，要求 `admin` 角色 |

### 素材字段约定

| kind | category | value | extra |
| --- | --- | --- | --- |
| `namePart` | `surname` / `given` / `first` / `epithet` | 片段本身 | 无 |
| `occupation` | 一般填 `occupation` | 职业名 | 无 |
| `background` | 一般填 `background` | 背景名 | 无 |
| `class` | 稳定 code（`ranger`） | 职业名 | `description` / `hitDie` / `primaryAbility` / `features[]` / `imageUrl` |
| `race` | 稳定 code（`elf`） | 种族名 | `description` / `abilityBonus` / `speed` |
| `inspiration` | 一般填 `hook` | 标题 | `text`（正文） |

姓名模板在规则系统上配置，用 `{category}` 引用 `namePart` 分组，例如 COC 的 `{surname}{given}`、DND 的 `{first}·{epithet}`。

### 车卡流程

- `CreationSteps` 决定前端 stepper 有几步、什么顺序；`Kind` 取值 `class` / `race` / `attributes` / `name` / `summary`。
- 选规则书（DND / COC）是流程前置条件，**不算步骤**。
- 目前只有 `class`（选择职业）实现了面板，其它类型前端会渲染占位面板，等实现后再接。
- 职业卡配图存在 `class` 素材的 `extra.imageUrl`；管理台上传的图片放在 `src/TRPGHelper.Api/App_Data/uploads/`（已在 .gitignore 里），通过 `/uploads/...` 访问。

### 关键词标记

- 文本里用 `[方括号]` 包起来的部分是**关键词**，前端统一渲染为「斜体 + 粗体 + 下划线」，有词条时可以点击。
- 解析在 `frontend/src/utils/keywords.ts`（`splitKeywords` / `stripKeywords`），渲染组件是 `frontend/src/components/KeywordText/KeywordText.vue`；管理台预览用同样的规则。
- 覆盖范围：所有**数据驱动的文本**——车卡流程（规则书名称/描述、步骤标题/说明、职业名、生命骰、主属性、描述、特性）、卡库卡片（卡名、系统/职业/种族/职业、简介）、卡详情（卡名、元信息、简介、属性名/公式、技能名、记录备注）、我的（昵称）。纯静态 UI 文案（按钮、分区标题）不接入；`uni.showToast` / `uni.showModal` 是原生弹窗，样式无法定制。
- 约定：`[]` 为空按普通文本处理；不支持嵌套；未闭合的 `[` 原样保留。

### 关键词弹窗（WordBook）

- 词条表 `Keywords`：`Key`（不含方括号，大小写不敏感唯一）、`Title`、`Content`、`ImageUrl`、`SortOrder`、`IsEnabled`。
- 公共接口 `GET /api/keywords` 返回启用中的词条，前端 `stores/keywords.ts` 启动时预加载成 map。
- 点击关键词会压入**弹窗栈**（`keywordStack`），像 Windows 窗口一层层叠上去：最新的一层在最前面，后面的层依次往左上退一点、变淡；最多同时展示 **10 层**，偏移到上限（手机约 24/56px，桌面约 70/90px）后不再移动，原地叠。
- 「关闭」播放退场动画后出栈，下一层滑到最前面；「关闭全部」清空整个栈。解释正文里可以再嵌 `[关键词]`，继续点会继续压栈。
- **没有词条的关键词原样显示 `[关键词]`**（不加下划线、不可点击）；词条接口失败时全部按原样显示。
- 管理台「关键词」页签：增删改查，支持配图上传；正文里引用到不存在的词条时会提示。
- 出厂词条：DND 六项属性 + 「属性检定」，互相引用，用来演示弹窗栈。

### 管理员授予

- 本地：`appsettings.Development.json` → `Admin:DevLoginIsAdmin = true`，开发登录即管理员。
- 线上：`Admin:BootstrapOpenIds` 填 openid 白名单，该账号登录时自动提升为 `admin`。
- 角色存在 `Users.Role`，JWT 里带 `role` 声明，`/api/admin/*` 由 `Admin` 策略保护。
