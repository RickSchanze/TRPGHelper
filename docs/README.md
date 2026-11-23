# 文档

- 产品/接口说明见根目录 [README.md](../README.md)
- 数据库结构由 EF Core 迁移维护：`src/TRPGHelper.Infrastructure/Data/Migrations`
- 领域规则：
  - 骰子表达式解析：`src/TRPGHelper.Domain/Dice/DiceRoller.cs`
  - 卡状态流转：`src/TRPGHelper.Domain/Cards/CardRules.cs`
  - 属性生成：`src/TRPGHelper.Domain/Cards/AttributeGenerator.cs`
