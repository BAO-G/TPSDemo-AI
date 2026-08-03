# 多 Agent 工作流对话流程文档

> 本文件记录"architect → coder → explore"多 Agent 协作开发 Unity TPS 游戏的完整对话流程、启动模板、阶段规划与铁律。每个 Agent 接手新对话时，必须先阅读本文件与 DEVLOG.md。

---

## 一、项目信息

- **引擎**：Unity 2022.3.62f2c1（LTS），URP 渲染管线
- **资源**：PolygonBattleRoyale 低多边形军事资源包（武器 / 载具 / 建筑 / 环境 / 特效齐全）
- **工作模式**：architect（Pro 规划）→ coder（Flash 开发）→ explore（审查）→ 用户确认
- **项目路径**：`K:\Unity\UnityProject\AI Test`
- **目标玩法**：第三人称战术射击（TPS）

---

## 二、新对话启动模板

> 每次开启新对话（或新 Agent 加入）时，先发送以下内容确认身份与项目状态：

```
@architect 请读取项目根目录的 DEVLOG.md 文件了解当前开发进度（路径：K:\Unity\UnityProject\AI Test\Assets\DEVLOG.md），再读取 WORKFLOW.md 了解工作流规范。确认身份和项目状态后，规划下一阶段。
```

---

## 三、完整对话流程（按阶段）

### 阶段 0 — 项目初始化（已完成 ✅）

- **对话 0-1：验证环境**
  - 发送：`@architect 请确认你的身份和当前项目状态...`
- **对话 0-2：执行初始化**
  - 发送：`@architect 规划阶段 0：安装 Input System 和 Cinemachine，创建目录结构...`

### 阶段 1 — 核心框架（已完成 ✅）

- **对话 1-1：规划**
  - 发送：`@architect 规划阶段 1 核心框架...`
- **对话 1-2：并行执行 A 组**
  - `@coder 实现玩家控制器`
  - `@coder 实现敌人 AI 基类`
- **对话 1-3：并行执行 B 组**
  - `@coder 实现武器系统`
  - `@coder 实现 Cinemachine 相机`
- **对话 1-4：收尾**
  - `@coder 实现 HUD`

### 阶段 2 — 战术系统（待开发）

- **对话 2-1：规划**
  - 发送：`@architect 规划阶段 2 战术系统。当前缺口：ADS瞄准（已有 Aim 右键输入但无逻辑，需要 FOV 60→40 平滑过渡、武器贴枪点、移动减速）、掩体系统（C 键低姿势+减速+减小判定）、受伤反馈（HUD 红屏闪动+治疗接口）、弹药管理（备弹池+弹匣分离+拾取物）。请拆分任务，标注依赖和可并行项。`
- **对话 2-2：并行执行**
  - `@coder 实现 ADS 瞄准系统：FOV 变化、武器位置调整、移速惩罚`
  - `@coder 实现受伤反馈与治疗：红屏闪动、治疗按键/拾取物`
  - `@coder 实现弹药管理系统：备弹池、换弹扣备弹、弹药拾取物`
  - `@coder 实现掩体系统：C 键低姿势、掩体检测`
- **对话 2-3：审查**
  - `@explore 审查阶段 2 所有脚本，检查编译错误，验证 FOV/治疗/弹药/掩体功能`

### 阶段 3 — 内容搭建（待开发）

- **对话 3-1：规划**
  - 发送：`@architect 规划阶段 3 内容搭建：用 PolygonBattleRoyale 建筑/环境资源搭建测试关卡、配置多种敌人变体、制作多把武器（步枪/冲锋枪/手枪 WeaponData 资产）、全场景烘焙 NavMesh。请输出场景布局方案和敌人/武器配置表。`
- **对话 3-2：并行执行**
  - `@coder 搭建主场景：铺建筑/掩体/地形`
  - `@coder 配置敌人：不同血量/速度的变体 + 巡逻路点`
  - `@coder 制作武器配置：突击步枪/冲锋枪/手枪 WeaponData 资产`
- **对话 3-3：审查**
  - `@explore 审查阶段 3 场景布局、敌人/武器配置与 NavMesh 烘焙结果`

### 阶段 4 — 打磨（待开发）

- **对话 4-1：规划**
  - 发送：`@architect 规划阶段 4 打磨：音效（射击/换弹/受伤 generate_audio）、动画（武器后坐动画、敌人死亡、玩家跑动）、特效（命中粒子、枪口闪光）、手感微调（灵敏度曲线）、性能优化（URP 质量档/Draw Call）。`
- **对话 4-2：按优先级执行**
  - `@coder 实现后坐力动画与射击手感微调`
  - `@coder 接入音效与特效`
  - `@coder UI 美化与性能优化`
- **对话 4-3：终审**
  - `@explore 终审全部功能，输出完整缺陷清单与优化建议`

---

## 四、常见问题速查表

| 问题 | 排查方向 |
|------|----------|
| 按键没反应 | PlayerInput defaultActionMap 是否设为 "Player"；InputActionAsset 是否绑定 |
| 敌人不动 | NavMesh 是否烘焙；敌人是否有 NavMeshAgent；Player 是否有 "Player" tag |
| UI 看不到 | 是否在 Game 视图（非 Scene 视图）；Canvas Scaler 分辨率是否匹配 |
| 编译报错 | read_console 查看具体错误；unity_reflect 验证 API |
| 资源被改动 | 是否直接改了 PolygonBattleRoyale 原文件（禁止！应复制到 Prefabs/Materials） |
| 相机抖动/穿墙 | Cinemachine 相机 Body/Aim 配置；碰撞体与层级是否设对 |
| 射击无伤害 | 射线检测层掩码是否包含敌人层；武器 Data 与敌人血量是否连线 |

---

## 五、铁律（每个 agent 必须遵守）

1. **编译检查**：修改 C# 脚本后必须 `read_console` 检查编译错误，无错误才能继续
2. **API 验证**：不确定 Unity API 时用 `unity_reflect` / `unity_docs` 验证，不凭记忆写代码
3. **资源复用**：不修改 PolygonBattleRoyale 原始文件，需要变体时复制到 `Assets/Prefabs/` / `Assets/Materials/`
4. **代码规范**：私有字段 `_驼峰`，公开字段/属性驼峰，中文注释说明关键逻辑
5. **解耦设计**：优先事件/委托解耦，避免直接互相引用
6. **进度确认**：每阶段完成必须截图（manage_camera screenshot）+ 用户确认后才能进入下一阶段
7. **场景要求**：场景内必须有 Camera 和 Directional Light；新建组件/类型前确认编译完成
8. **工作流强制**：architect（Pro 规划）→ coder（Flash 开发）→ explore（审查）→ 用户确认

---

*文档维护：随项目阶段推进，DEVLOG.md 记录实际进度，本文件保持对话模板与流程规范的最新版本。*
