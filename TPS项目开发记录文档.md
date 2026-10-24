# TPS 项目学习文档 —— 从零复现一款波次生存战术射击游戏

> **这是一份什么文档**
> 本文档基于本仓库（`k:\Unity\UnityProject\AI Test`）的**真实代码与资产**撰写，目标是带领读者像一位导师带学徒一样，
> **从新建空工程开始，一步步复现这个项目**：每一步先做什么、为什么这么做、怎么做。
> 文中所有代码片段均摘自项目源码，标注了所在文件与方法；所有"项目事实"（版本号、包依赖、目录、脚本职责、开发阶段）
> 都有仓库内证据支撑，证据来源以 `（证据：…）` 形式注明。
>
> **阅读约定**
> - `Assets/Scripts/...` 这类反引号路径 = 仓库内真实文件，可对照源码阅读；
> - 术语首次出现时给出英文对照；
> - 代码块标注"全文"的与源文件逐行一致；标注"节选"的结构与语句真实，
>   但行尾以 `//` 追加的**解说性注释为本文档所加**，源文件原有的中文注释一律保留原文；
> - 本项目由"AI 多 Agent 工作流"开发（详见 1.8 节），代码注释中保留了大量阶段编号（阶段 1～5）、
>   任务编号（T1-1、S3、4E…）与决策编号（D1～D7），本文档会沿用这套编号帮你建立"开发时间线"的心智地图。

**全书目录与写作进度**

| 章节 | 主题 | 状态 |
|------|------|------|
| 第 0 章 | 项目全景 | ✅ 已完成 |
| 第 1 章 | 工程搭建 | ✅ 已完成 |
| 第 2 章 | 数据与事件骨架（GameEvents / GameSettings / ObjectPool / GameManager） | ✅ 已完成 |
| 第 3 章 | 玩家系统（输入 → 控制 → 相机 → ADS → 掩体 → IK） | ✅ 已完成 |
| 第 4 章 | 战斗系统（WeaponData → FirearmWeapon → WeaponManager → 弹道特效 → 手雷 → 拾取物） | ✅ 已完成 |
| 第 5 章 | 敌人与波次（EnemyBase → EnemyPart → EnemyWaveGenerator / WaveConfig） | ✅ 已完成 |
| 第 6 章 | UI 系统（HUD → 准星/命中反馈 → 主菜单 → 暂停/结算 → 波次横幅） | ✅ 已完成 |
| 第 7 章 | 音频与场景串联 | ✅ 已完成 |
| 第 8 章 | 当前进度盘点与遗留问题 | ✅ 已完成 |

---

# 第 0 章 · 项目全景

## 【本章目标】

读完本章你将能回答：

1. 这个游戏**玩起来是什么样**：完整的一局从主菜单到结算经历了什么；
2. **技术栈为什么这么选**：Unity 2022.3 LTS、URP、现成美术资源包各自解决了什么问题；
3. **代码全景**：36 个脚本分成 6 个系统目录，谁依赖谁，消息怎么流动；
4. **开发时间线**：这个项目是按什么顺序、分哪几个阶段做出来的。

本章不要求你理解任何具体实现——那是第 2 章之后的事。本章的目标是让你在动工前"看过地图"。

## 【前置知识】

- **Unity Hub**：Unity 官方的编辑器/项目版本管理器，装哪个版本的 Unity、建项目都从它开始。
- **LTS（Long Term Support，长期支持版）**：Unity 官方承诺只修 Bug、不加新特性的稳定版本线，适合做正经项目。
- **渲染管线（Render Pipeline）**：Unity 把"模型怎么变成屏幕上的像素"这套流程做成了可替换的组件。内置管线（Built-in）是老默认；**URP（Universal Render Pipeline，通用渲染管线）**是轻量现代管线；HDRP 面向高端画质。选哪条管线决定了材质 Shader、后处理、性能开销的整个生态。
- **ScriptableObject（SO，可脚本化对象）**：一种"数据资产文件"，把数值配置从代码里抽出来变成 `.asset` 文件，策划在 Inspector 里改数值不用动代码。
- **事件 / 委托（Event / Delegate）**：C# 的发布-订阅机制。发布者只管喊一声"发生了 X"，不关心谁在听；订阅者各自响应。本项目用它做系统解耦。
- **对象池（Object Pool）**：高频生成/销毁的对象（子弹特效、手雷）反复 `Instantiate/Destroy` 会产生 GC 与卡顿，池化 = 用完回收、下次复用。
- **Prefab（预制体）**：可复用的"对象模板"，场景里放一份、代码里无限克隆。

## 0.1 这是一个什么游戏

**一句话**：低多边形画风的第三人称波次生存射击游戏（Wave Survival TPS）——你操纵一名持枪士兵，
在一块军事基地地图上对抗一波波来袭的敌人，撑过全部波次即胜利，被击倒则失败结算。

### 0.1.1 一局游戏的完整流程

以下流程不是想象，而是由 [MainMenu.unity](Assets/Scenes/MainMenu.unity) 与 [SampleScene.unity](Assets/Scenes/SampleScene.unity)
两个场景的真实对象、以及 [GameManager.cs](Assets/Scripts/Data/GameManager.cs) / [EnemyWaveGenerator.cs](Assets/Scripts/Enemy/EnemyWaveGenerator.cs)
的状态机还原出来的：

```text
┌─────────────────── MainMenu 场景 ───────────────────┐
│  标题 + 开始/设置/退出 按钮（DOTween 淡入入场）        │
│  设置面板：灵敏度 / 主音量 / 音效音量 / FOV / 自动射击   │
│  （PlayerPrefs 持久化，进游戏后由 GameSettings 应用）   │
└──────────────────────┬──────────────────────────────┘
                       │ 点击"开始" → Loading 读条 → 切场景
┌─────────────────── SampleScene 场景 ────────────────┐
│ ① Preparing：首波前 10 秒倒计时（HUD「第 N 波来袭」）  │
│ ② Spawning：按 WaveConfig 混编刷怪（首波 4/2/2）      │
│    出生点随机取"距玩家超安全距离"的刷怪点，防贴脸       │
│ ③ Fighting：战斗。三武器 + 手雷 + 拾取物 + ADS/掩体     │
│ ④ 波内全灭 → Intermission 5 秒倒计时 → 下一波         │
│ ⑤ 最后一波清空 → Victory 结算                         │
│   或：玩家血量归零 → 死亡动画 → 延迟 1.5 秒 → GameOver  │
│ ⑥ 结算面板：击杀数 / 波次 N/Max / 存活时间              │
│    → 重玩（重载场景）或 回主菜单                       │
└─────────────────────────────────────────────────────┘
```

为什么"死亡后要延迟 1.5 秒才弹结算"？[GameManager.cs](Assets/Scripts/Data/GameManager.cs) 的类注释写得很明白：

```csharp
// 摘自 GameManager.cs 类注释（Assets/Scripts/Data/GameManager.cs）
/// 死亡/胜利不置 timeScale=0（世界保持活动：Died 动画、敌人走动正常播放），
/// 只禁用玩家操控脚本 + 解锁鼠标
```

这是本项目一个有教学价值的设计判断：暂停菜单用 `Time.timeScale = 0` 冻结世界，
而**死亡/胜利刻意不冻结**——让死亡动画和"敌人补枪"的画面自然播完，再用协程延迟弹结算面板。
同一个类里管理两种"停止方式"，这是初学者最容易混淆的点，第 2 章会展开。

### 0.1.2 玩家手里的家伙

| 系统 | 内容 | 代码/资产证据 |
|------|------|---------------|
| 武器 | 手枪 / 步枪 / SMG 三把，数据驱动（伤害、射速、弹匣、备弹、散布、后坐力、ADS 倍率都是 SO 字段） | `Assets/Data/PistolData.asset` `RifleData.asset` `SMGData.asset`；[WeaponData.cs](Assets/Scripts/Combat/WeaponData.cs) |
| 手雷 | G 键投掷，抛物线 + 弹跳 + 引信爆炸，范围伤害按距离线性衰减；初始 3 枚、上限 3 枚 | [GrenadeThrower.cs](Assets/Scripts/Player/GrenadeThrower.cs)、[Grenade.cs](Assets/Scripts/Combat/Grenade.cs) |
| 战术动作 | 右键 ADS 机瞄（FOV 缩小 + 贴枪 + 移动减速）、C 键蹲伏（身高减半）、爆头判定 | [PlayerADSController.cs](Assets/Scripts/Player/PlayerADSController.cs)、[PlayerCoverController.cs](Assets/Scripts/Player/PlayerCoverController.cs)、[EnemyPart.cs](Assets/Scripts/Enemy/EnemyPart.cs) |
| 拾取物 | 弹药包（补备弹）、医疗包（消耗品式治疗）、手雷补给 | [AmmoPickup.cs](Assets/Scripts/Combat/AmmoPickup.cs)、[HealthPickup.cs](Assets/Scripts/Combat/HealthPickup.cs)、[GrenadePickup.cs](Assets/Scripts/Combat/GrenadePickup.cs) |
| 敌人 | 侦察兵 / 士兵 / 重装兵三种预制体，NavMesh 寻路 + 巡逻/追击/攻击 | `Assets/Prefabs/Enemy_Scout.prefab` `Enemy_Soldier.prefab` `Enemy_Heavy.prefab`；[EnemyBase.cs](Assets/Scripts/Enemy/EnemyBase.cs) |
| 打击感 | 曳光弹、命中血雾/烟尘、弹痕、HitMarker、爆头提示、红屏受击、爆炸震屏、武器摆动、头部起伏 | [CombatEffectsController.cs](Assets/Scripts/Combat/CombatEffectsController.cs)、[HitFeedbackUI.cs](Assets/Scripts/UI/HitFeedbackUI.cs)、[CameraShaker.cs](Assets/Scripts/Player/CameraShaker.cs)、[WeaponSway.cs](Assets/Scripts/Player/WeaponSway.cs)、[HeadBob.cs](Assets/Scripts/Player/HeadBob.cs) |

### 0.1.3 当前未完成的部分

- **4B 音效阶段长期挂起**：音频代码已全部就绪（[AudioManager.cs](Assets/Scripts/Audio/AudioManager.cs) 完整实现了
  2D UI 音源 + 3D 音源池 ×8、按武器名匹配枪声、脚步随机 pitch），但音效 `.wav/.mp3` 资产因 **API Key 未配置**
  无法生成，Inspector 里的 AudioClip 槽位为空——代码对此的处理是"资产为空时静默跳过"（AudioManager 类注释原话），
  所以游戏现在**全程无声但不会报错**。详见第 8 章。
- README 里提到的 `Scripts/Items/` 目录实际不存在（历史遗留描述），物品逻辑实际落在 `Combat/` 目录的三个 Pickup 脚本中。

## 0.2 技术选型与理由

"为什么这么选"比"选了什么"更重要。下表每一项都能在仓库里找到证据。

| 选型 | 证据 | 为什么 |
|------|------|--------|
| **Unity 2022.3.62f2c1（LTS）** | [ProjectVersion.txt](ProjectSettings/ProjectVersion.txt) | LTS 版本线只修缺陷不做破坏性变更，教程/资源包生态最厚；TPS 需要的 NavMesh、动画、物理在 2022.3 上全部成熟稳定 |
| **URP 14.0.12** | [manifest.json](Packages/manifest.json) `com.unity.render-pipelines.universal` | 低多边形（Low-poly）画风不需要 HDRP 的电影级光照，URP 在中低端机器上帧率稳、后处理（受击红晕 Vignette、bloom）开箱即用，且是移动端唯一现实选项——给"玩法原型"留了发布空间 |
| **PolygonBattleRoyale 资源包** | `Assets/PolygonBattleRoyale/`（Materials/Models/Prefabs/Scenes/Shader/Textures/URP 七个目录） | 学习目标Unity**工程能力**而不是建模/贴图。Synty Polygon 系列全包风格统一、面数低、含武器/建筑/载具/特效，还能直接当"场景积木"快速搭建可玩关卡 |
| **Input System 1.14.2（新输入系统）** | manifest.json；配置文件 `Assets/Input/PlayerInput.inputactions` | 新一代输入后端：动作映射（Action Map）与设备解耦，改键不改代码；直接支持手柄/键鼠切换。老 `Input.GetKey` 是全局轮询，项目越大越难维护 |
| **Cinemachine 3.1.7** | manifest.json；[CameraShaker.cs](Assets/Scripts/Player/CameraShaker.cs) 引用 `Unity.Cinemachine` 命名空间 | TPS 相机的跟随、碰撞、噪音震动（Perlin Noise）全是 Cinemachine 内置能力，自己写碰撞避障相机费时且难调 |
| **AI Navigation 1.1.7（NavMesh）** | manifest.json；[EnemyBase.cs](Assets/Scripts/Enemy/EnemyBase.cs) 类注释"需 NavMeshAgent 组件、场景需烘焙 NavMesh" | 波次生存的敌人要绕过建筑追玩家。寻路是"看起来智能"的最低成本方案 |
| **TextMeshPro 3.0.7** | manifest.json | ugui 的 `Text` 渲染模糊且耗；TMP 是事实上的 UI 文本标准（项目 HUD 同时用到两者，见第 6 章） |
| **DOTween** | `Assets/Plugins/Demigiant/DOTween/`、`Assets/Resources/DOTweenSettings.asset` | UI 淡入淡出/弹性缩放（主菜单按钮悬停放大、波次横幅 OutBack 弹入）用补间库写一行顶手写协程十行；主菜单/结算/横幅全靠它 |
| **lilToon 2.3.3（本地包）** | manifest.json `file:Packages/jp.lilxyzw.liltoon-2.3.3` | 为玩家角色模型 P08_Federica 提供卡通向着色器支持（该模型自带 lilToon 材质） |
| **MCP For Unity** | manifest.json `com.coplaydev.unity-mcp`（GitHub 引用） | AI 多 Agent 工作流的基础设施：让 AI 能读控制台、创建脚本、操作场景。纯人工复现本项目可跳过（1.8 节） |

**一个值得记住的选型方法论**：这个项目的技术栈没有一样是"炫技"的。URP、LTS、现成美术、成熟中间件，
每一项都在降低"把游戏做完"的风险。独立复现时，请抵抗住换最新版本/自研相机的诱惑——
本项目能走到今天，恰恰是因为把精力全花在了**玩法逻辑与系统架构**上。

## 0.3 目录结构总览

### 0.3.1 Assets 顶层（证据：目录实测）

```text
Assets/
├── Animations/           # 角色动画 FBX（多来源：POLYGON 动画包、Mixamo 等，rifle run/strafe/crouch 系列等）
├── AnimControllers/      # 动画状态机：PlayerAnimator / EnemyAnimator / CharacterAnimator.controller + UpperBodyMask.mask
├── Data/                  # ScriptableObject 数据资产：PistolData/RifleData/SMGData.asset、WaveConfig.asset、ExplosionShake.asset
├── Input/                 # PlayerInput.inputactions（Input System 动作映射）
├── Materials/             # 自制材质（弹痕/曳光/手雷轨迹材质、地面、玩家材质等）——不放 PolygonBattleRoyale 里
├── P08_Federica/          # 玩家角色模型资源（Model_Data），配 lilToon 着色
├── Plugins/Demigiant/     # DOTween 插件
├── PolygonBattleRoyale/   # 第三方美术包（只读！规则见 AGENTS.md）
├── Prefabs/               # 自制预制体 14 个（Player/敌人×3/武器×3/手雷/拾取物×3/特效×2/DemoScene_Content）
├── Resources/             # 动态加载资源（目前仅 DOTweenSettings.asset）
├── Scenes/                # MainMenu.unity + SampleScene.unity
├── Screenshots/           # 多 Agent 工作流的阶段验证截图（约 110 张，0.5 节的时间线证据）
├── Scripts/               # 全部游戏逻辑，按系统分 6 个子目录（见 0.3.2）
├── Settings/              # URP 管线资产三档（Performant/Balanced/HighFidelity）+ SampleSceneProfile（后处理 Volume 配置）
├── Textures/              # 贴图
├── UI/                    # UI 贴图（准星 crosshair.png、按钮常态/悬停贴图、菜单/面板底图）
└── TutorialInfo/          # URP 模板自带的新手资料（保留未删，无副作用）
```

两条**仓库内明文规则**（[AGENTS.md](AGENTS.md)）决定了这个目录形态：

1. 自制预制体放 `Assets/Prefabs/`，**不与 PolygonBattleRoyale 自带的混放**；
2. **不修改资源包原始文件**，需要变体时复制后修改。

这两条规则的价值在第 1 章会展开——它们是多人（含 AI）协作时防止"资源包升级即毁灭"和"谁也找不到东西"的护栏。

### 0.3.2 脚本清单（36 个，按系统分组，职责摘自各文件真实类注释）

**Data/（5 个）—— 全项目的地基，最先开发（第 2 章）**

| 脚本 | 职责（源自类注释） |
|------|--------------------|
| [GameEvents.cs](Assets/Scripts/Data/GameEvents.cs) | 全局静态事件总线（决策 D2）：全游戏唯一跨系统消息通道；事件只增不改签名 |
| [GameSettings.cs](Assets/Scripts/Data/GameSettings.cs) | 统一读取主菜单保存的设置（PlayerPrefs），游戏场景 Awake 时 `ApplyAll()` 应用 |
| [ObjectPool.cs](Assets/Scripts/Data/ObjectPool.cs) | 通用对象池（静态类）：键=预制体，栈式回收 + 可选预热，供特效/弹痕/曳光/手雷复用 |
| [GameManager.cs](Assets/Scripts/Data/GameManager.cs) | 游戏流程状态机（阶段 5）：Playing/Paused/GameOver/Victory、击杀计数、存活时间、暂停（timeScale）与结算（不冻结） |
| [WaveConfig.cs](Assets/Scripts/Data/WaveConfig.cs) | 波次配置 SO（阶段 5）：混兵种条目数组（D6：首波 4/2/2 三兵种混编） |

**Player/（11 个）—— 第 3 章**

| 脚本 | 职责 |
|------|------|
| [PlayerInputHandler.cs](Assets/Scripts/Player/PlayerInputHandler.cs) | 封装 InputAction，为其他脚本提供统一输入接口 |
| [PlayerController.cs](Assets/Scripts/Player/PlayerController.cs) | CharacterController 移动 + 跳跃 + 重力 + 鼠标视角；阶段 2 整合 ADS/掩体减速 |
| [PlayerHealth.cs](Assets/Scripts/Player/PlayerHealth.cs) | 扣血/回血/医疗包，事件通知其他系统 |
| [PlayerADSController.cs](Assets/Scripts/Player/PlayerADSController.cs) | 右键瞄准：FOV 变化、武器贴枪、移动减速 |
| [PlayerCoverController.cs](Assets/Scripts/Player/PlayerCoverController.cs) | C 键蹲伏：高度减半、移动减速、判定缩小 |
| [PlayerIKController.cs](Assets/Scripts/Player/PlayerIKController.cs) | 右手跟随武器骨骼，左手 IK 吸附护木；换弹时左手 IK 权重归零 |
| [GrenadeThrower.cs](Assets/Scripts/Player/GrenadeThrower.cs) | G 键投雷：播 toss 动画 → 延迟出手 → 赋初速；数量变化广播事件（4E） |
| [HeadBob.cs](Assets/Scripts/Player/HeadBob.cs) | 移动时相机跟随目标正弦起伏，只改 localPosition 不与 Cinemachine 打架（4F） |
| [WeaponSway.cs](Assets/Scripts/Player/WeaponSway.cs) | 按鼠标输入对武器挂点附加偏移（SmoothDamp 回位），与 ADS"基准+附加"叠加互不覆盖（4F） |
| [CameraShaker.cs](Assets/Scripts/Player/CameraShaker.cs) | 驱动 Cinemachine Perlin 噪声震屏，幅度指数衰减钳零（4E） |
| [FootstepReceiver.cs](Assets/Scripts/Player/FootstepReceiver.cs) | 动画事件接收器，消除"事件无接收者"报错 |

**Combat/（9 个）—— 第 4 章**

| 脚本 | 职责 |
|------|------|
| [WeaponData.cs](Assets/Scripts/Combat/WeaponData.cs) | 武器静态数据 SO：伤害、射速、弹匣、换弹、散布、ADS 倍率、备弹、模型引用 |
| [FirearmWeapon.cs](Assets/Scripts/Combat/FirearmWeapon.cs) | 挂武器物体上：射击、换弹、弹药管理；阶段 2 新增备弹池、ADS 散布倍率 |
| [WeaponManager.cs](Assets/Scripts/Combat/WeaponManager.cs) | 玩家武器管理：持有当前武器，把输入转发给武器 |
| [BulletTrail.cs](Assets/Scripts/Combat/BulletTrail.cs) | 曳光视觉（4C，决策 D1）：LineRenderer 两点连线，存活后自动回池 |
| [CombatEffectsController.cs](Assets/Scripts/Combat/CombatEffectsController.cs) | 战斗表现（4C/4D）：监听表现类事件，从对象池生成曳光/血特效/烟尘/弹痕 |
| [Grenade.cs](Assets/Scripts/Combat/Grenade.cs) | 手雷（4E）：Rigidbody 抛物线 + 弹跳 + 引信计时 → 范围伤害；逻辑与表现分离，走池化 |
| [GrenadePickup.cs](Assets/Scripts/Combat/GrenadePickup.cs) | 手雷补给（阶段 5，D7）：复制 HealthPickup 模式，上限 3 钳制在 GrenadeThrower |
| [AmmoPickup.cs](Assets/Scripts/Combat/AmmoPickup.cs) | 弹药拾取：靠近自动拾取，补充备弹池 |
| [HealthPickup.cs](Assets/Scripts/Combat/HealthPickup.cs) | 医疗包拾取：靠近自动拾取，增加医疗包数量 |

**Enemy/（3 个）—— 第 5 章**

| 脚本 | 职责 |
|------|------|
| [EnemyBase.cs](Assets/Scripts/Enemy/EnemyBase.cs) | 敌人基类：血量、NavMesh 移动、巡逻与追击攻击 |
| [EnemyPart.cs](Assets/Scripts/Enemy/EnemyPart.cs) | 部位标记（4C）：挂在头/身体碰撞体上，命中时区分爆头/身体倍率 |
| [EnemyWaveGenerator.cs](Assets/Scripts/Enemy/EnemyWaveGenerator.cs) | 波次生成器（阶段 5）：Preparing(10s)→Spawning→Fighting→Intermission(5s)→下一波/Victory；出生点随机取距玩家超最小距离的点（无满足则最远）；存活判定用 List 登记 + IsDead 轮询 |

**UI/（7 个）—— 第 6 章**

| 脚本 | 职责 |
|------|------|
| [UIManager.cs](Assets/Scripts/UI/UIManager.cs) | HUD：血条、弹匣/备弹、受伤红屏、治疗按键、医疗包数；阶段 2 新增红屏与备弹显示 |
| [CrosshairController.cs](Assets/Scripts/UI/CrosshairController.cs) | 准星：屏幕中心射线，命中敌人（EnemyBase）变红，否则白色 |
| [HitFeedbackUI.cs](Assets/Scripts/UI/HitFeedbackUI.cs) | 命中反馈（4D）：HitMarker 闪现 0.15s + HEADSHOT 爆头提示 |
| [WaveBannerController.cs](Assets/Scripts/UI/WaveBannerController.cs) | 波次横幅 + 来袭倒计时（阶段 5、S5b）；所有 Tween 存字段引用、触发前 Kill 防叠加（4D 热修复教训） |
| [MainMenuController.cs](Assets/Scripts/UI/MainMenuController.cs) | 主菜单：面板切换、设置持久化（PlayerPrefs）、Loading 读条、DOTween 动画 |
| [PauseMenuController.cs](Assets/Scripts/UI/PauseMenuController.cs) | 暂停菜单（纯 UI，阶段 5 起 timeScale/脚本禁用/场景加载职责已迁移 GameManager） |
| [GameOverPanelController.cs](Assets/Scripts/UI/GameOverPanelController.cs) | 结算面板（GameOver/Victory 共用）：暗底 + 标题 + 数据三行 + 重玩/回主菜单 |

**Audio/（1 个）—— 第 7 章**

| 脚本 | 职责 |
|------|------|
| [AudioManager.cs](Assets/Scripts/Audio/AudioManager.cs) | 音频管理单例：事件驱动播音只听事件不主动查询；2D UI 音源 ×1 + 3D 世界音源池 ×8；4B 资产为空时静默跳过 |

### 0.3.3 系统依赖图：谁在跟谁说话

本项目最重要的架构事实：**组件之间几乎不直接互相引用，跨系统消息全部走 GameEvents 事件总线**（AGENTS.md 代码规范原文："优先事件/委托解耦，避免直接互相引用"）。

先看代码风格。事件总线的样子（[GameEvents.cs](Assets/Scripts/Data/GameEvents.cs)，静态类）：

```csharp
// 摘自 GameEvents.cs（Assets/Scripts/Data/GameEvents.cs）—— 事件定义样式，共 21 个事件
public static event Action OnPlayerDamaged;                 // 玩家受击
public static event Action<Vector3> OnEnemyKilled;          // 敌人死亡（波次/计分订阅）
public static event Action<Vector3, Vector3> OnBulletTrail; // 曳光（起点, 终点）
public static event Action<int> OnWaveStarted;              // 波次开始（波号）

// 对应的 Raise 方法（内部 null 检查，命名 Raise{事件名去掉 On}）
public static void RaiseEnemyKilled(Vector3 pos) => OnEnemyKilled?.Invoke(pos);
```

而订阅方的固定姿势（[GameManager.cs](Assets/Scripts/Data/GameManager.cs) 的 `OnEnable` / `OnDisable`）：

```csharp
// 摘自 GameManager.cs 的 OnEnable/OnDisable
private void OnEnable()
{
    GameEvents.OnPlayerDeath += TriggerGameOver;    // 玩家死亡 → 结算流程
    GameEvents.OnEnemyKilled += HandleEnemyKilled;  // 敌人被击杀 → 击杀计数
}

private void OnDisable()
{
    GameEvents.OnPlayerDeath -= TriggerGameOver;    // 订阅方必须在 OnDisable/OnDestroy 退订
    GameEvents.OnEnemyKilled -= HandleEnemyKilled;
}
```

由此画出全项目的消息流向图（生产者 → 事件 → 消费者，全部可从代码 `Raise*` 调用点与 `+=` 订阅点验证）：

```text
 ── 生产者 ────────────────── 事件（GameEvents）────────── 消费者 ──────────────────
  PlayerHealth ──────→ OnPlayerDamaged / OnPlayerDeath ──→ UIManager(红屏) / GameManager(结算)
  FirearmWeapon ─────→ OnPlayerShot / OnReloadStart/End ─→ AudioManager(枪声/换弹)
       │            → OnHitConfirmed / OnSurfaceHit ────→ HitFeedbackUI(HitMarker)
       │                                               └→ CombatEffectsController(血/尘/弹痕)
       │            → OnBulletTrail ───────────────────→ CombatEffectsController(曳光, 经 ObjectPool)
  EnemyPart/EnemyBase→ OnEnemyDamaged / OnEnemyKilled / ─→ AudioManager / GameManager(计数)
                       OnHeadshotKilled / OnEnemyShot     / EnemyWaveGenerator(存活判定)
  Grenade ───────────→ OnExplosion ─────────────────────→ CombatEffectsController(爆炸特效+震屏)
  GrenadeThrower ────→ OnGrenadeCountChanged ──────────→ UIManager(手雷 HUD)
  EnemyWaveGenerator ─→ OnWaveStarted / OnWaveCleared ──→ WaveBannerController(横幅/倒计时)
  GameManager ────────→ OnGameOver / OnGameVictory / ────→ GameOverPanelController(结算面板)
                       OnKillCountChanged                / UIManager(击杀 HUD)
  Pickup 三兄弟 ──────→ OnPickupTaken ──────────────────→ AudioManager(拾取音, 4B 预留)
  结算面板按钮 ───────→ OnUIClick ─────────────────────→ AudioManager(UI 点击音, 4B 预留)

 ── 不走事件、直接引用的两类合法通道 ─────────────────────────────────────────────
  ① 同一物体内部的组件装配：PlayerController.GetComponent<PlayerInputHandler>() 等同物体组件
     （同一"玩家"物体上的兄弟组件互查，属于"组合"而非"耦合"）
  ② 数据通道：WeaponData/WaveConfig(SO) 在 Inspector 拖入；GameSettings(PlayerPrefs 静态读取)
     ObjectPool 是静态工具类：谁需要池化谁调用
```

记住这张图，后面每一章其实都在讲它的一个局部。**为什么用事件总线而不是互相引用？**
在 0.5 节的时间线里你会看到答案：UI 是很晚才定型、AudioManager 是事件就绪后"插入即用"的——
如果 PlayerHealth 直接引用 UIManager，每加一个"受击要反应的系统"就得改一次 PlayerHealth；
走事件的话，PlayerHealth 一行都不用动，新系统自己 `+=` 订阅即可。这就是"解耦"在开发节奏上的真实收益。

## 0.4 开发时间线还原

本项目由多 Agent 工作流分阶段推进（第 1.8 节详述工作流本身）。阶段划分不是猜的，
证据有三处：① 代码注释里的显式标记（"阶段2新增""4C""S3/S5"）；② 事件总线的演进注释
（早期事件 → "阶段5 新增"整批流程事件）；③ `Assets/Screenshots/` 里约 110 张按任务编号命名的验证截图。

| 阶段 | 内容 | 关键证据 |
|------|------|----------|
| **阶段 0：工程搭建** | 模板建项目（URP）、导入美术包、规划目录、写入 AGENTS.md 规则 | `Assets/Settings/` 三档 URP 资产为模板产物；AGENTS.md 全文 |
| **阶段 1：玩家基础** | 移动/跳跃（T1-1）、TPS 相机（T1-2）、初始 HUD（T1-5） | 截图 `T1-1_PlayerController_*.png` `T1-2_TPS_Camera_*.png` `T1-5_HUD_Play.png` `Phase1_Complete.png` |
| **阶段 2：战术扩展** | ADS 瞄准、C 键掩体蹲伏、备弹池、医疗包、受击红屏 | `PlayerController`"阶段2新增：ADS/掩体减速整合"、`PlayerHealth`/`UIManager`/`FirearmWeapon`/`WeaponManager` 的"阶段2新增"注释 |
| **阶段 3：场景与动画** | 主场景布局搭建、玩家动画状态机接入（走/跑/跳/蹲/死亡） | 截图 `Phase3_T1_Layout_TopDown.png` `Phase3_Complete_*.png` `PlayerAnimator_*.png`；`PlayerController` 中"角色动画驱动（阶段3接入）" |
| **阶段 4：战斗表现** | 4B 音效（**挂起，未阻塞后续任务**）→ 4C 曳光+部位伤害 → 4D 命中表现+HitMarker → 4E 手雷+震屏 → 4F 武器摆动+头部起伏 | `BulletTrail`(4C)、`EnemyPart`(4C)、`HitFeedbackUI`(4D)、`GrenadeThrower`/`CameraShaker`(4E)、`WeaponSway`/`HeadBob`(4F) 类注释 |
| **阶段 5：流程闭环** | S1 敌人三预制体 → S2 暂停职责迁移 GameManager → S3 死亡结算 → S4 波次刷怪 → S5 胜利结算 → S5b 来袭倒计时 → S6 击杀 HUD | `GameEvents`"流程（阶段5）"事件批、`EnemyWaveGenerator`(阶段5)、截图 `S1~S6_*.png` |
| **穿插：修复迭代** | 主菜单 UI 修复、HUD 血条修复、动画诊断系列、枪械对齐（腰射/ADS）、P08 角色迁移、受击红晕验证 | 截图 `UI_Fix_*` `HUD_*fix*` `Fix_AnimDiag_*` `Fix_GunAlign_*` `P08_migration_verify.png` `RedVignette_*` |
| **未完成** | 4B 音效资产（API Key 未配置，长期挂起） | `AudioManager`"资产为空时静默跳过"；AGENTS.md 进度清单 |

决策编号（D1～D7）是 architect 角色在方案阶段拍板的记录，散落在注释里：
D1 曳光用 LineRenderer、D2 用轻量静态事件总线、D5 胜利接入、D6 首波 4/2/2 混编、D7 手雷拾取复制 HealthPickup 模式。
你在后续章节会反复遇到它们——**每个 D 编号都是一次"有理由的选择"**，本文档讲解时会还原这个理由。

## 【本章自检】

读完本章，你应该能不假思索地回答：

1. 一局游戏从点击"开始"到结算，经历哪几个状态？（0.1.1）
2. 为什么死亡结算故意不把 timeScale 置 0？（0.1.1）
3. 项目为什么用 URP + LTS + 现成美术包的组合？（0.2）
4. "敌人死亡"这个消息，谁发的、谁收的？（0.3.3）
5. 阶段 2~5 分别给项目增加了什么？（0.4）

答不上来的，建议回看对应小节再进入第 1 章。

## 【常见坑】

- **把本项目当成"从上往下写代码"的项目**：实际开发顺序是"先地基（GameEvents 事件总线与 ObjectPool 等 Data 骨架）→ 再玩家 → 再战斗 → 再敌人 → 最后流程闭环"。如果你从 UI 开始写，会写不下去——UI 订阅的事件还没诞生。
- **忽略 0.3.3 的消息流向图**：后面每章都会回到这张图。跳过它，你会觉得 36 个脚本毫无规律；理解它，每个脚本都只是图上一个点。
- **照抄 README 的目录描述**：README 里 `Scripts/Items/` 的说法已过时（0.1.3），以 0.3 节实测结构为准——**读文档要读比代码新的文档**。

---

# 第 1 章 · 工程搭建

## 【本章目标】

从"电脑上只有 Unity Hub"开始，完成动工前的所有准备工作：

1. 用正确的模板和版本新建项目，确认 URP 生效；
2. 导入全部美术资源与功能包，理解每个包为什么存在；
3. 建立 0.3 节描述的目录结构，理解每条目录规则的用意；
4. 写入项目规则文件 AGENTS.md，建立（人或 AI 协作都适用的）开发纪律；
5. 理解本项目的"多 Agent 开发工作流"——即使你独自开发，这套流程也值得借鉴。

本章结束时，你的工程能打开、能跑、Console 零报错——但还没有一行游戏逻辑。

## 【前置知识】

- **Unity 模板（Template）**：新建项目时 Unity Hub 提供的起点工程。`3D (URP)` 模板自带 URP 管线资产与示例场景；`3D` 核心模板是旧内置管线，**两者外观相似但管线根本不同**。
- **Package Manager（包管理器）**：Unity 顶栏 `Window → Package Manager`，管理功能包。装包写进 [manifest.json](Packages/manifest.json)。
- **Render Pipeline Asset（管线资产）**：URP 的配置文件（.asset），决定阴影质量、抗锯齿、HDR 开关等。项目可准备多档，由 Quality 级别切换。
- **.meta 文件**：Unity 为每个资产生成同名 `.meta`（GUID 等）。**代码提交时必须连 .meta 一起提交**，否则引用断链。
- **PlayerPrefs**：Unity 内置的轻量键值持久化（存注册表/文件），适合存画质设置这类小数据。

## 1.1 新建项目：版本与模板

### 第 1 步：安装编辑器

Unity Hub → Installs → Install Editor → 选择 **2022.3.62f2c1**（或任何 2022.3.x LTS），
勾选模块时至少包含你的目标平台 Build Support 与文档。版本对齐很重要：
[ProjectVersion.txt](ProjectSettings/ProjectVersion.txt) 是项目对版本的唯一声明。

> 为什么锁 LTS 而不用更新的 Unity？Cinemachine 3.1.7、Input System 1.14.2、URP 14.0.12 都是对着
> 2022.3 验证过的组合。教学项目要的是"照着做一定能成"，追新版本等于给自己埋未知的坑。

### 第 2 步：选择模板

新建项目时选择 **`3D (URP)`（Universal 3D）模板**，项目名任意（本项目叫 `AI Test`）。

**怎么确定本项目用的是这个模板？** 打开工程后看 `Assets/Settings/`：

```text
Assets/Settings/
├── URP-Performant.asset / URP-Performant-Renderer.asset      # 低画质档
├── URP-Balanced.asset / URP-Balanced-Renderer.asset           # 中画质档
└── URP-HighFidelity.asset / URP-HighFidelity-Renderer.asset   # 高画质档
```

这套"三档 URP 资产 + Render Pipeline Asset 与 Renderer 成对"是 Universal 3D 模板的标准产物，
外加根目录的 `TutorialInfo/` 新手资料——两者都是模板自带的"出厂印记"。

> **为什么从 URP 模板起步，而不是先用内置管线再切换？**
> 内置管线的材质（Standard Shader）转到 URP 要全量重写映射，第三方资源包也要重新转换；
> 而 URP 模板从第一分钟就是正确的渲染生态。本项目实际走了模板路线——如果你的工程已经建在
> 内置管线上，切换步骤见 1.9"常见坑"第 1 条。

### 第 3 步：验证 URP 已生效

打开 `Edit → Project Settings → Graphics`，`Scriptable Render Pipeline Settings` 应指向
`URP-Performant`（或 Balanced/HighFidelity 之一）；`Quality` 面板里每个质量级别都绑定了对应 URP 资产。

再做一个 30 秒实验：Project 窗口右键 `Create → Material`，看 Shader 下拉——
如果列出的都是 `Universal Render Pipeline/...` 开头的条目，说明管线正确；
如果出现 `Standard`，说明还跑在内置管线上。

## 1.2 安装功能包

打开 `Window → Package Manager`，切换到 `Unity Registry`，按 [manifest.json](Packages/manifest.json)
里本项目的真实依赖安装。关键包如下（版本即本项目锁定版本）：

```jsonc
// 摘自 Packages/manifest.json（本项目的真实依赖，节选）
"dependencies": {
    "com.unity.render-pipelines.universal": "14.0.12",  // URP（模板已带）
    "com.unity.cinemachine": "3.1.7",                   // TPS 相机
    "com.unity.inputsystem": "1.14.2",                  // 新输入系统
    "com.unity.ai.navigation": "1.1.7",                 // NavMesh 寻路（敌人用）
    "com.unity.textmeshpro": "3.0.7",                   // HUD 文本（模板已带）
    // com.coplaydev.unity-mcp 见 1.8 节
}
```

- **Input System**：安装完成会弹窗询问"是否启用新输入后端（并禁用旧的）"→ 点 **Yes**，
  编辑器自动重启。不重启的话项目会同时激活两套输入后端，后续代码里 `InputAction` 相关类型无法工作。
- **AI Navigation**：2022.3 里 NavMesh 烘焙工具（Window → AI → Navigation）由这个包提供，不是内置的。
- **Cinemachine**：注意大版本差异——3.x 与 2.x 的 API 完全不同（命名空间从
  `Cinemachine` 变成 `Unity.Cinemachine`）。本项目用的就是 3.x，代码里两种引用方式都有实例：
  [PlayerADSController.cs](Assets/Scripts/Player/PlayerADSController.cs) 用 `using Unity.Cinemachine;`，
  [CameraShaker.cs](Assets/Scripts/Player/CameraShaker.cs) 直接用全限定名
  `Unity.Cinemachine.CinemachineBasicMultiChannelPerlin`（驱动 Perlin 噪声震屏的真实类型）。
  照抄网上 2.x 教程的代码会编译失败。

## 1.3 导入 DOTween 与 lilToon

**DOTween**（补间动画库，主菜单/结算/横幅动画全靠它）：

1. 从 DOTween 官网下载 Unity 包，双击导入；
2. 导入后菜单 `Tools → Demigiant → DOTween Utility Panel`，点 **Setup DOTween**——
   它会根据你的 Unity 版本启用对应模块，并在 `Assets/Resources/` 生成
   `DOTweenSettings.asset`（证据：本项目 `Assets/Plugins/Demigiant/DOTween/` 与
   `Assets/Resources/DOTweenSettings.asset` 均存在）。

**lilToon 2.3.3**（为玩家角色 P08_Federica 的卡通渲染服务）：
本项目采用**本地包**方式安装——把 `jp.lilxyzw.liltoon-2.3.3` 文件夹放进 `Packages/` 目录，
再在 [manifest.json](Packages/manifest.json) 里写：

```jsonc
"jp.lilxyzw.liltoon": "file:Packages/jp.lilxyzw.liltoon-2.3.3",  // 本地路径引用
```

`file:` 引用让包不经过网络安装，版本永远固定——适合这种"给特定模型配套、不打算升级"的着色器。

## 1.4 导入美术资源

### 1.4.1 PolygonBattleRoyale（场景/武器/敌人观感的主要来源）

1. 购买/下载 PolygonBattleRoyale 包（Synty 商店或资产商店）；
2. 将其解压/导入到 `Assets/PolygonBattleRoyale/`（保持包内目录结构原样）；
   验证：导入后应看到 `Materials / Models / Prefabs / Scenes / Shader / Textures / URP` 七个子目录；
3. 该包自带 **URP 兼容目录**（`PolygonBattleRoyale/URP/`），用于 URP 管线下的材质正确显示——
   这就是 1.1 节"先定管线再导资源"的原因：管线定了，包才知道该用哪套材质；
4. 抽查验证：把包内任意预制体（如一间房屋）拖入场景，材质不应显示为**粉红色**
   （粉色 = Shader 不被当前管线支持）。

**从此刻起立规矩**（[AGENTS.md](AGENTS.md) 原文）：

- 优先复用 `PolygonBattleRoyale/Models` 与 `Prefabs` 下的现成资源；
- **不要修改资源包原始文件**，需要变体时复制后修改；
- 自定义材质一律放 `Assets/Materials/`（本项目的 `FX_BulletTrail.mat`、`PlayerGreen.mat` 等 7 个材质就在这里）。

### 1.4.2 玩家角色模型（P08_Federica）

本项目玩家角色不是 PolygonBattleRoyale 自带模型，而是单独导入的 `P08_Federica` 角色资源
（含 `Model_Data/` 目录），其材质依赖 lilToon 着色器（1.3 节）——这解释了为什么一个军事风项目里
会出现卡通渲染包。这不是推断：抽查 `P08_skin.mat` 可见其 m_Shader GUID
`efa77a80ca0344749b4f19fdd5891cbe`，与 lilToon 包内 `Shader/lts_o.shader.meta`（不透明变体）
的 GUID 完全一致；模型自带 16 个同系列材质（P08_skin/Hair/Cloth 等）与 Matcap 贴图。
迁移与验证过程在 `Assets/Screenshots/P08_migration_verify.png` 留有记录。

配套的动画资产（`Assets/Animations/` 下 40 余个 FBX：持枪跑、平移、蹲行、跳跃三段、死亡等）
来自多个来源，[FootstepReceiver.cs](Assets/Scripts/Player/FootstepReceiver.cs) 的类注释
"接收角色动画（Mixamo/BSP/TPS）中回调的动画事件"如实记录了这一点。第 3 章动画接入时再展开。

## 1.5 建立目录结构

在 Project 窗口按 0.3.1 的总览图创建目录。三个要点：

1. **脚本目录在动第一行代码前就要规划好**。本项目实际落地为 6 个系统子目录：
   `Scripts/{Player, Combat, Enemy, UI, Data, Audio}`。
   有个诚实的细节：AGENTS.md 的目录约定里还写着 `Items/` 和 `Quest/`——那是从早期
   RPG 项目模板继承的预留位，TPS 实际用不上，**从未创建**（物品逻辑归 Combat、无任务系统）。
   教训：约定文档是活的，落地时按需裁剪，别让文档骗了后来的读者。
2. **`Prefabs/` 只放自制预制体**。本项目的 14 个预制体（`Player.prefab`、
   `Enemy_Scout/Soldier/Heavy.prefab`、`Weapon_Pistol/Rifle/SMG.prefab`、`Grenade_Thrown.prefab`、
   拾取物 ×3、特效 ×2、`DemoScene_Content.prefab`）全在这里，和资源包的 Prefabs 目录物理隔离——
   一眼可分辨"谁的产物归谁管"。
3. **`Screenshots/` 是"验证文化"的产物**：本项目每完成一个阶段就截图存档（约 110 张），
   这不是随手为之，而是工作流的强制规则（1.7 节）。建议你复现时同样保留——
   当你回头找"哪一步开始坏掉的"，截图序列就是最好的时光机。

创建完毕后 Commit 一次。**注意 .meta 文件必须入库**（.gitignore 模板已正确忽略 `Library/` 等缓存目录）。

## 1.6 场景规划

模板自带的 `Assets/Scenes/SampleScene.unity` 保留下来当**游戏主场景**
（本项目就是这么做的——它没有被删除或改名，而是在阶段 3 逐步改造为战斗关卡）；
再新建 `Assets/Scenes/MainMenu.unity` 作为**主菜单场景**。

双场景结构是流程闭环的物理基础：

```text
MainMenu.unity（ MainMenuController + Canvas：Play/Settings/Exit + 设置/Loading 面板 ）
    │  playButton.onClick → OnPlay()（摘自 MainMenuController.cs）
    │  LoadingPanel 读条 → SceneManager.LoadScene 进游戏场景
    ▼
SampleScene.unity（ 战斗、HUD、波次、结算 ）
    │  GameOverPanel / PauseMenu 的按钮
    │  → GameManager.Restart() 重载本场景 / GameManager.ReturnToMenu() 回主菜单
    ▼（SceneManager.LoadScene，摘自 GameManager.cs 的 Restart/ReturnToMenu）
MainMenu.unity 或 SampleScene.unity(重玩)
```

两场景的跳转逻辑分散在 [MainMenuController.cs](Assets/Scripts/UI/MainMenuController.cs)
与 [GameManager.cs](Assets/Scripts/Data/GameManager.cs)，第 7 章会完整梳理。
现在你只需要建好这两个空场景，各放一个 Camera 和 Directional Light
（AGENTS.md 强制规则第 3 条："场景内必须有 Camera 和 Directional Light"）。

## 1.7 写入项目规则：AGENTS.md

在项目根目录创建 `AGENTS.md`，把"任何人（或 AI）动工前必须知道的纪律"写死。本项目的 AGENTS.md
包含五类内容，逐条解释为什么值得写：

1. **项目概况**（引擎/资源/玩法/工作模式）：新协作者 30 秒建立认知，不必再问"这项目用什么版本"；
2. **强制工作流**：如"修改 C# 后必须 read_console 检查编译错误"、"每阶段完成要截图"、"每阶段输出功能总结表"——把验证变成流程而不是自觉；
3. **目录结构约定 + 资源复用规则**：1.5 节的全部内容，白纸黑字；
4. **代码规范**：

   ```csharp
   // 摘自 PlayerController.cs（Assets/Scripts/Player/PlayerController.cs）的真实命名风格
   public float walkSpeed = 3f;          // 公开字段/属性 → 驼峰（Inspector 可见）
   private CharacterController _characterController;  // 私有字段 → _驼峰
   ```

   `_驼峰` vs 驼峰一眼区分"内部状态"与"对外配置"，加上"中文注释关键逻辑"的要求，
   代码评审时无需反复确认意图；
5. **角色权限边界**（见 1.8 节）：写给 AI 协作者的"宪法"。

## 1.8 开发工作流：多 Agent 分工

本项目按《OpenCode 多 Agent 工作流与 Unity RPG 开发指南》开发（该指南是仓库外文档），
AGENTS.md 是它在本项目的落地。核心是**三种角色 + 一条流水线**：

| 角色 | 职责 | 允许 | 禁止 |
|------|------|------|------|
| **architect**（规划） | 读取现状 → 分析 → 输出方案文档 | 读代码、检索、反射 API | ❌ 创建/修改任何文件——**方案被批准前一个字都不写** |
| **coder**（实现） | 接收方案 → 实现代码 | 建脚本、改场景、组装预制体 | ❌ 自行规划、跳过依赖、修改方案 |
| **explore**（审查） | 审查 → 报告 | 读代码、读控制台 | ❌ 修改任何文件 |
| **用户** | 确认方案、验收阶段成果 | — | — |

流水线：`architect 出方案 → 用户确认 → coder 实现 → read_console 验证零编译错误 →
explore 审查 → 截图给用户确认 → 输出功能总结表 → 下一阶段`。

**这就是 0.4 节那些编号的来历**：architect 拍板的技术决策记为 D1、D2…（写进代码注释），
coder 的产出按任务编号归档（T1-1、4E、S3…），explore 发现的问题走修复迭代。
两个具体的"流程防呆"设计值得借鉴：

- **方案先行**：0.3.3 的事件总线、0.4 的波次状态机都是先有 architect 文档、后有代码——
  所以注释里才写得出"决策 D2"这种字样，半年后回看仍知道"为什么"；
- **验证内建于流程**：编译零错误（read_console）+ 视觉证据（截图）+ 用户确认，三关缺一不可。

**基础设施**：AI 操作编辑器依赖 MCP For Unity 包（[manifest.json](Packages/manifest.json)
里 `com.coplaydev.unity-mcp`，GitHub 引用），它把 read_console、创建脚本、场景操作等能力
暴露为 AI 可调用的工具。**你人工复现本项目时不需要装它**——它的作用对象是 AI，不是游戏；
但"每步之后看一眼 Console"的纪律，请原样保留。

最后一点：即使你完全单人开发，这套"方案 → 实现 → 审查"的节奏也值得保留——
写代码前先写一页方案（哪怕给自己看），写完假装自己是审查者读一遍。**角色即检查清单**。

## 1.9 验证方法：阶段验收清单

"工程搭建完成"的定义——逐项核对：

1. Unity 2022.3.x 打开工程，Console 无红字（模板项目天然满足）；
2. `Project Settings → Graphics` 指向 URP 资产；新建材质下拉无 `Standard` Shader（1.1 节）；
3. Package Manager 中 Cinemachine 3.1.7、Input System 1.14.2、AI Navigation 1.1.7 显示已安装；
4. PolygonBattleRoyale 导入完成：抽查 2-3 个预制体拖入场景，材质不粉、Console 无报错；
5. DOTween 菜单存在且 Setup 完成（`Tools → Demigiant`）；
6. `Scripts/{Player,Combat,Enemy,UI,Data,Audio}` 六个空目录就位；
7. 两个场景存在，各含 Camera 与 Directional Light；
8. AGENTS.md 就位于根目录；
9. 首次 Git 提交完成（含 .meta）。

对照本项目：以上 9 项全部有仓库证据（目录实测 + manifest + ProjectSettings）。
从下一章起，我们开始写第一行代码——先写谁？为什么？见第 2 章。

## 【常见坑】

1. **模板选错（选了内置管线的 `3D` 模板）**：URP 不是"装个包"那么简单——
   Graphics 设置、质量分级、已有材质转换三件事都要做，且第三方包的内置材质转换经常出幺蛾子。
   发现得早（还没导资源）就重建项目，发现得晚就忍痛执行 `Window → Rendering → Render Pipeline Converter`。
   一开始选对模板是最便宜的解法。
2. **Input System 弹窗点了 No（或没重启编辑器）**：后果是 `PlayerSettings` 里
   `Active Input Handling` 处于混合或旧模式，后续 `InputAction` 类型行为诡异甚至编译不报错但运行不响应。
   补救：`Edit → Project Settings → Player → Other Settings → Active Input Handling` 改为
   `Input System Package (New)` 后重启。
3. **按 2.x 教程写 Cinemachine**：本项目锁 3.1.7（`using Unity.Cinemachine;`）。
   2.x 的 `CinemachineVirtualCamera` 组件在 3.x 已改名/改结构，代码层几乎不可移植。
   查资料时先确认版本再照抄。
4. **把自制预制体/材质塞进 PolygonBattleRoyale 目录**：资源包升级或重导入时自制物会被
   覆盖或产生冲突；而且"哪些是自己的"会变成考古工作。规则：**自己的东西只进自己的目录**。
5. **直接改资源包原始文件**：同上，是协作事故的头号来源。要变体？复制 → 改副本 → 存到 `Assets/Materials/` 或 `Assets/Prefabs/`。
6. **忘提交 .meta / 手贱提交 Library**：Unity 项目版本控制两宗罪。用 Unity 官方 .gitignore 模板
   （本仓库根目录就有），Library/Temp/Obj 一律忽略，.meta 一个不漏。
7. **路径太随意**：本项目路径含空格（`AI Test`）证明 Unity 本身能容忍，但外部工具链
   （命令行构建脚本、某些插件）经常栽在空格/中文路径上。新项目建议纯 ASCII 无空格路径，
   复现本项目时引以为戒。
8. **跳过 AGENTS.md 直接开写**：纪律文档最大的价值不是"给别人看"，而是让三周后的你
   不再纠结"私有字段到底要不要下划线"这种每次都要重新决定的事。

---

# 第 2 章 · 数据与事件骨架

## 【本章目标】

写出 `Assets/Scripts/Data/` 目录的地基四件套：**GameEvents（事件总线）、GameSettings（设置读取）、
ObjectPool（对象池）、GameManager（流程状态机）**。学完本章你将能：

1. 说清"为什么第一批代码是它们"——后面 32 个脚本全部站在这四块地基上；
2. 掌握**静态事件总线**模式：何时用实例事件、何时走全局总线、订阅为什么必须退订；
3. 实现"键 = 预制体"的通用对象池，并知道**什么时候不该用池**（项目里有现成反例）；
4. 实现一个把"暂停/死亡/胜利"三种停止方式区分对待的流程状态机；
5. 完成 GameManager 在场景中的挂载与 Inspector 配置（本章给出项目实测配置值）。

本章结束时项目**仍然跑不出任何可见效果**——地基就是地基。但你会拥有一套让后续每个系统
都能"即插即用"的插座。

## 【前置知识】

- **static（静态成员/静态类）**：属于类本身而不属于任何实例的代码与数据。静态类（如 `GameEvents`）不需要挂到 GameObject 上就全程存活——这是"地基类不依赖场景"的关键。
- **event 与委托（Delegate）**：`public static event Action<int> OnKillCountChanged;` 声明一个"可被外部订阅、但只能被声明方触发"的消息通道。`Action<T>` 是无返回值、最多若干参数的内置委托类型。
- **MonoBehaviour 生命周期**：`Awake → Start → Update...`，其中 `OnEnable/OnDisable` 在对象激活/失活时成对触发——本项目用它做事件的订阅/退订配对。
- **单例模式（Singleton）**：全场景只有一个实例 + 一个全局访问点（`GameManager.Instance`）。Unity 里通常在 `Awake` 里做"已存在则销毁自己"的守卫。
- **协程（Coroutine）**：`StartCoroutine` + `yield return new WaitForSeconds(n)` 实现非阻塞的延迟逻辑。注意 `WaitForSeconds` 用的是**受 timeScale 缩放的时间**——这条性质在 2.4 节至关重要。
- **Time.timeScale**：全局时间缩放。置 0 时 `Time.deltaTime` 为 0，几乎所有 Update 移动、物理、动画、普通协程全部冻结——是"暂停"的标准做法，但也容易误伤"不该冻结的东西"。
- **PlayerPrefs**（第 1 章已介绍）：跨会话的键值持久化。

## 2.0 先想清楚：为什么第一批代码是它们

回看 0.3.3 的消息流向图：**每一条箭头都从 GameEvents 或指向 GameEvents**。
如果先写玩家再写事件总线，PlayerHealth 就得先直接引用 UIManager，等引入总线时再拆掉重接——
本项目走了相反的路：总线先立，各系统建成后自己订阅。

**但有一件事必须对读者诚实**：本章把四个类作为一个"骨架单元"教学，而**真实开发时间线并非如此**。
证据写在代码注释里：

- [GameEvents.cs](Assets/Scripts/Data/GameEvents.cs) 的类注释标着 **"决策 D2"**，属于最早期地基；
  但它的事件是**逐步生长的**——`OnSurfaceHit` 标注"4D 新增"、`OnGrenadeCountChanged` 标注"4E 新增"、
  整批流程事件标注"阶段5 新增"；
- [GameManager.cs](Assets/Scripts/Data/GameManager.cs) 的类注释写着 **"游戏流程状态机（阶段5）"**，
  且"接管原 PauseMenuController 的 timeScale/脚本禁用/场景加载职责"——即它是**阶段 5 才补上的收口者**，
  早期连"暂停"都还是 PauseMenuController 自己管的（阶段 5 的 S2 任务才把职责迁走，截图
  `S2_pause_panel_migrated.png` 记录了这次迁移）。

**给你的复现建议**：直接按本章顺序一步到位（写 GameManager 时事件总线已完整可用），
跳过真实项目"先写错再迁移"的弯路；但请记住这段历史——**"职责随阶段生长、旧类瘦身让位"**
是这个项目演进的真实方式，后面章节你还会见到它。

编辑器操作上，四件套里有三个（GameEvents / GameSettings / ObjectPool）是**纯静态 C# 类**，
不需要任何场景操作：在 `Assets/Scripts/Data/` 目录创建对应 `.cs` 文件（IDE 中直接新建即可，
Unity 会自动生成 .meta 并编译），`read_console` 确认零错误后即可继续。
只有 GameManager 需要（也值得）在场景里落地——见 2.4.4。

## 2.1 GameEvents：全局事件总线

### 2.1.1 设计目标

跨系统的消息传递有三种做法：直接引用（A 持有 B 调 B）、单例调用（A 找 B.Instance 调）、
事件广播（A 喊一声，谁关心谁订阅）。本项目在 AGENTS.md 里写死了选择：

> 优先事件/委托解耦，避免直接互相引用。

GameEvents 是这个决策的落地（"决策 D2"）。它的类注释浓缩了全部纪律，值得先读一遍原文
（[GameEvents.cs](Assets/Scripts/Data/GameEvents.cs) 第 4~8 行）：

```csharp
// 摘自 GameEvents.cs 类注释（Assets/Scripts/Data/GameEvents.cs）
/// 全局静态事件总线（决策 D2：轻量静态事件）
/// 全游戏唯一跨系统消息通道；事件只增不改签名（破坏性变更需 architect 批准）
/// 订阅方必须在 OnDisable/OnDestroy 退订
public static class GameEvents
```

### 2.1.2 实现：21 个事件 + 21 个 Raise 方法

事件按领域分组，全部是 `Action` 及其泛型变体（节选真实代码）：

```csharp
// 摘自 GameEvents.cs（Assets/Scripts/Data/GameEvents.cs，类 GameEvents）
public static class GameEvents
{
    // ===== 玩家 =====
    public static event Action OnPlayerDamaged;              // 玩家受击
    public static event Action OnPlayerDeath;               // 玩家死亡（GameManager/结算订阅）
    public static event Action<Vector3, string> OnPlayerShot; // 玩家开枪（枪口位置 + 武器名）
    public static event Action OnReloadStart;               // 换弹开始
    public static event Action OnReloadEnd;                 // 换弹结束

    // ===== 敌人 =====
    public static event Action<Vector3> OnEnemyKilled;      // 敌人死亡（波次/计分订阅）
    public static event Action OnHeadshotKilled;            // 爆头击杀（UI 提示）

    // ===== 战斗表现 =====
    public static event Action<Vector3, Vector3> OnBulletTrail; // 曳光（起点, 终点）
    public static event Action<Vector3, Vector3, bool> OnSurfaceHit; // 命中表面（点, 法线, 是否敌人）

    // ===== 流程（阶段5） =====
    public static event Action<int> OnWaveStarted;          // 波次开始（波号）
    public static event Action OnGameOver;                  // 玩家阵亡结算
    public static event Action OnGameVictory;               // 全部波次清空胜利
    public static event Action<int> OnKillCountChanged;     // 击杀数变化
    // ...（共 21 个事件，完整清单见源文件或 0.3.3 的流向图）
}
```

每个事件配一个同名的广播方法，命名规则 `Raise{事件名去掉 On}`：

```csharp
// 摘自 GameEvents.cs 的 Raise 方法
public static void RaiseEnemyKilled(Vector3 pos) => OnEnemyKilled?.Invoke(pos);
public static void RaisePlayerShot(Vector3 pos, string weaponName) => OnPlayerShot?.Invoke(pos, weaponName);
```

**为什么每个事件都要包一层 Raise 方法，而不是让生产者直接写 `OnEnemyKilled?.Invoke(...)`？**
三个理由，每个都在后续章节兑现：

1. **权限收口**：C# 的 `event` 只允许**声明类内部**触发。所有广播必须走 `GameEvents.Raise*`，
   意味着"谁在什么时候广播了什么"在源文件里一眼可查（本项目用 grep `Raise\w+\(` 就能列出全部广播点——
   0.3.3 的流向图就是这么画出来的）；
2. **null 检查集中**：`?.Invoke()` 保证"没人订阅也不炸"，生产者永远不用关心有没有听众；
3. **签名即契约**："只增不改签名"的纪律落在这里——事件参数列表一旦公布，改一下就是全项目编译错，
   所以注释写明"破坏性变更需 architect 批准"。

### 2.1.3 纪律：订阅方必须在 OnDisable 退订

订阅的标准姿势在第 0 章见过（GameManager 的 OnEnable/OnDisable 对称写法），这里补上**为什么**：
`static event` 的生命周期跟随**整个应用域**而不是场景。场景重载时，普通组件都被销毁，
但静态事件里的订阅列表还在——如果你忘了退订，下次场景加载后，一个已经指向"死对象"的委托还挂在列表里，
一旦事件广播就会触发 `MissingReferenceException`，或更糟：悄悄调用一个"复用了同一块内存"的僵尸方法。

本项目对这条纪律的执行非常严格，连 Button 监听都成对移除（[GameOverPanelController.cs](Assets/Scripts/UI/GameOverPanelController.cs) 的 `OnDisable`）：

```csharp
// 摘自 GameOverPanelController.cs 的 OnDisable
private void OnDisable()
{
    GameEvents.OnGameOver -= ShowGameOver;
    GameEvents.OnGameVictory -= ShowVictory;
    if (restartButton != null) restartButton.onClick.RemoveListener(OnRestartClicked);
    if (menuButton != null) menuButton.onClick.RemoveListener(OnMenuClicked);
    if (_showSeq != null && _showSeq.IsActive()) _showSeq.Kill();
}
```

### 2.1.4 双通道并存：实例事件 vs 全局总线

本项目**不是**把所有消息都塞进总线。[PlayerHealth.cs](Assets/Scripts/Player/PlayerHealth.cs) 同时维护着
两套通道，而且代码注释明确记录了这样做的历史与理由：

```csharp
// 摘自 PlayerHealth.cs（Assets/Scripts/Player/PlayerHealth.cs）
// 实例事件：给"紧耦合"的 HUD 用（UIManager 直接持有 PlayerHealth 引用后订阅）
public event Action<float> OnDamaged;
public event Action OnDeath;
public event Action<float, float> OnHealthChanged;
public event Action<int> OnMedkitChanged;

public void TakeDamage(float amount)
{
    ...
    OnDamaged?.Invoke(amount);            // 实例事件：UIManager 红屏/血条走这里
    ...
    if (_currentHealth <= 0f)
    {
        _animator?.SetTrigger("Died");
        OnDeath?.Invoke();
        GameEvents.RaisePlayerDeath();    // 阶段5：广播总线供 GameManager 结算（实例事件 OnDeath 保留不动）
    }
}
```

**划分规则**（从代码用法可归纳）：

| 通道 | 适合谁 | 项目中的实例 |
|------|--------|--------------|
| 实例事件（`public event`） | 同物体组件或已持引用的紧邻消费者 | UIManager 订阅 `OnHealthChanged` 画血条、订阅 `OnDamaged` 做红屏 |
| 全局总线（`GameEvents`） | 跨系统、互不相识的消费者 | GameManager 听 `OnPlayerDeath` 做结算、AudioManager 听 `OnPlayerShot` 播枪声 |

注意 `RaisePlayerDeath` 那行注释——"实例事件 OnDeath 保留不动"。阶段 5 接入流程时，正确做法是**加一条总线广播，而不是改造现有接口**：这就是"只增不改"在演进中的样子。

### 2.1.5 一个如实的观察：预留未接线的事件

grep 全项目广播点后可以确认：`OnPlayerDamaged` **至今没有任何生产者**——只有 AudioManager 单方面订阅着它
（[AudioManager.cs](Assets/Scripts/Audio/AudioManager.cs) L62 `GameEvents.OnPlayerDamaged += HandlePlayerDamaged;`）。
HUD 的受击红屏实际走的是 PlayerHealth 的实例事件 `OnDamaged`。这个"无广播方"的通道当前是哑的，
但保留它无害且有意义（4B 音效接入后，玩家受伤音可以从这里补广播）。
如实记录这个细节，是为了说明：**事件总线允许存在预留通道**，它不是设计失误，而是"只增"纪律的副产品。
（第 8 章的遗留问题清单会再次提到它。）

## 2.2 GameSettings：设置读取的统一入口

### 2.2.1 实现

完整实现只有 49 行（[GameSettings.cs](Assets/Scripts/Data/GameSettings.cs)），可以整读：

```csharp
// 摘自 GameSettings.cs（Assets/Scripts/Data/GameSettings.cs，类 GameSettings，全文）
public static class GameSettings
{
    // 与 MainMenuController 相同的键
    public const string KeySensitivity = "Settings.Sensitivity";
    public const string KeyMasterVolume = "Settings.MasterVolume";
    public const string KeySFXVolume = "Settings.SFXVolume";
    public const string KeyFOV = "Settings.FOV";
    public const string KeyAutoShoot = "Settings.AutoShoot";

    public static float Sensitivity { get; private set; } = 2f;
    public static float MasterVolume { get; private set; } = 0.8f;
    public static float SFXVolume { get; private set; } = 1f;
    public static float FOV { get; private set; } = 75f;
    public static bool AutoShoot { get; private set; }

    private static bool _loaded;

    /// <summary>读取全部设置（只读一次，之后缓存）</summary>
    public static void Load()
    {
        if (_loaded) return;
        Sensitivity = PlayerPrefs.GetFloat(KeySensitivity, 2f);
        MasterVolume = PlayerPrefs.GetFloat(KeyMasterVolume, 0.8f);
        SFXVolume = PlayerPrefs.GetFloat(KeySFXVolume, 1f);
        FOV = PlayerPrefs.GetFloat(KeyFOV, 75f);
        AutoShoot = PlayerPrefs.GetInt(KeyAutoShoot, 0) == 1;
        _loaded = true;
    }

    /// <summary>应用全部设置到运行时</summary>
    public static void ApplyAll()
    {
        Load();
        AudioListener.volume = MasterVolume;
    }

    /// <summary>写入自动射击开关（暂停菜单/主菜单共用：同步 PlayerPrefs 与运行时缓存）</summary>
    public static void SetAutoShoot(bool value)
    {
        AutoShoot = value;
        PlayerPrefs.SetInt(KeyAutoShoot, value ? 1 : 0);
    }
}
```

四个设计点：

1. **属性带默认值 + `Load()` 只读一次**：`_loaded` 守卫防止每帧读 PlayerPrefs（ PlayerPrefs 是文件/注册表 IO，不便宜）；
2. **`private set`**：外部只能读不能改，写入必须走 `SetAutoShoot` 这类"同步 PlayerPrefs + 运行时缓存"的专用方法，防止两处状态漂移；
3. **`ApplyAll()` 的职责边界**：如你所见，它当前只应用音量（`AudioListener.volume`）。
   灵敏度与 FOV 的"应用"发生在消费者处——PlayerController.Start 里 `mouseSensitivity = GameSettings.Sensitivity;`、
   PlayerADSController 里 `defaultFOV = GameSettings.FOV;`。如实指出：**ApplyAll 名不副实是历史的产物**（音量是第一个需要全局挂载点的设置）；
4. **谁在用它**（grep `GameSettings.` 全部 8 处实证）：PlayerController（灵敏度）、PlayerADSController（FOV）、
   WeaponManager（`GameSettings.AutoShoot` 自动射击开关）、AudioManager（ApplyAll + SFXVolume）、
   PauseMenuController（SetAutoShoot）。

### 2.2.2 键名的"考古地层"——以及为什么要用 const

读本项目时你会发现 PlayerPrefs 键名存在**三个历史层次**，像地质剖面一样诚实：

| 层 | 位置 | 写法 | 评述 |
|----|------|------|------|
| 最早 | [MainMenuController.cs](Assets/Scripts/UI/MainMenuController.cs) L41-45 | `public const string KeySensitivity = "Settings.Sensitivity";`（写入方自带键定义） | 键与写入方住一起 |
| 更早习惯残留 | [PauseMenuController.cs](Assets/Scripts/UI/PauseMenuController.cs) L74-80 | `PlayerPrefs.SetFloat("Settings.Sensitivity", v);` 字符串字面量直写 | **手写键名 = 隐患**：拼错一个字母，设置就静默失效且不报错 |
| 后来的统一层 | [GameSettings.cs](Assets/Scripts/Data/GameSettings.cs) L10-14 | 同样的 const 再声明一份 | GameSettings 注释自己都写着"与 MainMenuController 相同的键" |

这个地层是绝佳反面教材：理想形态是**键定义只存在一处**（比如 GameSettings），MainMenu/PauseMenu 统一引用。
本项目没做这次清理（两份 const 重复但至少字面量一致），复现时建议直接一步到位：键定义只放 GameSettings。

### 2.2.3 自动射击开关为什么特殊

`SetAutoShoot` 是四件套里唯一的写方法。原因：主音量/灵敏度/FOV 是"读进缓存"型的单向设置，
而**自动射击**在暂停菜单里打开后要**立即生效到正在运行的战斗逻辑**（WeaponManager 每帧判断
`if (GameSettings.AutoShoot && IsCrosshairOnEnemy())`），所以必须"运行时缓存 + 持久化"双写。
（这个开关的读取端在第 4 章 WeaponManager 一节还会出现。）

## 2.3 ObjectPool：键 = 预制体的通用池

### 2.3.1 实现

先讲问题：每开一枪要生成"曳光"，每颗手雷落地要"爆炸特效"，弹痕、血雾、烟尘全是一次性高频对象。
`Instantiate/Destroy` 每次都走完整的创建/回收、产生 GC 压力——打上三十秒就开始掉帧。
对象池的标准答案：**对象用完不销毁，关掉激活收回池里，下次要用再开出来**。

[ObjectPool.cs](Assets/Scripts/Data/ObjectPool.cs)（103 行）的实现有三个值得细看的机关。
先看整体骨架：

```csharp
// 摘自 ObjectPool.cs（Assets/Scripts/Data/ObjectPool.cs，类 ObjectPool）
public static class ObjectPool
{
    /// <summary>记录实例来源预制体的票据组件（Release 时按此回池）</summary>
    public class PoolTicket : MonoBehaviour
    {
        [HideInInspector] public GameObject sourcePrefab;
    }

    private static readonly Dictionary<GameObject, Queue<GameObject>> _pools = new();
    private static Transform _root;
```

**机关一：PoolTicket 票据**。`Release(instance)` 时池子需要知道"这个实例该回到哪个预制体的队列"。
方案是在实例上挂一个轻量组件记下 `sourcePrefab`——实例自己携带"我是从哪来的"凭证。
副作用是优雅的兜底：任何**没票据**的对象调用 Release 会被直接 Destroy（见下），池子对陌生对象是安全的。

```csharp
    // 摘自 ObjectPool.cs 的 Release 与 Get（节选）
    public static void Release(GameObject instance)
    {
        if (instance == null) return;
        var ticket = instance.GetComponent<PoolTicket>();
        if (ticket == null || ticket.sourcePrefab == null)
        {
            Object.Destroy(instance);   // 非池化对象直接销毁，调用方无需区分
            return;
        }
        instance.SetActive(false);
        instance.transform.SetParent(Root, false);
        GetQueue(ticket.sourcePrefab).Enqueue(instance);
    }

    public static GameObject Get(GameObject prefab, Vector3 pos, Quaternion rot)
    {
        ...
        GameObject inst;
        do
        {
            inst = queue.Count > 0 ? queue.Dequeue() : CreateInstance(prefab);
            // 队列中可能残留已销毁对象（场景切换边界），跳过
        } while (inst == null && queue.Count > 0);
        if (inst == null) inst = CreateInstance(prefab);
        ...
    }
```

**机关二：Get 里的 do-while 跳残骸**。场景切换时池根节点（`DontDestroyOnLoad`）活着，
但队列里的实例可能已经随场景销毁——Unity 的"假 null"机制让 `inst == null` 判定生效，
循环跳过这些尸体直到拿到活的或池空新建。这是池化代码最常被忽略的边界。

**机关三：Root 属性里的自愈**：

```csharp
    // 摘自 ObjectPool.cs 的 Root 属性（节选）
    private static Transform Root
    {
        get
        {
            if (_root == null)
            {
                _pools.Clear();   // 池根节点被销毁 → 队列里的引用全部失效，字典必须清空
                var go = new GameObject("[ObjectPool]");
                _root = go.transform;
                Object.DontDestroyOnLoad(go);
            }
            return _root;
        }
    }
```

静态类无法感知场景事件，但"根被销毁"这个状态本身就是信号：字典里所有队列必然装着已死对象，
`Clear()` 一次清空。**用惰性检查代替事件监听**——简单得近乎偷懒，但正确。

### 2.3.2 类注释里的两条使用约束

```csharp
// 摘自 ObjectPool.cs 类注释
/// 约束：池化对象脚本不得在 OnDisable 中破坏状态；Release 后不得再引用
```

为什么？池化对象"失活"时会触发 `OnDisable`——如果你在 `OnDisable` 里 Destroy 自己或改关键状态，
回池的对象就是坏的，下次 Get 出来直接报错或行为异常。"Release 后不得再引用"则是防双写：
你还攥着实例引用继续操作，下次 Get 可能把它发给别的调用方。

两条约束的活例子：[BulletTrail.cs](Assets/Scripts/Combat/BulletTrail.cs) 与 [Grenade.cs](Assets/Scripts/Combat/Grenade.cs)
都是在自己的生命周期结束时**只调** `ObjectPool.Release(gameObject)` 就干净退场，`OnDisable` 里什么都不做。

### 2.3.3 谁在用池，谁刻意不用池

预热（Prewarm）的责任在使用方——谁需要谁提前烧：

| 调用方 | 预热/使用（grep 实证） |
|--------|------------------------|
| [CombatEffectsController.cs](Assets/Scripts/Combat/CombatEffectsController.cs) | 预热曳光/血雾/烟尘/爆炸特效，Get 生成曳光与弹痕 |
| [GrenadeThrower.cs](Assets/Scripts/Player/GrenadeThrower.cs) | 预热手雷 2 个，投掷时 Get |
| [BulletTrail.cs](Assets/Scripts/Combat/BulletTrail.cs)、[Grenade.cs](Assets/Scripts/Combat/Grenade.cs) | 存活结束时 Release 回池 |

而**反例同样有教学价值**——敌人刻意不池化，理由写在 [EnemyWaveGenerator.cs](Assets/Scripts/Enemy/EnemyWaveGenerator.cs) 类注释：

```csharp
// 摘自 EnemyWaveGenerator.cs 类注释
/// 敌人 Instantiate 直出（不入对象池：尸体 10s 延迟销毁使池化复杂度不值）
```

敌人死亡后尸体要留 10 秒才销毁，"已死但仍在场上"的尸体塞回池里再发出来就是怪物 Bug。
要池化就得为尸体单做一套状态复位——复杂度收益比不划算。**"什么时候不用池"是池章节更重要的半课**。

### 2.3.4 一处注释与实现的出入

先读后写的回报来了：ObjectPool 的类注释写"栈式回收"，实际代码用的是 `Queue`（队列）。
两者在"复用顺序无所谓"的池场景里行为等价（注释可能是早期设计残留）。这不是 Bug，
但它提醒你：**写文档、做复现，以代码为准，不以注释为准**——本文档所有代码块都直接取自源文件。

## 2.4 GameManager：游戏流程状态机

### 2.4.1 实现：状态、单例、订阅

[GameManager.cs](Assets/Scripts/Data/GameManager.cs)（159 行）承担四件事：**流程状态、击杀计数、
两种停止方式、场景跳转**。骨架如下（真实代码节选）：

```csharp
// 摘自 GameManager.cs（Assets/Scripts/Data/GameManager.cs，类 GameManager）
public class GameManager : MonoBehaviour
{
    public enum GameState { Playing, Paused, GameOver, Victory }

    public static GameManager Instance { get; private set; }   // 单例

    [Header("暂停/结算时禁用的玩家操控脚本（自 PauseMenuController 迁移，场景拖入）")]
    [SerializeField] private MonoBehaviour[] disableOnPause;

    [Header("场景名（重玩=重载游戏场景；返回=主菜单）")]
    [SerializeField] private string gameplaySceneName = "SampleScene";
    [SerializeField] private string menuSceneName = "MainMenu";

    [Header("结算弹窗延迟（秒）")]
    [SerializeField] private float gameOverDelay = 1.5f;
    [SerializeField] private float victoryDelay = 1.0f;

    public GameState State { get; private set; } = GameState.Playing;
    public int KillCount { get; private set; }
    public int CurrentWave { get; private set; }
    public int MaxWave { get; private set; }
    public float SurvivalTime { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }  // 单例守卫
        Instance = this;
    }

    private void OnEnable()
    {
        GameEvents.OnPlayerDeath += TriggerGameOver;    // 只听事件，不主动查询任何系统
        GameEvents.OnEnemyKilled += HandleEnemyKilled;
    }
    private void OnDisable()
    {
        GameEvents.OnPlayerDeath -= TriggerGameOver;   // 静态事件必须退订（2.1.3 的纪律）
        GameEvents.OnEnemyKilled -= HandleEnemyKilled;
    }

    /// <summary>击杀计数（§4.1）：累加后广播 KillCountChanged（HUD 订阅）</summary>
    private void HandleEnemyKilled(Vector3 position)
    {
        KillCount++;
        GameEvents.RaiseKillCountChanged(KillCount);   // 消息接力：总线的价值在此
    }

    private void Update()
    {
        // 存活时间仅在 Playing 时累计（暂停 timeScale=0 时 deltaTime=0，天然不计时）
        if (State == GameState.Playing)
            SurvivalTime += Time.deltaTime;
    }
```

注意 `HandleEnemyKilled` 展示的**消息接力**模式：`EnemyBase 死亡 → RaiseEnemyKilled →
GameManager 计数 → RaiseKillCountChanged → UIManager 刷新击杀数`。
GameManager 在链条中间做"累加"加工再转发——它是数据的中游处理器，不是上帝对象。

**为什么不把击杀数记在 EnemyBase 或 UIManager 里？** 击杀数是**流程数据**：
结算面板（GameOverPanelController）要从它拉数据、胜利条件可能参考它。
流程数据归流程管理器，游戏内的实体（敌人）和表现层（UI）都不该拥有它。

### 2.4.2 本项目最有教学价值的一段：两种"停止方式"

```csharp
// 摘自 GameManager.cs 的 TriggerGameOver / Pause（节选）
public void TriggerGameOver()
{
    if (State != GameState.Playing) return;   // 状态守卫：死亡后重复 TakeDamage 会重复触发 OnDeath
    State = GameState.GameOver;
    SetScriptsEnabled(false);                 // 只禁玩家操控
    Cursor.lockState = CursorLockMode.None;
    Cursor.visible = true;
    StartCoroutine(GameOverRoutine());        // 世界继续跑，协程延迟 1.5s 后广播结算
}

public void Pause()
{
    if (State != GameState.Playing) return;
    State = GameState.Paused;
    Time.timeScale = 0f;                      // 整个世界冻结
    SetScriptsEnabled(false);
    Cursor.lockState = CursorLockMode.None;
    Cursor.visible = true;
}
```

| | 暂停（Paused） | 死亡/胜利（GameOver/Victory） |
|---|---|---|
| `Time.timeScale` | **0** —— 全世界冻结 | **保持 1** —— 世界继续活动 |
| 玩家操控 | 禁用 | 禁用 |
| 鼠标 | 解锁 | 解锁 |
| 观感 | 静止画面 + 暂停面板 | 死亡动画播完、敌人还在走动，1.5s 后结算面板淡入 |

为什么死亡不冻结？——**让"你死了"这件事被玩家看见**：死亡动画（`Died` Trigger）、
敌人补枪、镜头停在尸体上，这些"世界还在继续"的残酷感是表达的一部分。
这带来一个连锁技术决策：结算面板的 DOTween 动画必须用 `SetUpdate(true)`
（[GameOverPanelController.cs](Assets/Scripts/UI/GameOverPanelController.cs) L78），
保持"暂停面板"与"结算面板"同一动画模式防冲突——两处面板动画都声明了不受 timeScale 影响。

**状态守卫**也是全类反复出现的模式（`if (State != GameState.Playing) return;`）：
玩家死亡后尸体还可能被继续攻击、`TakeDamage` 还会被调用、`OnDeath` 还会再触发——
守卫保证状态机**只迁移一次**。这是"事件驱动状态机"最重要的防御习惯。

最后的场景跳转（重玩/回主菜单）用最朴素也最可靠的方式——整场景重载：

```csharp
// 摘自 GameManager.cs 的 Restart / ReturnToMenu
public void Restart()
{
    Time.timeScale = 1f;
    SceneManager.LoadScene(gameplaySceneName);   // 状态随场景重载全清（订阅者随对象销毁退订）
}
```

所有局内数据（血量、击杀、波次、手雷数）随场景一起清零，**不需要写任何一行"重置代码"**——
这是把"重置"外包给场景系统的经典取舍（代价：一切场景内状态都不得跨局存活）。

### 2.4.3 调用方清单：谁推它、谁拉它

grep `GameManager.Instance` 共 28 处，归类后只有四种用法（这是"接口收敛"的好范本）：

| 调用方 | 用法 | 代码位置 |
|--------|------|----------|
| [PauseMenuController.cs](Assets/Scripts/UI/PauseMenuController.cs) | 读 State 判断 Tab 有效性；调 `Pause()/Resume()/ReturnToMenu()` | Update/Pause/Resume |
| [EnemyWaveGenerator.cs](Assets/Scripts/Enemy/EnemyWaveGenerator.cs) | Start 时 `SetMaxWave`；开波时 `SetCurrentWave`；末波清空调 `TriggerVictory`；Update 读 State **防结算后仍刷怪** | L51-59 / L114-116 / L185-188 |
| [GameOverPanelController.cs](Assets/Scripts/UI/GameOverPanelController.cs) | 结算时拉 `KillCount/CurrentWave/MaxWave/SurvivalTime`；按钮调 `Restart()/ReturnToMenu()` | L61-98 |
| [UIManager.cs](Assets/Scripts/UI/UIManager.cs) | Start 时拉一次 `KillCount` 同步初始击杀显示 | L82-83 |

两个防御细节值得学：

1. EnemyWaveGenerator 的 Update 每帧先查 `GameManager.Instance.State != Playing` 就 return——
   因为死亡结算**不冻结 timeScale**，波次生成器若不显式检查会继续刷怪（暂停时反而不用查：
   timeScale=0 → deltaTime=0 → 天然不动）。同一行代码防住的是"不冻结"设计的副作用；
2. 所有调用方都先判 `GameManager.Instance != null`。甚至 TriggerVictory 还备了 null 兜底
   （`else GameEvents.RaiseGameVictory();`）——波次生成器在**没有 GameManager 的场景**（如测试关卡）也能独立工作。

### 2.4.4 编辑器操作：GameManager 落地（项目实测配置）

**第 1 步**：`SampleScene` 中新建空物体，命名 `GameManager`，挂上 `GameManager` 脚本。
（场景里已有名为 `GameManager` 的物体——证据：0.1.1 场景对象清单。）

**第 2 步**：Inspector 参数按项目实测值填写（用编辑器脚本读取本项目场景的 SerializedObject 得到）：

```text
gameplaySceneName = SampleScene
menuSceneName     = MainMenu
gameOverDelay     = 1.5
victoryDelay      = 1.0
```

**第 3 步**：`disableOnPause` 数组——项目实测共 8 项，来源物体的层级信息很有价值
（它其实是第 3 章玩家物体结构的预告）：

| # | 组件 | 所挂物体 |
|---|------|----------|
| 0 | PlayerController | Player |
| 1 | WeaponManager | Player |
| 2 | PlayerADSController | Player |
| 3 | PlayerCoverController | Player |
| 4 | **PlayerIKController** | **P08_Federica_Plain_C Variant（角色模型子物体）** |
| 5 | GrenadeThrower | Player |
| 6 | **WeaponSway** | **WeaponHolder（武器挂点子物体）** |
| 7 | **HeadBob** | **CameraTarget（相机目标子物体）** |

注意三个不在 Player 根上的组件：PlayerIKController 必须挂在 Animator 所在的模型物体上（类注释明说），
WeaponSway 挂武器挂点、HeadBob 挂相机目标——它们也属于"暂停/结算时要一并禁用的操控"。
**复现提示**：此刻你还没有玩家，数组先留空，等第 3、4 章把 Player 物体组装完回来填这 8 项——
真实项目也是阶段 5 才有 GameManager，数组在当时填上，顺序上完全一致。

**第 4 步（容易漏）**：`File → Build Settings`，把 `MainMenu` 和 `SampleScene` 两个场景拖入
Scene In Build 列表（勾选 Enabled）。`SceneManager.LoadScene("SampleScene")` 按名字加载的
前提是场景在 Build Settings 里——实测本项目两个场景均已登记。

## 2.5 骨架就位后的世界：新系统"零改动插入"

骨架是否合格，就看后来者接入时有多便宜。以 [AudioManager.cs](Assets/Scripts/Audio/AudioManager.cs)
为例——它整类**只听事件、不主动查询任何系统**（类注释："事件驱动播音，只听事件不主动查询"），
OnEnable 里一次性挂上 10 个订阅：

```csharp
// 摘自 AudioManager.cs 的 OnEnable（L59-68，10 个订阅一次挂完）
GameEvents.OnPlayerShot += HandlePlayerShot;
GameEvents.OnReloadStart += HandleReloadStart;
GameEvents.OnReloadEnd += HandleReloadEnd;
GameEvents.OnPlayerDamaged += HandlePlayerDamaged;
GameEvents.OnEnemyDamaged += HandleEnemyDamaged;
GameEvents.OnEnemyKilled += HandleEnemyKilled;
GameEvents.OnEnemyShot += HandleEnemyShot;
GameEvents.OnPickupTaken += HandlePickupTaken;
GameEvents.OnExplosion += HandleExplosion;
GameEvents.OnUIClick += HandleUIClick;
```

接入 AudioManager **不需要动 PlayerHealth、FirearmWeapon、Grenade、UI 里的任何一行代码**。
甚至它的音效资产至今为空（4B 挂起），事件链也照常运转、静默跳过——骨架的健壮性让"半成品"
也能安全挂在主干上。这就是本章四件套存在的意义。

## 【验证方法】

骨架没有画面，验证靠"编译 + 小实验 + 实测"。按顺序做：

1. **编译关**：创建完四个脚本后看 Console（`read_console`），零错误零警告才继续——AGENTS.md 强制规则第 1 条；
2. **GameEvents 自测**：建一个临时空物体挂下面这个脚本，进 Play 模式，Console 应打出**两条**
   日志（一条位置、一条计数），验证"广播→订阅"链路通；同时这个脚本示范了 2.1.3 的退订纪律
   （即使是用完即删的临时代码，也不写无法退订的 lambda 订阅）。
   **验证后删除该临时脚本与物体**（Unity 会自动清理 .meta）：

   ```csharp
   // 临时验证脚本（用完即删，不进仓库）
   using UnityEngine;
   public class BusSmokeTest : MonoBehaviour
   {
       void OnEnable()
       {
           GameEvents.OnEnemyKilled += OnKilled;
           GameEvents.OnKillCountChanged += OnKills;
       }
       void OnDisable()
       {
           GameEvents.OnEnemyKilled -= OnKilled;
           GameEvents.OnKillCountChanged -= OnKills;
       }
       void Start()
       {
           GameEvents.RaiseEnemyKilled(Vector3.zero);
           GameEvents.RaiseKillCountChanged(3);
       }
       void OnKilled(Vector3 p) => Debug.Log("[Bus] OnEnemyKilled @" + p);
       void OnKills(int c) => Debug.Log("[Bus] KillCount=" + c);
   }
   ```

3. **GameSettings 自测**：Play 模式下在临时脚本的 Start 里按此顺序写：

   ```csharp
   PlayerPrefs.SetFloat("Settings.Sensitivity", 4f);   // ① 先写盘
   GameSettings.ApplyAll();                            // ② 首次调用触发 Load，读到 4
   Debug.Log(GameSettings.Sensitivity);                // ③ 输出 4
   PlayerPrefs.SetFloat("Settings.Sensitivity", 6f);    // ④ 不重启再改盘
   GameSettings.ApplyAll();                            // ⑤ 再调用也读不到 6
   Debug.Log(GameSettings.Sensitivity);                // ⑥ 仍是 4 —— _loaded 守卫的效果
   ```

   顺序很重要：②之前从未访问过 GameSettings，`_loaded` 还是 false，Load 才会真正读盘；
   ⑤⑥则演示"只读一次"的缓存行为。想读到 6，重启 Play 模式即可。

4. **ObjectPool 自测**：临时脚本里对任意预制体（如 FX_BulletTrail）`Prewarm(p, 3)`，Hierarchy 出现
   `[ObjectPool]` 根且下面有 3 个 inactive 实例；`Get` 后 `Release`，再 `Get`——确认复用的是同一实例
   （GetInstanceID 相同）；
5. **GameManager 部分自测**：Build Settings 已登记两场景后进 Play，临时脚本调
   `GameManager.Instance.Pause()`，观察：Scene 视图停止、Inspector 里 State 变 Paused；
   再调 `Resume()` 还原。**完整行为**（结算面板、禁操控）要到第 3、4 章玩家与第 6 章 UI 就绪后才能验收——
   真实项目也是在阶段 5 的 S2/S3 任务时才完成这些验证（截图 `S2_pause_panel_migrated.png`、`S3_gameover_panel.png`）。

## 【常见坑】

1. **静态事件忘退订**：症状是场景重载后偶发 `MissingReferenceException` 或"幽灵调用"。规则零例外：
   `OnEnable` 加的每个 `+=` 都要有配对的 `OnDisable` `-=`。本项目连 Button 的 AddListener 都成对 RemoveListener（2.1.3）。
2. **改事件签名**：给 `OnEnemyKilled` 加个参数？全项目所有 Raise 调用点与订阅点全部编译错。
   这就是"只增不改签名"纪律的由来——需要新数据时新增一个事件（如 `OnSurfaceHit` 之于 4D），别改旧的。
3. **单例守卫漏写 Destroy**：场景里误放两个 GameManager，`Instance` 后到先至互相覆盖，暂停时一个禁用脚本一个不禁用。Awake 里"不是我就销毁自己"必须写全。
4. **WaitForSeconds 被 timeScale 冻结**：`WaitForSeconds` 用缩放时间。暂停（timeScale=0）期间协程纹丝不动——本章 GameManager 的结算协程之所以敢用，是因为死亡/胜利**不置 timeScale=0**。把 `WaitForSecondsRealtime`（不受缩放）和它的区别记进笔记，这是暂停系统第一大坑。
5. **DOTween 默认受 timeScale 影响**：暂停面板动画若不写 `SetUpdate(true)`，timeScale=0 时面板淡入根本不动。本项目 PauseMenuController 与 GameOverPanelController 的所有面板动画全部带 `SetUpdate(true)`（grep 可证）。
6. **PlayerPrefs 键名拼错**：不报错、只是静默读默认值。用 const、一处定义（2.2.2 的"考古地层"就是教训现场）。
7. **池化对象在 OnDisable 里搞事**：回池触发 OnDisable，在里面 Destroy/改状态 → 下次 Get 出来的是坏对象。池化对象只应在生命周期尽头调 `Release`（2.3.2 的约束）。
8. **Release 后继续引用实例**：你手里的引用在下次 Get 时可能已被发给别人。Release 之后当它不存在。
9. **场景没进 Build Settings**：`SceneManager.LoadScene("SampleScene")` 抛
   `Scene 'SampleScene' couldn't be loaded because it has not been added to the build settings`。第 2.4.4 第 4 步别跳过。
10. **把 GameManager 写成上帝对象**：它只管流程状态与流程数据（击杀数/波次/存活时间）。一旦开始在里面写"怎么移动""怎么开枪"，就违背了它存在的意义——它是状态机，不是脚本垃圾场。

---

# 第 3 章 · 玩家系统

## 【本章目标】

把"一个裸的胶囊体"变成"一个能跑能跳能瞄准能蹲伏、手持武器姿态自然的角色"。学完本章你将能：

1. 用 **Input System** 建立 13 个动作的输入映射，并理解"轮询式读取"封装层（PlayerInputHandler）的设计；
2. 用 **CharacterController** 实现带加减速平滑的走/跑/跳，且让动画步频与位移速度同步（消除滑步）；
3. 搭一套 Cinemachine 3 的 TPS 相机：角色转 Yaw、相机转 Pitch 的分离控制、肩部跟随、防穿墙；
4. 实现 ADS 瞄准（FOV + 贴枪 + 减速三联动）与切换式蹲伏；
5. 用**动画分层 + 左手 IK** 让角色自然握枪，换弹时左手自动让位；
6. 理解 HeadBob / WeaponSway 两个表现层脚本"各管一个通道、互不覆盖"的叠加算法。

本章是全书最大的一章（Player 目录 11 个脚本中的 10 个），按依赖顺序推进；
`GrenadeThrower` 留到第 4 章与手雷一起讲。

## 【前置知识】

- **CharacterController（角色控制器）**：Unity 内置的"运动学胶囊体"移动组件——不受物理力影响、靠 `Move()` 位移、自带 `isGrounded`。TPS 角色用它而非 Rigidbody（Rigidbody 会让角色被撞飞、站不稳，是 FPS/TPS 的经典反面选择）。
- **Input System 的三个层次**：`InputActionAsset`（动作映射资产，即 `Assets/Input/PlayerInput.inputactions`）→ `PlayerInput` 组件（把资产挂到物体上）→ 每个 `InputAction`（代码里按名字取出、读值）。
- **Action 的三种读取语义**：`ReadValue<T>()` 连续值（移动/视角）；`IsPressed()` 当前是否按住（射击/瞄准这类"持续状态"）；`WasPressedThisFrame()` 本帧刚按下（跳跃/换弹这类"单次触发"）。
- **Cinemachine 3 的两层结构**：`CinemachineBrain` 挂在真相机上（Main Camera）负责最终成像；`CinemachineCamera`（虚拟相机）定义跟随规则，通过 `CinemachineThirdPersonFollow` 等组件扩展行为。CM3 的所有组件都在 `Unity.Cinemachine` 命名空间下（第 1 章讲过与 2.x 的不兼容）。
- **动画分层（Layer）与 AvatarMask**：Animator 的层自上而下混合，上层可用遮罩只覆盖部分骨骼（如"上半身开枪"覆盖躯干手臂，不影响下半身跑动）。`UpperBodyMask.mask` 就是这种遮罩。
- **IK（Inverse Kinematics，反向运动学）**：正常动画是"根带动手"（正向），IK 反过来——把"手钉到指定点"，骨骼自动解算。Unity 在 `OnAnimatorIK` 回调里用 `SetIKPosition/Weight` 控制。
- **Blend Tree（混合树）**：把多个动画按参数连续混合（本项目用"前后/左右"两轴混合走路、平移、后退动画）。

## 3.0 Player 物体组装蓝图

动手前先看全貌。以下层级与组件分布是**实测**（读取 `Assets/Prefabs/Player.prefab` 的资产层级 + 场景中 Player 实例的组件清单）：

```text
Player  ←—— CharacterController(height=2, center=(0,1,0), radius=0.5)
   │        PlayerController / PlayerInputHandler / UnityEngine.InputSystem.PlayerInput
   │        PlayerHealth / WeaponManager（第4章）/ PlayerADSController / PlayerCoverController
   │        [场景实例另有：GrenadeThrower（第4章）]
   │
   ├── CameraTarget                        ←—— [场景实例有：HeadBob]
   │                                         （PlayerController 写它的 localRotation = 俯仰）
   │
   └── P08_Federica_Plain_C Variant        ←—— Animator(=PlayerAnimator.controller) + FootstepReceiver
                                             [场景实例另有：PlayerIKController]
       └── skeleton_P08/root/Hips/Spine/Chest/UpperChest/shoulder_R/Arm_R/forearm_R
           └── hand_R                       ←—— 右手骨骼：武器挂点的父级
               └── WeaponHolder             ←—— [场景实例有：WeaponSway]
                   └── (当前武器实例，第4章)
```

场景中另有一个独立的 `TPSCamera` 物体（不挂在 Player 下，3.3 节）。

**一个必须如实告诉你的实测发现**：`Player.prefab` 本体里**没有** PlayerIKController、WeaponSway、
HeadBob、GrenadeThrower——这四个组件是在**场景实例上追加的**（未 apply 回预制体）。
项目能正常跑（disableOnPause 引用的都是场景实例组件），但"prefab 与实例漂移"是协作隐患：
下次谁把 Player.prefab 拖进新场景，这四个组件就会缺席。
**复现建议**：直接把全部组件做进 Player.prefab 再拖入场景，避免复刻这个漂移。

**本章的时间线坐标**（截图与注释证据）：T1-1 移动、T1-2 相机（阶段 1）→ ADS/掩体（阶段 2"新增"注释）→
动画接入（阶段 3，截图 `PlayerAnimator_*`）→ WeaponSway/HeadBob（4F，截图 `4F_WeaponSway_HeadBob_play.png`）。
复现时按本章顺序即可一步到位。

## 3.1 输入层：inputactions + PlayerInputHandler

### 3.1.1 动作映射资产（编辑器操作）

在 `Assets/Input/` 创建 `PlayerInput.inputactions`（右键 Create → Input Actions），
建立名为 `Player` 的 Action Map，按下表添加 13 个动作与绑定（与项目实测一致）：

| 动作 | 类型 | 绑定 |
|------|------|------|
| Move | Value / Vector2 | WASD（2D 复合）+ 左摇杆 |
| Look | Value / Vector2 | `<Mouse>/delta` + 右摇杆 |
| Shoot | Button | 鼠标左键 |
| Aim | Button | 鼠标右键 |
| Reload | Button | R |
| Jump | Button | Space |
| Crouch | Button | C |
| Heal | Button | H |
| Pause | Button | Tab |
| SwitchWeapon1/2/3 | Button | 1 / 2 / 3 |
| Grenade | Button | G |

注意两件事：① 表里**没有 Sprint（冲刺）**——这个遗漏后来在代码里被"直读键盘"绕过（3.1.3），
如实说：这是本项目的真实历史层积，复现时你可以选择把 Sprint 正式加进资产；② 动作类型选 Value 还是 Button，
决定了代码里能调用哪些读取方法——选型原则见【前置知识】的三种语义。

然后在 Player 物体上挂 `PlayerInput` 组件，把 `.inputactions` 资产拖入，Default Map 设为 `Player`。

### 3.1.2 PlayerInputHandler：轮询式封装

[PlayerInputHandler.cs](Assets/Scripts/Player/PlayerInputHandler.cs)（175 行）是**所有输入的唯一出口**：
其他脚本一律向它要输入，绝不直接触碰 InputAction。核心是 `Awake` 里按名字取出 13 个 Action：

```csharp
// 摘自 PlayerInputHandler.cs 的 Awake（节选）
_playerInput = GetComponent<PlayerInput>();
...
_moveAction = _playerInput.actions.FindAction("Move");
_lookAction = _playerInput.actions.FindAction("Look");
_shootAction = _playerInput.actions.FindAction("Shoot");
...
// 确保 ActionMap 已启用（修复 PlayerInput 未自动激活的问题）
_playerInput.actions.FindActionMap("Player")?.Enable();
```

最后一行注释"修复 PlayerInput 未自动激活的问题"——又一个踩坑记录：Input System 在某些配置下
ActionMap 不随 Play 自动启用，读到的输入全是零。显式 `Enable()` 一次根除。

对外接口按语义分三类，调用方看名字就知道行为：

```csharp
// 摘自 PlayerInputHandler.cs（节选）
public Vector2 GetMoveInput()   => _moveAction != null ? _moveAction.ReadValue<Vector2>() : Vector2.zero;
public bool IsShootPressed()    => _shootAction != null && _shootAction.IsPressed();            // 按住=连发
public bool IsReloadPressed()   => _reloadAction != null && _reloadAction.WasPressedThisFrame(); // 单次
```

为什么封装一层？**三个真实收益**（都发生在本项目）：① 消费者（PlayerController 等 8 个脚本）
不关心设备与绑定——改键不改代码；② 统一加防御（`!= null` 守卫 + 兜底值），任何一环没配都不炸；
③ 可以在出口处加**全局规则**——最典型的是 `GetLookDelta`：

```csharp
// 摘自 PlayerInputHandler.cs 的 GetLookDelta
public Vector2 GetLookDelta()
{
    // 窗口失去焦点（鼠标移出游戏窗口/被按 Esc 释放）时忽略视角旋转
    if (!Application.isFocused) return Vector2.zero;
    // Editor 中额外检测：鼠标不在 Game 视图内时不转视角
    // （Application.isFocused 在 Editor 里恒为 true，鼠标移到 Hierarchy/Console/Inspector 时仍会驱动视角）
    if (!IsMouseInGameView()) return Vector2.zero;
    return _lookAction != null ? _lookAction.ReadValue<Vector2>() : Vector2.zero;
}
```

`IsMouseInGameView()` 是全项目最"黑客"的一段：`#if UNITY_EDITOR` 下用
`Resources.FindObjectsOfTypeAll<EditorWindow>()` 找到 GameView 窗口的屏幕矩形，
判断鼠标是否在 Game 视图内。它解决的是编辑器工作的真实痛点——
你正调 Inspector，角色却因为 `<Mouse>/delta` 还在生效而疯狂转视角。
**一旦输入出口统一，这种规则只需写一处**；如果每个脚本都自己 `ReadValue`，这个补丁要打八处。

### 3.1.3 一处例外：IsSprintHeld 直读键盘

```csharp
// 摘自 PlayerInputHandler.cs
/// <summary>冲刺键是否按住（直接检测键盘，不依赖 InputActionAsset）</summary>
public bool IsSprintHeld()
{
    return Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed;
}
```

为什么唯独冲刺绕开资产？回看 3.1.1 的表——Sprint 根本没在 inputactions 里定义。
这是历史层积：冲刺功能大概率是后加的，加的人选择了最短路径直读 `Keyboard.current`。
代价：手柄玩家无法冲刺。**如实记录，作为你复现时的改进点**（把 Sprint 加进资产，两行代码改回正轨）。

## 3.2 PlayerController：移动、重力与视角

[PlayerController.cs](Assets/Scripts/Player/PlayerController.cs)（195 行）是玩家系统的"主板"：
同物体兄弟组件几乎都通过它交换信息（`CurrentHealth` / `HorizontalSpeed` / `IsGrounded` 三个公开只读属性，
分别供 ADS/掩体做存活检查、HeadBob 做幅度、HeadBob 做地面判断）。

### 3.2.1 Update 四步与"单次 Move"的坑

```csharp
// 摘自 PlayerController.cs 的 Update
private void Update()
{
    HandleMovement();     // ① 算水平速度（含 ADS/蹲伏减速）
    HandleGravity();      // ② 算垂直速度（跳跃/重力）
    ApplyCombinedMove();  // ③ 一次性提交给 CharacterController
    HandleLook();         // ④ 视角
}
```

第③步是踩坑后固化下来的写法，注释原文值得抄录：

```csharp
// 摘自 PlayerController.cs 的 ApplyCombinedMove
/// <summary>水平速度+垂直速度合并为单次 Move：多次 Move 会导致 velocity 只计最后一次，动画取不到水平速度</summary>
private void ApplyCombinedMove()
{
    if (_characterController == null) return;
    _characterController.Move((_horizontalVelocity + new Vector3(0f, _verticalVelocity, 0f)) * Time.deltaTime);
}
```

CharacterController 每次 `Move` 都会覆盖内部 velocity 缓存——先水平后垂直分两次 Move，
`cc.velocity` 只剩垂直分量，LateUpdate 里驱动动画的速度就是错的。**一次 Move 提交合成速度**，
既保住了物理正确，又保住了下游消费者的数据。这类"注释里带原因的坑"是本项目代码的可贵之处。

### 3.2.2 水平移动：目标速度 → 加减速逼近

```csharp
// 摘自 PlayerController.cs 的 HandleMovement（节选）
Vector2 moveInput = _inputHandler.GetMoveInput();
Vector3 moveDirection = transform.TransformDirection(new Vector3(moveInput.x, 0f, moveInput.y));

_targetSpeedValid = moveInput.magnitude > 0.1f;
if (_targetSpeedValid)
{
    // TPS 设计：只有向前移动时才允许冲刺加速（后退/横移不加速）
    bool isForward = moveInput.y > 0.3f;
    _targetSpeed = (_inputHandler.IsSprintHeld() && isForward) ? sprintSpeed : walkSpeed;

    // ADS 减速（阶段2）
    if (_adsController != null)
        _targetSpeed *= 1f - _adsController.ADSProgress * (1f - _adsController.adsMoveSpeedMultiplier);
    // 掩体/蹲伏减速（阶段2）
    if (_coverController != null && _coverController.IsCrouching)
        _targetSpeed *= _coverController.crouchSpeedMultiplier;
}

// 目标速度向量（无输入时归零）
Vector3 targetVelocity = moveInput.magnitude > 0.1f
    ? moveDirection.normalized * _targetSpeed
    : Vector3.zero;

// 速度平滑：用加速度/减速度向目标速度逼近，避免瞬间启停造成滑步
float maxDelta = (targetVelocity.sqrMagnitude >= _horizontalVelocity.sqrMagnitude
    ? acceleration : deceleration) * Time.deltaTime;
_horizontalVelocity = Vector3.MoveTowards(_horizontalVelocity, targetVelocity, maxDelta);
```

三个设计点：

1. **ADS 减速是连续的**：乘的是 `1 - ADSProgress × (1 - 倍率)`——瞄准过渡进行到一半，减速也只生效一半。
   蹲伏是二值的（`IsCrouching` 直接乘 0.35），因为蹲伏本身是开关式状态；
2. **冲刺门禁**（`moveInput.y > 0.3f`）：TPS 传统——倒退/横移不开全速，防止"倒着跑比敌人追得还快"的滑稽感；
3. **MoveTowards 双速率平滑**：加速 14 m/s²、减速 18 m/s²——急停略快于起步，符合"操作响应灵敏"的手感直觉。
   没有这个平滑，按 W 瞬间 3 m/s、松 W 瞬间 0——动画永远追不上，满屏滑步。

另外注意死亡守卫：`HandleMovement` 与 `HandleLook` 开头都有
`if (_playerHealth != null && _playerHealth.CurrentHealth <= 0f) return;`——
死亡后由动画（Died Trigger）接管身体，控制器立即停止响应。

### 3.2.3 重力与跳跃：让动画精确匹配空中时间

```csharp
// 摘自 PlayerController.cs 的 HandleGravity（节选）
if (_characterController.isGrounded)
    _verticalVelocity = -2f;                       // 贴地恒定小下压：保证 isGrounded 稳定为真

if (_inputHandler != null && _inputHandler.IsJumpPressed() && _characterController.isGrounded)
{
    _verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);

    // 动态计算动画播放速度：让完整跳跃动画精确匹配实际空中时间
    float animDuration = 0.6f; // rifle jump.fbx 时长（18帧/30fps）
    float airTime = 2f * Mathf.Sqrt(2f * jumpHeight / Mathf.Abs(gravity));
    float jumpSpeed = animDuration / airTime;
    if (_animator != null)
    {
        _animator.SetFloat("JumpSpeed", jumpSpeed);
        _animator.SetTrigger("Jump");
    }
}
_verticalVelocity += gravity * Time.deltaTime;
```

跳跃初速度用标准公式 `√(h·-2g)`（由 `jumpHeight` 反推离地速度）。妙处在后面：
物理空中时间 `2√(2h/g)` 与动画时长（0.6 秒）通常**对不上**——要么动画播完人还悬着，
要么人落地了动画还剩半截。本项目的解法是给 `JumpSpeed`（动画倍速）赋值
`动画时长 / 实际空中时间`：改 `jumpHeight` 参数，动画播放速度自动重新匹配。
**用数据流把两个独立系统（物理与动画）锁在一起**，这个手法值得抄进你的工具箱。

### 3.2.4 视角：Yaw 与 Pitch 分离控制

```csharp
// 摘自 PlayerController.cs 的 HandleLook
private void HandleLook()
{
    if (_playerHealth != null && _playerHealth.CurrentHealth <= 0f) return;
    if (_inputHandler == null || _cameraTarget == null) return;

    Vector2 lookDelta = _inputHandler.GetLookDelta();
    transform.Rotate(0f, lookDelta.x * mouseSensitivity, 0f);   // Yaw：转角色本体

    _cameraPitch -= lookDelta.y * mouseSensitivity;
    _cameraPitch = Mathf.Clamp(_cameraPitch, pitchMin, pitchMax);  // -30° ~ +60°
    _cameraTarget.localRotation = Quaternion.Euler(_cameraPitch, 0f, 0f);  // Pitch：转相机目标
}
```

这是 TPS 的标准视角结构，也是理解整个相机系统的钥匙：

- **水平（Yaw）转角色本体**——相机跟着角色转，武器射向与移动方向一致；
- **俯仰（Pitch）不转角色**（人不能仰面朝天跑），只转 `CameraTarget`（一个空物体子节点）的 localRotation；
- Cinemachine 相机只负责"跟随 CameraTarget"（3.3 节），完全不知道俯仰的存在；
- 俯仰被钳制在 -30°～+60°（抬头少、低头多——俯瞰战场的需要）。

`ApplyRecoil(float amount)` 是同一通道的公共入口：射击后坐力直接加到 `_cameraPitch` 上
（火线抬升），第 4 章 FirearmWeapon 会调用它。

### 3.2.5 LateUpdate：动画驱动与"步频同步"

```csharp
// 摘自 PlayerController.cs 的 LateUpdate（节选）
private void LateUpdate()
{
    if (_animator == null) return;
    Vector3 localVelocity = transform.InverseTransformDirection(_horizontalVelocity); // 世界→角色本地
    _animator.SetFloat("Speed", _horizontalVelocity.magnitude);
    float normSpeed = _targetSpeed > 0.01f ? _targetSpeed : walkSpeed;
    _animator.SetFloat("ForwardSpeed", localVelocity.z / normSpeed);   // 归一化 -1~1 匹配 BlendTree
    _animator.SetFloat("StrafeSpeed", localVelocity.x / normSpeed);

    bool wantRun = _inputHandler != null && _inputHandler.IsSprintHeld() && localVelocity.z > 0.3f;
    if (_coverController != null && _coverController.IsCrouching) wantRun = false;
    _animator.SetBool("IsRun", wantRun);

    // 动画速度因子：实际速度 / 目标速度，让腿的步频与位移同步，消除起步/急停滑步
    if (_targetSpeedValid && _targetSpeed > 0.01f)
        _animator.SetFloat("AnimSpeed", Mathf.Clamp(_horizontalVelocity.magnitude / _targetSpeed, animSpeedMin, animSpeedMax));
    else
        _animator.SetFloat("AnimSpeed", 1f);

    _animator.SetBool("Grounded", _characterController.isGrounded);
}
```

为什么在 LateUpdate 而不是 Update？——**Update 算完速度、当帧就要驱动动画**，晚于其他脚本的
Update 正好保证读到的是最终速度（同帧内 LateUpdate 全部跑在所有 Update 之后）。

`AnimSpeed` 这一段是"滑步问题"的完整解法，值得细品：加减速平滑（3.2.2）让速度是渐变的，
但动画片段的步频是按"标称速度"录制的——起步瞬间实际 0.5 m/s 而动画按 3 m/s 迈腿，
看起来就是脚下打滑。把 `实际速度/目标速度` 作为动画倍速（钳制在 0.5~1.3），
腿的频率被实时对齐到位移速度。`Screenshots/Fix_AnimDiag_*` 系列截图记录了这一整轮动画诊断修复。

### 3.2.6 编辑器操作清单（PlayerController 部分)

1. Player 物体挂 `CharacterController`（实测：height=2、center=(0,1,0)、radius=0.5）；
2. 挂 `PlayerController`，Inspector 默认值即代码默认值（walkSpeed=3、sprintSpeed=6、
   acceleration=14、deceleration=18、pitchMin=-30、pitchMax=60 等）；
3. 建空子物体 `CameraTarget`（Inspector 实测只有 Transform；localPosition=(0, 1.6, 0)——身高 2 的肩头位置，即相机跟随点）；
4. `Start` 里的 `transform.Find("CameraTarget")` 按名字查找——**名字必须一字不差**；
5. 模型子物体的 Animator 上勾选/不勾选 Root Motion 均可——代码里强制
   `_animator.applyRootMotion = false`（移动唯一权威是 CharacterController）。

## 3.3 TPS 相机：Cinemachine 3 实测配置

场景中相机是**两层结构**（MCP 实测）：

```text
Main Camera   : Camera + CinemachineBrain + AudioListener + UniversalAdditionalCameraData
TPSCamera     : CinemachineCamera + CinemachineThirdPersonFollow
                + CinemachineBasicMultiChannelPerlin + CinemachineDeoccluder + CameraShaker
                Follow = Player/CameraTarget，Lens FOV = 55
```

逐个组件说清楚"它负责什么"：

| 组件 | 职责 | 本项目里的对应物 |
|------|------|------------------|
| CinemachineBrain（挂在 Main Camera） | 每帧取当前虚拟相机的位姿成像 | 唯一真相机，不直接控制 |
| CinemachineCamera | 虚拟相机主体：Follow 谁、Lens（FOV） | Follow=CameraTarget；FOV 由 PlayerADSController 每帧写 |
| CinemachineThirdPersonFollow | TPS 肩部跟随：肩部偏移、距离、阻尼 | 相机悬在角色侧后方的效果来源 |
| CinemachineBasicMultiChannelPerlin | Perlin 噪声震屏 | 由 [CameraShaker.cs](Assets/Scripts/Player/CameraShaker.cs) 驱动幅度（第 2 章 2.3.3 已讲震屏链路） |
| CinemachineDeoccluder | 相机防穿墙：射线被遮挡时把相机拉近 | 截图 `deoccluder_open_field.png` 记录过它引发的开阔地视角问题的排查 |

**为什么这套结构里 PlayerController 不需要知道相机存在**——回顾 3.2.4：角色转 Yaw、
CameraTarget 转 Pitch，相机只是 CameraTarget 的"跟随者"。解耦效果：换相机（肩射/瞄准镜/死亡镜头）
不用动 PlayerController 一行。

编辑器操作：建空物体 TPSCamera → Add Component 搜 `Cinemachine Camera` →
Follow 拖入 Player/CameraTarget → 加 `Cinemachine Third Person Follow`（调肩部偏移与距离）→
加 `Cinemachine Deoccluder`（默认参数即可）→ 加 `Cinemachine Basic Multi Channel Perlin`
（AmplitudeGain 初始 0，由代码控制）→ 把 CameraShaker 脚本也挂这里。
Main Camera 上加 `Cinemachine Brain`（装 Cinemachine 包后自动出现的组件）。

## 3.4 PlayerHealth（上章已详述）

[PlayerHealth.cs](Assets/Scripts/Player/PlayerHealth.cs) 挂在 Player 根上：血量、扣血、
消耗式医疗包（H 键触发 `TryHeal()`，由 UIManager 调用）、死亡时
`_animator.SetTrigger("Died")` + `GameEvents.RaisePlayerDeath()`。
事件通道设计（实例事件 vs 总线）在第 2 章 2.1.4 已完整拆解，此处不重复。

## 3.5 PlayerADSController：瞄准三联动

[PlayerADSController.cs](Assets/Scripts/Player/PlayerADSController.cs)（115 行）管理"右键瞄准"时
同时发生的三件事：**FOV 收窄、武器贴枪、移动减速**。减速已经在 3.2.2 由 PlayerController 侧实现
（读 `ADSProgress`），所以本类只管前两件 + 广播进度：

```csharp
// 摘自 PlayerADSController.cs 的 Update
bool wantADS = _inputHandler.IsAimHeld();
float target = wantADS ? 1f : 0f;
_adsProgress = Mathf.MoveTowards(_adsProgress, target, adsTransitionSpeed * Time.deltaTime);  // 线性过渡

// 平滑过渡 FOV
if (_cinemachineCamera != null)
    _cinemachineCamera.Lens.FieldOfView = Mathf.Lerp(defaultFOV, adsFOV, _adsProgress);

// 武器贴枪：ADS 时武器从默认挂点向屏幕中线/眼线偏移
if (_weaponHolder != null)
{
    Vector3 adsTarget = _defaultWeaponPosition + adsPositionOffset;
    Quaternion adsRotTarget = _defaultWeaponRotation * Quaternion.Euler(adsRotationOffset);
    _weaponHolder.localPosition = Vector3.Lerp(_defaultWeaponPosition, adsTarget, _adsProgress);
    _weaponHolder.localRotation = Quaternion.Slerp(_defaultWeaponRotation, adsRotTarget, _adsProgress);
}

if (_animator != null)
    _animator.SetBool("IsAiming", _adsProgress > 0.5f);   // Aiming 动画层
```

`_adsProgress` 是本章的"枢纽变量"——它同时驱动：相机 FOV（本类）、武器位置（本类）、
移动减速（PlayerController）、HeadBob 幅度衰减（3.8.1）、WeaponSway 收窄（3.8.2）、
动画 IsAiming（本类）。**一个标量描述整个瞄准状态，多个系统各自订阅式读取**——
这就是"属性即接口"的好处，不需要为 ADS 发任何事件。

Start 里两段"防御性获取引用"的注释充满了踩坑史的味道，值得整体读一遍：

```csharp
// 摘自 PlayerADSController.cs 的 Start（节选）
[Header("相机引用（场景中拖入 TPSCamera，F4：替代字符串查找）")]
[SerializeField] private CinemachineCamera tpsCamera;

[Header("武器挂点引用（场景中拖入 WeaponHolder；实际挂在 hand_R 骨骼下，直接子级查找会落空）")]
[SerializeField] private Transform weaponHolder;
```

- **F4 修复**：早期版本用 `GameObject.Find("TPSCamera")` 字符串查找，改成了序列化引用拖入
  （找不到时兜底按名查找一次 + `LogWarning`——注意是警告不是报错，缺引用只损失 FOV 过渡不炸游戏）；
- **hand_R 骨骼下的 WeaponHolder**：`transform.Find` 只查**直接子级**，而 WeaponHolder 深埋在
  `…/forearm_R/hand_R/WeaponHolder`——注释原话"直接子级查找会静默落空，导致 ADS 贴枪整体失效"。
  兜底方案是 `GetComponentsInChildren<Transform>(true)` 递归找名字匹配。

编辑器操作：在 Player（根物体）挂 PlayerADSController；把场景 TPSCamera（CinemachineCamera 组件）
拖入 tpsCamera 槽；把 hand_R 下的 WeaponHolder 拖入 weaponHolder 槽。
参数默认值：defaultFOV=55、adsFOV=40、adsTransitionSpeed=10、adsMoveSpeedMultiplier=0.5、
adsPositionOffset=(-0.12, 0.45, 0.15)（注释："抬至眼线并对齐屏幕中线"——这三个数是拿准星对枪膛**手工校**出来的）。

还有一个容易被忽略的细节：**主菜单的 FOV 设置实际由本类应用**——
`Start` 里 `defaultFOV = GameSettings.FOV; _cinemachineCamera.Lens.FieldOfView = defaultFOV;`。
第 2 章说过 GameSettings.ApplyAll 只管音量，FOV 的"应用到世界"发生在 ADS 控制器拿到相机的这一刻。

## 3.6 PlayerCoverController：切换式蹲伏

[PlayerCoverController.cs](Assets/Scripts/Player/PlayerCoverController.cs)（66 行）是本章最短的控制器，核心全在 Update：

```csharp
// 摘自 PlayerCoverController.cs 的 Update
// 切换式蹲伏：按一下蹲下，再按一下站起（不要求一直按住）
if (_inputHandler.IsCrouchPressed())
    _isCrouching = !_isCrouching;

_targetHeight = _isCrouching ? crouchHeight : _standHeight;

// 平滑过渡高度和中心
float newHeight = Mathf.Lerp(_characterController.height, _targetHeight, crouchTransitionSpeed * Time.deltaTime);
_characterController.height = newHeight;

Vector3 targetCenter = _isCrouching ? _crouchCenter : _standCenter;
_characterController.center = Vector3.Lerp(_characterController.center, targetCenter, crouchTransitionSpeed * Time.deltaTime);

if (_animator != null)
    _animator.SetBool("IsCrouch", _isCrouching);
```

两个细节：

1. **切换式而非按住式**——用 `IsCrouchPressed()`（本帧按下）翻转布尔。
   PlayerInputHandler 同时提供了 `IsCrouchHeld()`（按住），两个方法就是为这两种蹲伏语义准备的，
   最终选择了切换式（战术射击惯例：蹲下后可以松开手继续操作）；
2. **center 必须跟着 height 走**：Start 里 `_crouchCenter = new Vector3(_standCenter.x, crouchHeight * 0.5f, _standCenter.z)`——
   胶囊体的 center 是从物体原点算的，height 缩到 1 而 center 还停在 1，胶囊体就悬空在膝盖位置、
   `isGrounded` 变 false、跳跃和 HeadBob 全部失灵。**center.y = 新高度的一半** 保证脚底始终贴地。

代码默认参数：crouchHeight=1（正好是站立 2 的一半）、crouchSpeedMultiplier=0.35
（注释给出换算：蹲走≈1.05 m/s）、crouchTransitionSpeed=15。
移动减速在 PlayerController 侧乘算（3.2.2），动画切换在 Base Layer 的 Crouch 状态（IsCrouch 参数）。

## 3.7 动画分层与 PlayerIKController：持枪的手

### 3.7.1 动画控制器实测：5 层 16 参数

`Assets/AnimControllers/PlayerAnimator.controller` 是玩家动画的中枢（MCP 实测）：

| 层 | 状态 | 覆盖范围 |
|----|------|----------|
| Base Layer | Idle / Walking Locomotion / Running Locomotion / Jump / Crouch / Die | 全身：位移、跳跃、蹲、死 |
| Aiming | Empty / Aiming | 全身瞄准姿态（IsAiming） |
| Reload | Empty / Reload | 全身换弹（Reload Trigger，第 4 章用） |
| ChangeWeapon | Empty / WeaponEquip | 全身切枪（ChangeWeapon Trigger，第 4 章用） |
| UpperBody | Empty / Fire_Upper / HitReaction / Toss | **只有上半身**（AvatarMask：`UpperBodyMask.mask`） |

16 个参数全部由代码驱动（PlayerController 8 个：Speed/ForwardSpeed/StrafeSpeed/IsRun/AnimSpeed/Grounded/Jump/JumpSpeed；
PlayerHealth 2 个：Hit/Died；PlayerADSController 1 个：IsAiming；PlayerCoverController 1 个：IsCrouch；
第 4 章武器脚本 3 个：Fire/Reload/ChangeWeapon；GrenadeThrower 1 个：Toss），对照表就是
3.2.5 / 3.5 / 3.6 里出现过的 `SetFloat/SetBool/SetTrigger` 调用。

UpperBody 层是这套结构的精华：**开枪后坐（Fire_Upper）、受击（HitReaction）、投雷（Toss）
都只覆盖躯干和手臂**——跑步中开枪时，腿继续按跑步动画循环，上身独立播放射击动作。
没有分层，每次开枪全身动画都要打断，"边跑边打"就不存在了。

### 3.7.2 PlayerIKController：左手贴护木

武器挂在 `hand_R`（右手骨骼）下，右手跟随武器是"父子关系自然成立"；左手则要靠 IK 吸附到
武器护木上的挂点 `LeftHandIk`（武器预制体上的子物体，第 4 章组装）。[PlayerIKController.cs](Assets/Scripts/Player/PlayerIKController.cs)（69 行）：

```csharp
// 摘自 PlayerIKController.cs 的 OnAnimatorIK
private void OnAnimatorIK(int layerIndex)
{
    IkCallCount++;
    if (_animator == null) return;

    // 换弹时左手让位给换弹动画
    var weapon = transform.root.GetComponentInChildren<FirearmWeapon>();
    bool isReloading = weapon != null && weapon.IsReloading;
    float leftWeight = isReloading ? 0f : 1f;

    if (_leftHandIk != null)
    {
        _animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, leftWeight);
        _animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0f);   // 旋转保留动画自然朝向，避免手部扭曲
        if (leftWeight > 0f)
            _animator.SetIKPosition(AvatarIKGoal.LeftHand, _leftHandIk.position);
    }
    else
    {
        _animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0f);
        _animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0f);
    }
}
```

四个技术决策，每个都对应一个新手坑：

1. **挂在 Animator 所在物体上**（类注释原话："此脚本必须挂在 Animator 所在的 GameObject 上——
   OnAnimatorIK 由该对象的 Animator 触发"）。挂在 Player 根上则回调永远不来，IK 静默失效；
2. **位置权重 1、旋转权重 0**：手"贴到"护木即可，手腕朝向交给动画原样保留——
   强行写旋转权重会让手腕以物理上正确、视觉上扭曲的角度折过去；
3. **换弹让位**：换弹动画要左手抓弹匣，IK 权重归零把左手还给动画；
4. **引用失效自愈**：切枪后武器实例换了，`_leftHandIk` 指向旧对象——Update 里
   `if (_leftHandIk == null) RefreshWeaponRefs();` 每次切枪后自动找回新武器的挂点
   （Unity 假 null 特性：旧实例销毁后 `== null` 判定成立）。

两处如实记录：`_reloadLayerIndex`（`GetLayerIndex("Reload")` 的缓存）**已赋值但从未参与判断**——
实际的换弹让位走的是 `weapon.IsReloading` 而非层索引，这个字段是未用代码；
`public static int IkCallCount` 是调试 IK 时的诊断计数器（统计 OnAnimatorIK 是否被调用），留在了代码里。

## 3.8 表现层四件套：各管一个通道

### 3.8.1 HeadBob：只改 localPosition

[HeadBob.cs](Assets/Scripts/Player/HeadBob.cs) 挂在 CameraTarget 上（Cinemachine 跟随目标），
产生走路时镜头的自然起伏。类注释第一句就点明架构：

```csharp
// 摘自 HeadBob.cs 类注释
/// 挂载在 Player/CameraTarget 上（Cinemachine 跟随目标）
/// 只改 localPosition：PlayerController 每帧写入 CameraTarget 的 localRotation（俯仰视角），
/// 两者各管一个通道，不与 Cinemachine 相机控制打架
```

**通道分离**是本项目反复出现的模式：CameraTarget 这个物体的 localRotation 归 PlayerController（俯仰）、
localPosition 归 HeadBob（起伏）、世界位置归 PlayerController 移动。三个写入者互不碰面、永不打架。

起伏波形的核心（LateUpdate 节选）：

```csharp
// 摘自 HeadBob.cs 的 LateUpdate（节选）
// 幅度随实际速度缩放（走小跑大），蹲伏/ADS 再乘衰减倍率
float amplitude = _bobbingAmount * Mathf.Clamp01(speed / _playerController.walkSpeed);
if (_coverController != null && _coverController.IsCrouching) amplitude *= _crouchMultiplier;
if (_adsController != null) amplitude *= Mathf.Lerp(1f, _adsMultiplier, _adsController.ADSProgress);

// 垂直弹跳为步频（2 倍频正弦），左右重心横摆为半频
targetOffset = new Vector3(
    Mathf.Sin(_timer) * amplitude * _lateralMultiplier,
    Mathf.Sin(_timer * 2f) * amplitude,
    0f);
```

物理直觉：人走路每一步重心垂直弹一下（步频），左右摆的重心晃是**两步一个来回**（半频）——
所以垂直是 `Sin(2t)`、横向是 `Sin(t)`。停止移动时 `SmoothDamp` 平滑归位，而不是瞬间定格。
幅度链：`基础幅度 × 速度比 × 蹲伏0.5 × ADS渐变0.4`——ADS 瞄准时镜头最稳，符合射击手感。

### 3.8.2 WeaponSway：基准 + 附加的叠加算法

[WeaponSway.cs](Assets/Scripts/Player/WeaponSway.cs)（91 行）挂在 WeaponHolder 上，
让武器"落后于视角"产生拖拽感（4F 阶段产物）。它要解的**架构难题**是：
ADS 控制器每帧在 Update 里写 WeaponHolder 的位置（贴枪基准），本脚本也要写位置（摇摆附加量）——
两个写入者同写一个 Transform，通常结局是互相覆盖或越叠越歪。项目注释记录的解法：

```csharp
// 摘自 WeaponSway.cs 类注释
/// 叠加规则：PlayerADSController 在 Update 写入「贴枪基准」局部位置/旋转，
/// 本脚本在 LateUpdate 用「基准 + 附加偏移」方式叠加并记录本帧附加量，
/// 下一帧先减去上帧附加量还原基准，因此与 ADS 互不覆盖、也不累积漂移
```

代码骨架（LateUpdate）：

```csharp
// 摘自 WeaponSway.cs 的 LateUpdate（节选）
// 还原本帧基准：当前局部值减去上一帧的附加量 = ADS/动画写入的原始值
Vector3 basePosition = transform.localPosition - _lastAppliedPositionDelta;
Quaternion baseRotation = Quaternion.Inverse(_lastAppliedRotationDelta) * transform.localRotation;

... // 按鼠标增量算出本帧目标附加量（位置/旋转），SmoothDamp 平滑，含 ADS 收窄

// 挂点父级是手骨（任意朝向+缩放），先把偏移量转换到挂点局部空间再写入
Vector3 worldPositionDelta = playerRight * _positionOffset.x + Vector3.up * _positionOffset.y;
Vector3 localPositionDelta = parent.worldToLocalMatrix.MultiplyVector(worldPositionDelta);
...
transform.localPosition = basePosition + localPositionDelta;
transform.localRotation = localRotationDelta * baseRotation;
```

三个层次的聪明：① **还原基准再叠加**（加减法互逆，永不累积漂移）；② **世界方向 → 骨骼局部**的转换——
挂点的父级是 hand_R 手骨（任意奇怪朝向），直接把偏移量塞 localPosition 会沿骨骼轴向歪斜，
必须按"玩家右轴/世界上轴"构造世界偏移再 `worldToLocalMatrix` 转入局部，鼠标左右移动才是"屏幕上的左右"；
③ **旋转增量以世界轴构造再共轭到局部**（`Inverse(parentRot) × world × parentRot`）——同理。

ADS 收窄：`swayScale = Lerp(1, 0.3, ADSProgress)`——瞄准越深、摇摆越小（瞄准镜不乱晃）。
死亡归零（与 ADS 控制器一致的存活检查）。

### 3.8.3 CameraShaker 与 FootstepReceiver

[CameraShaker.cs](Assets/Scripts/Player/CameraShaker.cs) 全文已在第 2 章 2.3.3 拆过（Perlin 噪声驱动、
幅度指数衰减钳零、`Shake` 取历史最大值防连爆互相覆盖）。它是"挂在 TPSCamera 上"的第五个组件。

[FootstepReceiver.cs](Assets/Scripts/Player/FootstepReceiver.cs)（31 行）的存在理由是一次 Console 报错：
动画片段（Mixamo/BSP/TPS 多来源）自带 `PlayFootstepSound`、`FinishedReloading` 等动画事件，
场景里没有同名公共方法就会每次播动画都刷"事件无接收者"错误。解法是挂在 Animator 物体上做接收器：
脚步事件真的播声（可选 AudioSource），其余四个（换弹/切枪系列）是**空方法**——
注释原话："换弹完成由 FirearmWeapon.ReloadRoutine 控制，此处忽略"。
**空接收器模式**：不使用事件但必须接住它，防止报错噪音淹没真报错。

## 3.9 组装清单与回填

把本章所有编辑器操作汇总成一张从零组装清单（顺序即依赖顺序）：

| # | 操作 | 位置 |
|---|------|------|
| 1 | 建 inputactions（13 动作）+ 挂 PlayerInput 组件 | `Assets/Input/` / Player 根 |
| 2 | 挂 CharacterController（h=2, center=(0,1,0), r=0.5）与 PlayerController | Player 根 |
| 3 | 建子物体 CameraTarget | Player 下 |
| 4 | 挂 PlayerInputHandler / PlayerHealth / PlayerADSController / PlayerCoverController | Player 根 |
| 5 | 导入角色模型（P08_Federica），设 Animator 的 Controller = PlayerAnimator.controller，挂 FootstepReceiver | 模型子物体 |
| 6 | 在 hand_R 下建 WeaponHolder 空物体 | 骨骼深处 |
| 7 | 场景建 TPSCamera（CinemachineCamera + ThirdPersonFollow + Deoccluder + Perlin + CameraShaker），Follow=CameraTarget；Main Camera 挂 CinemachineBrain | 场景级 |
| 8 | ADS 控制器 Inspector：拖入 tpsCamera 与 weaponHolder 引用 | Player 根 |
| 9 | 模型物体挂 PlayerIKController；WeaponHolder 挂 WeaponSway；CameraTarget 挂 HeadBob | 3.0 实测位置 |
| 10 | **回填** 2.4.4 的 disableOnPause 数组（本章产出 6 项：PlayerController/PlayerADSController/PlayerCoverController/PlayerIKController/WeaponSway/HeadBob；其余 2 项 WeaponManager、GrenadeThrower 属于第 4 章，届时补齐 8 项） | GameManager |
| 11 | 把 Player 拖成 `Assets/Prefabs/Player.prefab`（**连同第 9 步的三个组件一起**，避免 3.0 说的 prefab 漂移） | Assets/Prefabs |

## 【验证方法】

按本章顺序逐段验证（对照 `Assets/Screenshots/` 里的历史验证截图）：

1. **输入与移动**（对照 `T1-1_PlayerController_PlayMode.png`）：Play 后 WASD 移动、
   Shift+W 冲刺、Space 跳跃；松开按键后角色应**渐停**而非急停（3.2.2 的平滑）；
2. **视角**（对照 `T1-2_TPS_Camera_PlayMode.png`）：鼠标转视角，角色随 Yaw 转身、
   相机俯仰有 -30°/+60° 钳制；编辑器里把鼠标移到 Inspector 上，**视角不应旋转**（3.1.2 的 IsMouseInGameView）；
3. **动画同步**（对照 `Fix_WalkAnim_Verified.png`、`Fix_AnimDiag_*`）：走路/跑步/平移动画随方向混合，
   起步与急停**无滑步**（AnimSpeed 因子生效）；跳跃动画落地瞬间正好播完（JumpSpeed 匹配）；
4. **ADS**（对照 `Fix_GunAlign_ADS.png`）：按住右键——FOV 从 55 收到 40、武器抬起对准屏幕中线、
   移动变慢、动画切 Aiming 层；松开全部平滑还原；
5. **蹲伏**：按 C——身高从 2 平滑过渡到 1、速度变 0.35 倍、动画切 Crouch；
   再按 C 还原；蹲下时跳跃仍可用（center 调整正确则 isGrounded 稳定）；
6. **IK**：左手贴在武器护木上；按 R 换弹（暂无武器逻辑时可临时 SetTrigger("Reload") 验证）时左手松开；
7. **表现层**（对照 `4F_WeaponSway_HeadBob_play.png`）：移动时镜头自然起伏（HeadBob）、
   快速甩视角时武器"拖"在后面（WeaponSway）、ADS 时两者幅度都明显收敛；
8. **暂停禁用**：按 Tab 暂停后，PlayerController 等操控组件 Inspector 显示 disabled、移动无效——
   disableOnPause 回填生效。

## 【常见坑】

1. **applyRootMotion 忘关**：动画（尤其 Mixamo 带 root motion 的走路片段）会直接驱动角色位移，与 CharacterController 抢方向盘——症状是"动画走两步、胶囊体飘一米"。本项目在 Start 强制置 false。
2. **多次 Move 拆分速度**：水平垂直分开 Move，`cc.velocity` 只剩最后一次的——下游（动画）拿不到水平速度。一次 Move 合并提交（3.2.1 注释原文）。
3. **transform.Find 找不到深层子物体**：它只查直接子级且**静默失败**返回 null——WeaponHolder 在 hand_R 下十几层，ADS 贴枪因此整体失效过（3.5 注释）。深找用 `GetComponentsInChildren` 或直接序列化引用。
4. **字符串 Find 的脆弱性**：改个名字就断链且不报错。F4 修复把相机引用改成 Inspector 拖入（3.5）；名字不匹配时至少要 LogWarning 兜底。
5. **OnAnimatorIK 不触发**：IK 脚本挂在 Player 根上而不是 Animator 物体上——回调由"挂脚本对象的 Animator"触发，挂错位置=静默失效（PlayerIKController 类注释加粗警告）。
6. **IK 旋转权重写死 1**：位置对了但手腕以诡异角度反折。位置权重 1 + 旋转权重 0，让动画管姿态、IK 只管"贴到"（3.7.2）。
7. **蹲伏只改 height 不改 center**：胶囊体悬空、isGrounded 抖动、跳跃失效。center.y = 高度的一半（3.6）。
8. **切枪后 IK 引用僵死**：缓存了旧武器的 LeftHandIk 引用，切枪后左手吸在空气中的旧位置。`null 检查 + 每帧尝试刷新`的自愈模式（3.7.2）。
9. **动画事件无接收者刷屏**：第三方动画自带事件，Console 每帧红字。空接收器模式接住它（3.8.3）。
10. **编辑器里"视角自转"**：鼠标悬停在其他面板，`<Mouse>/delta` 仍在生效——在输入出口统一拦截（IsMouseInGameView），而不是指望 Unity 的 isFocused（它在编辑器恒真）。
11. **两个脚本同写一个 Transform**：ADS 写贴枪基准、Sway 写摇摆，互相覆盖或累积漂移——"基准+附加、下帧还原"的代数解法（3.8.2），或一开始就做通道分离（HeadBob 的注释哲学）。
12. **CM2 教程照抄到 CM3**：组件名、命名空间、Inspector 结构全变了（第 1 章坑 3 的相机版）：`CinemachineVirtualCamera`→`CinemachineCamera`，`FreeLook/Collider`→`ThirdPersonFollow/Deoccluder`。查文档先看版本。

---

# 第 4 章 · 战斗系统

## 【本章目标】

给第 3 章的"能跑能瞄准的角色"装上牙齿。学完本章你将能：

1. 用 **ScriptableObject 数据驱动**定义三把性能各异的武器（同一套代码、三种手感）；
2. 实现"**准星射击**"（射线方向跟相机而非枪口）、ADS 散布收窄、后坐力上抬、爆头双倍伤害；
3. 实现**切枪不刷满弹**（弹药缓存随槽位保存/恢复）与"脏检查"式的弹药事件广播；
4. 用 ObjectPool 池化高频特效，并掌握**第三方粒子池化的必做适配**（stopAction）；
5. 实现瞄点手雷（所见即所投）：抛物线 + 弹跳 + 引信 + 范围伤害 + 震屏；
6. 掌握"Trigger + Tag 过滤"的拾取物标准模板。

本章还会如实告诉你两条**至今未接通的线路**（玩家曳光、部分拾取音效）——
它们是"只增不改"演进方式的真实代价，也是你复现时的改进点。

## 【前置知识】

- **ScriptableObject（SO）**（第 0 章介绍过）：数据资产。本章是它在项目里最典型的用法——"武器参数"与"武器代码"分离。
- **Raycast（射线检测）**：从某点沿某方向发射一条射线，返回命中的碰撞体、命中点、法线。射击游戏"子弹"的标准实现（真正的子弹飞行体极少用于快节奏射击，因为子弹太快、检测用射线一步到位）。
- **Physics.OverlapSphere**：球形范围内收集所有碰撞体——爆炸范围伤害的标准查询。
- **物理材质（Physic Material）**：控制碰撞的摩擦/弹性。`Assets/Materials/GrenadeBounce.physicMaterial`（bounciness=0.3）负责手雷的低弹性弹跳。
- **ParticleSystem 的 stopAction**：粒子播完后的动作——默认 `Destroy` 会销毁宿主对象，**池化场景必须改成 `Disable`**（本章 4.4 的主角之一）。
- **Instantiate 的执行时序**：`Instantiate` 后组件的 `Start` 会延迟到当帧稍后才执行——在 `Start` 之前写入的字段如果在 `Start` 里又被赋值，会被覆盖。这条时序知识是 4.3"F1 修复"的核心。

## 4.0 战斗系统全景

回看 0.3.3 流向图，本章产出的是图上最密集的一块：**FirearmWeapon 是全项目最大的事件生产者**
（一次命中最多广播 4 个事件：PlayerShot / HitConfirmed / SurfaceHit / HeadshotKilled），
**Grenade、Pickup 是另外两个源头**，而 CombatEffectsController 是
最大的事件消费者。阶段坐标：射击/换弹/弹药管理在阶段 1-2 已成形（"阶段2 新增备弹池、ADS 散布"注释），
4C 加部位伤害与曳光，4D 加命中表现，4E 加手雷与震屏，拾取物中医疗/弹药为阶段 2、手雷补给为阶段 5（D7）。

```text
本章的事件生产-消费全景（箭头方向 = 消息流）
FirearmWeapon.Shoot ──→ RaisePlayerShot(枪口位置, SO名) ──→ AudioManager（枪声，资产暂空）
      │              → RaiseHitConfirmed ──────────────────→ HitFeedbackUI（HitMarker）
      │              → RaiseSurfaceHit(点, 法线, 是否敌人) ──→ CombatEffectsController（血雾 / 烟尘+弹痕）
      │              → RaiseHeadshotKilled（爆头击杀时）───→ HitFeedbackUI（HEADSHOT 提示）
      │              → ApplyRecoil ────────────────────────→ PlayerController（后坐力，非事件，直调）
FirearmWeapon.Reload → RaiseReloadStart / RaiseReloadEnd ──→ AudioManager（音效，资产暂空）
Grenade.Explode      → RaiseExplosion(位置) ───────────────→ CombatEffectsController（爆炸特效+震屏）
                                                  （+ AudioManager 爆炸音，资产暂空）
EnemyBase 射击       → RaiseBulletTrail(起点,终点) ────────→ CombatEffectsController（曳光）※见4.4
GrenadePickup        → RaisePickupTaken ──────────────────→ AudioManager（拾取音，资产暂空）※见4.6
```

## 4.1 WeaponData：一把枪就是一行数据

[WeaponData.cs](Assets/Scripts/Combat/WeaponData.cs) 全文只有 22 行，但它决定了三把武器的全部性格：

```csharp
// 摘自 WeaponData.cs（全文，类 WeaponData）
[CreateAssetMenu(fileName = "WeaponData", menuName = "Combat/WeaponData")]
public class WeaponData : ScriptableObject
{
    public string weaponName = "默认步枪";
    public float damage = 25f;
    public float fireRate = 600f;        // RPM（每分钟射速）
    public int magazineSize = 30;
    public float reloadTime = 2f;        // 秒
    public float maxRange = 200f;
    public float bulletSpread = 0.02f;   // 散布角度（弧度）
    public float recoilAmount = 1.5f;    // 后坐力幅度
    public float adsSpreadMultiplier = 0.3f;     // ADS时散布倍率（越小越准）
    public int reserveAmmoMax = 120;              // 最大备弹量
    public int ammoPickupAmount = 30;              // 单个弹药包恢复备弹量
    public GameObject weaponPrefab;      // 武器模型预制体引用
    public GameObject muzzleFlashPrefab; // 枪口特效（可选）
}
```

**为什么用 SO 而不是把参数写在 FirearmWeapon 里？** ① 三把武器**共享同一份射击代码**，
只有数据不同——加第 4 把枪不改一行代码、只建一个新资产；② 参数在 Inspector 里改完即生效（运行中也行），
平衡调试不用等编译；③ `ammoPickupAmount`、`weaponPrefab` 这种"武器自己才知道"的参数有了归属地。

三把武器的实测数值（MCP 读取 `Assets/Data/` 三个 .asset 资产）：

| 字段 | RifleData（步枪） | PistolData（手枪） | SMGData（冲锋枪） | 设计解读 |
|------|------|------|------|------|
| damage | 25 | **30** | 15 | 手枪单发最高：射速慢就要打得疼 |
| fireRate (RPM) | 600 | 300 | **900** | SMG 泼水，手枪点射 |
| magazineSize | 30 | 12 | 40 | 配合射速的弹匣容量 |
| reloadTime | 2 | 1.5 | 1.8 | — |
| maxRange | 200 | 100 | 120 | 手枪近战定位 |
| bulletSpread (rad) | 0.02 | **0.015** | 0.035 | 手枪最准（腰射基准），SMG 最飘 |
| recoilAmount | 0.8 | 1.2 | 0.6 | 射速越快、单发后坐越小 |
| adsSpreadMultiplier | 0.3 | 0.25 | 0.35 | ADS 把散布压到 1/3 左右 |
| reserveAmmoMax | 120 | 60 | 160 | — |
| ammoPickupAmount | 30 | 12 | 40 | 弹匣整数倍，捡一包=满弹匣 |
| weaponPrefab | Weapon_Rifle | SM_Wep_Pistol_Heavy_01 | SM_Wep_SubMGun_01 | 视觉模型（4.2 编辑器操作） |

**数值三角**：伤害-射速-散布互相牵制，三把武器在三角里占了三个不同的点。这就是数据驱动的手感设计——
先让参数跑起来，再调数值找平衡，而不是先写三套射击代码。

编辑器操作：Project 窗口右键 `Create → Combat → WeaponData` 三次，分别命名为
`RifleData` / `PistolData` / `SMGData`，按上表填入实测值，存放在 `Assets/Data/`。

## 4.2 FirearmWeapon：射击核心

[FirearmWeapon.cs](Assets/Scripts/Combat/FirearmWeapon.cs)（189 行）挂在武器物体上。
先看它的两个成员脚本之外的引用：`weaponData`（Inspector 拖入 SO）与 `firePoint`（枪口挂点 Transform）。

### 4.2.1 Shoot()：一次扣扳机的完整旅程

```csharp
// 摘自 FirearmWeapon.cs 的 Shoot（节选）
public void Shoot()
{
    if (!CanShoot()) return;

    Transform origin = firePoint != null ? firePoint : transform;
    Vector3 direction = Camera.main != null ? Camera.main.transform.forward : origin.forward;
    // ↑ 关键：射线起点在枪口，方向却跟相机——"准星射击"

    // 应用散布（ADS时散布缩小）
    if (weaponData.bulletSpread > 0f)
    {
        float adsFactor = 1f;
        if (_adsController != null)
            adsFactor = 1f - _adsController.ADSProgress * (1f - weaponData.adsSpreadMultiplier);

        float spreadDeg = weaponData.bulletSpread * adsFactor * Mathf.Rad2Deg;
        direction = Quaternion.Euler(
            Random.Range(-spreadDeg, spreadDeg),
            Random.Range(-spreadDeg, spreadDeg),
            0f
        ) * direction;
    }

    if (Physics.Raycast(origin.position, direction, out RaycastHit hit, weaponData.maxRange))
        ApplyHit(hit);

    _currentAmmo--;
    _nextFireTime = Time.time + (60f / weaponData.fireRate);   // RPM 换算成冷却秒数
    PlayMuzzleFlash();
    GameEvents.RaisePlayerShot(muzzle.position, weaponData.name);  // 注：muzzle 在此节选前一行已取
    if (_playerAnimator != null) _playerAnimator.SetTrigger("Fire");   // UpperBody 层 Fire_Upper
    if (_playerController != null) _playerController.ApplyRecoil(weaponData.recoilAmount); // 视角上抬
}
```

六个值得拆开讲的设计点：

1. **准星射击**：`direction = Camera.main.transform.forward`——射线**起点**在枪口、**方向**却跟相机。
   为什么？TPS 里枪口朝向与屏幕准星永远有夹角（肩部偏移），如果按枪口 forward 发射线，
   弹着点会偏离准星，玩家会觉得"明明瞄着却打不中"。跟相机方向，所见即所得；
2. **ADS 散布收窄是连续的**：又是 `ADSProgress` 枢纽变量（第 3 章的老朋友）——瞄准到一半，散布只收一半；
   散布角 = `bulletSpread × adsFactor`，随机偏转的角度直接乘进方向向量；
3. **RPM → 冷却时间**：`60f / fireRate`，600 RPM = 0.1 秒一发，`CanShoot` 里用 `Time.time >= _nextFireTime` 判定；
4. **一条射线，一次结算**：命中进 `ApplyHit`（4.2.2），没命中什么都不生成——**本项目玩家侧目前没有"打偏的曳光"**（见 4.4 的如实记录）；
5. **射击的四联反馈**：枪口特效（视觉）+ RaisePlayerShot（音效事件）+ Fire Trigger（上身动画）+ ApplyRecoil（视角上抬）——打击感的"多条通道同时刺激"；
6. **射线没有 LayerMask**：`Physics.Raycast(origin.position, direction, ..., maxRange)` 未传过滤层——
   射线会与一切碰撞体交互（包括玩家自己的胶囊体）。当前能跑是因为起点在枪口、方向近似平行于身体；
   **生产项目应传 `~PlayerLayer` 之类的掩码**，复现时建议补上（一行改动）。如实记录：这是本项目的已知粗糙点。

`CanShoot()` 是四条件守卫：有 WeaponData、弹匣有弹、冷却结束、未在换弹——
每帧被 WeaponManager 轮询（按住鼠标=连发）。

### 4.2.2 ApplyHit：部位伤害与事件生产

```csharp
// 摘自 FirearmWeapon.cs 的 ApplyHit
private void ApplyHit(RaycastHit hit)
{
    var part = hit.collider.GetComponentInParent<EnemyPart>();
    var enemy = part != null ? part.Owner : hit.collider.GetComponentInParent<EnemyBase>();
    if (enemy != null)
    {
        bool headshot = part != null && part.isHead;
        float damage = headshot ? weaponData.damage * enemy.headshotMultiplier : weaponData.damage;
        bool killed = enemy.TakeDamage(damage);

        GameEvents.RaiseHitConfirmed();
        GameEvents.RaiseSurfaceHit(hit.point, hit.normal, true);   // 血特效
        if (killed && headshot)
            GameEvents.RaiseHeadshotKilled();
        return;
    }

    GameEvents.RaiseSurfaceHit(hit.point, hit.normal, false);      // 命中烟尘+弹痕
    var player = hit.collider.GetComponentInParent<PlayerHealth>();
    if (player != null)
        player.TakeDamage(weaponData.damage);
}
```

- **部位判定**：命中碰撞体先向上找 [EnemyPart.cs](Assets/Scripts/Enemy/EnemyPart.cs)（部位标记，挂在敌人头/身体的碰撞体上），
  有则读 `part.isHead` 判爆头。爆头倍率**不在 WeaponData 上，在 EnemyBase 上**
  （`headshotMultiplier = 2f`，注释："爆头伤害倍率（FirearmWeapon 读取）"）——
  设计理由：倍率是"敌人有多脆"的属性，不同敌人可以不同（第 5 章会看到三类敌人继承同一基类）；
- **事件批量生产**：一次命中最多广播 HitConfirmed + SurfaceHit(敌) + HeadshotKilled 三个事件——
  HUD 的 HitMarker、战场的血雾、爆头提示各自订阅，互不知晓；
- **打中玩家也掉血**：最后一行的 PlayerHealth 分支说明射线不区分敌我——
  手雷误伤自己以外，贴墙射击也可能伤到自己（与无 LayerMask 粗糙点同源）。

### 4.2.3 换弹与弹药

```csharp
// 摘自 FirearmWeapon.cs 的 ReloadRoutine
private IEnumerator ReloadRoutine()
{
    _isReloading = true;
    if (_playerAnimator != null) _playerAnimator.SetTrigger("Reload");
    GameEvents.RaiseReloadStart();
    yield return new WaitForSeconds(weaponData.reloadTime);

    int needed = weaponData.magazineSize - _currentAmmo;
    int toReload = Mathf.Min(needed, _reserveAmmo);   // 备弹不足时只补一部分
    _currentAmmo += toReload;
    _reserveAmmo -= toReload;

    _isReloading = false;
    GameEvents.RaiseReloadEnd();
}
```

`Reload()` 入口四守卫：有数据、未在换弹、弹匣未满、备弹>0。协程等 `reloadTime` 秒后
从备弹池转弹——**备弹池**是阶段 2 加的深度：弹药从"只有弹匣"变成"弹匣+备弹"两层（`reserveAmmoMax`），
弹药包补给才有了去处。`IsReloading` 属性供第 3 章 IK 控制器判断"左手让位"——第 3、4 章的接口在这里接上。

### 4.2.4 两个如实记录点

1. **枪口特效没走对象池**：`PlayMuzzleFlash` 用 `Instantiate` + `Destroy(_muzzleFlash, 1f)`——
   与 4.4 的池化体系不一致（高频射击下枪口特效是 Instantiate/Destroy 压力点之一）。如实记录，复现时建议池化；
2. **切枪弹药缓存（F1 修复）**：`pendingMagazine / pendingReserve` 两个 `[HideInInspector]` 公开字段
   + 注释"WeaponManager 在实例化后、Start 前写入；-1 表示无缓存"——这是 4.3 的主角，先按下不表。

### 4.2.5 编辑器操作：制作武器预制体

以 `Weapon_Rifle.prefab` 实测结构为例（Pistol/SMG 同模式复制）：

```text
Weapon_Rifle（根）
  components : MeshFilter / MeshRenderer / MeshCollider / FirearmWeapon(weaponData=RifleData, firePoint=FirePoint)
  ├── Model/                     ← 包内武器模型（SM_Wep_Rifle_Assault_* 七个部件）
  ├── FirePoint                  ← 枪口空物体（射线起点/枪口特效位置）
  └── LeftHandIk                ← 护木 IK 挂点（第 3 章 PlayerIKController 吸附点）
```

步骤：① 建空物体，命名 Weapon_Rifle；② 从 `PolygonBattleRoyale` 拖入武器模型做子物体（Model/）；
③ 建空子物体 FirePoint（放到枪口位置）与 LeftHandIk（放到护木位置）；
④ 挂 FirearmWeapon，把 RifleData 拖入 weaponData、FirePoint 拖入 firePoint；
⑤ 存成 `Assets/Prefabs/Weapon_Rifle.prefab`；⑥ 复制两份改名为 Weapon_Pistol / Weapon_SMG，
换模型与对应 SO。**注意两层引用的分工**：WeaponManager 的 `weapons[]` 数组放的是这些
**带 FirearmWeapon 的自制武器预制体**；而 WeaponData.weaponPrefab 字段放的是**视觉模型**——
FirearmWeapon.Start 里只有"当武器物体自身没有 MeshRenderer"时才会实例化 weaponPrefab 兜底
（RifleData.weaponPrefab 填的是 Weapon_Rifle 自身，实际不会触发兜底）。

## 4.3 WeaponManager：槽位、切枪与弹药事件

[WeaponManager.cs](Assets/Scripts/Combat/WeaponManager.cs)（151 行）挂在 Player 根上，是"武器和外界"的唯一通道。

### 4.3.1 Update：输入转发 + 脏检查广播

```csharp
// 摘自 WeaponManager.cs 的 Update（节选）
int switchIndex = _inputHandler.GetWeaponSwitchIndex();
if (switchIndex >= 0) TrySwitchWeapon(switchIndex);

if (_inputHandler.IsShootPressed())
    _currentWeapon.Shoot();                              // 按住=连发（CanShoot 内部控射速）

// 自动射击：设置开启且准星指向敌人时自动开火
if (GameSettings.AutoShoot && IsCrosshairOnEnemy())
    _currentWeapon.Shoot();

if (_inputHandler.IsReloadPressed())
    _currentWeapon.Reload();

// 检测弹匣变化
int current = _currentWeapon.GetCurrentAmmo();
if (current != _lastAmmo)
{
    _lastAmmo = current;
    OnAmmoChanged?.Invoke(current, _currentWeapon.GetMaxAmmo());
}
// 备弹变化同模式（OnReserveAmmoChanged）...
```

两处设计：

1. **自动射击**（主菜单可开关）依赖 `CrosshairController.IsOnEnemy`——
   这是跨系统的**第三种协作方式**：事件广播（不知谁听）、实例事件（紧耦合）、
   以及这里的**运行时查询**（FindAnyObjectByType + 读公开属性）。武器不认识准星，
   但问一句"准星上有敌人吗"——适合低频、只读的场景；
2. **脏检查广播**：弹药数量没有"每次变更都广播"，而是每帧对比、变了才 Invoke——
   `OnAmmoChanged(int, int)` 是**实例事件**（`public event Action<int,int>`），HUD（UIManager）订阅它刷弹药显示。
   又一个"实例事件给紧邻消费者"的例子（与总线并行不悖）。

### 4.3.2 F1 修复：切枪不再刷满弹

先看错误做法会怎样：`EquipWeapon` 销毁旧武器 → `Instantiate` 新武器 →
新武器的 `Start` 把弹药初始化为满——**切枪成了作弊码**。修复方案是三段配合：

```csharp
// 摘自 WeaponManager.cs 的 EquipWeapon（节选）
// F1：换下武器前把当前弹药存入对应槽位缓存
if (_currentWeapon != null && oldSlotIndex >= 0)
    _ammoCache[oldSlotIndex] = (_currentWeapon.GetCurrentAmmo(), _currentWeapon.GetReserveAmmo());

if (_currentWeapon != null)
    Destroy(_currentWeapon.gameObject);

var weapon = Instantiate(weaponPrefab, weaponHolder);
// 位置/旋转使用武器预制体内保存的挂枪姿势（不再重置，否则切枪后武器偏离双手）

_currentWeapon = weapon.GetComponent<FirearmWeapon>();
...
// F1：槽位有缓存则写入挂起值，武器 Start 时恢复（Instantiate 后 Start 延迟执行，直接 Set 会被覆盖）
if (_ammoCache.TryGetValue(_currentWeaponIndex, out var cached))
{
    _currentWeapon.pendingMagazine = cached.mag;
    _currentWeapon.pendingReserve = cached.reserve;
}
```

以及 FirearmWeapon.Start 里的消费端（4.2.4 提过的 pending 字段）：

```csharp
// 摘自 FirearmWeapon.cs 的 Start（节选）
_currentAmmo = weaponData.magazineSize;
_reserveAmmo = weaponData.reserveAmmoMax;

if (pendingMagazine >= 0)
    _currentAmmo = Mathf.Min(pendingMagazine, weaponData.magazineSize);
if (pendingReserve >= 0)
    _reserveAmmo = Mathf.Min(pendingReserve, weaponData.reserveAmmoMax);
pendingMagazine = -1;
pendingReserve = -1;
```

为什么必须用 pending（挂起值）而不是 Instantiate 后直接 `_currentAmmo = 10`？
注释把原因写死了：**"Instantiate 后 Start 延迟执行，直接 Set 会被覆盖"**——
你先给字段赋值 10，随后引擎才调用 Start，Start 里的 `= magazineSize` 把你的 10 冲掉。
pending 字段让 Start "知情地"消费缓存（再钳制到上限防作弊）——
这是 Unity 生命周期时序坑的标准解法，值得单独背下来。

切枪动画由 `TrySwitchWeapon` 里一句 `_animator?.SetTrigger("ChangeWeapon")` 驱动
（ChangeWeapon 层的 WeaponEquip 状态，第 3 章 3.7.1 的层表）。

### 4.3.3 编辑器操作

WeaponManager 挂 Player 根；Inspector 实测配置：

```text
weapons[0] = Weapon_Rifle      （按 1）
weapons[1] = Weapon_SMG        （按 2）
weapons[2] = Weapon_Pistol     （按 3）
weaponHolder = Player 骨骼 hand_R 下的 WeaponHolder
```

场景初始武器是 Weapon_Rifle（Start 时从 weaponHolder 下找已有的 FirearmWeapon——
初始那把要么是场景里手动摆的，要么按你的组装方式在 Start 后手动 EquipWeapon）。

## 4.4 弹道与命中表现：BulletTrail + CombatEffectsController

### 4.4.1 BulletTrail：两点连线（决策 D1）

[BulletTrail.cs](Assets/Scripts/Combat/BulletTrail.cs)（37 行，全文短到可以整读）：

```csharp
// 摘自 BulletTrail.cs（几乎全文）
[RequireComponent(typeof(LineRenderer))]
public class BulletTrail : MonoBehaviour
{
    public float lifetime = 0.08f;
    private LineRenderer _line;
    private float _timer;

    public void Show(Vector3 start, Vector3 end)
    {
        _line.positionCount = 2;
        _line.SetPosition(0, start);
        _line.SetPosition(1, end);
        _timer = lifetime;
        enabled = true;
    }

    private void Update()
    {
        _timer -= Time.deltaTime;
        if (_timer <= 0f)
            ObjectPool.Release(gameObject);        // 活完一生，回池
    }
}
```

"决策 D1"选择了 LineRenderer 两点连线做曳光（而不是射弹模型/拉伸贴图）——
0.08 秒的存活时长只是视觉暂留。注意它对 ObjectPool 约束（2.3.2）的执行：
`OnDisable` 什么都不做，生命周期尽头只调 Release。

### 4.4.2 CombatEffectsController：表现层总成

[CombatEffectsController.cs](Assets/Scripts/Combat/CombatEffectsController.cs)（161 行）订阅三个表现事件、
管理五种特效（场景级单例，Inspector 实测配置）：

```text
bulletTrailPrefab = FX_BulletTrail   预热 8（trailPrewarmCount）
bloodFxPrefab     = BloodSplat_FX    预热 4（PolygonBattleRoyale 包内特效）
impactSmokePrefab = GunShot_Smoke_FX 预热 4（包内特效）
explosionFxPrefab = Explosion_Small_FX 预热 2（包内特效）
bulletDecalPrefab = FX_BulletDecal   不预热——走环形队列（maxDecals=50）
cameraShaker      = TPSCamera 的 CameraShaker（amp=0.8, range=25）
```

订阅/退订的固定姿势（OnEnable/OnDisable 对称，2.1.3 的纪律）：

```csharp
// 摘自 CombatEffectsController.cs 的 OnEnable
GameEvents.OnBulletTrail += HandleBulletTrail;
GameEvents.OnSurfaceHit += HandleSurfaceHit;
GameEvents.OnExplosion += HandleExplosion;
```

**本节最重要的一段代码**——池化第三方特效的必做适配：

```csharp
// 摘自 CombatEffectsController.cs 的 ConfigurePooledParticles
/// <summary>包内 FX 预制体默认播完 Destroy（破坏池化）；统一改为 Disable 停止后自动隐藏回池</summary>
private void ConfigurePooledParticles(GameObject go)
{
    var systems = go.GetComponentsInChildren<ParticleSystem>(true);
    foreach (var ps in systems)
    {
        var main = ps.main;
        if (main.stopAction == ParticleSystemStopAction.Destroy)
            main.stopAction = ParticleSystemStopAction.Disable;
    }
}
```

Synty 包的特效预制体默认"播完自毁"——**自毁会把池化对象直接杀死**，
下一次 Get 出来的就是残尸（或池内根本没有可用实例）。每次从池里取出来都把
stopAction 强制改成 Disable，粒子播完只是失活（失活触发回池判定路径）。
**用别人的特效资产进池，永远检查这一项。**

弹痕的环形队列与防闪面：

```csharp
// 摘自 CombatEffectsController.cs 的 SpawnDecal（节选）
if (_decals.Count >= maxDecals)
{
    do { decal = _decals.Dequeue(); } while (decal == null && _decals.Count > 0);  // 复用最早
}
...
decal.transform.position = point + normal * 0.01f;   // 沿法线外偏 0.01m 防 z-fighting（闪面）
decal.transform.rotation = Quaternion.LookRotation(normal);
```

弹痕是会无限累积的场景物体（别的特效寿命都短），所以用"50 个上限的环形队列"覆盖复用；
贴图 Quad 沿法线外偏 0.01 米防止与墙面共面闪烁（z-fighting）。

爆炸震屏的完整数据链（跨第 2、3、4 章的接口闭环）：

```text
Grenade.Explode → RaiseExplosion(位置) → HandleExplosion：
  SpawnFx(Explosion_Small_FX, pos)                        ← 爆炸特效（池化）
  dist = 主相机到爆点距离 → falloff = 1 - dist/25         ← 25m 内衰减
  cameraShaker.Shake(0.8 × falloff)                        ← 第 3 章的 CameraShaker
    → Perlin.AmplitudeGain（NoiseProfile = Assets/Data/ExplosionShake.asset ← CM3 NoiseSettings）
```

`ExplosionShake.asset` 的身份在此揭晓：它是 Cinemachine NoiseSettings（Perlin 噪声曲线配置），
挂在 TPSCamera 的 Perlin 组件上——名字里带"Explosion"是因为它为爆炸震屏调校的噪声曲线。

### 4.4.3 如实记录：玩家射击目前没有曳光

grep 全部广播点可以证实：`RaiseBulletTrail` 的调用者**只有 EnemyBase**（敌人开枪时的曳光）。
FirearmWeapon.Shoot 广播了 PlayerShot/HitConfirmed/SurfaceHit，**唯独没有广播曳光**——
玩家的子弹命中才出表现（血雾/烟尘），飞行弹道视觉缺失。
这是"只增不改"演进的真实缝隙（4C 先给敌人做了曳光，玩家侧没回填）。
复现时的补全只需一行（放进 Shoot 的 Raycast 分支后）：

```csharp
GameEvents.RaiseBulletTrail(origin.position, hit.point);   // 命中曳光
// 未命中时可加：RaiseBulletTrail(origin.position, origin.position + direction * maxRange);
```

CombatEffectsController 的订阅端早就绪——**事件总线的价值：生产者缺席时消费者已经能跑**。

## 4.5 手雷：GrenadeThrower + Grenade

### 4.5.1 GrenadeThrower：投掷的"上半身动画 + 瞄点弹道"

[GrenadeThrower.cs](Assets/Scripts/Player/GrenadeThrower.cs)（139 行）挂在 Player 根。实测配置：
`grenadePrefab=Grenade_Thrown、initialGrenades=3、maxGrenades=3、throwDelay=0.9、throwForwardSpeed=12、throwUpSpeed=5`。

流程（TryThrow）：

```csharp
// 摘自 GrenadeThrower.cs 的 TryThrow / ThrowRoutine / Throw（节选合并）
public void TryThrow()
{
    if (GrenadeCount <= 0 || _throwRoutine != null) return;   // 无雷/投掷中不可连投

    GrenadeCount--;
    GameEvents.RaiseGrenadeCountChanged(GrenadeCount);        // HUD 手雷数立即减

    if (_animator != null) _animator.SetTrigger("Toss");      // UpperBody 层 Toss 状态
    _throwRoutine = StartCoroutine(ThrowRoutine());
}
private IEnumerator ThrowRoutine()
{
    yield return new WaitForSeconds(throwDelay);   // 0.9s：起手动画到出手瞬间
    Throw();
}
```

- **动画驱动的出手时机**：不是按下 G 立刻出手，而是等 toss 动画播放 0.9 秒到"出手帧"
  才生成手雷——视觉与逻辑对齐。投掷中 `_throwRoutine != null` 阻止连投；
- **数量上限 3**：`AddGrenades` 里 `Mathf.Min(count+add, maxGrenades)` 钳制——
  第 5 章波次生成器"每波补 1 雷"、以及 GrenadePickup 都调这个方法，上限一处管死；
- **瞄点投掷**（Throw 的核心）：

```csharp
// 摘自 GrenadeThrower.cs 的 Throw（节选）
Vector3 origin = transform.position + Vector3.up * throwOriginHeight;   // 胸口 1.5m

// 瞄点投掷（所见即所投）：与 CrosshairController 同源，从准星（主相机屏幕中心）发射线，
// 命中点即投掷目标；未命中（朝天）取视线 60m 处。参考 CS 的准星指向投掷
Vector3 aimDir = aimCamera != null ? aimCamera.forward : transform.forward;
Vector3 target = origin + aimDir * aimRaycastRange;
var cam = Camera.main;
if (cam != null)
{
    var ray = new Ray(cam.transform.position, cam.transform.forward);
    if (Physics.Raycast(ray, out var hit, aimRaycastRange, ...))
        target = hit.point;
}
Vector3 dir = (target - origin).normalized;
origin += dir * 0.5f;   // 出手点沿投掷方向前移 0.5m，避免与自身胶囊碰撞

var go = ObjectPool.Get(grenadePrefab, origin, Quaternion.identity);
rb.velocity = dir * throwForwardSpeed + Vector3.up * throwUpSpeed;   // 12 前抛 + 5 上抬
rb.angularVelocity = UnityEngine.Random.insideUnitSphere * spinSpeed; // 自转便于肉眼追踪
```

与射击同源的"准星哲学"：准星指哪，手雷落哪（射线取目标点，再从胸口往目标点抛）。
出手点前移 0.5 米防止手雷生成在自己胶囊体里。

三个如实记录点：① 类注释写"初速 = 视线方向×10 + 向上×7（计划 §5.5）"，
**实现是 12 + 5**（字段默认值）——又一处注释与实现不符（2.3.4 的教训再现：以代码为准）；
② `[Header("引用（场景序列化，禁止 Find 系列新增）")]`——把纪律写进 Inspector 本身；
③ 死亡处理订阅的是 **PlayerHealth 的实例事件** `health.OnDeath += OnPlayerDeath`
（同物体直引，不用总线），OnDestroy 成对退订——死亡后停投掷协程。

### 4.5.2 Grenade：物理、引信与范围伤害

[Grenade.cs](Assets/Scripts/Combat/Grenade.cs)（126 行）挂在 `Grenade_Thrown.prefab` 上
（实测组件：MeshFilter/MeshRenderer/SphereCollider/Rigidbody/Grenade/**TrailRenderer 拖尾**）。
实测参数：fuse=5、radius=5、maxDamage=100、edgeFactor=0.3、**groundStopSpeed=5**（注意：代码默认 1.5，
预制体上被 Inspector 覆盖为 5——调参痕迹）。

**池化状态复位**（OnEnable，2.3.2 约束的教科书式执行）：

```csharp
// 摘自 Grenade.cs 的 OnEnable
private void OnEnable()
{
    // 从池中取出时重置引信与拖尾（池化对象状态必须干净）
    _fuseTimer = fuseTime;
    _exploded = false;
    var trail = GetComponent<TrailRenderer>();
    if (trail != null) trail.Clear();      // 不清拖尾，第二颗雷会拖着上一颗的轨迹
}
```

**滚动钳停**（FixedUpdate）——一个真实的物理坑：

```csharp
// 摘自 Grenade.cs 的 FixedUpdate 与注释
// 滚动钳停：接地 + 低速 → 速度/角速直接归零（摩擦力对球体滚动几乎无衰减作用）
if (_grounded && _rb.velocity.magnitude < groundStopSpeed)
{
    _rb.velocity = Vector3.zero;
    _rb.angularVelocity = Vector3.zero;
}
_grounded = false;  // 每个物理帧重置，由本帧碰撞事件重新置位
```

球体在地面滚动的阻力几乎为零——不钳停的话手雷会一直滚出爆炸半径的"最佳落点"。
`_grounded` 的判定在碰撞回调里：接触面**法线 y > 0.5** 才算地面（区别墙面/敌人身体）。

**碰撞手感**：碰到敌人快速卸力（`velocity *= 0.15`——砸在敌人身上就地滚落，而不是弹飞）；
一般碰撞的低弹性由 `Assets/Materials/GrenadeBounce.physicMaterial`（bounciness=0.3）负责。

**爆炸结算**（Explode）：

```csharp
// 摘自 Grenade.cs 的 Explode（节选）
// OverlapSphere 收集半径内碰撞体，同一目标（敌人可能有头/身多个碰撞体）只结算一次
var damaged = new HashSet<Object>();
var hits = Physics.OverlapSphere(pos, explosionRadius);
foreach (var hit in hits)
{
    var enemy = hit.GetComponentInParent<EnemyBase>();
    var player = enemy == null ? hit.GetComponentInParent<PlayerHealth>() : null;
    Object target = enemy != null ? (Object)enemy : (Object)player;
    if (target == null || !damaged.Add(target)) continue;

    // 伤害按爆心到目标最近表面点的距离线性衰减
    float dist = Vector3.Distance(pos, hit.ClosestPoint(pos));
    float falloff = Mathf.Clamp01(1f - dist / explosionRadius);
    float damage = Mathf.Lerp(maxDamage * edgeDamageFactor, maxDamage, falloff);

    if (enemy != null) enemy.TakeDamage(damage);
    else player.TakeDamage(damage);
}

GameEvents.RaiseExplosion(pos);
ObjectPool.Release(gameObject);       // 广播完表现事件才回池
```

三个精度点：① `HashSet` 去重——部位伤害（头/身双碰撞体）让"同一敌人被结算两次"成为必须处理的问题；
② 距离用 `ClosestPoint`（到目标表面）而非中心——爆心贴脸时衰减正确；③ 衰减曲线从
爆心 100 线性降到边缘 30（edgeFactor=0.3），不是归零——边缘仍有威慑。
敌我通吃：手雷是玩家也能炸死自己的武器。

## 4.6 拾取物三兄弟

[AmmoPickup.cs](Assets/Scripts/Combat/AmmoPickup.cs)、[HealthPickup.cs](Assets/Scripts/Combat/HealthPickup.cs)、
[GrenadePickup.cs](Assets/Scripts/Combat/GrenadePickup.cs) 是同一模板的三次实例化
（GrenadePickup 类注释自供："复制 HealthPickup 模式（D7）"）。模板的全部要素：

```csharp
// 摘自 HealthPickup.cs（几乎全文，另两个只换"给谁加什么"）
public class HealthPickup : MonoBehaviour
{
    public int medkitAmount = 1;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;          // ① 只认 Player（tag 实测已设）
        var playerHealth = other.GetComponent<PlayerHealth>();
        if (playerHealth == null) return;
        playerHealth.AddMedkit(medkitAmount);          // ② 调目标系统的加法接口
        Destroy(gameObject);                          // ③ 用完销毁
    }

    private void Awake()                                // ④ 兜底：无 Collider 自动补 2m 球形 Trigger
    {
        var col = GetComponent<Collider>();
        if (col == null)
        {
            var sphereCol = gameObject.AddComponent<SphereCollider>();
            sphereCol.isTrigger = true;
            sphereCol.radius = 2f;
        }
        else col.isTrigger = true;
    }
}
```

三者差异只有"②调谁加什么"：

| 拾取物 | 调用 | 数量来源 | 上限钳制位置 |
|--------|------|----------|--------------|
| HealthPickup | `PlayerHealth.AddMedkit(1)` | medkitAmount | PlayerHealth（maxMedkits=5） |
| AmmoPickup | `FirearmWeapon.AddReserveAmmo(amount)` | **当前武器的 WeaponData.ammoPickupAmount**（捡一包=一满弹匣） | WeaponData.reserveAmmoMax |
| GrenadePickup | `GrenadeThrower.AddGrenades(1)` | grenadeAmount | GrenadeThrower（maxGrenades=3） |

注意 AmmoPickup 补的是**当前手里那把武器**的备弹（从 weaponHolder 现查 FirearmWeapon）——
拿着 SMG 捡弹药包补 SMG 弹，切枪后数值随武器走，与 F1 缓存一致。

**如实记录（4.6 版）**：三个兄弟里**只有 GrenadePickup 广播了 `OnPickupTaken`**
（弹药/医疗拾取静默——它们诞生于阶段 2，事件是 4B 音效预留期加的，只有阶段 5 的新手雷补上了广播）。
复现时建议给另两个 Pickup 各补一行 `GameEvents.RaisePickupTaken();`。
拾取物自身用 `Destroy` 销毁（不池化）——拾取频率远低于子弹特效，如实记录即可。

编辑器操作：拾取物预制体（`GrenadePickup.prefab`、`AmmoPickup_Pile.prefab`、
`HealthPickup_MedBox.prefab`）= 包内模型 + 对应 Pickup 脚本 + 任何 Collider（Awake 会兜底改 Trigger）；
场景中 `Pickups` 空物体下摆放若干实例。Player 物体的 tag 必须设为 `Player`（实测已设）。

## 4.7 组装与回填

本章的编辑器操作汇总（接在 3.9 之后）：

| # | 操作 | 位置 |
|---|------|------|
| 1 | 创建三个 WeaponData SO（4.1 实测数值表） | `Assets/Data/` |
| 2 | 制作三把武器预制体（Model + FirePoint + LeftHandIk + FirearmWeapon） | `Assets/Prefabs/Weapon_*.prefab` |
| 3 | 挂 WeaponManager，weapons 数组拖入三把武器（0=Rifle/1=SMG/2=Pistol） | Player 根 |
| 4 | 场景建 CombatEffectsController 物体，五个特效槽 + cameraShaker 按实测配置拖入 | 场景级 |
| 5 | 制作 FX_BulletTrail（LineRenderer+BulletTrail）/ FX_BulletDecal（Quad+弹孔贴图）预制体 | `Assets/Prefabs/` |
| 6 | 制作 Grenade_Thrown（模型+SphereCollider+Rigidbody+Grenade+TrailRenderer），Rigidbody 挂 GrenadeBounce 物理材质 | `Assets/Prefabs/` |
| 7 | Player 挂 GrenadeThrower，拖 grenadePrefab 与 aimCamera（TPSCamera） | Player 根 |
| 8 | 制作三个拾取物预制体，场景 Pickups 下摆放 | `Assets/Prefabs/` + 场景 |
| 9 | **回填 disableOnPause 最后 2 项**：WeaponManager（Player 根）+ GrenadeThrower（Player 根）——8 项齐，2.4.4 的清单闭环 | GameManager |
| 10 | 创建 ExplosionShake（CM3 NoiseSettings）挂到 TPSCamera 的 Perlin NoiseProfile | `Assets/Data/` |

## 【验证方法】

对照 `Assets/Screenshots/` 历史截图逐项验证：

1. **射击方向**（对照 `Fix_Shoot_Verify.png`、`TPS_PlayMode_AfterRecoil.png`）：准星指哪打哪
   （打近处墙角验证枪口与准星的夹角被相机方向修正）；按住左键连发，射速 600 RPM≈每 0.1 秒一发；
   开枪瞬间视角上抬（recoil 0.8）后可以压回来；
2. **ADS 手感**：右键瞄准时弹着散布肉眼可见收窄（0.3 倍）；
3. **部位与爆头**（对照 `fix_shoot_feedback.png`、`TPS_HitReaction_Verification.png`）：
   爆头伤害=面板伤害×2，且 HUD 出 HEADSHOT 提示（需第 6 章 HitFeedbackUI，可暂看 Console 无报错）；
4. **换弹与弹药**：R 播换弹动画、备弹递减、弹匣补满；备弹不足时只补到备弹余量；
5. **切枪弹药缓存（F1）**：打空几发→按 2 切走→再按 1 切回——**弹药数保持打空后的值**（不回满弹）；
   这是 F1 修复的验收标准；
6. **命中表现**（对照 4D 相关截图）：打敌人出红色血雾、打墙出烟尘+弹痕；连打 51 枪弹痕环形覆盖最早的；
7. **手雷全链**（对照 `4E_grenade_hud.png`）：准星瞄墙角投掷，手雷应落在准星所指处附近；
   落地弹跳约 0.3 弹性、慢滚后停住（钳停）；5 秒引信爆炸——爆炸特效+按距离衰减的震屏；
   站在半径内会被自己的手雷炸伤；
8. **拾取**：走近弹药包——当前武器备弹+30（Rifle）；医疗包+1（上限 5 后不再增加）；
   手雷包+1（上限 3）；HUD 手雷数联动（OnGrenadeCountChanged）；
9. **暂停禁用完整版**：Tab 暂停后武器不能开火、手雷不能投——8 项 disableOnPause 全部生效。

## 【常见坑】

1. **射线从枪口 forward 打**：弹着点偏离准星（肩部相机固有夹角），玩家感觉"打不准"。方向跟 `Camera.main.transform.forward`（4.2.1 的第一设计点）。
2. **Instantiate 后直接写字段被 Start 覆盖**：生命周期时序坑。要么用 pending 挂起值让 Start 消费（F1 模式），要么在 Instantiate 后手动补一次初始化调用。
3. **粒子 stopAction=Destroy 混进对象池**：第三方特效默认自毁——池里取一个死一个。每次 Get 后强制改 Disable（ConfigurePooledParticles，4.4.2）。
4. **池化对象不复位**：第二颗手雷一出膛就炸（引信没重置）、拖尾带着上一颗的轨迹。OnEnable 复位一切状态（4.5.2）。
5. **球体落地滚个不停**：摩擦对滚动几乎无衰减——接地+低速时手动归零钳停（4.5.2 的物理坑）。
6. **OverlapSphere 对多碰撞体重复结算**：敌人有头/身两个碰撞体，一次爆炸掉两管血。HashSet 按目标去重（4.5.2）。
7. **射线无 LayerMask**：当前项目如此（4.2.1 如实记录）——打偏的射线可能命中任何碰撞体（含自己的胶囊、未来的拾取物 Trigger）。生产化第一步就是加掩码。
8. **爆头倍率放错地方**：倍率是敌人的属性（EnemyBase.headshotMultiplier），不是武器的——不同敌人可以有不同脆的头。武器只管基础伤害。
9. **弹痕 z-fighting**：贴花与墙面共面会闪烁。沿法线外偏 0.01m（4.4.2）。
10. **CompareTag("Player") 不生效**：忘给 Player 物体设 tag，三个 Pickup 一起哑火且无报错。组装清单里那一步不能省。
11. **切枪武器姿态错乱**：Instantiate 后重置局部位置会把预制体里校好的挂枪姿势清掉——本项目注释明确"位置/旋转使用武器预制体内保存的挂枪姿势（不再重置）"（4.3.2）。
12. **上限三处管三样**：手雷上限在 Thrower、医疗上限在 PlayerHealth、备弹上限在 WeaponData——各自归各自系统，别集中写在一个"物品管理器"里（本项目用实例证明了这个边界的合理性）。

---

# 第 5 章 · 敌人与波次

## 【本章目标】

给战场加上"会追你、会打你、打死了还会一波波再来"的对手。学完本章你将能：

1. 用一个 **EnemyBase 基类** + NavMesh 寻路实现巡逻/追击/站桩射击的完整敌人 AI，
   并靠**参数差异**做出三种性格迥异的敌人（Scout/Soldier/Heavy）；
2. 实现**运行时自动挂载**的爆头判定（代码收缩胶囊体 + 在头骨上生成球形碰撞体——
   含本项目一次完整的 4D 热修复案例）；
3. 用 **WaveConfig（SO）** 定义 5 波混编进攻，用 **EnemyWaveGenerator** 的五态状态机驱动
   "倒计时 → 逐个刷怪 → 等全灭 → 波间休整 → 胜利"的完整循环；
4. 理解"尸体延迟销毁"给存活判定、对象池带来的连锁影响（为什么判活用 IsDead 而不是 active）。

## 【前置知识】

- **NavMesh 与 NavMeshAgent**：NavMesh（导航网格）是"场景里可以走的地面"的烘焙数据；NavMeshAgent 是角色身上的寻路组件——给它一个目标点，它自动绕障物走过去。烘焙工具由 AI Navigation 包提供（第 1 章装过），Window → AI → Navigation。
- **HumanBodyBones**：Unity 对人形 Avatar（人体骨骼映射）的骨骼枚举——`GetBoneTransform(HumanBodyBones.Head)` 直接拿到头骨 Transform，这是本章"自动挂头"的基础。
- **决策树式 AI**：每帧按"if-else 优先级"选择行为（攻击 > 追击 > 巡逻），没有行为树/GOAP 那么高级，但透明、可调试——对波次射击游戏的敌人足够。
- **ScriptableObject 嵌套序列化**：SO 里可以放 `[System.Serializable]` 的子类数组（Wave → entries），Inspector 里逐波展开编辑。

## 5.0 全景：三类敌人、一个基类、波次驱动一切

阶段时间线（注释与截图证据）：基础敌人 AI（巡逻/追击/近战）诞生于早期阶段；
**4C** 把攻击改成远程射线 + 加部位伤害（"远程攻击(4C 新增)""部位(4C 新增)"的 Header 原文）；
**阶段 5** 的 S1 任务制作了三个敌人预制体（截图 `S1_enemy_prefabs_verify.png`）、
S4 加入波次刷怪（`S4_wave1_hunting.png`）、S5 接胜利流程。

**一个先声夺人的实测发现**：用编辑器脚本清点场景——当前 **active 的 EnemyBase 数量为 0**。
场景 YAML 里那些 `Enemy_Scout_04`、`Enemy_Soldier_02` 之类的实例，是早期手动摆放的**测试敌人，
现已全部禁用**（SetActive(false) 留在场景里）。最终形态的玩法里，所有敌人都由波次生成器在运行时
Instantiate 出来——巡逻模式（与测试敌人配套）在当前玩法中只是"找不到玩家时的兜底行为"。
这解释了 `Waypoints` 物体如今是空容器的原因。复现时你可以把禁用实例清掉，只保留波次链路。

## 5.1 EnemyBase：一个基类的三种性格

[EnemyBase.cs](Assets/Scripts/Enemy/EnemyBase.cs)（321 行）是唯一的大文件，也是三个预制体共用的全部逻辑。
三个敌人**没有子类**——性格全部来自 Inspector 参数差异（实测值）：

| 参数 | Scout（侦察兵） | Soldier（士兵） | Heavy（重装） | 性格解读 |
|------|------|------|------|------|
| 模型 | Character_SportyMale_01 | Character_MilitaryMale_01 | Character_MercenaryMale_01 | 包内三个人形 |
| maxHealth | 60 | 100 | **200** | Scout 三枪倒、Heavy 要整梭梭 |
| attackDamage | 2 | 3 | **6** | Heavy 打人最疼 |
| attackCooldown | **0.8** | 1 | 1.5 | Scout 射速最快（高频低伤） |
| patrolSpeed | 4 | 3 | 2 | — |
| chaseSpeed | **6.5** | 5.5 | **3.5** | Scout 追得上冲刺玩家，Heavy 慢吞吞 |
| detectRange / attackRange | 20 / 15（三者相同） | | | 行为半径全靠速度与血量区分 |
| accuracy / headshotMultiplier / corpseDuration | 0.7 / 2 / 10（三者相同） | | | 见 5.1.3 / 4.2.2 / 5.1.4 |

**这就是数据驱动的第二次胜利**（第一次是 4.1 的武器）：不加一行代码，策划调参数出三种敌人。

### 5.1.1 决策树：攻击 > 寻敌追击 > 检测追击 > 巡逻

```csharp
// 摘自 EnemyBase.cs 的 Update（核心决策树）
private void Update()
{
    if (_isDead) return;

    // 未找到玩家（未设置 Tag）时保持巡逻
    if (_playerTransform == null) { Patrol(); return; }

    float distance = Vector3.Distance(transform.position, _playerTransform.position);
    if (distance <= attackRange)
    {
        AttackPlayer();
    }
    else if (huntPlayer)
    {
        // 波次寻敌模式：无视 detectRange 直奔玩家（出生点在 20m 外不站桩）
        ChasePlayer();
    }
    else if (distance <= detectRange)
    {
        ChasePlayer();
    }
    else
    {
        Patrol();
    }
}
```

`huntPlayer` 是**阶段 5 加的字段**（Header 原文"波次模式(阶段5新增)"），它改写了决策树的中间层：
波次刷出的敌人**无视 20m 检测范围、出生即全图索敌**。为什么需要它？波次敌人的出生点
刻意选在"距玩家 20m 以外"（5.4 节）——如果靠 detectRange 才追击，敌人会先傻站到玩家自己走近。
字段上的注释是全项目"坑文化"的代表作：

```csharp
// 摘自 EnemyBase.cs 的 huntPlayer 字段注释
public bool huntPlayer;  // 寻敌模式：…（波次生成器刷出的敌人置 true；无 Start 重置逻辑，
                         // 实例化后直赋安全，坑 67）
```

"无 Start 重置逻辑"意味着：**这个字段的安全性完全依赖"敌人是 Instantiate 直出的"**——
每个实例都是全新对象，true 直赋不会污染下一个。如果哪天把敌人改成池化复用（2.3.3 说过为什么不），
忘加复位逻辑的话，从池里出来的敌人会永远全图索敌——"坑 67"就是给未来改动者的警报。

### 5.1.2 巡逻与追击：把 NavMeshAgent 用干净

```csharp
// 摘自 EnemyBase.cs 的 ChasePlayer（节选）
_navAgent.isStopped = false;              // 从攻击状态恢复移动
_navAgent.updateRotation = true;          // 把转向权还给 NavMeshAgent
_navAgent.speed = chaseSpeed;
_navAgent.SetDestination(_playerTransform.position);
```

三个工程细节，每个都是踩坑后写的：① 攻击时 agent 被停住（isStopped），切回追击必须**先恢复**，
否则"永远卡在原地"；② `updateRotation` 是一个"转向权"开关——攻击时要**从 NavMeshAgent 手里抢过来**
（见下节），移动时必须还回去；③ 巡逻里还有一个 `pathPending` 跳过（"路径计算中时跳过，
避免重复设点抖动"）——NavMeshAgent 的 SetDestination 是异步寻路，寻路未完成时反复设点会让
路点动画抽搐。巡逻循环用 `hasPath / remainingDistance <= stoppingDistance` 判定"到达"后切下一个路点。

### 5.1.3 站桩射击：转向权、散布与掩体挡弹

```csharp
// 摘自 EnemyBase.cs 的 AttackPlayer（节选）
_navAgent.isStopped = true;                // 用 isStopped 停住，代替 SetDestination(自身)，避免路径重算阻塞
_navAgent.velocity = Vector3.zero;
if (_navAgent.updateRotation) _navAgent.updateRotation = false;
// NavMeshAgent 只在移动时自动转向；站桩攻击必须接管转向，否则模型背对玩家开枪

// 每帧水平转向玩家（在冷却判断之前，保证转动连续）
Quaternion targetRot = Quaternion.LookRotation(toPlayer);
transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, attackTurnSpeed * Time.deltaTime);

if (Time.time - _lastAttackTime < attackCooldown) return;
if (_playerHealth == null || _playerHealth.CurrentHealth <= 0f) return;   // 玩家死后停火

GameEvents.RaiseEnemyShot(transform.position);      // 音效（资产暂空）

// Raycast 即时弹道：胸口 → 玩家胸口，accuracy 越低弹道偏转越大
Vector3 origin = transform.position + Vector3.up * 1.5f;
Vector3 target = _playerTransform.position + Vector3.up * 1.2f;
Vector3 dir = (target - origin).normalized;
float spreadDeg = (1f - Mathf.Clamp01(accuracy)) * 15f;   // accuracy 0.7 → ±4.5°
...
if (Physics.Raycast(origin, dir, out RaycastHit hit, range))
{
    GameEvents.RaiseBulletTrail(origin, hit.point);      // 命中曳光
    var hitPlayer = hit.collider.GetComponentInParent<PlayerHealth>();
    if (hitPlayer != null) hitPlayer.TakeDamage(attackDamage);
}
else
{
    GameEvents.RaiseBulletTrail(origin, origin + dir * range);   // 打空的曳光
}
```

四个设计点：

1. **敌人的射线攻击是"即时弹道"**：命中与否由一次 Raycast 决定，**掩体天然挡弹**
   （射线先打到掩体就停，曳光也画到掩体上）。这正好呼应 4.4.3 的发现——
   **曳光系统当前是给敌人做的**（玩家未接线），敌人的曳光有"命中"和"打空"两种；
2. **accuracy=0.7 的含义**：散布角 = (1-0.7)×15 = ±4.5°，命中率由射线说了算；
3. **站桩攻击的三件事**：停住（isStopped）、转向权接管（updateRotation=false + 每帧 RotateTowards）、
   玩家死亡停火（CurrentHealth 检查）。注释"模型背对玩家开枪"描述的正是没接管转向时的滑稽现场；
4. 攻击同样**无 LayerMask**——敌人的射线会被**其他敌人挡住**（挡弹的友军成了人肉掩体，
   命中判 PlayerHealth 所以不会误伤友军），与玩家侧的粗糙点同源、同解法（加层掩码）。

### 5.1.4 TakeDamage 与 Die：死亡是一道工序

```csharp
// 摘自 EnemyBase.cs 的 TakeDamage / Die（节选）
public bool TakeDamage(float amount)
{
    if (_isDead) return false;                 // 尸体不可再鞭
    _currentHealth -= amount;
    GameEvents.RaiseEnemyDamaged(transform.position);   // 音效
    _animator?.SetTrigger("Hit");              // UpperBody 层 HitReaction
#if UNITY_EDITOR
    Debug.Log($"<color=red>💥 {gameObject.name} 受到 {amount} 伤害！剩余血量 {_currentHealth}</color>");
#endif
    if (_currentHealth <= 0f) { Die(); return true; }   // 返回值供爆头判定（4.2.2）
    return false;
}

private void Die()
{
    if (_isDead) return;
    _isDead = true;
    GameEvents.RaiseEnemyKilled(transform.position);    // 波次判活 + GameManager 计数（消息接力）
    _animator?.SetTrigger("Died");
    if (_navAgent != null) { _navAgent.isStopped = true; _navAgent.enabled = false; }
    foreach (var col in GetComponentsInChildren<Collider>()) col.enabled = false;   // 尸体不挡弹/不挡路
    Destroy(gameObject, corpseDuration);        // 10 秒后收尸
}
```

死亡是一组**有序的清理工序**：广播（下游波次/计各自处理）→ 死亡动画 → 关 NavAgent →
**禁用全部碰撞体**（尸体不能再挡玩家的子弹和 NavMesh 路）→ 延迟 10 秒销毁。
注意 `TakeDamage` 里那段 `#if UNITY_EDITOR` 的彩色 Debug.Log——**受击时 Console 打彩色伤害日志**，
是调平衡时期的调试设施，被编译隔离在真机构建之外。这种"调试痕迹保留在代码里"的务实风格，
配合 `Screenshots/` 的验证截图，让整个项目"过程可见"。

而 `Destroy(gameObject, corpseDuration)` 这个"延迟销毁"决定向下传导出两个架构后果
（下一节与 5.4 都会回到它）：① 波次生成器**不能用 activeInHierarchy 判存活**；② 敌人不适合入池（2.3.3）。

### 5.1.5 SetupHeadPart：爆头判定的两次热修复

这是全项目最有教学价值的方法之一——**头部的爆头碰撞体是运行时代码自动挂的**，
而它的注释完整记录了两次修复：

```csharp
// 摘自 EnemyBase.cs 的 SetupHeadPart 注释与实现（节选）
/// 头部部位碰撞体（4C 部位伤害）：人形 Avatar 时在 Head 骨骼挂球形碰撞体 + EnemyPart 标记
/// 同时收缩根部胶囊体——默认 2m 胶囊把头部包在内部，射线先命中胶囊导致爆头判定失效（4D 热修复）
/// 二次修复（4D 实测）：胶囊 1.4 + 头球 r=0.22 球心偏移 +0.08 对准视觉头骨（首版 r=0.18 无偏移时，
/// 下颌段被胶囊遮挡、头顶段超出球体，可判头区域仅 ~1.5~1.77 一条窄带，实战几乎无法触发爆头）
private void SetupHeadPart()
{
    var bodyCap = GetComponent<CapsuleCollider>();
    if (bodyCap != null) { bodyCap.height = 1.4f; bodyCap.center = new Vector3(0f, 0.7f, 0f); }
    ...
    var head = _animator.GetBoneTransform(HumanBodyBones.Head);
    var col = head.gameObject.AddComponent<SphereCollider>();
    col.center = new Vector3(0f, 0.08f, 0f);   // Head 骨骼枢轴在颈部，球心上移对准头骨
    col.radius = 0.22f;
    var part = head.gameObject.AddComponent<EnemyPart>();
    part.isHead = true;
}
```

完整复盘：4C 加部位伤害时发现**爆头根本打不出来**——人形默认 2m 高的 CapsuleCollider
把头包在内部，玩家射线永远先命中胶囊（fallback 走身体伤害）。第一次修复：**胶囊收缩到 1.4m**
给头让出空间；实测发现可判头区域只剩 1.5~1.77 一条窄带（球体和胶囊的交界），第二次修复：
**头球半径 0.18→0.22、球心 +0.08 对准视觉头骨**。两条注释把"几何学调参"的全过程留档——
你复现时直接抄最终值（胶囊 1.4/0.7、球 0.22/+0.08），但要知道这些数是试出来的，不是算出来的。

也正因为头碰撞体是运行时挂的，**三个敌人预制体上的 EnemyPart 数量实测为 0**——
不修资产、代码自装配。身体部位没有 EnemyPart：4.2.2 的 `ApplyHit` 里 `part == null`
时 fallback 到 `GetComponentInParent<EnemyBase>()`，天然覆盖"打身体"。

### 5.1.6 动画驱动

LateUpdate 里两行：`Speed = navAgent.velocity.magnitude`（用**实际寻路速度**驱动 Blend Tree，
静止=0 自然回 Idle）、`Grounded = true`（注释："NavMeshAgent 始终贴地，直接置 true 让 Jump 状态正常退出"）。
敌人动画控制器（MCP 实测）只有两层：Base Layer（Locomotion/Jump/Idle/Die）+
UpperBody（Fire_Upper/Reload_Upper/HitReaction/Empty）——比玩家少 Aiming/Reload/ChangeWeapon 三个独立层。
注意 UpperBody 里虽然存在 Reload_Upper 状态与 Reload 参数，EnemyBase 却从不 SetTrigger("Reload")——
状态在，无人驱动（如实记录：动画资产复用留下的闲置状态）。

## 5.2 EnemyPart：部位标记

[EnemyPart.cs](Assets/Scripts/Enemy/EnemyPart.cs) 全文 24 行，短到可以整读：

```csharp
// 摘自 EnemyPart.cs（全文）
public class EnemyPart : MonoBehaviour
{
    /// <summary>是否为头部（爆头倍率判定）</summary>
    public bool isHead;

    private EnemyBase _owner;

    /// <summary>所属敌人（首次访问时向上查找并缓存）</summary>
    public EnemyBase Owner
    {
        get
        {
            if (_owner == null)
                _owner = GetComponentInParent<EnemyBase>();
            return _owner;
        }
    }
}
```

它的存在意义是给碰撞体**贴标签**："我是这个敌人的头"。`Owner` 用懒查找（首次访问才
GetComponentInParent 并缓存）——因为 part 挂在头骨上，而 EnemyBase 在几十层之上的根物体，
构造时查浪费，用时查一次正好。消费端回看 4.2.2 的 `ApplyHit`：`part != null && part.isHead`
→ 爆头倍率；`part == null` → fallback 找 EnemyBase（身体）。

## 5.3 WaveConfig：波次的数据定义

[WaveConfig.cs](Assets/Scripts/Data/WaveConfig.cs)（26 行全文）是嵌套序列化 SO 的典型：

```csharp
// 摘自 WaveConfig.cs（全文）
[CreateAssetMenu(fileName = "WaveConfig", menuName = "TPS/WaveConfig")]
public class WaveConfig : ScriptableObject
{
    public Wave[] waves;

    [System.Serializable]
    public class Wave
    {
        public WaveEntry[] entries;         // 混兵种条目
        public float spawnInterval = 1.5f;  // 波内生成间隔（秒）
        public float startDelay = 5f;       // 波间准备时间（首波由生成器覆盖为 10s）
    }

    [System.Serializable]
    public class WaveEntry
    {
        public GameObject enemyPrefab;
        public int count;
    }
}
```

实测 `Assets/Data/WaveConfig.asset` 的 5 波配置：

| 波 | 编成 | interval | startDelay |
|----|------|----------|------------|
| 1 | Soldier×4 + Scout×2 + Heavy×2（**D6：首波 4/2/2 混编**） | 1.5s | 5s（首波实际 10s，见下） |
| 2 | Soldier×4 + Scout×4 | 1.5s | 5s |
| 3 | Soldier×5 + Scout×3 + Heavy×2 | 1.5s | 5s |
| 4 | Soldier×6 + Scout×4 + Heavy×2 | 1.5s | 5s |
| 5 | Soldier×5 + Scout×5 + **Heavy×4** | 1.5s | 5s |

读表可知难度曲线：总量 8 → 8 → 10 → 12 → 14，Heavy 从 2 稳步加到 4。
注意 `startDelay` 字段注释的那句"首波由生成器覆盖为 10s"——波间休息 5 秒，
但**游戏开始的准备时间**由生成器自己的 `firstWaveDelay=10` 字段控制（不归 SO 管）——
"第一波"与"波与波之间"是两个不同的计时器，配置权分置在两个组件上（复现时别找错地方）。

## 5.4 EnemyWaveGenerator：五态状态机

[EnemyWaveGenerator.cs](Assets/Scripts/Enemy/EnemyWaveGenerator.cs)（195 行）是波次玩法的发动机。
类注释就是它的架构图（值得整读）：

```csharp
// 摘自 EnemyWaveGenerator.cs 类注释
/// 敌人波次生成器（阶段5）：WaveConfig 驱动刷怪
/// 状态机：Preparing(首波10s) → Spawning(interval 逐刷) → Fighting(等全灭) → Intermission(startDelay 倒计时) → 下一波 | Victory
/// 敌人 Instantiate 直出（不入对象池：尸体 10s 延迟销毁使池化复杂度不值）
/// 出生点：距玩家 > minSpawnDistance 的点随机，无满足则取最远；NavMesh.SamplePosition 容差微调（不重烘焙）
/// 存活判定：List 登记 + IsDead 轮询（不能用 activeInHierarchy——尸体延迟销毁期间仍 active）
```

```text
Preparing ──10s──→ Spawning ──队列清空──→ Fighting ──全灭──→ Intermission ──5s──→ (下一波 Spawning)
   │                                                               │
   │                        Fighting 阶段末波全灭                      │
   └───────── Update 首行：非 Playing 不推进（防结算后刷怪） ──────→ Finished + TriggerVictory
```

### 5.4.1 状态机主体（Update switch）

```csharp
// 摘自 EnemyWaveGenerator.cs 的 Update（节选）
if (GameManager.Instance != null && GameManager.Instance.State != GameManager.GameState.Playing) return;
// 非 Playing（暂停/结算）不推进波次（暂停 timeScale=0 时 deltaTime=0 天然冻结，此处防结算后仍刷怪）

switch (_phase)
{
    case WavePhase.Preparing:
        _phaseTimer -= Time.deltaTime;
        if (_phaseTimer <= 0f) StartWave();
        break;

    case WavePhase.Spawning:
        _spawnTimer -= Time.deltaTime;
        if (_spawnTimer <= 0f && _spawnQueue.Count > 0)
        {
            SpawnNext();
            _spawnTimer = waveConfig.waves[_waveIndex].spawnInterval;   // 1.5s 逐个出
        }
        if (_spawnQueue.Count == 0)
            _phase = WavePhase.Fighting;
        break;

    case WavePhase.Fighting:
        _aliveEnemies.RemoveAll(e => e == null || e.IsDead);   // 清死、清已销毁
        if (_aliveEnemies.Count == 0)
            OnWaveCleared();
        break;

    case WavePhase.Intermission:
        _phaseTimer -= Time.deltaTime;
        if (_phaseTimer <= 0f) { _waveIndex++; StartWave(); }
        break;
}
```

三个值得点名的决策：

1. **Update 首行的 State 检查**是给"死亡结算不冻结 timeScale"（2.4.2）打补丁——
   暂停时天然冻结不用管，结算时世界还在跑、必须显式不刷怪；
2. **Fighting 的判活**：`RemoveAll(e => e == null || e.IsDead)`——
   `e == null` 清掉尸体已被 Destroy 的（Unity 假 null），`IsDead` 清掉刚死还没收尸的——
   **正是"尸体 10 秒延迟销毁"逼出来的双条件**。类注释里那句"不能用 activeInHierarchy"
   是踩过坑后的结论（尸体销毁前一直 active，用它判活波次永远不推进）；
3. **Spawning 与 Fighting 分开**：敌人逐个出来（1.5s 一个）的过程中不算"波开始清怪"，
   全部出完才进入等待全灭——状态机里每个状态只干一件事。

### 5.4.2 开波与刷怪：队列展开 + 出生点选择

```csharp
// 摘自 EnemyWaveGenerator.cs 的 StartWave / SpawnNext / PickSpawnPosition（节选）
private void StartWave()
{
    var wave = waveConfig.waves[_waveIndex];
    _spawnQueue.Clear();
    foreach (var entry in wave.entries)
        for (int i = 0; i < entry.count; i++)
            _spawnQueue.Add(entry.enemyPrefab);      // SO 编成 → 一维出怪队列

    if (GameManager.Instance != null)
        GameManager.Instance.SetCurrentWave(CurrentWaveNumber);
    GameEvents.RaiseWaveStarted(CurrentWaveNumber);   // 波次横幅（第 6 章）
    if (restockGrenadePerWave && grenadeThrower != null)
        grenadeThrower.AddGrenades(1);                  // 每波补 1 雷（D7），上限 3 钳制
    _phase = WavePhase.Spawning;
}

private void SpawnNext()
{
    var prefab = _spawnQueue[0];
    _spawnQueue.RemoveAt(0);
    var pos = PickSpawnPosition();
    var go = Instantiate(prefab, pos, Quaternion.identity);
    go.name = prefab.name + "_W" + CurrentWaveNumber + "_" + (++_spawnedCount);   // 如 Enemy_Scout_W1_3
    var enemy = go.GetComponent<EnemyBase>();
    if (enemy != null)
    {
        enemy.huntPlayer = true;          // 坑 67：直赋安全（新实例）
        _aliveEnemies.Add(enemy);
    }
}

private Vector3 PickSpawnPosition()
{
    ...
    // 距玩家 > minSpawnDistance 的点随机；无满足则取最远；SamplePosition 贴合 NavMesh
    foreach (var p in spawnPoints)
    {
        float d = Vector3.Distance(p.position, playerPos);
        if (d > farthestDist) { farthestDist = d; farthest = p; }
        if (d > minSpawnDistance) candidates.Add(p);
    }
    Transform pick = candidates.Count > 0
        ? candidates[Random.Range(0, candidates.Count)]   // 安全距离内随机
        : farthest;                                       // 全不满足取最远（兜底）
    ...
    if (NavMesh.SamplePosition(pos, out NavMeshHit hit, spawnSampleRadius, NavMesh.AllAreas))
        pos = hit.position;                               // 容差内吸附 NavMesh（不重烘焙）
}
```

四处工程巧思：

1. **SO 编成展开成一维队列**：出怪顺序就是 entries 顺序循环平铺（Soldier×4 先出完再 Scout×2……），
   `spawnInterval` 统一节奏——想做出怪花样只需改展开逻辑，状态机不用动；
2. **命名带波号序号**（`_W1_3`）——运行时在 Hierarchy 里一眼分清"这是第几波的第几个"，
   与场景里遗留的禁用测试敌人（无 `_W` 后缀）天然区分；
3. **出生点三层筛选**：20m 外随机（实测 `minSpawnDistance=20`）→ 全不满足取最远（玩家蹲在某个
   出生点上时的兜底）→ `NavMesh.SamplePosition` 在 2m 容差内吸附到 NavMesh
   （注释："不重烘焙"——出生点没落在 NavMesh 上时微调，而不是重烘场景）；
4. **每波补 1 颗手雷（D7，实测 restock=true）**：波次系统与资源系统握手的一行——
   上限钳制在 GrenadeThrower 里（4.5.1），生成器只管"给"。

### 5.4.3 收官：末波清空 → Victory

```csharp
// 摘自 EnemyWaveGenerator.cs 的 OnWaveCleared（节选）
private void OnWaveCleared()
{
    GameEvents.RaiseWaveCleared(CurrentWaveNumber);       // "WAVE N CLEARED" 横幅

    if (_waveIndex >= waveConfig.waves.Length - 1)
    {
        _phase = WavePhase.Finished;
        // D5：末波清空 → 胜利流程（GameManager 延迟 1s 后广播 OnGameVictory 弹结算）
        if (GameManager.Instance != null)
            GameManager.Instance.TriggerVictory();
        else
            GameEvents.RaiseGameVictory();                // 无 GameManager 的测试场景也能跑
    }
    else
    {
        _phaseTimer = waveConfig.waves[_waveIndex].startDelay;   // 波间休息（5s）
        _phase = WavePhase.Intermission;
    }
}
```

胜利流程（D5）在这里接入第 2 章的状态机：末波全灭 → `TriggerVictory` →
1 秒延迟 → `OnGameVictory` → 结算面板。**else 分支的兜底**让波次生成器在没有任何流程管理器的
测试场景里也能独立验收（2.4.3 提过的防御式设计）。同样地，Start 里 `SetMaxWave(TotalWaves)`
把总波数交给 GameManager——结算面板"N/5"显示的数据源。

### 5.4.4 为什么敌人不进对象池（再次）

生成器选择 `Instantiate` 直出，类注释给了理由："尸体 10s 延迟销毁使池化复杂度不值"。
把 5.1.4 的死亡工序连起来看：死亡 → 禁碰撞/禁导航 → 播 Die 动画 → **10 秒后才销毁**。
若入池：回池时机（等 10 秒？）与回池复位（碰撞体/导航/血量/动画/ huntPlayer——见坑 67）
都要重做一套——而波次敌人每波最多十几个，Instantiate 的开销完全可接受。
**"什么时候不用池"在这个项目里是被认真决策过的**，不是没想到。

## 5.5 敌人预制体与场景组装

### 5.5.1 制作敌人预制体（实测结构）

```text
Enemy_Scout / Enemy_Soldier / Enemy_Heavy（根）
  components : CapsuleCollider + NavMeshAgent(speed=3.5, stoppingDistance=0.5) + EnemyBase
  └── Character_*_Male_01        ← 包内人形模型，Animator(=EnemyAnimator.controller, Humanoid)
```

步骤：① 拖入包内人形模型做子物体（注意设为 **Humanoid** Avatar——SetupHeadPart 依赖
`avatar.isHuman`，Generic 类型将不挂头，爆头失效）；② 根物体挂 CapsuleCollider、
NavMeshAgent、EnemyBase；③ 按 5.1 差异表填参数；④ 存入 `Assets/Prefabs/`。
`NavMeshAgent.speed` 的实测值 3.5 是摆设——运行时被 EnemyBase 的 patrolSpeed/chaseSpeed 覆盖；
`stoppingDistance=0.5` 生效（决定"到达路点"的判定半径）。

### 5.5.2 场景组装（实测配置）

1. **烘焙 NavMesh**（没有它一切寻路都是空中楼阁）：选中地形/建筑模型 → Static（Navigation Static）→
   `Window → AI → Navigation`（AI Navigation 包，1.1.7）→ Bake。敌人巡逻/追击/出生点吸附全依赖它；
2. 建 `SpawnPoints` 空物体，下面摆 10 个空子物体 `Spawn_1 ~ Spawn_10`（实测 10 个，
   散布在地图边缘——生成器用"距玩家最远/超 20m"的规则选择）；
3. 建空物体挂 EnemyWaveGenerator，Inspector 实测配置：
   `waveConfig = WaveConfig.asset`、`spawnPoints` 数组拖入 10 个出生点、
   `firstWaveDelay=10`、`minSpawnDistance=20`、`restockGrenadePerWave=true`、
   `grenadeThrower` 拖入 Player 的 GrenadeThrower（**它还兼任"玩家位置来源"**——出生点距离、
   选点都以它为准，注释原话"兼作玩家位置来源"）；
4. 清理或保留场景里禁用的测试敌人（5.0 的实测发现——它们不参与运行）。

## 【验证方法】

对照历史截图（`S1_enemy_prefabs_verify.png`、`S4_wave1_hunting.png`、`S5b_countdown_ui.png`、
`S5_victory_panel.png`）按序验证：

1. **敌人行为**：波次敌人出生后直奔玩家（huntPlayer，无 20m 检测期）；靠近到 15m 停下、
   **面朝玩家**射击（转向权接管的验收点）；玩家躲到掩体后，敌人曳光打在掩体上、不掉血（即时弹道）；
2. **爆头判定**（5.1.5 两次修复的验收）：瞄准头顶——伤害×2 且 HUD 出 HEADSHOT 提示；
   瞄身体不出爆头。若几乎打不出爆头，回查胶囊体是否收缩到 1.4、头球是否 0.22/+0.08；
3. **三种性格**：Scout 追击时几乎赶得上你的冲刺速度；Heavy 站桩但一枪 6 伤害；
   同一枪打 Scout 明显比 Heavy 掉血快（60 vs 200）；
4. **波次流程全链**：开局 10 秒倒计时（HUD）→ 首波 8 个敌人按 1.5s 间隔逐出
   （Hierarchy 里出现 `_W1_1` 命名）→ 全灭后"WAVE 1 CLEARED"横幅 + 5 秒倒计时 → 第 2 波……
   → 第 5 波全灭 → 1 秒后胜利结算（波次 5/5）；
5. **每波补雷**：手雷 HUD 在每波开始时 +1（上限 3）；
6. **击杀联动**：每杀一个敌人 HUD 击杀数 +1（EnemyKilled → GameManager 计数 → KillCountChanged）；
7. **尸体纪律**：死亡敌人 10 秒后消失；尸体存在期间不挡子弹、不挡寻路（碰撞已禁用）；
   波次推进不被尸体卡住（IsDead 判活）；
8. **结算停刷**：玩家死亡后（世界仍活动）不再有新敌人刷出——Update 首行 State 检查生效。

## 【常见坑】

1. **NavMesh 没烘焙**：NavMeshAgent 全体失灵，Console 刷 "SetDestination could not find path"。Bake 是本章所有移动的前提（5.5.2 第 1 步）。
2. **默认 2m 胶囊吞掉爆头**：头包在胶囊里，射线永远先命中身体——4D 热修复的原案（5.1.5），收缩胶囊 + 头球外扩两步走。
3. **用 activeInHierarchy 判敌人存活**：尸体延迟销毁期间一直 active，波次永远不进下一波。用 `IsDead` + null 双条件（5.4.1）。
4. **池化复用忘重置 huntPlayer**：直出+直赋安全（坑 67 注释），一旦改池化就得在 OnEnable 复位——这是字段注释里埋的警报。
5. **站桩攻击不接管转向**：NavMeshAgent 只在移动时转模型，站桩开枪=背对玩家射击（5.1.3 注释原文）。`updateRotation=false` + 每帧 RotateTowards。
6. **isStopped 后不恢复**：攻击状态停住的 agent，切回追击若忘 `isStopped=false`，敌人永远定身——本类每个移动方法开头都先恢复（5.1.2）。
7. **寻路中重复设点**：`pathPending` 期间再 SetDestination 会抖动，巡逻循环要先跳过（5.1.2）。
8. **人形模型没设 Humanoid**：`GetBoneTransform(HumanBodyBones.Head)` 拿不到头骨，SetupHeadPart 静默跳过、爆头判定整个不存在（5.5.1）。
9. **敌人射线无 LayerMask**：会被友军挡弹（成了人肉掩体——顺带是个 feature）；与玩家侧同源问题，同解法。
10. **出生点贴脸**：全图只有一两个出生点且靠近玩家，波次一开就被围殴。出生点数量（实测 10）与 `minSpawnDistance=20` 的筛选要配套。
11. **把波次节奏写死在代码里**：本项目把编成/间隔/休息全放进 WaveConfig SO——"数据随时可调；测试期可临时调低再还原"（类注释原话），复现时请保持这一点。
12. **忘了敌人也要 GameEvents 纪律**：死亡/受击/开枪全走总线（RaiseEnemyKilled/Damaged/Shot），别在敌人里直接拉 UIManager——本章的敌人对 HUD/计分/音效一无所知，这正是它们能随时被三预制体替换的原因。

---

# 第 6 章 · UI 系统

## 【本章目标】

为游戏装上"会说话的眼睛"。学完本章你将能：

1. 搭一套完整 HUD（血条 / 弹药 / 医疗 / 手雷 / 击杀数 / 受伤红屏）并理解
   **一个 UI 类里三种数据源模式并存**（实例事件、全局总线、运行时轮询）的选型逻辑；
2. 实现"指到敌人就变红"的准星（屏幕中心射线）并把它同时用作自动射击的判定源；
3. 实现 HitMarker / HEADSHOT 爆头提示与波次横幅倒计时——并掌握 DOTween 在 UI 上的
   **"存引用、触发前 Kill"纪律**（本项目 4D 热修复的直接教训）；
4. 完成主菜单（设置持久化 + Loading 读条）、暂停面板与胜利/死亡结算面板；
5. 理解 UI 层与游戏层的边界：**UI 只听、只显示，最多"代为转达"**（按钮调 GameManager）。

## 【前置知识】

- **uGUI 的三件套**：`Canvas`（画布，UI 的根，本项目全部为 Screen Space - Overlay 模式——场景 YAML 实测 `m_RenderMode: 0`）、`GraphicRaycaster`（让画布能被点击）、`EventSystem`（按钮/滑块的总调度，**每个场景都要有一个**，实测两场景均有）。
- **CanvasGroup**：uGUI 的"整组透明度/可交互"组件——`alpha`（透明度）、`interactable`（可交互）、`blocksRaycasts`（挡不挡点击）。本项目所有面板的显示/隐藏/淡入淡出都靠它。
- **Text 与 TextMeshPro**：legacy `Text`（本项目 HUD 全用这个）与 TMP（更清晰但要字体资产）。本项目 UI 文本是中文（"弹药: 30 / 120"），用 legacy Text + 默认字体即可显示；换 TMP 需配置中文字体资产。
- **ScreenPointToRay**：把屏幕坐标转成世界射线——"屏幕中心往前打一条线"的标准做法（准星、瞄点投掷都用它）。
- **DOTween 的 UI 动词**：`DOFade`（CanvasGroup/Image/Text 透明度）、`DOScale`、`DOPunchScale`（脉冲弹跳）、`Sequence`（串联）；`SetUpdate(true)` 让动画不受 timeScale 冻结（第 2 章坑 5 的 UI 版）；`DOKill()`/`Kill()` 终止进行中的动画——本章反复出现的最后一个是重点。

## 6.0 UI 全景：六个控制器与它们的"听"

本章是"事件总线"成果的验收章——**UI 层几乎不主动查询游戏状态，全部靠订阅**。
先把六个控制器的数据源全景列出（每个格子都能在源码里指认）：

| 控制器 | 场景 | GameEvents 订阅 | 实例事件订阅 | 运行时轮询/查询 |
|--------|------|------------------|--------------|------------------|
| [UIManager.cs](Assets/Scripts/UI/UIManager.cs) | SampleScene | OnGrenadeCountChanged、OnKillCountChanged | PlayerHealth 的 OnHealthChanged/OnDamaged/OnMedkitChanged；WeaponManager 的 OnAmmoChanged/OnReserveAmmoChanged | 每 12 帧兜底刷血条；H 键检测 |
| [CrosshairController.cs](Assets/Scripts/UI/CrosshairController.cs) | SampleScene | — | — | 每帧屏幕中心射线 |
| [HitFeedbackUI.cs](Assets/Scripts/UI/HitFeedbackUI.cs) | SampleScene | OnHitConfirmed、OnHeadshotKilled | — | — |
| [WaveBannerController.cs](Assets/Scripts/UI/WaveBannerController.cs) | SampleScene | OnWaveStarted、OnWaveCleared | — | Update 轮询波次生成器属性 |
| [PauseMenuController.cs](Assets/Scripts/UI/PauseMenuController.cs) | SampleScene | — | — | Tab 键 + GameManager.State 查询 |
| [GameOverPanelController.cs](Assets/Scripts/UI/GameOverPanelController.cs) | SampleScene | OnGameOver、OnGameVictory | — | 结算时拉 GameManager 四项数据 |
| [MainMenuController.cs](Assets/Scripts/UI/MainMenuController.cs) | MainMenu | — | — | 按钮/滑块回调 |

三种模式的选型逻辑（现在你应该能自己推导了）：**会"变化"的游戏数据且发布者单一 → 实例事件**
（血量/弹药，HUD 与 Player 同场景紧耦合）；**跨系统广播 → 总线**（击杀数要过 GameManager 加工）；
**连续状态或需要每帧判断 → 轮询**（准星射线、倒计时秒数、Tab 键）。

## 6.1 UIManager：HUD 全家桶

（215 行）挂在 SampleScene 的 Canvas 物体上，六件 HUD 控件全部 Inspector 拖入
（名字与槽位对应：`HealthBar`→healthSlider、`AmmoText`、`MedkitText`、`GrenadeText`、
`KillText`、红晕图→redVignetteImage——场景对象名实测可一一对应）。

### 6.1.1 双通道订阅：一个类里的教科书

```csharp
// 摘自 UIManager.cs 的 Awake（节选）——同物体紧耦合：实例事件
GameObject player = GameObject.FindWithTag("Player");
...
_playerHealth.OnHealthChanged += UpdateHealthBar;      // 血条
_playerHealth.OnDamaged += OnPlayerDamaged;             // 红屏
_playerHealth.OnMedkitChanged += UpdateMedkitDisplay;   // 医疗包数
_weaponManager.OnAmmoChanged += UpdateAmmoDisplay;      // 弹匣
_weaponManager.OnReserveAmmoChanged += UpdateReserveDisplay;   // 备弹

// 摘自 UIManager.cs 的 OnEnable——跨系统：总线
GameEvents.OnGrenadeCountChanged += UpdateGrenadeDisplay;   // 手雷（GrenadeThrower 广播）
GameEvents.OnKillCountChanged += UpdateKillDisplay;         // 击杀（GameManager 加工后广播）
```

同一张 HUD，左边一半插在 Player 的实例事件上，右边一半挂在全局总线上——
第 2 章 2.1.4 的"双通道规则"在 UI 侧的标准答案。**退订纪律两处都有**：
实例事件在 `OnDestroy` 成对退订，总线事件在 `OnDisable` 成对退订（生命周期不同的两类通道，
退订位置也要跟着走）。

### 6.1.2 "主动拉取"：错过初始广播怎么办

订阅有个天生的时序裂缝：如果**发布者的 Start 比 UI 的 Start 先跑完**，初始那一次广播
（比如 GrenadeThrower.Start 里 `RaiseGrenadeCountChanged(3)`）就发生在你订阅之前——错过。
UIManager 的解法是**订阅之后再主动拉一次现值**（Start 里）：

```csharp
// 摘自 UIManager.cs 的 Start（节选，两处注释都值得读）
// 初始化手雷显示（Start 晚于 GrenadeThrower 的初始广播，这里主动拉一次）
if (thrower != null)
    UpdateGrenadeDisplay(thrower.GrenadeCount);

// 初始化击杀显示（Start 主动拉取，防错过初始广播——4E 手雷计数同模式）
if (GameManager.Instance != null)
    UpdateKillDisplay(GameManager.Instance.KillCount);
```

**"订阅 + 首拉"是事件驱动 UI 的标准姿势**——凡是"初始化时广播一次、之后按需广播"的数据，
订阅端永远要在 Start 里补一次主动拉取。同文件还有一处同思路的时序处理：

```csharp
// 摘自 UIManager.cs 的 SyncAmmoAfterWeaponReady
private IEnumerator SyncAmmoAfterWeaponReady()
{
    yield return null;   // 等一帧，让 WeaponManager.Start（找初始武器）跑完
    if (_weaponManager != null)
        UpdateAmmoDisplay(_weaponManager.GetCurrentAmmo(), _weaponManager.GetMaxAmmo());
}
```

武器初始化在"当帧稍后"完成（4.3.2 讲过的 Instantiate/Start 时序），UI 等一帧再同步弹药——
比事件、比轮询都朴素的第三种时序解法。

### 6.1.3 血条：事件为主、轮询兜底

```csharp
// 摘自 UIManager.cs 的 Update
private void Update()
{
    // 兜底刷新血条
    if (_playerHealth != null && Time.frameCount % 12 == 0)
        UpdateHealthBar(_playerHealth.CurrentHealth, _playerHealth.maxHealth);

    // 检测治疗按键
    if (_inputHandler != null && _inputHandler.IsHealPressed())
        _playerHealth?.TryHeal();
}
```

血条明明已经订阅了 `OnHealthChanged`，为什么还要每 12 帧兜底刷一次？
**防御性轮询**：万一某个掉血路径忘了广播（历史 bug 或新代码破坏事件链），血条最多
12 帧后自愈，玩家不会看到一个"假血条"。12 帧一次的开销可忽略。
这种"事件为主、低频轮询兜底"的混搭在展示层是务实的。

另一个值得讨论的点：**治疗按键（H）写在 UIManager 里**——输入→PlayerHealth.TryHeal()。
按"UI 不该碰游戏逻辑"的洁癖标准这是越界（该写在 PlayerController 里）；
按本项目的务实标准：治疗是"按键→HUD 医疗包数变化"的联动行为，放 UI 层让这条链最短。
如实记录这个争议点，复现时你可以把按键检测挪进 PlayerController——功能不变，架构更纯。

### 6.1.4 受伤红屏：协程版"闪一下"

```csharp
// 摘自 UIManager.cs 的 OnPlayerDamaged / RedFlashRoutine
private void OnPlayerDamaged(float damage)
{
    if (redVignetteImage == null) return;
    if (_redFlashCoroutine != null) StopCoroutine(_redFlashCoroutine);   // 新伤覆盖旧伤
    _redFlashCoroutine = StartCoroutine(RedFlashRoutine());
}

private IEnumerator RedFlashRoutine()
{
    SetVignetteAlpha(redFlashPeakAlpha);       // 瞬间红色峰值（0.4）
    float elapsed = 0f;
    while (elapsed < redFlashDuration)         // 0.35 秒渐隐
    {
        elapsed += Time.deltaTime;
        SetVignetteAlpha(Mathf.Lerp(redFlashPeakAlpha, 0f, elapsed / redFlashDuration));
        yield return null;
    }
    SetVignetteAlpha(0f);
}
```

受伤时"瞬间打红、快速消退"的径向晕影（redVignetteImage，一张径向渐变贴图）。
连续挨打时 `StopCoroutine` 让新的闪烁覆盖旧的——与 CameraShaker 的"取最大值"
（2.3.3）是同一问题的两种解法：红屏选"最新优先"（持续掉血要一直有反馈），震屏选"最强优先"。
哪个对？都对——取决于反馈的语义。`Screenshots/RedVignette_*`、`vignette_*` 系列截图
记录了这套红屏的完整调试过程。

## 6.2 CrosshairController：准星射线

全文只有 41 行，可以整读：

```csharp
// 摘自 CrosshairController.cs（全文，类 CrosshairController）
public class CrosshairController : MonoBehaviour
{
    public Image crosshairImage;
    public Color normalColor = Color.white;
    public Color enemyColor = Color.red;
    public float maxDistance = 200f;
    public LayerMask raycastLayers = ~0; // 默认所有层

    /// <summary>准星当前是否指向敌人（供自动射击等系统使用）</summary>
    public bool IsOnEnemy { get; private set; }

    private void Update()
    {
        if (crosshairImage == null) return;
        if (Camera.main == null) return;

        // 屏幕中心射线
        Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f));
        IsOnEnemy = false;

        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, raycastLayers))
        {
            if (hit.collider.GetComponentInParent<EnemyBase>() != null)
                IsOnEnemy = true;
        }

        crosshairImage.color = IsOnEnemy ? enemyColor : normalColor;
    }
}
```

三个观察：

1. **`raycastLayers = ~0`**：全项目唯一**自带 LayerMask 字段**的射线——射击（4.2）、
   敌人（5.1）、瞄点（4.5）的射线都是裸奔，只有准星把掩码做成了 Inspector 参数。
   类注释里的"A+B 方案"是项目内部方案编号，仓库内无更多文档说明其具体所指
   （从代码看本类确实同时完成"射线判定 + 变色显示"两件事，仅作参考性解读）；
2. **`IsOnEnemy` 是公开属性**——它让这个"表现层脚本"成了自动射击（4.3.1）的判定源。
   UI 不只是给玩家看的，**准星的"指哪"本身就是游戏逻辑的一部分**；
3. 准星贴图 `Assets/UI/crosshair.png` 挂在 Canvas 上（Inspector 拖入 crosshairImage）。

## 6.3 HitFeedbackUI：HitMarker 与 HEADSHOT（4D）

（71 行）订阅两个命中事件，用"闪现"与"弹出"分别反馈普通命中与爆头击杀：

```csharp
// 摘自 HitFeedbackUI.cs（节选）
private void OnEnable()
{
    GameEvents.OnHitConfirmed += HandleHitConfirmed;
    GameEvents.OnHeadshotKilled += HandleHeadshotKilled;
}
...
/// <summary>命中：标记放大淡出（连发时先杀旧动画防叠加闪烁）</summary>
private void HandleHitConfirmed()
{
    if (hitMarkerImage == null) return;
    hitMarkerImage.DOKill();                    // ← 关键：正在淡出的旧动画立即终止
    hitMarkerImage.gameObject.SetActive(true);
    hitMarkerImage.color = Color.white;
    hitMarkerImage.transform.localScale = Vector3.one * markerScale;   // 1.3 倍弹出
    hitMarkerImage.DOFade(0f, markerShowTime).OnComplete(() => ...SetActive(false));   // 0.15s 淡出
}
```

连发时 HitMarker 每 0.15 秒就要重播一次——**上一发还没淡完新的一发又来**，
不 `DOKill()` 就会有两条 fade 叠在一起闪烁。HEADSHOT 提示同理，但用的是 Sequence + Kill：

```csharp
// 摘自 HitFeedbackUI.cs
private Sequence _headshotSeq;   // 连续爆头时先杀旧序列，防止旧 OnComplete 中途隐藏新提示
```

注释把不 Kill 的后果写得非常具体："旧 OnComplete 中途隐藏新提示"——爆头提示正在显示时
又爆了一个头，旧序列走到自己的淡出环节把新提示藏掉了。**这类 bug 只在连续触发时出现**，
单测永远发现不了。

类注释对控件来历的交代是："Image/Text 引用场景中赋值（均为程序化生成控件）"。
但**编辑器实测场景层级后发现注释与现状不符**：`HitMarker` 与 `HeadshotText` 如今是
Canvas 下**已保存的场景控件**（默认 active=False，槽位直接引用它们）——
推测它们最初是运行时程序化生成的，某次修复时被固化保存进了场景，类注释没有随之更新。
**再次验证"以代码/实测为准，不以注释为准"**：本项目的程序化 UI 如今一处都不存在。

## 6.4 WaveBannerController：横幅 + 倒计时（阶段 5，S5b）

（150 行）是六个 UI 控制器里动画管理最重的。它的类注释第一段就把纪律写在脸上：

```csharp
// 摘自 WaveBannerController.cs 类注释
/// 纪律（4D 热修复教训）：所有 Sequence/Tween 存字段引用，触发前 Kill 旧 tween 防叠加
```

本类持有**四个** tween 引用（`_bannerSeq / _countdownSeq / _pulseTween / _fadeOutTween`），
外加 `KillCountdownTweens()` 与 `KillAllTweens()` 两个清理函数——每次触发任何动画前，
先把同通道的旧动画杀掉。这套纪律与 6.3 同源（4D 热修复的教训写进了后续代码的类注释），
但推广到了"多条通道各自管理"的规模。

### 6.4.1 两种数据源的混合：事件开横幅，轮询跑倒计时

```csharp
// 摘自 WaveBannerController.cs（节选）
private void OnEnable()
{
    GameEvents.OnWaveStarted += HandleWaveStarted;       // "WAVE N"
    GameEvents.OnWaveCleared += HandleWaveCleared;       // "WAVE N CLEARED"
}

private void HandleWaveStarted(int wave) => ShowBanner("WAVE " + wave);

private void Update()
{
    // 倒计时窗口：首波准备(Preparing)与波间(Intermission)均显示
    string phase = waveGenerator.PhaseName;
    bool counting = (phase == "Preparing" || phase == "Intermission")
                    && waveGenerator.CountdownRemaining > 0f;
    ...
    // 每秒刷新文本 + 跳动脉冲
    string text = "第 " + NextWaveNumber() + " 波来袭 " + Mathf.CeilToInt(waveGenerator.CountdownRemaining) + "s";
    if (text != _lastCountdownText)   // 文本变了才跳
    {
        _lastCountdownText = text;
        bannerText.text = text;
        if (_pulseTween != null && _pulseTween.IsActive()) _pulseTween.Kill();
        _pulseTween = bannerText.transform.DOPunchScale(Vector3.one * tickPunch, 0.25f, 1, 0.5f).SetUpdate(true);
    }
}
```

**横幅**是离散事件（波开始/清空），走总线订阅——发布者是波次生成器（5.4.2 的
`RaiseWaveStarted/RaiseWaveCleared`）；**倒计时**是连续状态（剩余秒数每帧在变），
事件驱动要每秒广播一次太浪费——直接轮询生成器的公开属性（`PhaseName`、
`CountdownRemaining`，5.4 里 `[SerializeField]` 旁边的三个只读属性就是为这准备的数据出口）。
"每秒跳一下"的检测也很讲究：不是计时器，而是**比较文本内容**（`text != _lastCountdownText`）
——文本一变就是过了一秒，脉冲触发，天然防重复。

### 6.4.2 让位逻辑与 SetUpdate

横幅和倒计时**共用同一个 bannerText**（一物两用节省控件）——于是必须处理"同时发生"：
波清空的横幅弹出时倒计时不能抢，注释原文"大字横幅播放中让位，结束后再弹入倒计时"
（`if (_bannerSeq != null && _bannerSeq.IsActive()) return;`）。

全部动画都带 `SetUpdate(true)`——倒计时/横幅出现的时机附近可能被暂停（Tab）打断，
不受 timeScale 冻结的动画保证暂停恢复后面板状态连贯（2.4.2 的连锁决策之一）。

## 6.5 MainMenuController：主菜单

（233 行，挂 MainMenu 场景）的核心结构是**三个 CanvasGroup 面板**：
`menuPanel`（主菜单）/ `settingsPanel`（设置）/ `loadingPanel`（读条）。
设置项的读写与 PlayerPrefs 键名的"考古地层"在第 2 章 2.2.2 已拆过，这里补 UI 侧的三个部件。

**面板切换**的通用函数（淡出旧的、淡入新的）：

```csharp
// 摘自 MainMenuController.cs 的 SetPanel
private void SetPanel(CanvasGroup show, CanvasGroup hide)
{
    if (hide != null)
    {
        hide.interactable = false;
        hide.blocksRaycasts = false;                        // 淡出后不再挡点击
        hide.DOFade(0f, panelFadeDuration).OnComplete(() => hide.gameObject.SetActive(false));
    }
    if (show != null)
    {
        show.gameObject.SetActive(true);
        show.alpha = 0f;
        show.interactable = true;
        show.blocksRaycasts = true;
        show.DOFade(1f, panelFadeDuration);
    }
}
```

注意 CanvasGroup 的**三件套同步**（alpha / interactable / blocksRaycasts）——
只改 alpha 不关后两项，"看不见的面板"仍然会挡住背后的按钮点击（新手 UI 的头号灵异事件）。

**按钮悬停缩放**用 EventTrigger 挂 PointerEnter/Exit 两个回调打 DOScale（hoverScale 1.06）；
**Slider 填充手动控制**是给 Unity 的小坑打的补丁（类内注释："代码创建的 Slider 自动 Fill 不生效，
改为监听值变化更新 fillRect"）——每帧把 fillRect 的 anchorMax 拉到 normalizedValue。

**Loading 读条**（1.6 节流程图里"Loading 读条 → 切场景"的实现体）值得整段读：

```csharp
// 摘自 MainMenuController.cs 的 LoadGameSceneAsync（节选）
var op = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("SampleScene");
op.allowSceneActivation = false;                 // 加载完成也先不切，等动画

// 进度条模拟动画（TPS Shooter 同款：约 1 秒平滑走完）+ 百分比实时刷新
if (loadingProgress != null)
{
    loadingProgress.DOValue(1f, 1f).SetEase(Ease.OutCubic)
        .OnUpdate(() => loadingProgressText.text = Mathf.RoundToInt(loadingProgress.value * 100f) + "%");
}

// 等待场景加载完成（真实进度达到 0.9 即完成）
while (op.progress < 0.9f)
    yield return null;

yield return new WaitForSeconds(0.3f);           // 动画收尾停留
loadingProgress.value = 1f;
loadingProgressText.text = "100%";
yield return new WaitForSeconds(0.2f);
op.allowSceneActivation = true;                  // 现在才真正切场景
```

这段代码**诚实而老练**：异步加载的真实 progress 在 allowSceneActivation=false 时**永远停在 0.9**
（Unity 的设计——最后 0.1 是"激活"），所以进度条干脆做成纯动画（1 秒走完的假进度），
真实加载用 `progress < 0.9f` 判完成，动画与真进度各走各的、在结尾汇合。
假进度条是行业惯例，这里把它写明白了。

## 6.6 PauseMenuController 与 GameOverPanelController

这两个面板的状态机职责（timeScale、禁用脚本、场景加载）在阶段 5 已全部迁移 GameManager
（2.4），它们如今是**纯 UI**。

**PauseMenuController**（182 行）的骨架：Update 里查 `GameManager.Instance.State`
决定 Tab 的行为（结算态无响应、Paused 态则 Resume、Playing 态则 Pause），
面板显示全带 `SetUpdate(true)`（timeScale=0 下动画要能动）。设置面板的四个滑块直写
PlayerPrefs 字符串字面量（2.2.2 考古地层的第三层），自动射击开关走 GameSettings.SetAutoShoot
（F2：与主菜单对称）。**滑块 Fill 的手动修复**（SetupSliderFills）与主菜单同一套——
两个类各有一份相同逻辑的代码（复现时值得抽成静态工具类，如实指出这是一处可优化重复）。

**GameOverPanelController**（99 行）是"死亡/胜利共用一个面板"的实现：

```csharp
// 摘自 GameOverPanelController.cs 的 Show（节选）
private void ShowGameOver() => Show(deathTitle, deathColor);   // "YOU DIED" 红
private void ShowVictory() => Show(victoryTitle, victoryColor); // "VICTORY" 绿

private void Show(string title, Color color)
{
    titleText.text = title;
    titleText.color = color;

    // 数据从 GameManager 拉取（重玩后场景重载，面板状态随之全清）
    if (GameManager.Instance != null)
    {
        kills = GameManager.Instance.KillCount;
        wave = GameManager.Instance.CurrentWave;
        maxWave = GameManager.Instance.MaxWave;
        survival = GameManager.Instance.SurvivalTime;
    }
    statsText.text = string.Format("击杀数：{0}\n存活波次：{1}/{2}\n存活时间：{3:00}:{4:00}",
        kills, wave, maxWave, (int)(survival / 60f), (int)(survival % 60f));
    ...
    _showSeq = DOTween.Sequence().SetUpdate(true);   // 淡入 + 标题 OutBack 弹出
}
```

同一面板换两套文案与配色——**一套 UI 服务两个终点**，比做两个面板省一半维护。
击杀数/波次/存活时间全部**拉**自 GameManager（2.4.1 的"流程数据归流程管理器"在消费端闭环）；
`SetUpdate(true)` 的原因在类注释里写明："死亡不置 timeScale=0，但保持与暂停动画同模式防冲突"。
按钮回调里先 `GameEvents.RaiseUIClick()`（2.1 的那个至今无音效资产的调用——4B 预留）
再转调 `GameManager.Instance.Restart()/ReturnToMenu()`——**UI 层不执行跳转，只代为转达**。

## 6.7 场景组装

> 以下 SampleScene 层级为**编辑器脚本实测**（MCP 连接 Unity 后逐层遍历 Canvas 与槽位引用）；
> MainMenu 场景当时未加载，其层级仍由场景对象名（YAML）+ 脚本槽位重建，控件与槽位对应关系是代码级事实。

**SampleScene 的 Canvas**（Screen Space - Overlay，含 EventSystem）——
六个 UI 脚本**全部挂在同一个 Canvas 物体上**（外加 CanvasScaler 与 GraphicRaycaster）：
UIManager、CrosshairController、PauseMenuController、HitFeedbackUI、GameOverPanelController、WaveBannerController。

```text
Canvas（挂上述 6 脚本）
├── HealthBar（Slider → UIManager.healthSlider；Background / Fill Area / Handle Slide Area）
├── AmmoText      （Text → UIManager.ammoText：    "弹药: {弹匣} / {备弹}"）
├── MedkitText    （Text → UIManager.medkitText：  "医疗包: {数量}"）
├── RedVignette   （Image → UIManager.redVignetteImage，径向渐变受伤晕影）
├── Crosshair     （Image → CrosshairController.crosshairImage，crosshair.png）
├── HitMarker     （Image → HitFeedbackUI.hitMarkerImage，默认 active=False）
├── HeadshotText  （Text  → HitFeedbackUI.headshotText，默认 active=False）
├── GrenadeText   （Text → UIManager.grenadeText： "手雷: {数量}"）
├── KillText      （Text → UIManager.killText：    "击杀: {数量}"）
├── WaveBannerText（Text → WaveBannerController.bannerText，横幅与倒计时共用）
├── PausePanel（active=False，CanvasGroup → PauseMenuController.pausePanel）
│     └── Mask / Title / ResumeButton / SettingsButton / MainMenuButton
├── PauseSettingsPanel（active=False，CanvasGroup → settingsPanel）
│     └── PanelBg / Title / SensitivitySlider / MasterVolumeSlider / SFXVolumeSlider
│          / FOVSlider / BackButton / AutoShootToggle
└── GameOverPanel（active=False，CanvasGroup → GameOverPanelController.panel）
      └── TitleText / StatsText / RestartButton / MenuButton
```

**MainMenu 的 Canvas**（Screen Space - Overlay，含 EventSystem）：

```text
Canvas（挂 MainMenuController）
├── Title / Subtitle
├── MenuPanel        （CanvasGroup：PlayButton / SettingsButton / ExitButton）
├── SettingsPanel     （CanvasGroup：四个滑块 + AutoShootToggle + BackButton）
└── LoadingPanel     （CanvasGroup：ProgressBar(Slider) + 百分比 Text + 标题呼吸动效）
```

组装步骤：① 每场景建 Canvas + EventSystem（GameObject → UI → Canvas/Event System）；
② 按上表摆放控件并**命名与脚本槽位一一对应**（命名不一致就是未来断链的隐患——3.5 的 F4 教训）；
③ 把脚本挂到 Canvas，Inspector 把各控件拖入对应槽位；④ PauseMenu 的 `grenadeThrower` 等跨物体引用拖入。

## 【验证方法】

HUD 是本项目修得最勤的部分（`HUD_*`、`hud_healthbar_*`、`fix_HUD`、`fix_dead_hud` 等十余张截图），
逐项对照：

1. **血条**（对照 `hud_healthbar_final_verify.png`）：初始满格、被打随血量动、12 帧兜底（可临时把
   OnHealthChanged 广播注释掉验证兜底生效）；
2. **红屏**（对照 `vignette_real_attack.png`、`RedVignette_Fix_Verification.png`）：受击瞬间
   0.4 透明度峰值、0.35 秒消退；连续受击时重新打满（新覆盖旧）；
3. **弹药**：开枪递减、换弹补满、备弹同步、切枪联动（F1）；医疗/手雷/击杀数各自联动；
   死亡后 HUD 血条归零但不再响应（`fix_dead_hud.png`）；
4. **准星**：指着敌人变红、移开变白；开启自动射击（主菜单/暂停设置）后指敌自动开火；
5. **命中反馈**（对照 `fix_shoot_feedback.png`）：每发命中 HitMarker 闪 0.15 秒（连发不闪烁——
   DOKill 生效）；爆头击杀出 HEADSHOT 弹出提示；
6. **横幅倒计时**（对照 `S5b_countdown_ui.png`）：开局"第 1 波来袭 10s"每秒跳动，
   开波"WAVE 1"弹出停 2 秒淡出，清空"WAVE 1 CLEARED"，波间倒计时正确显示下一波号；
7. **主菜单**（对照 `UI_Fix_MainMenu_GameView.png`）：三面板切换淡入淡出、滑块即时写盘
   （改灵敏度进游戏立即生效）、Loading 假进度条 1 秒走完后切场景；
8. **暂停与结算**（对照 `S2_pause_panel_migrated.png`、`S3_gameover_panel.png`、`S5_victory_panel.png`）：
   Tab 淡入暂停面板（暂停中世界冻结）、设置滑块生效；死亡 1.5 秒后红色 YOU DIED + 三行数据、
   胜利绿色 VICTORY；重玩按钮重载场景后所有 HUD 归零。

## 【常见坑】

1. **忘建 EventSystem**：按钮/滑块全部无响应且不报错——每个场景一个（两场景实测都有，别在复制场景时弄丢）。
2. **CanvasGroup 只改 alpha**：看不见的面板仍挡点击。三件套（alpha/interactable/blocksRaycasts）必须同步（6.5 的 SetPanel）。
3. **timeScale=0 下 UI 动画冻结**：暂停面板淡入卡死在 alpha=0。所有暂停态面板动画必须 `SetUpdate(true)`（本项目 PauseMenu/GameOverPanel/WaveBanner 全部如此）。
4. **Tween 不 Kill 就重触发**：连发 HitMarker 叠加闪烁、连续爆头提示被旧 OnComplete 藏掉（4D 热修复原案）。存字段引用、触发前 Kill（6.3/6.4 的纪律）。
5. **代码创建的 Slider 没有 Fill**：uGUI 的已知怪癖，手动驱动 fillRect 的 anchorMax（MainMenu 与 PauseMenu 各打了一次同样的补丁——重复代码，复现时建议抽公共工具）。
6. **错过初始广播**：订阅者 Start 晚于发布者 Start，首帧数据丢失（血条空、手雷数空）。订阅后 Start 里主动拉一次现值（6.1.2）。
7. **legacy Text 显示中文**：默认字体（Arial 动态 fallback）能显示，但换字体资产时中文会变方块——本项目 HUD 文本含中文（"弹药:"），换字体必须带中文字符集。
8. **准星图片与射线中心不一致**：Image 没放屏幕正中（anchor 错），射线判定与视觉准星错位——Image 的 anchor 必须设 (0.5, 0.5)。
9. **每帧字符串拼接的 GC**：`$"弹药: {current}"` 在 Update 级频率下产生持续 GC 分配。本项目弹药/血条走事件+12 帧兜底、倒计时按秒触发，频率可接受；若做实时数值（如 FPS 显示）请改 StringBuilder 或数值变化才更新。
10. **UI 里写游戏逻辑**：按钮回调里直接 LoadScene/改 timeScale——本项目已把这些职责收回 GameManager（阶段 5 S2 迁移），UI 只"代为转达"。复现时守住这条线。
11. **同一反馈做两套 UI**：本项目死亡/胜利共用一个结算面板（换文案配色），比两套面板少一半维护——先想清楚"差异是不是只有文案"再决定拆不拆。
12. **波次倒计时用事件广播**：连续状态（每帧变化的秒数）用事件等于每帧 Invoke——轮询生成器公开属性 + 文本变化检测（6.4.1）才是对的粒度。

---

# 第 7 章 · 音频与场景串联

## 【本章目标】

本章是"串联章"——把前六章的独立系统放进**一局游戏的完整时间线**里跑一遍：

1. 读完 [AudioManager.cs](Assets/Scripts/Audio/AudioManager.cs)（208 行）——本项目"事件驱动、插入即用"
   架构的最佳验证对象：它**代码全就绪、资产全为空**（13 个音效槽位实测零填），却不妨碍整局游戏正常运行；
2. 画出 **MainMenu → SampleScene → 结算 → 重玩/回菜单**的完整时序图，理解场景重载时
   "什么被销毁、什么幸存、什么躺在磁盘上"；
3. 复盘 ObjectPool 在一局游戏中的完整生命周期（预热 → 吞吐 → 场景切换自愈）。

## 【前置知识】

- **AudioSource 的两个维度**：2D 音（`spatialBlend=0`，UI 音，永远满音量）与 3D 音（`spatialBlend=1`，世界音，按距离衰减）；
  **AudioListener**（挂在相机上的"耳朵"，全场景只该有一个）负责最终混音。
- **Play() vs PlayOneShot()**：Play 会**打断**该音源正在播的一切；PlayOneShot 把音效"叠加"进音源，互不打断。
  枪声/脚步这类可重叠的音效必须用 PlayOneShot。
- **Unity 的"假 null"**：Inspector 里从未赋值的引用字段不是真正的 `null`——它是一个"指向空"的序列化包装。
  用 `== null` 比较它会返回 true（Unity 重载了运算符），但**直接访问它的成员会抛
  `UnassignedReferenceException`**。本章实测槽位时正好撞上了一次，教学价值极高。
- **场景重载的边界**：`SceneManager.LoadScene` 卸载当前场景时，场景内所有对象销毁——
  但 `DontDestroyOnLoad` 的对象、`static` 的数据、PlayerPrefs 的磁盘数据**不受影响**。
  订阅了静态事件的场景对象销毁时若不退订，就变成"幽灵订阅"（2.1.3）。

## 7.0 全景：本章三件事

```text
① AudioManager —— 事件→声音的终点站（当前静音但满勤）
② 场景串联 —— 开始/重玩/回菜单三条链路的时序与对象存活
③ ObjectPool —— 从 Prewarm 到场景自愈的一局生命周期
```

## 7.1 AudioManager 全解

### 7.1.1 结构：单例 + 运行时自建音源

AudioManager 挂在 SampleScene 的 `AudioManager` 空物体上（场景级，**没有 DontDestroyOnLoad**——
每个需要音效的场景自己放一个）。实测编辑态该物体上**一个 AudioSource 组件都没有**：
全部音源由 `SetupSources()` 在 Awake 里动态创建：

```csharp
// 摘自 AudioManager.cs 的 SetupSources
/// <summary>创建 2D UI 音源 + 3D 音源池</summary>
private void SetupSources()
{
    _uiSource = gameObject.AddComponent<AudioSource>();
    _uiSource.spatialBlend = 0f;      // 2D：UI 点击音
    _uiSource.playOnAwake = false;

    for (int i = 0; i < sfxSourceCount; i++)      // 默认 8 个
    {
        var src = gameObject.AddComponent<AudioSource>();
        src.spatialBlend = 1f;        // 3D：世界音
        src.playOnAwake = false;
        _sfxSources.Add(src);
    }
    ApplyVolumes();
}
```

**为什么用音源池而不是一音一源？** 同一瞬间可能叠加枪声、脚步、换弹、命中——
9 个 AudioSource（1 UI + 8 SFX）轮询复用，够塞下一帧的所有音效且不用动态增删组件：

```csharp
// 摘自 AudioManager.cs 的 AcquireSfxSource
/// <summary>轮询取一个空闲 3D 音源，全部占用时复用最早的（Newest wins 交给 PlayOneShot 叠加）</summary>
private AudioSource AcquireSfxSource(Vector3 pos)
{
    if (_sfxSources.Count == 0) return null;
    var src = _sfxSources[_nextSfxIndex];
    _nextSfxIndex = (_nextSfxIndex + 1) % _sfxSources.Count;   // 环形轮询
    src.transform.position = pos;
    src.volume = GameSettings.SFXVolume;                        // SFX × Master（Master 由 AudioListener 承担）
    return src;
}
```

注释里那句"Newest wins 交给 PlayOneShot 叠加"值得琢磨：轮询到哪个源就用哪个，
即便它还在播上一声——因为 PlayOneShot 是叠加式的，旧声音不会被掐断，只是两声混在一起。
**对短促音效，这比"找空闲源"的方案简单且听感无损**。

### 7.1.2 事件 → 声音：分发层

10 个订阅的"处理函数→播放"的分流只有两条路：`Play2D(clip)`（UI 音，进 `_uiSource`）
与 `Play3D(clip, pos)`（世界音，Acquire 一个 SFX 源）。两个入口的第一行都是
`if (clip == null) return;`——**这就是"资产为空时静默跳过"的全部实现**。没有警告、没有日志，
空槽位=这一路音效不存在。整个音频系统因此可以在零资产状态下安全运行（7.2 的主角）。

### 7.1.3 按武器名匹配枪声：一次广播多路分发

```csharp
// 摘自 AudioManager.cs 的 PlayShot
/// <summary>按武器名选枪声音效播放（3D，位置=枪口）</summary>
public void PlayShot(Vector3 pos, string weaponName)
{
    _currentWeaponPosition = pos;
    AudioClip clip;
    if (!string.IsNullOrEmpty(weaponName))
    {
        string n = weaponName.ToLowerInvariant();
        if (n.Contains("smg")) clip = playerShotSMG;
        else if (n.Contains("pistol")) clip = playerShotPistol;
        else clip = playerShotRifle;    // 步枪为默认兜底
    }
    else
    {
        clip = playerShotRifle;
    }
    Play3D(clip, pos);
}
```

回忆 4.2.1：`GameEvents.RaisePlayerShot(muzzle.position, weaponData.name)` 传的是
**SO 的资产名**（"RifleData" / "PistolData" / "SMGData"）。用 `Contains` 子串匹配——
"RifleData" 不含 smg/pistol → 默认步枪兜底；"PistolData" 含 pistol；"SMGData" 含 smg。
三把武器各配各的枪声，**广播方（FirearmWeapon）对音效一无所知**，匹配逻辑全在音频侧消化。

还有一个藏在 `_currentWeaponPosition = pos;` 里的补丁——**换弹音的位置问题**：

```csharp
// 摘自 AudioManager.cs（字段注释）
// 换弹音跟随玩家武器位置（换弹事件无位置参数，缓存最近一次开枪位置）
private Vector3? _currentWeaponPosition;

private void HandleReloadStart()
{
    if (_currentWeaponPosition == null) return;
    PlayReload(_currentWeaponPosition.Value, false);
}
```

`OnReloadStart` 事件**没有位置参数**（GameEvents 21 个事件里它是最"裸"的那批），
但换弹音是 3D 音需要坐标——缓存最近一次开枪的枪口位置顶上。**"事件签名不够能用现场信息补"**
的务实小技巧；代价是"没开过枪就换弹则无换弹音"（边界上可接受，如实记录）。

### 7.1.4 脚步音的双通道现状

音频相关的脚步实现有两处，实测都**没接通**：

1. `AudioManager.PlayFootstep(pos, speed)`（随机变体 + 随机 pitch 0.9~1.1）——
   grep 全项目**没有任何调用方**。动画事件只能调用"挂在该 Animator 物体上的组件"的公共方法，
   而 AudioManager 挂在场景物体上，角色模型的动画事件够不着它——这个方法是**备而未用的死接口**
   （4B 接入时的候选入口之一）；
2. [FootstepReceiver.cs](Assets/Scripts/Player/FootstepReceiver.cs) 的 `footstepAudio`
   （角色模型物体上的独立 AudioSource，3.8.3 介绍过）——实测**未拖入音源**，`PlayFootstepSound()`
   因此空转。

如实结论：**当前版本脚步完全无声**，两条路都只差"把音效拖进槽位"这最后一步。

### 7.1.5 音量链

```text
主音量 Master ──→ AudioListener.volume（全局耳朵的总音量，GameSettings.ApplyAll 应用）
音效音量 SFX  ──→ 每个 3D 音源的 volume（AcquireSfxSource 播放时设置）
UI 音         ──→ _uiSource.volume = 1（Master 由 AudioListener 统一承担）
```

即：3D 音效的实际响度 = SFX × Master（两个乘法节点），UI 音 = Master。
暂停菜单里拖主音量滑块立即改 AudioListener（PauseMenuController L76 直写），
SFX 音量在每次播放时读缓存——**音量体系不需要任何事件广播，全是拉取**。

## 7.2 4B 挂起：实证与接入路径

### 7.2.1 槽位实测：13 路全空

用编辑器脚本反射 AudioManager 的全部字段，实测结果：

```text
playerShotRifle / playerShotSMG / playerShotPistol = <空>
reloadStartClip / reloadEndClip                     = <空>
playerHurtClip / enemyHurtClip / enemyDeathClip / enemyShotClip = <空>
pickupClip / explosionClip / uiClickClip            = <空>
footstepClips[] = 0 个
结论：单 AudioClip 槽位已填 = 0 / 12，脚步数组 0 —— 全部为空
```

实测过程还撞上一个小插曲，正好把 7.1 的"假 null"知识点演给你看：反射拿到
`playerShotRifle` 字段后直接访问 `.name`，抛出的是
`UnassignedReferenceException: The variable playerShotRifle of AudioManager has not been assigned`
——**Inspector 空槽位的引用不是真 null**，用 `== null` 判空为 true（Unity 重载的比较），
访问成员却直接炸。这就是 2.3.1 ObjectPool"假 null 跳残骸"用 `inst == null` 能工作的原因，
也是新手"明明判了空还是 NullReference"怪谈的真相。

### 7.2.2 为什么挂起：API Key 未配置

AudioManager 类注释写着"音频资产在 4B 生成后由 Inspector 拖入"——**4B 阶段的原计划是用
AI 生成音效**（Unity MCP 工具链里有音频生成能力，但需要外部 AI 服务的 API Key，
本项目从未配置），于是资产生成环节停摆至今。AGENTS.md 的进度清单也如实记录着这一条。

### 7.2.3 接入路径：零代码改动的"最后一公里"

第 2 章 2.5 承诺过"插入即用"，现在可以精确兑现：**接入 4B 不需要写一行代码**——

1. 准备 13 路 `.wav/.mp3`（三枪声、换弹始末、四种人声、拾取/爆炸/UI、脚步若干变体）；
2. 拖入 AudioManager 的对应槽位 + 把脚步数组填几个变体（想走 FootstepReceiver 通道的话再拖它的 AudioSource）；
3. 完成。事件订阅、按武器名分发、3D 位置、音量链全部现成。

如果想让 7.1.4 的 `PlayFootstep(pos, speed)` 活过来，需要在 FootstepReceiver 或
动画事件链上补一行转发（如 `AudioManager.Instance.PlayFootstep(transform.position, 0)`）——
这是接入时唯一可选的代码改动。**解决思路的另一半**（配置 Key 用生成工具/改用免费音效包）
属于第 8 章的遗留问题清单。

### 7.2.4 一个如实的边界事实：主菜单没有声音

MainMenu 场景里**没有 AudioManager 物体**（YAML 实测：该场景只有 Main Camera 上的
AudioListener）。即使明天把资产全拖进去，主菜单的按钮点击也不会响——`OnUIClick`
在主菜单场景没有订阅者。这不是 bug：AudioManager 是场景级的，哪个场景要声音就放一个；
但"主菜单要音效"如果将来提上日程，方案要么是 MainMenu 也放一个 AudioManager，
要么把它改造成 DontDestroyOnLoad 的跨场景单例（改动会牵扯音源池重建时机——7.3 会讲到为什么）。

## 7.3 场景串联：跳转时序与存活清单

### 7.3.1 开始游戏链（MainMenu → SampleScene）

```text
[MainMenu 场景]
① 点击 PlayButton → MainMenuController.OnPlay()
② SetPanel(loadingPanel, menuPanel)                —— 主菜单淡出、Loading 淡入
③ LoadSceneAsync("SampleScene") + allowSceneActivation = false
④ 假进度条动画（1s）∥ 真实加载进行中（progress 封顶 0.9）
⑤ op.progress >= 0.9（加载完毕）+ 动画收尾停留 0.5s → allowSceneActivation = true
[SampleScene 加载激活，按 Unity 生命周期初始化]
⑥ 各对象 Awake：GameManager 单例守卫 / AudioManager 建 9 音源 / UIManager 订阅实例事件
   （PlayerInputHandler 的 FindAction 也在 Awake）
⑦ 各对象 Start（顺序不定，但相互容忍——本项目靠"订阅+首拉"和一帧等待消化时序）：
   - PlayerController：GameSettings.ApplyAll() 应用灵敏度、锁鼠标
   - PlayerADSController：应用 FOV 到相机
   - GrenadeThrower：Prewarm 手雷 ×2、广播初始手雷数
   - CombatEffectsController：Prewarm 曳光 ×8 / 血 ×4 / 烟 ×4 / 爆炸 ×2
   - EnemyWaveGenerator：SetMaxWave(5)、进入 Preparing（10 秒倒计时开始）
   - UIManager：Start 首拉手雷/击杀现值（总线的订阅在 OnEnable，比 Start 更早完成）
[游戏进行中]
⑧ 波次循环（5.4）……直到死亡或胜利
```

注意 ⑥⑦ 的设计姿态：**场景初始化顺序不受任何人控制**（Unity 不保证场景内对象 Start 顺序），
本项目的解法散布在各章——"订阅+首拉"（6.1.2）、一帧等待（SyncAmmoAfterWeaponReady）、
事件时序容忍（空槽静默）。**不依赖初始化顺序，是场景重载型架构的基本功**。

### 7.3.2 结束链：重玩 / 回主菜单

```csharp
// 摘自 GameManager.cs 的 Restart / ReturnToMenu（第 2 章已贴，此处看"清零"视角）
public void Restart()
{
    Time.timeScale = 1f;
    SceneManager.LoadScene(gameplaySceneName);   // 整场重载 = 万物归零
}
```

- **重玩**：重载 SampleScene——血量、弹药、波次、手雷、击杀数、禁用脚本状态全部随场景销毁归零。
  第 2 章说过这是"把重置外包给场景系统"，现在补上代价清单（下表）；
- **回主菜单**：重载 MainMenu——SampleScene 的一切销毁，主菜单读到的只有 PlayerPrefs。

### 7.3.3 存活清单：一次 LoadScene 到底销毁了什么

| 类别 | 成员 | 重载后的命运 |
|------|------|--------------|
| 场景对象 | Player、敌人、HUD、GameManager、AudioManager…… | **销毁**——挂在他们身上的订阅随之消失（前提：退订纪律执行过，静态事件列表不残留死引用） |
| 跨场景对象 | `[ObjectPool]` 根物体（DontDestroyOnLoad，2.3.1）与**池内全部实例** | **幸存**——Release 时实例都 `SetParent(Root)` 挂到了 DontDestroyOnLoad 的根下，场景卸载销毁不了它们；这就是"重玩时池化对象直接沿用"的机制来源 |
| static 数据 | GameEvents 的 21 个事件列表（退订后为空）、GameSettings 缓存（`_loaded` 与五个值）、ObjectPool 的 `_pools` 字典 | **幸存**——静态数据生命周期是整个应用域 |
| 磁盘 | PlayerPrefs（灵敏度/音量/FOV/自动射击） | **幸存**，下次会话还在 |

上表中间那行值得单独强调：**重玩时池化对象直接沿用**——你第一局炸过的手雷、
打过的曳光，第二局从池里捞出来接着用（OnEnable 复位保它们干净，4.5.2）。
这就是"池根 DontDestroyOnLoad"在场景重载型游戏里的真实收益：**重玩零预热成本**
（Prewarm 只在 Start 跑一次，第二次 Start 是新场景的 CombatEffectsController——
它会再 Prewarm，但池已有存货，预热实例只是入了队，不浪费也不冲突）。

### 7.3.4 静态事件的"幽灵"防线（再强调一次）

重玩链路上最凶险的暗礁：场景销毁时，任何**忘了退订**的静态订阅会指向死对象，
下一次广播就是 `MissingReferenceException`（2.1.3）。本项目的防线是全项目纪律化的
"OnEnable 订阅 / OnDisable 退订"——第 6 章你已看到六个 UI 类、第 4 章的特效控制器、
本章的 AudioManager 全部如此；唯一在 OnDestroy 退订的是实例事件（生命周期不同）。
**如果你复现时只用一局测试，永远发现不了漏退订**——测重玩链（重玩三连以上）是必须的验收步骤。

## 7.4 ObjectPool 一局生命周期复盘

把 2.3 的静态池放进一局游戏的具体时间线：

```text
T0  场景激活
    ├─ GrenadeThrower.Start ──────→ Prewarm(Grenade_Thrown, 2)        池：雷×2
    └─ CombatEffectsController.Start → Prewarm(曳光8 / 血4 / 烟4 / 爆2)  池：四类16

战斗中（每秒都在发生）
    ├─ 玩家命中 → Get(曳光) → 0.08s → Release      （吞吐最快）
    ├─ 敌人开枪 → Get(曳光) → 0.08s → Release      （玩家曳光未接线，4.4.3）
    ├─ 命中表面 → Get(血/烟) → 粒子播完(改Disable) → 失活
    ├─ 弹痕 ──→ 不走池：环形队列上限50，最早实例被复用（4.4.2）
    ├─ 手雷 ──→ Get(雷) → 5s 引信 → Explode → Release（OnEnable 复位引信/清拖尾）
    └─ 爆炸 ──→ Get(爆) → 粒子播完 → 失活

T1  死亡 → 结算 → 重玩（Restart 重载场景）
    ├─ [ObjectPool] 根 + 池内全部实例：幸存（DontDestroyOnLoad）
    ├─ 新场景的 Prewarm 再跑：预热实例入队排在存货后面（不冲突）
    └─ 静态 _pools 字典：幸存，票据（PoolTicket）继续有效

T2  回主菜单（ReturnToMenu 重载 MainMenu）
    └─ 池根依然幸存，只是没人 Get 它——静静等待下一次开局
```

三个此前散落各章的事实在这条时间线上汇成一个完整图景：**预热责任在使用方**（2.3.3）、
**池化约束的执行者**（Grenade 的 OnEnable 复位、BulletTrail 的"活完即回"）、
**谁不进池**（敌人 5.4.4、弹痕的环形队列、拾取物的 Destroy、枪口特效的 Instantiate——
四种"不池化"各有各的理由）。判断你自己的项目该不该池化某个对象时，
本项目给出的判据清单：**频率高吗？状态能干净复位吗？生命周期有"延迟尾巴"（尸体）吗？**

## 7.5 组装清单

| # | 操作 | 位置 |
|---|------|------|
| 1 | 建空物体 `AudioManager`，挂 AudioManager 脚本（音源运行时自建，无需手挂 AudioSource） | SampleScene 场景级 |
| 2 | （4B 接入时）把 13 路音效资产拖入对应槽位、footstepClips 填若干变体 | Inspector |
| 3 | 想启用 FootstepReceiver 通道：角色模型物体加 AudioSource 并拖入 footstepAudio | 模型子物体 |
| 4 | 确认两个场景都在 Build Settings（2.4.4 第 4 步已完成） | File → Build Settings |
| 5 | 确认 MainMenu 是否需要音效（当前不需要则无需动作，7.2.4） | — |

## 【验证方法】

4B 挂起让本章的验证分两层：

1. **零资产状态的验证（当前就能做）**：Play 整局游戏——**全程无声但无任何报错**。
   Console 不应出现 UnassignedReferenceException（所有播音入口都判空了）；
   结算后连点重玩三局——Console 依旧干净（退订纪律 + 池幸存的联合验证）；
2. **接入一条音的链路验证（拖一个资产即可）**：随便找一段 .wav 拖进 `explosionClip`——
   扔手雷，爆炸位置应发出 3D 音（近响远轻）；拖进 `uiClickClip`——结算面板按钮应出 2D 音。
   这两步验证"零代码接入"的承诺（7.2.3）；
3. **音量链**：暂停菜单把主音量拉到 0——爆炸音消失（AudioListener.volume=0）；
   拉满主音量、SFX 减半——爆炸音减半（7.1.5 的乘法链）；
4. **场景切换**：主菜单点击开始，Loading 1 秒后进游戏、首波 10 秒倒计时正确开始
   （7.3.1 时序跑通）；重玩后手雷池行为正常（连续快速扔三颗雷不出现"第二颗秒爆"——
   OnEnable 复位有效）；
5. **池幸存**：第一局扔两颗雷后死亡重玩——Hierarchy 的 `[ObjectPool]` 根在场景切换前后
   始终存在，且第一局回收的实例仍在其中（DontDestroyOnLoad 生效；注意第二局 Prewarm 会新建
   预热实例使子物体数**增加**，那是正常的——存货加新货，互不冲突）。

## 【常见坑】

1. **Play() 播可重叠音效**：连发枪声互相掐断（同源 Play 是独占的）。可重叠音一律 PlayOneShot（7.1.1）。
2. **一音一源**：给每种音效建一个 AudioSource——对象膨胀且难管理。本项目 9 源轮询 + PlayOneShot 叠加（7.1.1 的"Newest wins"）。
3. **两个 AudioListener**：场景里再放一个相机/音频监听器，Console 报 "There are 2 audio listeners"。本项目每场景恰好一个（挂 Main Camera）。
4. **空槽位访问成员**：`UnassignedReferenceException`——假 null 比较要用 `== null`，直接取 `.name` 会炸（7.2.1 的现场演示）。
5. **事件无位置参数硬要 3D 播**：像换弹音那样缓存最近枪口位置（7.1.3），或者给事件加参数——但那违反"只增不改"（改动签名全项目编译错）。
6. **动画事件调用别的物体的方法**：动画事件只找**同物体**组件的公共方法——AudioManager 挂场景物体时角色动画事件够不着它（7.1.4 PlayFootstep 变死接口的原因）。
7. **重玩不测**：只测一局永远发现不了漏退订/池不复位——连玩三局是最低验收标准（7.3.4）。
8. **场景切换音源池踩坑**：若把 AudioManager 改成 DontDestroyOnLoad（7.2.4 的未来方案），新场景再放一个会双实例（单例守卫会 Destroy 后来者，但要确认你想要的是哪一个活——跨场景单例要迁移 Awake 的守卫逻辑，别两处都留）。
9. **Prewarm 放进池类自己**：让 ObjectPool"知道"该预热什么——池就不再通用。预热永远在使用方 Start（2.3.3 的纪律）。
10. **弹痕进对象池**：长寿命场景装饰物不适合"用完即还"的池——上限环形覆盖（4.4.2）才是对的形状。

---

# 第 8 章 · 当前进度盘点与遗留问题

## 【本章目标】

给整个项目做一次"毕业体检"：

1. 一张**已完成功能总表**——这个项目此刻能做什么，一目了然；
2. 一份**遗留问题完整清单**——本写作过程中用"先读后写"逐文件实测挖出的全部
   未接线、注释过时、技术债，每项标注证据章节与修复成本；
3. **4B 音效挂起的解决思路**（三套方案）与**下一步开发方向建议**（按投入分级）；
4. 给准备复现的你一份"毕业设计"方向清单。

## 【前置知识】

无新技术——本章是盘点与决策。唯一的方法论是：**盘点要靠证据（grep/实测/截图），
不靠记忆**——本文档全部清单均可按章节引用复核。

## 8.1 项目完成度总表

代码规模实测：`Assets/Scripts/` 下 **36 个脚本、共 3482 行**——平均每脚本不足百行，
印证了"事件解耦 + 数据驱动"架构的瘦身效果。功能完成度（按系统）：

| 系统 | 已完成 | 状态 |
|------|--------|------|
| 骨架（Data×5） | 21 事件总线 / 设置读取缓存 / 键=预制体对象池 / 四态流程状态机 / 波次 SO | ✅ 完整 |
| 玩家（11） | 移动/冲刺/跳跃/ADS/蹲伏/医疗/输入封装/IK/HeadBob/WeaponSway/震屏 | ✅ 完整（Sprint 未入输入资产，3.1.3） |
| 战斗（9） | 三武器数据驱动/部位伤害爆头/切枪弹药缓存/命中表现/手雷全链/拾取×3 | ✅ 完整（玩家曳光未接线，8.2.1） |
| 敌人（3） | 三类型参数差异 AI/NavMesh 寻敌/远程攻击/自动爆头碰撞体/五波状态机 | ✅ 完整 |
| UI（7） | HUD 全套/准星/HitMarker/波次横幅倒计时/主菜单设置 Loading/暂停/双终点结算 | ✅ 完整 |
| 音频（1） | 事件驱动播音、9 音源、按武器分发、音量链——**代码全就绪** | ⚠️ 资产全空（4B） |

**一局游戏能玩到什么**：主菜单调好灵敏度/FOV → Loading → 10 秒倒计时 → 五波
混编敌人（8→14 只）逐波进攻 → 三把武器 + 手雷 + 掩体 + 拾取物对抗 → 死亡出红色
YOU DIED / 清完第五波出绿色 VICTORY → 结算三行数据 → 重玩或回主菜单——
**全流程可跑、全程无声、无报错**。

## 8.2 遗留问题完整清单

以下是逐章实测时挖出的全部问题，按"影响玩家体验的顺序"分级。
每项都能在指认章节找到证据。

### 8.2.1 未接线：广播/消费两端只通了一半（5 项）

| # | 问题 | 证据 | 修复成本 |
|---|------|------|----------|
| 1 | **4B 音效**：13 路槽位全空，游戏全程无声 | 7.2.1 实测 | 拖资产即通（8.3） |
| 2 | **玩家射击没有曳光**：`OnBulletTrail` 只有 EnemyBase 广播 | 4.4.3 grep | 1 行（Shoot 的 Raycast 分支后补 `RaiseBulletTrail(origin.position, hit.point)`，未命中分支可再补一行） |
| 3 | **弹药/医疗拾取无广播**：只有 GrenadePickup 发 `OnPickupTaken` | 4.6 | 各补 1 行 |
| 4 | `OnPlayerDamaged` 事件无生产者（AudioManager 单方面订阅） | 2.1.5 | 1 行（PlayerHealth.TakeDamage 里补 `RaisePlayerDamaged()`），补后 4B 的玩家受伤音即刻可用 |
| 5 | 脚步音双通道都断：`PlayFootstep` 无调用方 + FootstepReceiver 音源未拖 | 7.1.4 | 拖资产 + 1 行转发 |

**共同点**：五项全是"消费端就绪、生产端缺席"——事件总线架构让这些缺口
**不炸、不报错、只静默**（这既是它的优点，也是它的代价：缺口要靠盘点才现形）。

### 8.2.2 注释/文档与实现不符（4 项，均已在文中指认）

| # | 注释说 | 实际是 | 证据 |
|---|--------|--------|------|
| 1 | ObjectPool"栈式回收" | `Queue` 队列（行为等价） | 2.3.4 |
| 2 | GrenadeThrower"初速 10+7" | 12+5 | 4.5.1 |
| 3 | HitFeedbackUI"程序化生成控件" | 场景固定控件（被固化保存过） | 6.3 实测 |
| 4 | README/AGENTS.md 提及 `Scripts/Items/` | 目录从未存在（物品逻辑在 Combat） | 0.1.3 / 1.5 |

修复成本：各 1 分钟改注释。**教训大于修复**：这个项目的注释在"当初"都是对的——
注释是快照，代码是河流；每次改动后同步注释（或像本项目一样把决策理由写进注释但接受数值漂移）。

### 8.2.3 已知粗糙点与技术债（按风险排序）

| # | 问题 | 证据 | 说明与建议 |
|---|------|------|------------|
| 1 | **三处射线无 LayerMask**（玩家射击、敌人攻击、手雷瞄点） | 4.2.1 / 5.1.3 | 贴脸射击可能伤到自己、友军挡敌弹。生产化第一刀：加层掩码（准星已是范例，6.2） |
| 2 | **PlayerPrefs 键名三处定义**（const / 字面量 / const） | 2.2.2 | 拼错即静默失效。统一引用 GameSettings 的 const |
| 3 | **prefab 与场景实例漂移**：IK/Sway/HeadBob/GrenadeThrower 只在实例上 | 3.0 实测 | 重新拖 Player.prefab 进新场景会缺四组件。apply 回 prefab 一次解决 |
| 4 | **枪口特效 Instantiate/Destroy 未池化** | 4.2.4 | 高频射击的 GC 点，池化 30 分钟工作量 |
| 5 | **Slider Fill 修复代码两份重复**（主菜单/暂停菜单） | 6.6 | 抽静态工具类 |
| 6 | 死代码/诊断残留：`_reloadLayerIndex`、`IkCallCount`、`PlayFootstep` | 3.7.2 / 7.1.4 | 接 4B 时 PlayFootstep 可复活；其余可删或注释"诊断用" |
| 7 | **场景遗留禁用测试敌人** + 空 Waypoints 容器 | 5.0 实测 | 清理资产减小场景体积（不影响运行） |
| 8 | **Sprint 未进 Input System 资产**（直读键盘，手柄无法冲刺） | 3.1.3 | 两行代码回正轨；也是手柄支持的第一步 |
| 9 | MainMenu 场景无 AudioManager（主菜单天生无声） | 7.2.4 | 看需求决定是否补 |
| 10 | Editor 彩色受击 Debug.Log 常驻（编译隔离） | 5.1.4 | 可接受；量大时可加开关 |

## 8.3 4B 音效：挂起原因与解决思路

**原因**（7.2.2）：4B 阶段计划用 Unity MCP 工具链的 AI 音频生成能力产出音效，
但需要外部 AI 服务的 **API Key——从未配置**，资产生成停摆至今。代码侧（订阅、分发、
音源池、音量链、13 个槽位）全部就绪并经过"零资产安全运行"验证。

**三套解决思路**：

- **方案 A：配置 API Key 走生成工具**（还原原计划）。配置后用 MCP 的音频生成工具
  （`mcp_unityMCP_generate_audio`，需要 fal.ai 等服务的 Key）按 13 路清单逐个生成、
  自动导入——与项目的 AI 工作流最连贯；适合枪声、爆炸这类"合成感可接受"的音。
- **方案 B：免费/付费音效资产包**（最快落地）。匹配低模军事画风，可从
  Synty 配套音效包、Kenney（CC0）、freesound.org 获取——脚步、UI 点击、换弹机械声
  这类真实录音更有质感。拖入槽位即完成（零代码，7.2.3 已验证路径）。
- **方案 C：自录/自制**（完全可控）。嘴巴拟声 + Audacity 变调，对原型项目够用。

**接入后的验收**（对照 7.5）：拖一个 `explosionClip` 试 3D 衰减、拖 `uiClickClip` 试 2D、
音量链两节点验证、重玩三局 Console 干净——四步过了，4B 就算正式关闭。
顺手把 8.2.1 的 #2~#5 四个"一行广播"补上，事件总线上的每一路音效才真正全通。

## 8.4 下一步开发方向建议

**短期收尾（合计约一天）**——按性价比排序：4B 资产（8.3）→ 玩家曳光 1 行 →
拾取广播 2 行 → LayerMask → 键名统一 → prefab 回写。做完这六件，项目从
"可玩原型"升到"无已知毛病的可玩原型"。

**中期内容与体验（各独立，可挑着做）**：

- **玩法纵深**：更多波次/波次修饰词（如 4×Wave5）、新武器（霰弹枪/狙击——
  复制 WeaponData 资产 + 一个模型，零新代码）、手雷变体（烟雾/闪光）、敌人新行为
  （掩体侧移、包抄——EnemyBase 决策树加分支）；
- **平衡调校**：WaveConfig 数值、三武器伤害三角、敌人 accuracy——全是 Inspector 工作，
  正好练习"数据驱动"的手感迭代；
- **手柄支持**：Sprint 入输入资产（8.2.3 #8）→ inputactions 补手柄绑定 →
  UI 加 EventSystem 的 InputSystemUIInputModule。架构已备好（3.1 的封装层让这成为纯配置活）。

**长期工程化（作品集/发布向）**：

- Addressables 管理资源包、按波次动态加载；构建管线（7.3 的时序图就是构建检查单）；
- 性能剖析（Unity Profiler 验证池化收益、Overdraw 检查 URP 场景）；
- 存档系统（最高波次记录——PlayerPrefs 三行，或者 JSON 化做完整战绩）。

**给复现读者的"毕业设计"清单**：本文档从第 0 章做到第 7 章的所有【验证方法】
全绿，即视为复现成功。之后的最小毕业挑战：**把 8.2 清单全部清零**——
那正是"读别人的项目→维护别人的项目"的第一课：**接手一个代码库的第一件事不是加功能，
是把已知的坑填平。**

## 8.5 结语：这个项目值得带走的三件事

1. **架构三支柱**：静态事件总线（GameEvents，21 路、"只增不改"）、数据驱动
   （WeaponData/WaveConfig/敌人参数差异——36 个脚本 3482 行养出一个完整游戏）、
   池化决策（该池的池、不该池的给出理由）。它们合起来回答了一个问题：
   **如何让一个项目在多个 Agent（或多个人）接手下仍不散架**；
2. **"以代码/实测为准，不以注释为准"**——本文档写作期间四处撞上注释与实现的偏差
   （8.2.2），每次都是先读后写救了文档的准确性。读项目、写文档、做复现，
   永远从打开源文件开始，而不是相信任何转述（包括本文档——欢迎复核）；
3. **过程可见的价值**：110 张验证截图、代码注释里的阶段/任务/决策/坑编号、
   AGENTS.md 的角色纪律——这个项目把"开发过程"本身做成了可审计的资产。
   无论你是否使用 AI 工作流，**把每一步的证据留下来**，都是对未来的自己
   和协作者最大的善意。

## 【验证方法】

本章的验证是"清单的可复核性"：

1. 对 8.2 每一项，打开对应章节的证据引用（grep 命令、实测记录），确认问题现状仍成立——
   项目继续开发后，这份清单要随之更新（这正是 AGENTS.md"每阶段输出功能总结"的循环）；
2. 8.1 总表跑一遍 0.1.1 的一局流程 + 第 3~7 章的各章验证，绿色即"完成度声明"有效；
3. 自行盘点一个陌生项目时，复用本文档的方法：grep 广播点/订阅点画流向图（0.3.3）、
   反射读 Inspector 槽位（7.2.1）、场景对象清点（5.0）——**盘点三板斧：读代码、查场景、跑一局**。

## 【常见坑】

1. **盘点靠记忆**：写总结时凭印象列清单——必然遗漏静态依赖（总线订阅、DontDestroyOnLoad 幸存者）。用 grep 与实测。
2. **只盘功能不盘重玩链**：功能表全绿但重玩第三局崩（幽灵订阅）——完成度声明必须包含 7.3.4 的连玩三局。
3. **把"未接线"当 bug 修**：五处静默缺口是架构选择（只增不改）的代价，修复要顺着事件总线补广播，而不是拉直连引用绕过总线——那会破坏第 2 章的全部意义。
4. **修注释不修代码（或反之）**：8.2.2 的四项要么改注释对齐现状、要么按注释改实现——先想清楚哪边是"对的意图"，再动另一边。
5. **清债不看成本**：8.2.3 有十项，全部清零可能不如先做 8.4 的内容扩展——技术债的清偿顺序由"风险×成本"决定，不由洁癖决定（本清单已按风险排序）。
6. **毕业设计直接加新系统**：新功能会放大未清的债（在无 LayerMask 的射线上加更多枪=更多自伤路径）。先清 8.2 再扩张，是本项目演进史（阶段 5 的 S 系列任务就是"收口"）给出的答案。

---

> **全书完成**：第 0～8 章共九章已全部交付（全景 / 工程搭建 / 数据与事件骨架 / 玩家系统 /
> 战斗系统 / 敌人与波次 / UI 系统 / 音频与场景串联 / 进度盘点与遗留问题）。
> 本文档由"先读后写"方法撰写：全部 36 个脚本通读、全部事实经 grep 或 Unity 编辑器实测核验、
> 五处注释与实现的偏差如实指认。愿你复现顺利——以及，欢迎复核本书的每一个断言。
