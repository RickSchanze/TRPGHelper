# TRPG 卡器（TRPGHelper）

一个记录"我车了哪些卡、留了哪些卡、还能怎么车新卡"的电子卡器。

- 后端：.NET 10（ASP.NET Core Minimal API + EF Core + SQLite）
- 前端：uni-app（Vue 3 + TypeScript + Vite），一套代码编译到微信小程序和 H5
- 当前是**骨架版**：登录、卡库、新建向导、状态流转（留卡）、辅助素材（属性/姓名/职业/灵感）已跑通，业务细节后续逐个补。

## 目录结构

```
TRPGHelper/
├─ src/
│  ├─ TRPGHelper.Api/             # Web API 入口（端点、DTO、鉴权、Swagger/Scalar）
│  ├─ TRPGHelper.Domain/          # 实体、骰子引擎、卡规则、属性生成
│  └─ TRPGHelper.Infrastructure/  # EF Core DbContext、迁移、JWT、微信登录客户端
├─ tests/TRPGHelper.Tests/        # 骰子 / 状态流转 / 属性生成的单元测试
├─ frontend/                      # uni-app 前端（微信小程序 + H5）
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

已经启动的两个地址：

- 后端：<http://localhost:5080>（健康检查 /health，文档 /scalar/v1）
- 前端 H5：<http://localhost:5173>

### 3. 登录

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
| `Cors:AllowedOrigins` | 允许的浏览器来源；为空表示允许任意来源 |

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
| GET | `/api/generator/names` | 随机姓名 |
| GET | `/api/generator/occupations` | 随机职业 |
| GET | `/api/generator/inspirations` | 车卡灵感 |

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
