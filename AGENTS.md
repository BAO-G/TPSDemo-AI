# AI Test 项目规则（TPS 战术射击游戏）

## 项目概况

- 引擎：Unity 2022.3.62f2c1（LTS），URP 渲染管线
- 资源：PolygonBattleRoyale 低多边形军事包（武器/载具/建筑/环境/特效齐全）
- 目标玩法：第三人称战术射击（TPS），按文档《OpenCode 多 Agent 工作流与 Unity RPG 开发指南》流程开发
- 工作模式：architect（Pro 规划）→ coder（Flash 开发）→ explore（审查）→ 用户确认

## 强制工作流（每个 agent 必须遵守）

1. 修改任何 C# 脚本后，必须 read_console 检查编译错误，无错误才能继续
2. 创建/修改脚本、预制体、场景后，需要刷新时用 refresh_unity
3. 场景内必须有 Camera 和 Directional Light
4. 新组件/类型使用前确认编译完成（editor_state 的 isCompiling）
5. 涉及 Unity API 不确定时，用 unity_reflect / unity_docs 验证
6. 每阶段完成要截图（manage_camera screenshot）给用户确认

## 目录结构约定

- Assets/Scripts/{Player,Combat,Items,Quest,UI,Enemy,Data} 按系统分目录
- Assets/Scenes/ 场景文件
- Assets/Prefabs/ 自制预制体（不要和 PolygonBattleRoyale 自带的混放）
- Assets/Resources/ 动态加载资源

## 资源复用规则

- 优先使用 PolygonBattleRoyale/Models 与 Prefabs 下的现成资源
- 自定义材质放 Assets/Materials/
- 不要修改资源包原始文件，需要变体时复制后修改

## 代码规范

- 私有字段 `_驼峰`，公开字段/属性驼峰
- 中文注释说明关键逻辑，不写废话注释
- 优先事件/委托解耦，避免直接互相引用

## 角色权限边界（铁律，不可越权）

| 角色 | 职责 | 允许操作 | 禁止操作 |
|------|------|----------|----------|
| **architect** (Pro) | 读取现状 → 分析 → 输出方案 | read、grep、glob、explore、unity_reflect | ❌ 创建/修改脚本、场景、预制体、资产 |
| **coder** (Flash) | 接收方案 → 实现代码 | create_script、apply_text_edits、manage_scene、manage_gameobject | ❌ 自行规划、跳过依赖、修改方案 |
| **explore** | 审查 → 报告 | read、read_console、grep、unity_reflect | ❌ 修改任何文件 |

> **architect 的任务以"输出方案文档"为终点**，不是以"完成代码"为终点。每次 @architect 时，architect 必须先声明自己的角色边界再开始工作。
