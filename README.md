# TRPG 卡器（TRPGHelper）

一个记录"我车了哪些卡、留了哪些卡、还能怎么车新卡"的电子卡器。

仓库：<https://github.com/RickSchanze/TRPGHelper>

- 后端：.NET 10（ASP.NET Core Minimal API + EF Core + SQLite）
- 前端：uni-app（Vue 3 + TypeScript + Vite），一套代码编译到微信小程序和 H5
- 管理台：Vue 3 + Vite + Element Plus（`admin/`），维护规则系统、属性模板与生成器素材
- 当前是**骨架版**：登录、卡库、新建向导、状态流转（留卡）、辅助素材（属性/姓名/职业/灵感）已跑通，业务细节后续逐个补。
- 素材已经**全部可配置**：姓名片段、职业、背景、职业卡、种族、灵感都存在 SQLite 里，由管理台维护。

## 目录结构

```
TRPGHelper/
├─ src/
│  ├─ TRPGHelper.Api/             # Web API 入口（端点、DTO、鉴权、Swagger/Scalar）
│  ├─ TRPGHelper.Domain/          # 实体、骰子引擎、卡规则、属性生成
│  └─ TRPGHelper.Infrastructure/  # EF Core DbContext、迁移、JWT、微信登录客户端
├─ tests/TRPGHelper.Tests/        # 骰子 / 状态流转 / 属性生成 / 素材库的单元测试
├─ frontend/                      # uni-app 前端（微信小程序 + H5）
├─ admin/                         # 后台管理台（Vue 3 + Element Plus）
├─ docs/
└─ TRPGHelper.slnx
```

## 快速开始

### 1. 后端

```powershell
dotnet run --project src/TRPGHelper.Api
```

- 监听 `http://localhost:5080`
- 启动时自动建库（SQLite，位于 `src/TRPGHelper.Api/App_Data/trpghelper.db`）并写入 COC 7 版 / DND 5 版两个内置规则
- API 文档（仅 Development）：<http://localhost:5080/scalar/v1>
- 健康检查：<http://localhost:5080/health>

### 2. 前端

```powershell
cd frontend
npm install
npm run dev:mp-weixin   # 开发产物在 frontend/dist/dev/mp-weixin，用微信开发者工具导入
npm run build:mp-weixin # 正式产物在 frontend/dist/build/mp-weixin
npm run dev:h5          # 浏览器预览 http://localhost:5173
```

微信开发者工具里请勾选「不校验合法域名」（本地调试 http://localhost:5080 需要）。

H5 在桌面浏览器下也有适配：宽度 ≥ 900px 时使用居中版心（最大 1100px）、固定顶栏、卡片网格和鼠标悬停反馈，隐藏小程序式的导航条与底部 tabBar。

已经启动的地址：

- 后端：<http://localhost:5080>（健康检查 /health，文档 /scalar/v1）
- 前端 H5：<http://localhost:5173>
- 管理台：<http://localhost:5174>

### 3. 管理台（内容配置）

```powershell
cd admin
npm install
npm run dev        # http://localhost:5174，/api 代理到 5080
npm run build      # 产物在 admin/dist
```

管什么（当前阶段先只开放「概览」和「车卡流程」；素材库、规则系统页面暂时从菜单隐藏，代码保留）：

| 页面 | 能改什么 |
| --- | --- |
| 概览 | 系统 / 素材 / 用户 / 卡数量，按素材类型分布 |
| 车卡流程 | 配置车卡流程有几步、先后顺序、每步的标题与说明；**「选择职业」这一步点「编辑职业」可以逐张改职业的描述和配图，右侧实时预览前端职业卡**；右侧抽屉可以预览前端 stepper（选规则书不算步骤） |
| 关键词 | 维护 `[方括号]` 词条：关键词、标题、解释正文（可再嵌关键词）、配图（可上传）；弹窗预览与「未定义引用」提示 |
| 规则系统（暂隐藏） | 属性模板（key + 名称 + 骰子公式）；姓名模板（`{surname}{given}`、`{first}·{epithet}`） |
| 素材库（暂隐藏） | 姓名片段、职业、背景、职业卡、种族、灵感的增删改查 |

职业卡配图可以填地址（`/static/classes/ranger.jpg`、`https://...`），也可以直接在管理台上传（存到 `App_Data/uploads/`，通过 `/uploads/...` 提供）。

职业卡支持**关键词标记**：描述 / 特性 / 主属性 / 生命骰里用 `[方括号]` 包起来的部分，前端渲染成「斜体 + 粗体 + 下划线」。有词条的关键词可以点击，弹出解释（解释正文里还能继续嵌关键词，弹窗是栈式，有「关闭」和「关闭全部」）；没有词条的关键词原样显示 `[关键词]`。词条在管理台「关键词」页签维护，接口是 `GET /api/keywords`。

管理员鉴权复用现有 JWT（`User.Role`，`admin` 角色才放行 `/api/admin/*`）：

- 本地开发：`appsettings.Development.json` 里 `Admin:DevLoginIsAdmin = true`，管理台点「本地开发登录」直接进。
- 线上：在 `Admin:BootstrapOpenIds` 里填管理员的微信 openid，该账号登录后自动成为 admin。
- 其他账号仍可正常用小程序登录，只是拿不到 admin 角色。

数据落在 SQLite 的 `GameSystems` / `GeneratorEntries` / `CreationSteps` 表，首次启动会按 `GeneratorSeedData` 写入出厂素材（180 条）和默认流程（1 步「选择职业」），之后以管理台为准；改完前端下次请求即生效。

### 4. 登录

- 本地开发：登录页点「本地开发登录」（需要 `ASPNETCORE_ENVIRONMENT=Development`，即默认 `dotnet run` 的环境，`appsettings.Development.json` 里 `Auth:EnableDevLogin = true`）。
- 微信小程序：在 `src/TRPGHelper.Api/appsettings.json` 填入
  ```json
  "WeChat": { "AppId": "你的小程序 AppId", "AppSecret": "你的 AppSecret" }
  ```
  并在 `frontend/src/manifest.json` 的 `mp-weixin.appid` 填入同一个小程序 AppId。

## 配置说明

| 配置 | 说明 |
| --- | --- |
| `ConnectionStrings:Default` | SQLite 连接串，相对路径按内容根目录解析 |
| `Jwt:Key` | 至少 32 字符，生产环境务必替换 |
| `Jwt:AccessTokenMinutes` | 登录有效期，默认 7 天 |
| `WeChat:AppId` / `WeChat:AppSecret` | 微信小程序登录（code2session） |
| `Auth:EnableDevLogin` | 是否允许本地开发登录，生产环境请设为 false |
| `Admin:DevLoginIsAdmin` | 本地开发登录是否直接给 admin 角色（默认在 Development 下开启） |
| `Admin:BootstrapOpenIds` | 自动提升为管理员的微信 openid 白名单 |
| `Cors:AllowedOrigins` | 允许的浏览器来源；为空表示允许任意来源（开发环境已含 5173 / 5174） |

前端后端地址通过 `VITE_API_BASE` 覆盖，默认 `http://localhost:5080`（见 `frontend/.env.example`）。

## 已有接口

| 方法 | 路径 | 说明 |
| --- | --- | --- |
| POST | `/api/auth/wechat/login` | 微信 code2session 登录 |
| POST | `/api/auth/dev-login` | 本地开发登录 |
| GET / PUT | `/api/auth/me` | 当前用户 / 更新昵称头像 |
| GET | `/api/systems` | 规则系统与属性模板 |
| GET | `/api/characters` | 卡库列表（status / systemCode / keyword / favorite / sort / 分页） |
| GET | `/api/characters/stats` | 卡库统计（总数、留存、各状态） |
| POST | `/api/characters` | 新建卡（不传属性时按规则自动掷一组） |
| GET | `/api/characters/{id}` | 卡详情（含操作记录、可流转状态） |
| PUT | `/api/characters/{id}` | 更新资料 / 卡面 |
| DELETE | `/api/characters/{id}` | 删除卡 |
| POST | `/api/characters/{id}/status` | 状态流转：待用 / 使用中 / 留卡 / 归档 |
| POST | `/api/characters/{id}/notes` | 加一条备注 |
| POST | `/api/generator/roll` | 掷骰（`d20`、`4d6kh3`、`3d6*5`、`2d6*5+30`） |
| POST | `/api/generator/attributes` | 按规则生成属性（掷骰 / 标准数组） |
| GET | `/api/generator/classes` | 职业卡列表（数据来自素材库，含 `imageUrl`） |
| GET | `/api/generator/races` | 种族列表（数据来自素材库） |
| GET | `/api/generator/names` | 随机姓名（按系统的姓名模板拼） |
| GET | `/api/generator/occupations` | 随机职业（职业 + 背景池） |
| GET | `/api/generator/inspirations` | 车卡灵感 |
| GET | `/api/creation-flow` | 车卡流程步骤（按 `systemCode` 过滤；规则书选择不算步骤） |
| GET | `/api/keywords` | 关键词词条（启用中的，供前端点击弹窗） |

管理台接口（都需要 `admin` 角色，见 `AdminEndpoints.PolicyName`）：

| 方法 | 路径 | 说明 |
| --- | --- | --- |
| GET | `/api/admin/overview` | 概览统计（系统 / 素材 / 用户 / 卡 / 各类型素材数） |
| GET | `/api/admin/systems` | 规则系统列表（含属性模板、姓名模板） |
| POST | `/api/admin/systems` | 新建规则系统 |
| PUT | `/api/admin/systems/{id}` | 更新规则系统（改 code 会同步搬移素材） |
| DELETE | `/api/admin/systems/{id}` | 删除规则系统（有卡引用时 409） |
| GET | `/api/admin/generator` | 素材列表（`kind` / `systemCode` / `keyword` / `enabled` / 分页） |
| POST | `/api/admin/generator` | 新增素材 |
| PUT | `/api/admin/generator/{id}` | 更新素材（含启停） |
| DELETE | `/api/admin/generator/{id}` | 删除素材 |
| GET | `/api/admin/creation-steps` | 车卡流程步骤列表 |
| POST | `/api/admin/creation-steps` | 新增流程步骤 |
| PUT | `/api/admin/creation-steps/{id}` | 更新流程步骤 |
| DELETE | `/api/admin/creation-steps/{id}` | 删除流程步骤 |
| POST | `/api/admin/assets` | 上传图片（multipart，字段名 `file`，≤5 MB），返回 `/uploads/...` 地址 |
| GET | `/api/admin/keywords` | 关键词词条列表（可按关键字搜索） |
| POST | `/api/admin/keywords` | 新增关键词词条 |
| PUT | `/api/admin/keywords/{id}` | 更新关键词词条 |
| DELETE | `/api/admin/keywords/{id}` | 删除关键词词条（前端恢复成原样显示） |

素材类型（`kind`）：`namePart`、`occupation`、`background`、`class`、`race`、`inspiration`。职业卡 / 种族的扩展字段放在 `extra` 里，例如职业卡：
```json
{ "description": "荒野的猎手", "hitDie": "d10", "primaryAbility": "敏捷 / 感知", "features": ["宿敌", "战斗风格"] }
```

## 车新卡流程

目前专注 DND 5E 车卡，页面顶部只写 **WHO ARE YOU / 你是谁**（标题用宋体，其余保持默认字体），流程先做 2 步：

1. **规则书**：DND 5E / COC 7E（COC 先标「即将开放」）
2. **职业**：游侠、战士、武僧、德鲁伊、圣武士五张职业卡（大图单列 + 特点两栏），选中后「下一步」才可用

职业卡配图：`frontend/src/static/classes/ranger.jpg`、`fighter.jpg`、`monk.jpg`、`druid.jpg`、`paladin.jpg`，想换图同名覆盖，或改 `frontend/src/config/classes.ts`。

选择进度存在本机（uni storage，key `trpg_create_draft`），刷新 / 退出再进会恢复；点「完成」才会 `POST /api/characters` 提交，卡名暂由后端随机生成，等后续加上命名步骤再替换。

职业来自后端：`GET /api/generator/classes?systemCode=dnd5e`

### 规则书图标替换

开发期用的是临时素材，正式版可以随时替换：

- 直接同名覆盖：`frontend/src/static/rulebooks/dnd5e.png`、`coc7.png`
- 想换文件名或加新规则书：改 `frontend/src/config/rulebooks.ts` 里的 `icon` / `accent`

当前素材来自 [game-icons.net](https://game-icons.net)（Delapouite，CC BY 3.0），说明见 `frontend/src/static/rulebooks/README.md`。COC 的图标先留着，等 COC 车卡流程做好再接回来。

## 状态流转

```
草稿 --→ 待用 --→ 使用中
           │  ╰──────→ 留存 --→（取出）→ 待用 / 使用中
           ╰──────────→ 归档 --→ 待用 / 留存
```

- 「留卡」= 转入 `retained`，并记录 `retainedAt` 与一条操作日志；
- 每次状态变更都会写 `CardLogs`，详情页按时间展示，方便回看"这张卡什么时候留的"。

## 开发命令

```powershell
dotnet build TRPGHelper.slnx          # 构建
dotnet test TRPGHelper.slnx           # 单元测试
dotnet ef migrations add <Name> --project src/TRPGHelper.Infrastructure --startup-project src/TRPGHelper.Api --output-dir Data/Migrations
```

## 下一步（待一点一点加）

- [ ] 卡面编辑：技能、物品、背景、头像上传
- [ ] 微信小程序订阅消息 / 分享卡片
- [ ] 多规则系统自定义属性模板
- [ ] 留卡夹分类、标签、检索
- [ ] 车卡历史统计与导出
