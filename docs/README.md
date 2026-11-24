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

- 文本里用 `[方括号]` 包起来的部分是**关键词**，前端统一渲染为「斜体 + 粗体」。
- 解析在 `frontend/src/utils/keywords.ts`（`splitKeywords` / `stripKeywords`），渲染组件是 `frontend/src/components/KeywordText/KeywordText.vue`；管理台的职业卡预览用同样的规则。
- 覆盖范围：所有**数据驱动的文本**——车卡流程（规则书名称/描述、步骤标题/说明、职业名、生命骰、主属性、描述、特性）、卡库卡片（卡名、系统/职业/种族/职业、简介）、卡详情（卡名、元信息、简介、属性名/公式、技能名、记录备注）、我的（昵称）。纯静态 UI 文案（按钮、分区标题）不接入；`uni.showToast` / `uni.showModal` 是原生弹窗，样式无法定制。
- 约定：`[]` 为空按普通文本处理；不支持嵌套；未闭合的 `[` 原样保留。
- 数据里存的是带方括号的原文，关键词列表 / 跳转等交互后续再加。

### 管理员授予

- 本地：`appsettings.Development.json` → `Admin:DevLoginIsAdmin = true`，开发登录即管理员。
- 线上：`Admin:BootstrapOpenIds` 填 openid 白名单，该账号登录时自动提升为 `admin`。
- 角色存在 `Users.Role`，JWT 里带 `role` 声明，`/api/admin/*` 由 `Admin` 策略保护。
