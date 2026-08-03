# TPS Demo - AI

第三人称战术射击游戏（TPS）示例项目，按 AI Agent 多阶段工作流开发。

## 项目信息

- 引擎版本：Unity 2022.3.62f2c1（LTS）
- 渲染管线：URP（Universal Render Pipeline）
- 资源包：PolygonBattleRoyale 低多边形军事包（武器 / 载具 / 建筑 / 环境 / 特效）

## 当前功能

- 第三人称角色控制（移动 / 视角）
- 玩家生命值与死亡反馈
- 枪械系统（武器数据驱动，射击 / 换弹 / 命中反馈）
- 基础敌人 AI
- HUD 界面（血条 / 死亡状态）

## 目录结构

```
Assets/
├── Scripts/           # 游戏逻辑脚本
│   ├── Combat/        # 战斗系统（武器、射击）
│   ├── Player/        # 玩家控制、生命值、输入
│   ├── Enemy/         # 敌人 AI
│   ├── UI/            # 界面逻辑
│   ├── Data/          # 数据定义
│   └── Items/         # 物品系统
├── Scenes/            # 场景（SampleScene）
├── Prefabs/           # 自制预制体
├── Resources/         # 动态加载资源
├── Materials/         # 自定义材质
├── Data/              # 游戏数据资产（如 RifleData）
└── PolygonBattleRoyale/  # 第三方资源包（勿修改原文件）
```

## 运行方式

1. 使用 Unity 2022.3.62f2c1 打开项目
2. 打开 `Assets/Scenes/SampleScene.unity`
3. 点击 Play 运行

## 开发说明

- 自定义脚本按系统分目录存放，私有字段使用 `_驼峰` 命名
- 优先复用 PolygonBattleRoyale 现成资源，需要变体时复制后修改
- 组件间优先使用事件 / 委托解耦
