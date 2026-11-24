# 架构说明

## 技术栈

- 后端：.NET 10 / ASP.NET Core Minimal API / EF Core + SQLite
- 玩家前端：uni-app（Vue 3 + TypeScript + Vite），H5 + 微信小程序
- 管理台：Vue 3 + Vite + Element Plus（`admin/`）
- 内容配置：仓库根目录 `data/*.xlsx`（人类可读，Excel 直接编辑）

## 目录结构

```
TRPGHelper/
├─ data/                          # 内容配置（中文命名 xlsx，人类可读）
│  ├─ 规则系统.xlsx               #   规则系统 + 属性模板（含图标 / 主色 / 是否开放）
│  ├─ 姓名片段.xlsx               #   姓名片段
│  ├─ 职业.xlsx                   #   职业卡（描述 / 配图 / 主色 / 特性）
│  ├─ 随机职业.xlsx               #   随机职业池
│  ├─ 背景.xlsx                   #   随机背景池
│  ├─ 种族.xlsx                   #   种族
│  ├─ 职业灵感.xlsx               #   车卡灵感
│  ├─ 车卡流程.xlsx               #   车卡流程步骤
│  ├─ 职业等级.xlsx               #   职业 1-20 级成长
│  ├─ 关键词.<分类>.xlsx          #   关键词词条，按分类拆文件（属性 / 语法 / 战斗 …）
│  └─ README.md
├─ docs/                          # 项目知识（本文件、内容格式、说明）
├─ src/
│  ├─ TRPGHelper.Api/             # Web API、端点、鉴权、启动初始化
│  ├─ TRPGHelper.Domain/          # 实体、骰子引擎、卡规则
│  └─ TRPGHelper.Infrastructure/  # EF Core、内容加载 / 同步 / 导出、JWT、微信登录
├─ tests/TRPGHelper.Tests/        # 单元测试（含从 data/ 加载内容的测试）
├─ frontend/                      # uni-app 玩家端
├─ admin/                         # 管理台
└─ TRPGHelper.slnx
```

## 数据分层

| 数据 | 存放 | 说明 |
| --- | --- | --- |
| 内容（规则系统、素材、流程、关键词、职业成长） | `data/*.xlsx` → SQLite | 文件是唯一事实来源，启动时同步进库；管理台改动需要导出回文件 |
| 玩家数据（用户、角色卡、卡操作记录） | SQLite `src/TRPGHelper.Api/App_Data/trpghelper.db` | 只存在数据库，不导出 |

### 启动流程

1. `DatabaseInitializer` 执行 EF 迁移（SQLite）。
2. `ContentPaths` 定位 `data/`：优先用配置 `Content:DataPath`；否则从内容根目录、程序目录逐级向上找含 `systems.xlsx` 的 `data/`。
3. `ContentLoader` 读取 xlsx 并校验（必填列、id 唯一、跨表 id 存在、骰子公式、等级范围、关键词跨文件不重名）。
4. `ContentSynchronizer` 同步进库：规则系统按 id upsert（有卡引用的系统不会被删），素材 / 流程 / 关键词 / 等级成长整份替换。
5. 规则书图标 / 主色、职业卡配图与主色也都在内容文件里（`iconUrl` / `accent` 列），前端不再硬编码。

内容格式细节见 [content-format.md](content-format.md)。

## 接口分组

| 前缀 | 说明 |
| --- | --- |
| `/api/auth/*` | 微信 / 本地开发登录，`GET/PUT /me` |
| `/api/systems`、`/api/characters/*` | 规则系统、卡库、卡详情、状态流转、备注 |
| `/api/generator/*` | 掷骰、属性生成、姓名 / 职业 / 种族 / 灵感（读内容库） |
| `/api/creation-flow` | 车卡流程步骤 |
| `/api/keywords` | 关键词词条 |
| `/api/classes/{classCode}/levels` | 职业 1-20 级成长 |
| `/api/admin/*` | 管理台：概览、规则系统、素材、流程、关键词、等级成长、图片上传、内容导出（需要 `admin` 角色） |

鉴权：JWT，`Users.Role` 为 `admin` 时才能访问 `/api/admin/*`；本地开发登录或 `Admin:BootstrapOpenIds` 白名单自动提权。

## 关键词系统

- 文本里 `[关键词]` → 前端 `KeywordText` 渲染为斜体加粗下划线，有词条时可点击。
- 点击压入弹窗栈，`KeywordModal` 按「窗口叠窗口」的方式展示，最多 10 层，偏移到上限后原地叠；「关闭」退一层，「关闭全部」清空。
- 解释正文可以再嵌 `[关键词]`，继续点会继续压栈。
- 没有词条（或词条停用）时原样显示 `[关键词]`。
- 词条在 `data/keywords.xlsx` 维护，前端启动时通过 `GET /api/keywords` 预加载。

## 车卡流程

- 选规则书（DND 5E / COC 7E）是前置条件，**不算步骤**。
- 步骤由 `data/creation-flow.xlsx` 配置（数量、顺序、类型、标题、说明）。
- 目前只有 `class`（选择职业）实现了面板；职业卡右上角「详情 ›」打开 1-20 级成长弹窗。

## 管理台

- 菜单：概览、车卡流程（含「编辑职业」「成长」）、关键词。
- 「规则系统」「素材库」页面暂时隐藏（代码保留）。
- 内容改动是写数据库的，**要持久化必须点「概览 → 导出内容到 data/」**，否则下次启动会被 xlsx 覆盖。

## 本地开发注意

- 后端 `http://localhost:5080`、前端 H5 `http://localhost:5173`、管理台 `http://localhost:5174`。
- 开发登录需要 `ASPNETCORE_ENVIRONMENT=Development` 且 `Auth:EnableDevLogin=true`；管理台登录还需要 `Admin:DevLoginIsAdmin=true`。
- 本机三个端口只监听 IPv6（`localhost` 可用，`127.0.0.1` 不通）；事件日志不可写时需要 `Logging__EventLog__LogLevel__Default=None`；esbuild 临时目录异常时可以设置 `TEMP/TMP`。
