# 文档

项目知识都在这个目录里：

| 文档 | 内容 |
| --- | --- |
| [architecture.md](architecture.md) | 技术栈、目录结构、内容 / 玩家数据分层、启动流程、接口分组、关键词与车卡流程、本地开发注意 |
| [content-format.md](content-format.md) | `data/*.xlsx` 每个文件 / 工作表的列定义、id 引用规则、取值约定 |
| [translation.md](translation.md) | DND 5E 译名与取材约定（以 5E 不全书为准，含易错词表） |
| [character-creation-gaps.md](character-creation-gaps.md) | 对照 5E 不全书：车卡还缺什么（背景 / 技能 / 语言 / 装备 / 法术 / 卡面，按优先级与迭代拆分） |
| [race-content.md](race-content.md) | 种族内容生成脚本 `tools/build_race_content.py`：产出、用法、内容来源与约定 |
| [../data/README.md](../data/README.md) | 内容目录速查（怎么改、怎么导出） |

## 代码位置速查

| 关注点 | 代码 |
| --- | --- |
| 内容加载 / 同步 / 导出 | `src/TRPGHelper.Infrastructure/Content/` |
| 内容导出接口 | `src/TRPGHelper.Api/Endpoints/AdminEndpoints.cs`（`POST /api/admin/content/export`） |
| 骰子表达式 | `src/TRPGHelper.Domain/Dice/DiceRoller.cs` |
| 卡状态流转 | `src/TRPGHelper.Domain/Cards/CardRules.cs` |
| 属性生成 | `src/TRPGHelper.Domain/Cards/AttributeGenerator.cs` |
| 管理台页面 | `admin/src/views/` |
| 关键词组件 | `frontend/src/components/KeywordText`、`KeywordModal` |
| 内容生成脚本 | `tools/build_race_content.py`（种族词条与 traits） |
| 数据库迁移 | `src/TRPGHelper.Infrastructure/Data/Migrations` |

产品向的介绍和启动步骤见根目录 [README.md](../README.md)。
