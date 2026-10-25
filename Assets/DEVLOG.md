# TPS 战术射击游戏 — 开发日志

> 最后更新：2026-10-01 | 当前进度：**阶段 5 流程闭环 S1~S6 全部完成**（敌人预制体重建/GameManager/结算界面/波次生成/胜利流程/击杀HUD/手雷补给），待 explore 审查 + 用户 Play 终验；4B 音效挂起（无 fal.ai Key）；待开发：阶段6 AI 深度。规划基准见 [DEVELOPMENT_PLAN.md](DEVELOPMENT_PLAN.md)，阶段 5 详设见 [PHASE5_PLAN.md](PHASE5_PLAN.md)，实施记录见文末「阶段 5 实施记录」。

---

## 项目概况

| 项目 | 详情 |
|------|------|
| 引擎 | Unity 2022.3.62f2c1 LTS |
| 渲染管线 | URP |
| 资源包 | PolygonBattleRoyale（373 预制体） |
| 主场景 | Assets/Scenes/SampleScene.unity |
| Input | 新 Input System（仅） |
| 相机 | Cinemachine 3.1.7 |

---

## 阶段 1 完成状态

### 已实现功能（24/26）

| 编号 | 功能 | 按键 | 状态 |
|------|------|------|------|
| 1 | WASD 移动 | WASD | ✅ |
| 2 | Shift 奔跑 | Shift+WASD | ✅（刚修复） |
| 3 | 跳跃 | 空格 | ✅ |
| 4 | 重力 | 自动 | ✅ |
| 5 | 鼠标旋转视角 | 鼠标 | ✅ |
| 6 | 射击（Raycast+散布） | 左键 | ✅ |
| 7 | 换弹 | R | ✅ |
| 8 | 后坐力（枪口上跳） | 自动 | ✅（刚修复） |
| 9 | 弹药管理 | 自动 | ✅ 30发弹匣 |
| 10 | 枪口特效 | 自动 | ✅ |
| 11 | 玩家血量 | 受击 | ✅ |
| 12 | 敌人 AI（巡逻/追击/攻击） | 自动 | ✅ |
| 13 | Cinemachine 第三人称相机 | 自动 | ✅ 5m跟随 |
| 14 | HUD 血条 | 自动 | ✅ 左上角 |
| 15 | HUD 弹药文字 | 自动 | ✅ 右下角 |

### 未实现（留待阶段 2）

| 功能 | 说明 |
|------|------|
| ADS 瞄准 | Aim 右键已绑定，无 FOV/贴枪/减速逻辑 |
| 掩体系统 | 未开始 |
| 受伤反馈/治疗 | 未开始 |
| 弹药备弹池/拾取 | 未开始 |

---

## 场景结构

```
SampleScene (8 根对象)
├── Directional Light
├── Global Volume (URP)
├── Player (0,2,0, tag="Player")
│   ├── Component: CharacterController, PlayerController, PlayerInputHandler
│   │             PlayerInput, PlayerHealth, WeaponManager
│   ├── CameraTarget (子物体，头部高度)
│   ├── WeaponHolder (子物体，武器挂点 + 步枪实例)
│   └── PlayerModel (绿色 Cube 占位)
├── Ground (Plane 10×10)
├── Main Camera (CinemachineBrain)
├── TPSCamera (CinemachineCamera + ThirdPersonFollow, Follow=CameraTarget, 5m)
├── ShootTestTarget (测试靶，可删除)
└── Canvas (UIManager + HealthBar Slider + AmmoText)
```

---

## 文件清单

### 脚本（8 个）
- `Assets/Scripts/Player/PlayerController.cs`
- `Assets/Scripts/Player/PlayerInputHandler.cs`
- `Assets/Scripts/Player/PlayerHealth.cs`
- `Assets/Scripts/Combat/WeaponData.cs`
- `Assets/Scripts/Combat/FirearmWeapon.cs`
- `Assets/Scripts/Combat/WeaponManager.cs`
- `Assets/Scripts/Enemy/EnemyBase.cs`
- `Assets/Scripts/UI/UIManager.cs`

### 资产
- `Assets/Input/PlayerInput.inputactions`
- `Assets/Data/RifleData.asset`（伤害25/600RPM/30发）
- `Assets/Prefabs/Weapon_Rifle.prefab`
- `Assets/Materials/GroundGray.mat`
- `Assets/Materials/PlayerGreen.mat`

---

## 已知坑

1. PlayerInput 的 `defaultActionMap` 必须设为 "Player"，且 PlayerInputHandler.Awake 中手动 Enable()
2. Cinemachine 3.x 用 `CinemachineCamera` 而非旧版 `CinemachineVirtualCamera`
3. HUD 用 UnityEngine.UI.Text（非 TMP，项目未导入 TMP Essentials）
4. UI 只在 Game 视图可见，Scene 视图看不到 Canvas
5. HealthBar Slider 的 Fill Image 必须是 Simple 类型：ugui Slider 检测到 Filled 类型时改用 fillAmount 驱动（见 Slider.cs UpdateVisuals），若 fillMethod 配成 Radial360 会导致血条视觉不随血量横向收缩。已于 2026-08-03 修复并保存场景

---

## 对话流程速查

新对话启动后发送：
```
@architect 请读取 Assets/DEVLOG.md 了解项目进度，规划阶段 4 打磨：音效/特效/死亡动画/手感/性能
```

日常开发：
```
@architect 规划XXX → 自动派 @coder 执行
@coder 实现XXX
@explore 审查XXX
```

每个 coder 完成必须 `read_console` 检查编译错误。

---

## 阶段 2 完成状态

> 完成日期：2026-08-03 | 编译：0 错误 0 警告

### 新增脚本（4 个）
| 文件 | 说明 |
|------|------|
| `Assets/Scripts/Player/PlayerADSController.cs` | ADS 瞄准：FOV 60→40、武器贴枪、ADS 进度暴露 |
| `Assets/Scripts/Player/PlayerCoverController.cs` | 掩体/蹲伏：C 键降低 CharacterController 高度 + 减速 |
| `Assets/Scripts/Combat/AmmoPickup.cs` | 弹药拾取物（Trigger，自动补备弹） |
| `Assets/Scripts/Combat/HealthPickup.cs` | 医疗包拾取物（Trigger，自动补医疗包） |

### 修改脚本（8 个）
| 文件 | 修改要点 |
|------|----------|
| `WeaponData.cs` | +adsSpreadMultiplier、+reserveAmmoMax、+ammoPickupAmount |
| `PlayerInputHandler.cs` | +IsCrouchHeld()、+IsHealPressed() |
| `PlayerController.cs` | +CurrentHealth属性、整合 ADS/掩体减速 |
| `FirearmWeapon.cs` | +备弹池 _reserveAmmo、+ADS散布倍率、换弹从备弹扣除 |
| `WeaponManager.cs` | +OnReserveAmmoChanged 事件 |
| `PlayerHealth.cs` | +TryHeal()消耗医疗包、+maxMedkits上限5、+OnMedkitChanged |
| `UIManager.cs` | 红屏→径向红影（RedVignette）、弹药格式"弹匣/备弹"、医疗包显示 |
| `EnemyBase.cs` | 未修改（阶段1已有） |

### 新增资产
| 文件 | 说明 |
|------|------|
| `Assets/Textures/RedVignette.png` | 256×256 径向渐变纹理（60%半径起→边缘红色，alpha=128） |
| `Assets/Input/PlayerInput.inputactions` | +Crouch(C键)、+Heal(H键) |

### 按键一览
| 按键 | 功能 |
|------|------|
| 鼠标右键按住 | ADS 瞄准（FOV 60→40、散布缩小、武器贴枪、移速减半） |
| C 键按住 | 蹲伏/掩体（高度 2m→1m、移速减半） |
| H 键按下 | 消耗医疗包恢复 30 血（满血时无效，上限 5 个） |
| R 键按下 | 换弹（从备弹池扣除，备弹=0 无法换弹） |

### 已知坑（阶段2追加）
6. PlayerADSController 通过 `GameObject.Find("TPSCamera")` 查找摄像机，改名会断
7. AmmoPickup / HealthPickup 脚本存在但场景中无实例，需在阶段3手动放置
8. ADS 和蹲伏减速可叠加（0.5×0.5=0.25x），手感可能过慢
9. 医疗包初始 3 个，上限 5 个

---

## 对话流程速查（阶段2）
```
@architect 规划阶段2 → 派 @coder 执行 Task 0~5 → @explore 审查 →
修复 M1-M3 缺陷 + 优化红影 → 完成
```

## 下一阶段
阶段 4：打磨 — 音效（射击/换弹/受伤）、命中粒子、敌人死亡动画、手感微调、性能优化

---

## 阶段 3 完成状态

> 完成日期：2026-08-03 | 编译：0 错误 0 警告 | NavMesh 烘焙：79ms

### 关卡结构（60×60m 测试场，LevelGround + NavMeshSurface）

```
SampleScene 新增分组
├── LevelGround（60×60 Plane + NavMeshSurface，已烘焙）
├── Level_Buildings：村庄区（House×2/仓库/木屋/帐篷）+ 东南小楼 + 靶场（Range_Wall×3/靶×2）
├── Level_Cover：集装箱×4 + 油桶/木箱/轮胎堆/反坦克锥/沙堤等 22 件掩体
├── Level_Roads：主干道 Road_Straight×5（沿 Z 轴）
├── Level_Scenery：树×10/灌木×3/远山×4/云×4/铁丝网×6
├── Level_Vehicles：残骸×3（轿车/卡车/ buggy，作掩体）+ 完好×2（装甲车/轿车，装饰）
├── Enemies：8 敌人（士兵×4/侦察×2/重装×2）
├── PatrolPoints：WP_Village/Container/Range 共 8 路点
└── Pickups：弹药堆×3 + 医疗箱×2
旧 Ground（10×10）已停用保留，旧 Enemy_01/02 与旧路点已删除
```

### 修改脚本（5 个）
| 文件 | 修改要点 |
|------|----------|
| `PlayerInputHandler.cs` | +GetWeaponSwitchIndex()（1/2/3 切枪） |
| `WeaponManager.cs` | +weapons 槽位数组、+TrySwitchWeapon()、切枪检测前置于武器判空 |
| `EnemyBase.cs` | +Animator 挂钩（LateUpdate 同步 Speed、攻击 Fire 触发） |
| `FirearmWeapon.cs` | +射击 Fire/换弹 Reload 动画触发 |
| `PlayerController.cs` | +动画 Speed/Jump 驱动、关闭 Root Motion |

### 新增资产
| 文件 | 说明 |
|------|------|
| `Assets/Data/SMGData.asset` | 冲锋枪：伤害15/900RPM/40发/备弹160 |
| `Assets/Data/PistolData.asset` | 手枪：伤害30/300RPM/12发/备弹60 |
| `Assets/Prefabs/Weapon_SMG.prefab` / `Weapon_Pistol.prefab` | 武器包装预制体（复制步枪结构换模型换数据） |
| `Assets/Prefabs/Enemy_Soldier/Scout/Heavy.prefab` | 三变体敌人（军人/运动装/佣兵模型 + 差异化血量速度伤害） |
| `Assets/Prefabs/AmmoPickup_Pile.prefab` / `HealthPickup_MedBox.prefab` | 拾取物（弹药堆/医疗箱模型） |
| `Assets/AnimControllers/CharacterAnimator.controller` | 人形共用控制器：Locomotion BlendTree(0/3/7) + Fire/Reload/Jump 状态 |
| `Assets/Input/PlayerInput.inputactions` | +SwitchWeapon1/2/3（1/2/3 键） |

### 动画接入说明
- Basic Shooter Pack 16 个 Mixamo FBX 已从 Generic 批量重导入为 Humanoid（自动 Avatar 映射）
- PolygonBattleRoyale 角色预制体自带人形 Avatar（CharactersAvatar），与 Mixamo 动画直接兼容
- 玩家绿色 Cube 已替换为 Character_MilitaryMale_01

### 按键新增
| 按键 | 功能 |
|------|------|
| 1 / 2 / 3 | 切换步枪 / 冲锋枪 / 手枪 |

### 已知坑（阶段3追加）
10. Mixamo 动画若后续新增，必须确认 Rig 为 Humanoid，否则 Blend Tree 不驱动
11. 场景静态布局变更后需重新调 NavMeshSurface.BuildNavMesh 烘焙
12. 射击/换弹为全身覆盖状态（无上身在 mask），开火瞬间会短暂打断跑动动画
13. 敌人远程攻击（15m）才播射击动画，无命中特效（留待阶段 4）
14. 旧 Ground 停用未删，确认无问题后可移除
15. 载具均为静态摆放，无驾驶功能（已确认方案）

---

## 阶段 3 热修复（2026-08-03 用户反馈）

> 编译：0 错误 0 警告

### 修复内容
| 问题 | 根因 | 修复 |
|------|------|------|
| 枪支错位/枪口朝天 | 阶段3复制预制体时误删步枪主模型、残留枪口配件；且 Polygon 武器模型以 +Y 为枪管方向（枪口在 y=0.94），直接挂入水平挂点导致竖立 | 重建 Weapon_Rifle/SMG/Pistol 三个预制体：直接内嵌 Polygon 原版模型（天然 +Z 向前），重新计算 FirePoint 至包围盒最前端 |
| ADS 瞄准时枪支坠地 | adsWeaponPosition=(0,-0.15,0.3) 是绝对位置，Lerp 后挂点直接落到 y=-0.15（地面） | PlayerADSController 改为相对偏移：adsPositionOffset=(-0.12, 0.45, 0.15)，抬至眼线并对齐屏幕中线 |
| 持枪高度与模型不匹配 | WeaponHolder 还在绿色 Cube 时代的位置 (0.3,1.2,0.5) | 移至人形模型右手胸前 (0.12, 1.05, 0.35) |
| 走/跑无动画（玩家） | **双根因**：① CharacterAnimator 的 Locomotion Blend Tree `m_MaxThreshold=1`（代码创建 BlendTree 默认 0~1，未重算），Speed>1 全部钳制到 idle；② PlayerController 每帧两次 Move（水平+重力分开），第二次 Move 使 `velocity` 只计垂直分量，LateUpdate 读到的水平速度永远为 0 | ① 修正 MaxThreshold=7（与子项阈值 0/3/7 匹配）；② PlayerController 改为单次合并 Move：水平速度存 `_horizontalVelocity` 成员，与垂直速度合并后一次 Move，动画直接用 `_horizontalVelocity.magnitude` 驱动 |
| 走/跑无动画（敌人） | 仅受 Blend Tree MaxThreshold=1 影响（NavMeshAgent.velocity 无多次 Move 问题） | 同上①，阈值修复后敌人动画自动恢复 |

### 动画问题验证结果（修复后实测）
- Speed=5 → walking.fbx 权重 0.5 + rifle run.fbx 权重 0.5（阈值 3~7 区间混合，正确）
- Speed=8 → rifle run.fbx 权重 1.00（正确）
- 角色实际位移正常，临时验证组件已删除
- 教训：上轮“动画正常”的结论错误——当时钩子测试用单次 Move 绕过了双 Move 问题，且 Thread.Sleep 阻塞帧更新导致权重读数不可信；动画问题必须在真实输入路径上验证

### 修改文件
- `Assets/Scripts/Player/PlayerADSController.cs`：adsWeaponPosition/Rotation → adsPositionOffset/RotationOffset（相对偏移）
- `Assets/Scripts/Player/PlayerController.cs`：双 Move 合并为单次 ApplyCombinedMove，动画改由 _horizontalVelocity 驱动
- `Assets/AnimControllers/CharacterAnimator.controller`：Locomotion Blend Tree MaxThreshold 1→7
- `Assets/Prefabs/Weapon_Rifle/SMG/Pistol.prefab`：重建（内嵌原版模型 + 正确 FirePoint）
- 场景：WeaponHolder 位置更新 + 步枪实例换新

### 已知坑（热修复追加）
16. WeaponData.weaponPrefab 字段仅用于“无模型时自动实例化”，包装预制体已内嵌模型时该字段不参与逻辑，改数据时不必担心
17. ADS 偏移可在 Player 的 PlayerADSController 组件上调（相对值，不再受挂点位置影响）
18. CharacterController 每帧多次 Move 会使 velocity 只反映最后一次位移，动画取速度必须用自行维护的水平速度或合并为单次 Move
19. 代码创建 BlendTree 后必须确认 min/maxThreshold 与子项阈值一致（默认 0~1 会钳制参数），或用 UseAutomaticThresholds

---

## 阶段 3.5：动画系统大修（2026-08-04 用户反馈）

> 编译：0 错误 0 警告 | 动画使用率：13/16

### 问题与修复

| 问题 | 根因 | 修复 |
|------|------|------|
| 射击/换弹时移动 → 人物定住滑步 | Fire/Reload 是 AnyState 全身覆盖动画 | 创建 UpperBody 层（Avatar Mask），Fire/Reload/HitReaction 移到上半身 |
| 跳跃空中僵硬/落地延迟 | ExitTime 条件 + 拼接动画 | 简化为单状态：rifle jump.fbx 完整弧线，动态计算 JumpSpeed 匹配空中时间，仅 Grounded 条件退出 |
| 左脚弯曲扭曲 | Idle 与 Walking 在 1D Blend Tree 中 Speed 0~3 范围内持续混合 | Idle 独立成单独状态，与 Locomotion 永不混合；Idle↔Locomotion 通过 Speed 阈值切换 |
| 只有前向移动动画 | Blend Tree 是 1D Simple | 升级为 2D Freeform Directional：X=StrafeSpeed, Y=ForwardSpeed，8 节点覆盖 360° |
| 5 个移动动画不循环 | FBX 导入设置 loopTime=false | walking backwards / run backwards / strafe×3 / strafe right 全部改为 loopTime=true |
| 缺少快速侧移动画 | strafe.fbx / strafe (2).fbx 未加入 | 判定方向后加入 Blend Tree (±7,0) |
| 受伤无动画反馈 | hit reaction.fbx 未接入 | UpperBody 层新增 HitReaction 状态，PlayerHealth 和 EnemyBase 的 TakeDamage 中触发 |

### 当前 Animator Controller 结构

```
Base Layer:
  Idle (默认) ←→ Locomotion (2D Blend Tree, 8节点)
                  ↓ Jump触发
                Jump (单动画 + 动态 JumpSpeed)

UpperBody Layer (Avatar Mask):
  Empty ← AnyState → Fire_Upper / Reload_Upper / HitReaction
```

### 2D Blend Tree 节点

| X (Strafe) | Y (Forward) | 动画 |
|:--:|:--:|------|
| 0 | 0 | walking |
| 0 | 7 | rifle run |
| 0 | -3 | walking backwards |
| 0 | -5 | run backwards |
| 3 | 0 | strafe right |
| -3 | 0 | strafe left |
| 7 | 0 | strafe.fbx（快速右移） |
| -7 | 0 | strafe (2).fbx（快速左移） |

### 动画使用率（13/16）

| 动画 | 用途 |
|------|------|
| rifle aiming idle | Idle 站立 |
| walking / rifle run | 前向走/跑 |
| walking backwards / run backwards | 后退走/跑 |
| strafe left / right / strafe / strafe (2) | 侧移慢/快 |
| rifle jump | 跳跃 |
| firing rifle / reloading | 上半身射击/换弹 |
| hit reaction | 上半身受击 |
| **toss grenade** | ❌ 未使用 |
| **turn left** | ❌ 未使用 |
| **turning right 45°** | ❌ 未使用 |

### 已知坑（本次追加）

20. Idle 和 Locomotion 是两个独立状态，通过 Speed 阈值（>0.3 / <0.2）切换，不再是同一 Blend Tree 的子节点
21. JumpSpeed 由 PlayerController 根据 jumpHeight 和 gravity 动态计算（animDuration / airTime），改 jumpHeight 后动画速度自动适配
22. 2D Blend Tree 参数来自世界速度转本地速度（InverseTransformDirection），ForwardSpeed=z, StrafeSpeed=x
23. UpperBody 层的 Avatar Mask 禁用了下半身（Hips + 双腿），Fire/Reload/Hit 只影响上半身
24. 6 个 FBX 的 loopTime 通过 ModelImporter 持久化修改，重新导入不会丢失（**注意：部分修改实际未生效，见阶段 3.5 热修复**）
25. 跳跃高度在 PlayerController.jumpHeight，当前 1.0m（Inspector 序列化值优先于脚本默认值）

---

## 阶段 3.5 热修复（2026-08-04 二次反馈：侧移动画/速度手感）

> 编译：0 错误 0 警告 | 运行时验证：8/8 移动动画全部循环

### 问题与修复

| 问题 | 根因 | 修复 |
|------|------|------|
| 右移/左移/后退腿部动画不自然（播一遍后定格滑行） | **DEVLOG 声称已修的 loopTime 实际只生效 3/5**：strafe right / strafe left / walking backwards / run backwards 四个 FBX 的 loopTime 仍为 0，非循环动画在 BlendTree 中播 1~1.4s 后冻结在最后一帧 | 用 SerializedObject 修改 ModelImporter.m_ClipAnimations[].loopTime=true + SaveAndReimport，meta 与运行时 isLooping 双重确认 |
| 走/跑速度过快（5/8 m/s），与 Mixamo 动画自然步频（走~1.7/跑~4.5）不匹配导致滑步 | walkSpeed=5 sprintSpeed=8 | walkSpeed→3.0、sprintSpeed→6.0（脚本默认值 + 场景 Inspector 序列化值同步修改并保存） |

### 动画-速度匹配说明（无需改 BlendTree）
- 2D Freeform Directional 按"参数点与节点位置距离"计算权重，速度降到 3/6 后权重自动落到正确区间
- 实测（Play 模式驱动参数验证）：StrafeSpeed=3 → strafe right 权重 1.00；StrafeSpeed=6 → strafe.fbx 73%+strafe right 24%；ForwardSpeed=6 → rifle run 89%
- 敌人 patrolSpeed=3/chaseSpeed=6 与玩家 walk/sprint 一致：玩家不跑会被追上、跑了可保持距离

### turn left / turning right 45° 决策（不接入，保留资源）
- 两者是"原地转身"一次性动画（31 帧≈1s 转 45°）
- 玩家不接入：TPS 鼠标平滑转向（帧率远超 45°/s），强插转身动画会反复打断移动动画造成滑步抖动，主流 TPS（PUBG/APEX）均不用
- 敌人不接入：NavMeshAgent 平滑旋转寻路，硬插需暂停寻路+角度检测，与攻击/巡逻状态耦合，收益低风险高
- 适用场景留待：掩体切角（cover pop-out）、敌人警戒转身、投掷物动作；toss grenade 同理留给手雷功能

### 修改文件
- `Assets/Animations/Basic Shooter Pack/strafe left.fbx` / `strafe right.fbx` / `walking backwards.fbx` / `run backwards.fbx`：loopTime 0→1
- `Assets/Scripts/Player/PlayerController.cs`：walkSpeed 5→3、sprintSpeed 8→6
- `Assets/Scenes/SampleScene.unity`：Player 组件 Inspector 速度值同步（已保存）

### 已知坑（热修复追加）
26. **loopTime 修复必须用 SerializedObject 改 m_ClipAnimations[].loopTime 再 SaveAndReimport**，改 meta 顶层 `loop` 字段不生效；修完用运行时 `clip.isLooping` 复核，不要信 DEVLOG 历史结论
27. 场景中 Player 组件序列化的速度值是权威值（脚本默认值只在无序列化值时生效），改速度必须同步场景 Inspector 值并保存场景
28. 编辑器 execute_code 中模拟的 InputSystem 键盘状态会被 PlayerLoop 重置，无法跨帧模拟持续按键；验证 BlendTree 用直接 SetFloat 参数法（见本次验证方法）

---

## 阶段 3.5 热修复 2（2026-08-04 第三次反馈：移动时脚部/身体向右扭曲）

> 编译：0 错误 0 警告 | 修复后实测：玩家 Hips 偏差 0.1°，敌人 0.6~3.7°（转弯自然姿态）

### 根因
- **CharactersAvatar（包内 Characters.fbx 生成的共享 Avatar）的 pre-rotation 补偿有系统性误差**：所有使用该 Avatar 的角色（玩家 + 3 种敌人 8 个实例）在动画驱动后 Hips 世界朝向左偏 **-47.7°**（强制 Idle 状态实测三种模型均稳定 -47.7°，此前 ±7° 波动是巡逻/走路动画中 Hips 自身转向）
- 表现：角色身体整体左偏 47.7°，走路时身体/脚斜向移动方向，看起来"脚向右扭曲"
- 骨骼树中 Toes_L/Toes_R（脚趾尖）不在 Humanoid 映射中、动画不驱动，保持绑定姿势——但与 bind pose 一致，网格渲染正常，无需处理

### 为什么不能改 Root 骨骼/模型旋转
- Root 是 Animator 的 **root bone**，每帧被 Animator 覆盖回动画根旋转（实测 Play 中 localRotation 恒为 0），改场景/prefab 的 Root 旋转无效
- 必须在**动画评估之后**（LateUpdate）对 Hips 做**世界空间绕 Y 补偿**：`hips.rotation = Quaternion.Euler(0f, 47.7f, 0f) * hips.rotation`（刚体旋转，动画姿态不变）

### 修改文件
- `Assets/Scripts/Player/PlayerController.cs`：LateUpdate 末尾 +Hips 补偿
- `Assets/Scripts/Enemy/EnemyBase.cs`：LateUpdate 末尾 +Hips 补偿（与玩家同一 47.7°）

### 已知坑（追加）
29. **CharactersAvatar 系统性 Hips 偏斜 -47.7°**：修改 Hips 骨骼的 pre-rotation 不可行（包内资源且无 API），运行时 LateUpdate 世界空间补偿是标准解法；若未来换 Avatar/模型需重新实测补偿值（强制 Idle 下 Hips 世界 Y - 自身 Y）
30. 验证动画偏差必须**等待动画状态稳定**（手动 anim.Update 多帧或强制 Idle 后读数），巡逻中读数是动画转向噪声
31. 敌人走路时 0.6~3.7° 偏差是 NavMeshAgent 转弯滞后（angularSpeed 120°/s），属正常现象，勿当 bug

---

## 阶段 3.5 热修复 3（2026-08-04 第四次反馈：整个人物朝向右边 —— 动画系统终极修复）

> 编译：0 错误 0 警告 | 实测：玩家 Hips 偏差 0.0°，敌人全部 0~4°（转弯自然姿态）

### 为什么上次的 Hips 补偿是错误方向（本次教训）
- Hips 补偿（+47.7°）后身体骨骼正了，但 **Head 朝右 +90°、Ankle 朝后 180°、四肢/手指全部大偏差**——CharactersAvatar 的 pre-rotation 对 Polygon 怪绑定姿势的补偿是**逐骨骼错乱**的，非单一 Hips 偏差
- 实测（Idle）40 块骨骼偏差各不相同：Spine -180°、Head +90°、Ankle ±155~180°、手指 ±50~178°
- 尝试 AvatarBuilder.BuildHumanAvatar 重建（Polygon 绑定姿势 / mixamorig 姿势 / 全 0 姿势 3 种输入）均不可靠（Hips 偏差 37.7° / -136.9°），Unity 算法不可控

### 终极方案：Generic 动画 + 骨骼重命名（不再走 Humanoid retarget）
1. **16 个动画 FBX 全部改为 Generic 导入**（ModelImporter.animationType=Generic，动画曲线按骨骼路径直接驱动，无 pre-rotation 参与）
2. **场景骨骼重命名为 mixamorig 名**（UpperLeg_L→mixamorig:LeftUpLeg 等 42 块，按 human 映射对应），曲线路径 `mixamorig:Hips/mixamorig:Spine` 直接匹配
3. **Hips 直接挂 Animator 根下**（去掉 Root 夹层；Root 是 prefab 实例时需先 UnpackPrefabInstance，否则 SetParent 被 prefab 系统拒绝——这是"SetParent 无效"的真正原因）
4. **Hips 曲线根旋转修正**：Generic 曲线把动画 FBX 根旋转烘焙进 Hips（各动画 33°~74° 恒定 Y 偏转），生成修正版 `.anim`：`Q'(t) = inv(Q0) × Q(t)`（Q0=首帧），基线归零、动画摆动保留
5. **BindPoseFixer 组件**（新建 Assets/Scripts/Player/BindPoseFixer.cs）：骨骼参考姿势改变后 Start 时复制网格实例并重算 `mesh.bindposes = bones[i].worldToLocalMatrix`（不修改共享资产）
6. **UpperBodyGeneric.mask**（新建）：Generic 模式 humanoid 部位 mask 无效，从 Spine 递归 AddTransformPath 生成骨骼路径 mask，替换 UpperBody 层

### 修改文件/资产
| 类型 | 内容 |
|------|------|
| 导入设置 | 16 个动画 FBX：Humanoid→Generic（Basic Shooter Pack 为项目资产，可改） |
| 动画 | `Assets/Animations/Baked/` 16 个修正版 .anim（Hips 根旋转去除） |
| 骨骼 | 玩家 PlayerModel + 敌人 prefab×3：骨骼改名 mixamorig + Hips 重挂根 + avatar=null（敌人 prefab 嵌套实例已 Unpack） |
| 脚本 | 新增 `BindPoseFixer.cs`；`PlayerController.cs`/`EnemyBase.cs` 移除 Hips 补偿代码 |
| 控制器 | `CharacterAnimator.controller`：13 处动画引用→Baked .anim；UpperBody 层 mask→UpperBodyGeneric |
| 场景 | SampleScene：玩家骨骼结构/avatar/组件，已保存 |

### 已知坑（追加）
32. **Polygon 角色 + Mixamo 动画的 Humanoid retarget 系统性不可用**（CharactersAvatar pre-rotation 逐骨骼错乱），正确路线是 Generic 动画 + 骨骼改名 match 曲线路径；改动后 Animator 必须 avatar=null
33. **prefab 实例内 SetParent 被静默拒绝**（返回成功但父级不变），必须先 PrefabUtility.UnpackPrefabInstance 才能改层级
34. Generic 动画曲线的骨骼路径**不含动画根对象名**（`mixamorig:Hips/...`），场景骨骼须与 Animator 根同层（Hips 直接挂 Animator 根）
35. Generic 曲线会把 FBX 根旋转烘焙进 Hips（各动画 33°~74° 恒定偏转），必须用 `inv(Q0)×Q(t)` 修正；验证用 Idle 下 Hips 世界 Y 是否≈0
36. SkinnedMeshRenderer 没有 mesh 属性（只有 sharedMesh），复制网格实例用 Instantiate(sharedMesh) 后改 bindposes 再赋回
37. Generic 模式下 UpperBody 层的 humanoid AvatarMask 失效（覆盖全身），需用 TransformPaths 形式 mask（AddTransformPath 递归生成）
38. prefab 资产修改后**场景中已有实例不会自动同步**，需重新加载场景
39. Mixamo 动画曲线自带站姿内容：rifle aiming idle 头部 -20°、腿微张（绑定姿势），属动画内容非 bug

---

## 阶段 3.5 热修复 4（2026-08-04 第五次反馈：模型钢丝状 —— 蒙皮绑定矩阵修复）

> 编译：0 错误 0 警告 | 修复后实测：玩家/敌人蒙皮包围盒正常（2.05×1.89×0.54 人形）

### 根因
- BindPoseFixer 初版在 **Start 时读运行时骨骼世界矩阵**计算 mesh.bindposes，但 Generic Animator 在 Start 前已评估过动画（骨骼已被覆盖为动画姿势）→ 绑定矩阵与顶点数据错乱 → 蒙皮顶点塌到骨骼线上（钢丝状）
- 教训：**蒙皮绑定矩阵与"运行时骨骼姿势"无关，必须基于固定参考姿势（T-pose）在 Edit 模式预计算**

### 修复
| 文件 | 内容 |
|------|------|
| `Assets/Scripts/Data/BindPoseData.cs`（新建） | ScriptableObject：boneNames[] + bindposes[]（按骨骼名匹配，顺序无关） |
| `Assets/Data/CharacterBindPose.asset`（新建） | Edit 模式预计算：读玩家激活网格 smr.bones 的 T-pose 世界矩阵求逆，49 块骨骼 |
| `BindPoseFixer.cs`（重写） | Start 时按骨骼名从 poseData 匹配 bindposes，复制网格实例应用，不读运行时骨骼 |
| 玩家 PlayerModel + 敌人 prefab×3 | BindPoseFixer 组件赋值 poseData |

### 验证
- bindpose×boneWorld ≈ 1（绑定一致）；顶点模型空间范围 ±1.02m（T-pose 臂展，网格数据未损坏）
- 蒙皮世界包围盒：玩家 2.05×1.89×0.54、敌人 1.97×1.89×1.69（含迈步摆腿）——人形正常

### 已知坑（追加）
40. **bindposes 必须在 Edit 模式基于固定参考姿势预计算**（读骨骼世界矩阵求逆），绝不能运行时读骨骼姿势（Animator 覆盖后全错）
41. 蒙皮健康度检查：`SkinnedMeshRenderer.bounds` 应接近人形尺寸（约 2×1.8×0.5），若塌成细线/细条即绑定矩阵错乱
42. BindPoseData 按骨骼名匹配（smr.bones 顺序与资产顺序无关），同一 Characters.fbx 骨架的所有角色共用一份

---

## 阶段 3.5 热修复 5（2026-08-04 最终修复：钢丝状彻底解决 + 蒙皮公式纠正）

> 编译：0 错误 0 警告 | 实测：玩家/8 敌人蒙皮头顶 y≈1.87m 全部人形正常，Hips 偏差 0.0°

### 钢丝状的真正根因链（依次修复）
1. **T-pose 化未持久化**：骨骼改名（mixamorig）后，T-pose 化映射表键（Polygon 名）不匹配 → 场景骨骼一直是 Polygon 原始绑定姿势，BindPoseData 基于错误姿势生成 → 蒙皮全错
   - 修复：映射表改用 mixamorig 名，**改完立即 SaveScene**
2. **参考姿势必须匹配动画曲线基线**：各动画 FBX 骨骼基线不同（LeftFoot 差 139°、手臂 63~99°），单一参考姿势无法匹配全部动画
   - 修复：**16 个 .anim 全部骨骼归一化**（Q'(t) = inv(Q0) × Q(t)，基线归零），骨骼参考姿势全 0（Hips 及其余全部 identity）
3. **bindposes 公式错误（本次核心）**：用了 `worldToLocalMatrix`（世界逆），正确公式是 **模型空间逆**：
   `bindPose[i] = (模型根.worldToLocalMatrix × bones[i].localToWorldMatrix).inverse`
   - Unity 蒙皮 = 骨骼世界矩阵 × bindPose × 顶点：世界×模型空间逆 自动组合出"模型根×顶点"的世界输出
   - 文档验证（Mesh.bindposes）：bindPose = 骨骼绑定姿势变换矩阵的逆
4. **场景实例 override 优先于 prefab**：场景中敌人实例的 BindPoseFixer 引用旧 guid 资产 → 重载场景后为 null
   - 修复：**直接在场景实例上赋值**（不能只改 prefab）+ 保存场景
5. **CreateAsset 不覆盖已存在资产**：重复生成 BindPoseData 全部静默失败，数据停留在旧版本
   - 修复：先 DeleteAsset 再 CreateAsset（safety_checks 放行）

### 最终修复状态
- 动画：16 个 Baked .anim（全骨骼归一化）+ controller 引用 13 处 + Generic rig
- 骨骼：玩家 + 敌人 prefab 全部 0 参考姿势（位置保持 Polygon 关节），Hips 挂 Animator 根
- BindPoseData（模型空间逆，49 骨骼）+ BindPoseFixer（按名匹配应用）
- 验证：CPU 蒙皮采样玩家头顶 (-4.03, 1.89, -4.06)、脚底 (-4.08, 0.08, -3.87) 完全人形

### 已知坑（追加）
43. **bindposes 正确公式 = inv(骨骼在网格根空间的矩阵)**（`模型根.worldToLocal × bone.localToWorld` 求逆），不是世界矩阵逆；两者仅当模型根在原点时才等价
44. **验证蒙皮必须过滤 activeSelf**（组合体有 20 个网格），GetComponentInChildren 默认返回层级第一个（可能非激活）
45. 场景中 prefab 实例的组件 override 优先于 prefab 资产，改 prefab 后实例引用不更新的场景对象必须直接在实例上改
46. AssetDatabase.CreateAsset 对已存在路径静默失败（不覆盖不报错），更新资产必须先 DeleteAsset
47. 动画资产删除后 controller 引用变 missing（motion 判空），重建引用需按状态名/BlendTree 节点位置显式设置

---

## 阶段 3.5 回滚（2026-08-04：动画系统探索失败，恢复对话开始状态）

> 背景：本日多次尝试修复动画/蒙皮问题（loopTime、Hips 补偿、Avatar 重建、Generic 化、骨骼改名、bindposes 重算），最终导致模型显示异常（朝向错乱/钢丝状/T-pose）。由于 git 仅有阶段 2 提交（阶段 3/3.5 未提交），无法用 git 精确回退，采用**手动重建恢复**。

### 恢复内容
| 项目 | 操作 |
|------|------|
| 16 个动画 FBX | Generic → **Humanoid**（重新导入） |
| CharacterAnimator.controller | 引用 Baked .anim → **FBX clip**（13 处）+ UpperBody 层 mask → **UpperBodyMask** |
| 玩家 PlayerModel | 删除被破坏的组合体 → **重新实例化包内 Character_MilitaryMale_01.prefab**（Animator=CharacterAnimator + CharactersAvatar + applyRootMotion=false） |
| 8 个敌人 | 删除 → **重建**（包内角色 prefab + EnemyBase/NavMeshAgent/CapsuleCollider + 原位置/朝向/路点分配/差异化数值） |
| 速度 | 恢复 walk=5 / sprint=8（脚本 + 场景），**用户确认后改回并保留 walk=3 / sprint=6（降速需求）** |
| 清理 | 删除 Baked/、BindPoseFixer.cs、BindPoseData.cs、UpperBodyGeneric.mask、CharacterBindPose.asset、PlayerAvatar 测试资产、旧敌人 prefab×3 |

### 恢复后状态（= 对话开始时）
- 玩家/敌人：原生 Polygon 模型 + CharactersAvatar + Humanoid Mixamo 动画（含 47.7° Hips 朝向偏差的已知小瑕疵）
- 敌人 prefab 已删除（场景实例为无 prefab 关联对象，后续如需可重新制作）
- 编译 0 错误，场景已保存

### 教训（重要）
48. **git 提交必须在每个阶段完成后立即执行**（本次因阶段 3/3.5 未提交，恢复只能手动重建，工作量巨大且易错）；建议恢复确认后立即提交
49. Polygon 骨骼 + Mixamo Humanoid 动画的 retarget 偏差（47.7° 等）是已知限制，若要根治需专用工具链（烘焙/重定向），不建议在运行时盲目补偿

---

## 会话 2 记录（2026-08-04：动画问题深入排查 → 回滚 → 换 P08 模型 → TPS 动画测试）

> 本段记录 83e041e 提交后的全部工作与结论，供下一轮对话直接续接。

### 一、本次会话时间线
1. **初始动画讨论**（已归档）：turn left/turn right 决策不接入、4 个移动动画 loopTime 修复、速度降为 3/6
2. **朝向问题链**：Hips 偏 -47.7° → 试 Hips 补偿/BuildHumanAvatar/Root 旋转均失败 → Generic 化+骨骼改名+蒙皮重算导致钢丝状 → **手动回滚重建** → **git 提交 83e041e**
3. **动画烘焙方案试水**：创建 TestBake 测试场景 + AnimationBaker 工具，最终 CPU 验证走路蒙皮成功（头顶 1.83m/脚底 -0.05m），但用户看到测试场景麻花 → **放弃，删除全部测试产物，git 恢复干净**
4. **CSDN 文章评估**（Configure Avatar Force T-Pose 法）：方向正确（改 Avatar 参考姿势，不动蒙皮，无钢丝风险），需手动操作 + 自有资源
5. **换 P08 模型**：P08_Federica_Plain_C Variant 移植为玩家模型（替换 PlayerModel）——功能/动画/网格全部成功，但 **P08 也是 Polygon 骨骼（Hips 绑定 270°），同样 -47.7° 偏差**
6. **TPS Shooter 动画测试**：导入 Humanoid/ 目录动画 → 实测能 retarget 到 P08，但**偏差依旧（证伪"Mixamo 动画问题"假设）**；TPS 动画自带 Avatar 不能用（骨骼不匹配，动画不驱动）；替换走/跑为 TPS 动画 → 偏差 60° 更严重

### 二、当前项目状态（未提交变更）
```
M  Assets/AnimControllers/CharacterAnimator.controller   ← 前向走/跑节点已换成 TPS walk/run
?? Assets/Animations/Humanoid/                          ← TPS Shooter 动画资源（Equiped/Freehands/Vehicle）
?? Assets/Animations/WeaponPickUp/                      ← 拾取动画
?? Assets/Animations/rifle runT.fbx                     ← 用户测试用动画
?? Assets/P08_Federica/                                 ← P08 角色模型（自有资源，含 liltoon shader）
?? Packages/jp.lilxyzw.liltoon-2.3.3/ + ProjectSettings/lilToonSetting.json
M  Assets/Scenes/SampleScene.unity                      ← 玩家模型已换成 P08（PlayerModel=P08 实例）
```
- 玩家 = P08_Federica_Plain_C Variant（Player 子物体 PlayerModel，Animator=CharacterAnimator + P08 自带 Avatar + applyRootMotion=false）
- 玩家速度 walk=3/sprint=6；敌人 士兵追5.5/侦察6.5/重装3.5
- 编译 0 错误

### 三、核心结论（重要认知）
1. **47.7° 偏差与动画源无关**：Mixamo 与 TPS 动画 retarget 到 Polygon 模型都偏（实测 TPS 偏 60° 更明显）；偏差由**目标模型骨骼绑定姿势 + Avatar pre-rotation** 决定
2. **P08 也中招**：P08 Hips 绑定旋转 (0,270,0)、skeleton_P08 (270,0,0)，绑定姿势 Hips 世界前向 = -X（非标准），同样是 Polygon 骨骼问题
3. **Configure 里看着标准 ≠ pre-rotation 正确**：Configure 显示参考姿势（经归一化）/网格 bind pose 补偿，但运行时 pre-rotation 实际补偿误差 -47.7°
4. **TPS 动画自带 Avatar 不能移植**：Avatar 必须匹配模型骨骼（实测换 TPS Avatar 后动画完全不驱动）
5. **唯一根治方向**：**Configure P08 自己 Avatar → Force T-Pose / Apply 重新生成**（P08 是自有资源 Assets/P08_Federica/，可改）——尚未执行，待尝试

### 四、待决策/待办
- [ ] **走/跑动画去留**：当前前向走/跑已换成 TPS（偏差 60° 更差），建议还原回 Mixamo，待用户确认
- [ ] **Configure P08 Avatar 修复偏差**（Force T-Pose → Apply），成功后验证 Hips 是否归 0
- [ ] **提交 git**：P08 移植 + TPS 动画导入 + controller 改动（确认后提交，防再丢进度）
- [ ] 阶段 4 玩法开发

### 五、关键资源路径
- P08 模型：`Assets/P08_Federica/Model_Data/fbx/P08_Federica_forUnity_250729.fbx`（Avatar 名 P08_Federica_forUnity_250729Avatar）
- TPS 动画：`Assets/Animations/Humanoid/EquipedAnimations/`（持枪：Walk/run/Idle/Reload/RifleEquip/Jump/Death/Crouch）
- 玩家控制器：`Assets/AnimControllers/CharacterAnimator.controller`（BlendTree 节点0/1 当前为 TPS 动画）

---

## 会话 3 记录（2026-08-06~07：移动动画重建、武器 IK、准星系统）

> 本段记录 984e409 提交后的全部工作与结论，供下一轮对话直接续接。

### 一、时间线

1. **武器双挂点方案失败并回滚**：最初尝试 HipHolder/ADSHolder 双挂点（武器移 Player 根），期间误删 WeaponHolder、搞坏场景内存缓存，git checkout 后因编辑器未重载场景导致问题依旧；最终强制重载磁盘场景恢复
2. **移动动画"定住/极慢"根因链**：
   - ① git 提交的 controller 中 Locomotion/Crouch 的 motion=NULL（BlendTree 从未持久化）
   - ② 代码重建 BlendTree 子节点时未设 `timeScale` → 序列化为 **0.01（1% 播放速度）** → "慢到像静止"
   - ③ **状态转换从未持久化**：`AddTransition` 创建的 AnimatorStateTransition 未 `AddObjectToAsset` 注册 → 磁盘 YAML 引用全 `fileID: 0`；iKPass 的 `controller.layers = layers` 操作破坏内存缓存后彻底失效 → "只有位移无动画"
3. **状态机按 TPS Shooter 重建**（对照 `K:\Unity\UnityProject\TPS Shooter`）：Walking/Running 分离 + ±1.0 归一化 BlendTree + 仅前向冲刺
4. **武器 IK 系统**：武器挂右手 hand_R + 左手护木 IK（Unity 内置 Animator IK）
5. **准星系统**（A+B）：复制 TPS crosshair.png + CrosshairController 敌人变色

### 二、移动动画重建（PlayerAnimator.controller）

```
Base Layer:
  Idle ←→ Walking Locomotion (Speed>0.3 / <0.2)
  Walking ↔ Running Locomotion (IsRun)
  Walking/Running → Jump (Jump) → Walking (Grounded)
  Walking/Running → Crouch (IsCrouch) → Walking (IsCrouch off)
  AnyState → Die (Died)

Walk2D (9节点 ±1.0)：Idle Aiming(0,0) + walk_fwd/bwd/left/right(±1) + walk_45×4(±0.7)
Run2D (6节点 ±1.0，照抄 TPS)：run_45_left(-0.7,0.7) run(0,1) run_45_right(0.7,0.7) idle(0,0) walk_left(-1,0) walk_right(1,0)
Crouch2D (9节点 ±1.0)：crouch_idle + 8 方向 crouch_walk
```

**关键设计（与 TPS Shooter 一致）**：
- BlendTree 节点坐标 **±1.0 归一化**（参数 ForwardSpeed/StrafeSpeed = 实际速度/目标速度）
- **只有前向（localVelocity.z>0.3）才 IsRun=true 并加速**，后退/横移按 Shift 用走路速度 → Run2D 不需要后退节点
- PlayerController.LateUpdate：`ForwardSpeed = localVelocity.z / normSpeed`、`IsRun = sprint && forward`

### 三、三个动画"定住/极慢"根因（重要教训）

| 根因 | 现象 | 修复 |
|------|------|------|
| BlendTree motion=NULL（git 提交版本缺失）| 移动无动画 | 重建 Locomotion2D/Crouch2D + `AddObjectToAsset` 持久化 |
| **ChildMotion.timeScale 未初始化 → 0.01** | 动画慢到静止 | 遍历所有 BlendTree 子节点设 `timeScale=1, cycleOffset=0` |
| **AnimatorStateTransition 未 AddObjectToAsset** | 切枪后全无动画（磁盘 fileID=0，iKPass 操作破坏内存缓存后暴露）| 重建全部转换 + `AddObjectToAsset` 注册，验证磁盘 YAML fileID 非 0 |

**教训**：
50. 代码创建 BlendTree 子节点必须显式设 `timeScale=1`（C# struct 默认序列化为 0.01）
51. **AddTransition 创建的转换对象必须 `AssetDatabase.AddObjectToAsset` 注册**，否则磁盘引用 fileID=0（内存正常但重载丢失）
52. **禁用 `controller.layers = layers` 修改层属性**（会破坏未注册对象引用），改 iKPass 等层字段后必须重导入验证
53. `AssetDatabase.ImportAsset(path, ForceUpdate)` 可强制从磁盘重载（覆盖损坏的内存缓存）
54. 恢复场景要用"新建临时场景→重新加载目标场景"强制丢弃编辑器内存状态（git checkout 只改磁盘）

### 四、武器 IK 系统（手部贴合）

```
Player/P08.../skeleton/.../hand_R（武器挂右手，用户手动改，Humanoid 骨骼 reparent 被 Unity 拒绝）
  └── WeaponHolder (scale 0.5)
      └── Weapon_Rifle/SMG/Pistol
          ├── Model / FirePoint
          └── LeftHandIk（护木 IK 目标挂点）
```

- **PlayerIKController.cs**（挂 P08 模型上，OnAnimatorIK 必须与 Animator 同对象）：左手位置 IK 吸附 LeftHandIk，旋转权重 0（保留动画朝向）；换弹时左手权重归零（`FirearmWeapon.IsReloading`）
- **Base Layer iKPass=true**（OnAnimatorIK 触发前提）
- **武器预制体**：三把武器都设置挂枪姿势（localPos/localRot）+ LeftHandIk 挂点；`WeaponManager.EquipWeapon` 不再重置 local（用预制体值）→ 切枪后手部贴合
- 实测：Rifle 握把→右手 0.006 / 左手→护木 0.000；SMG 0.021；Pistol 0.044

**教训**：
55. **Humanoid 骨骼层级受保护，prefab 内 SetParent 被静默拒绝**（返回成功父级不变）
56. **给预制体加子物体必须"实例化临时副本→修改→SaveAsPrefabAsset 覆盖"**，`new GameObject + SetParent(预制体资产)` 不会保存
57. `OnAnimatorIK` 回调只由**挂载脚本的 GameObject 上的 Animator** 触发，脚本必须与 Animator 同对象
58. Humanoid 的 `OnAnimatorIK` 需要某层启用 **IK Pass**（`layer.iKPass=true`）
59. 武器挂右手骨骼后**右手不能用 IK**（形成 手→武器→挂点→手 反馈循环），只左手 IK 到护木
60. 验证 IK 生效用"移动挂点看手是否跟随"，`GetIKPositionWeight` 在 execute_code 里读到的值不可靠（读取时机）

### 五、准星系统（A+B）

- `Assets/UI/crosshair.png`（复制 TPS Shooter，Sprite 导入 128×128）
- Canvas/Crosshair：屏幕中心 48×48 Image
- **CrosshairController.cs**（挂 Canvas）：每帧 `Camera.main` 屏幕中心射线 → `hit.collider.GetComponentInParent<EnemyBase>()` → 命中敌人变红，否则白色
- 射线与射击同源（屏幕中心），所见即所射

### 六、清理

- 删除场景根 3 个游离 LeftHandIk 残留（new GameObject 失败留下的垃圾对象）
- cullingMode：CullUpdateTransforms → **AlwaysAnimate**（角色短暂出视野动画冻结的隐患）

### 七、当前文件清单（新增/修改）

| 文件 | 说明 |
|------|------|
| `Assets/Scripts/Player/PlayerIKController.cs` | 新建：左手护木 IK |
| `Assets/Scripts/UI/CrosshairController.cs` | 新建：准星 + 敌人变色 |
| `Assets/Scripts/Combat/FirearmWeapon.cs` | +IsReloading 属性 |
| `Assets/Scripts/Combat/WeaponManager.cs` | EquipWeapon 不重置 local |
| `Assets/Scripts/Player/PlayerController.cs` | 参数归一化 + IsRun 前向冲刺 |
| `Assets/AnimControllers/PlayerAnimator.controller` | Walking/Running/Crouch BlendTree ±1.0 + 转换持久化 + iKPass |
| `Assets/Prefabs/Weapon_Rifle/SMG/Pistol.prefab` | 挂枪姿势 + LeftHandIk |
| `Assets/UI/crosshair.png` | 准星贴图 |
| `Assets/Scenes/SampleScene.unity` | 场景同步 |

### 八、遗留/待办

- [ ] Pistol 握把贴合 0.044 可微调（预制体 localPosition）
- [ ] 阶段 4：音效 / 粒子 / 敌人死亡 / 手雷 / 性能

---

## 阶段 4 记录（2026-08-07 提交 + 2026-08-31 验证补交）

> 编译：0 错误 0 警告 | Play 模式验证：暂停/恢复/设置面板全部通过（2026-08-31）

### 已完成（3 次提交 + 本次补交）

| 提交 | 内容 |
|------|------|
| `958332e` | 阶段4-UI：主菜单场景（标题/开始/设置/退出 + Loading 读条 + DOTween 动画）、设置持久化（灵敏度/音量/FOV/自动射击）、程序化 UI 纹理、BuildSettings 接入 |
| `9cc0626` | 场景替换方案A（DemoScene 800m 关卡接入 SampleScene + NavMesh 300×300 烘焙）、后处理增强（ColorAdjustments 军事色调/Bloom/Vignette）、游戏设置接入 |
| `247f7b7` | 暂停菜单（Tab 键暂停/继续 + 设置面板 + 返回主菜单）、后坐力调小（步枪 1.5→0.8 / 冲锋枪 1→0.6 / 手枪 2→1.2）、设置滑块范围优化（FOV 55~100 / 灵敏度 1~10） |
| 本次（2026-08-31） | 暂停菜单体验优化：① 暂停检测从 `Keyboard.current` 轮询迁移到 Input System（新增 Pause 动作绑定 Tab + `IsPausePressed()`，与 Crouch/Heal 同模式）；② 暂停/设置面板 DOTween 淡入 `SetUpdate(true)`（timeScale=0 时动画不冻结）；③ CloseSettings 返回时暂停面板 alpha 归零重新淡入 |

### 验证结果（2026-08-31 Play 模式实测）

- 编译 0 错误；`_pauseAction` 运行时 enabled、1 个 Tab 绑定
- Pause()：timeScale=0、面板 0.2s 淡入至 alpha=1.00（timeScale=0 下正常播放）、鼠标解锁、disableOnPause 5/5 脚本禁用
- Resume()：timeScale=1 恢复
- OpenSettings/CloseSettings：面板切换正常，返回时暂停面板重新淡入
- 编辑器内模拟 Tab 按键不可靠（已知坑 28），输入链路以"绑定存在 + 与 Crouch/Heal 同模式"佐证

---

## 会话 4 记录（2026-08-31：角色腾空问题系统性排查与修复）

> 编译：0 错误 0 警告 | 修复后全岛 3055 点落点扫描 0 异常

### 一、问题现象
角色移动到特定区域（草地/土路）时脚底悬空约 0.3m，从道路走上草地时明显"弹起"。

### 二、系统性排查过程
1. **移动代码检查**：PlayerController 重力/单次合并 Move/地面检测逻辑正常（`isGrounded` 重置 -2、jumpHeight 逻辑无异常）
2. **CharacterController 参数检查**：胶囊 2m/radius 0.5/slopeLimit 45/stepOffset 0.3/skinWidth 0.08，全部标准
3. **全岛网格射线扫描**（31×31 点）：地形布局摸清——岛屿 x∈[-50,60] z∈[-110,50]，草地板块 y≈0.3，道路 y=0.0，水面 y=-2.9
4. **静态碰撞分析**：草地板块（SM_Env_Grass_*）碰撞体顶比可见网格顶高 0.30~0.35m
5. **胶囊落点扫描**（3055 点，用 CharacterController.Move 真实物理路径）：修复前草地落点 0.36（悬空）
6. **排除项**：道具上浮假说被推翻（8m 大步移动的穿透假象；真实小步行走不会爬上坦克/集装箱）；全岛无隐形平台

### 三、根因
**草地/土路板块的 MeshCollider 使用手工凸包代理网格（`_Convex` 后缀），其表面比可见地形网格高 0.3m**：
- 渲染网格 `SM_Env_Grass_Square_01`：701 顶点，局部 Y 最高 0.464
- 碰撞网格 `SM_Env_Grass_Square_01_Convex`：16 顶点凸包，局部 Y 最高 0.765
- 角色实际站在"隐形凸包壳"上，脚底悬空（Synty 场景对可行走地面误用凸包代理；凸包只适合小道具）

### 四、修复方案
可行走地面的 MeshCollider 换用渲染网格（精确三角网格碰撞）：
- `mc.sharedMesh = mf.sharedMesh; mc.convex = false;`
- 修复 41 处：Grass_Square×18 + Grass_Circle_01×13 + Grass_Circle_02×9 + DirtRoad_End×1
- 码头（Dock）凸包与网格一致（偏差 0.00）不修；道路用 `_Collision` 代理本来就吻合不修
- 静态对象用非凸包 MeshCollider 合法（凸包限制仅针对 Rigidbody）
- NavMesh 重新烘焙（318ms）：敌人行走高度与新地面一致（8 敌人 onNavMesh 全过）

### 五、修复后回归验证
| 测试 | 结果 |
|------|------|
| 全岛 3055 点胶囊落点扫描 | 全部 y=0.08（视觉地面+skinWidth），0 异常 |
| 草地 (40,-30) | 0.36 → 0.08 ✅ |
| 土路 (30,-50) | 0.36 → 0.08 ✅ |
| 道路 (0,-15) | 0.08 → 0.08（不回归）✅ |
| 纵穿/横穿全岛行走 | Y 全程 ≤0.28，无腾空、无穿模 |
| 敌人 | 8/8 onNavMesh，贴地偏差 0.04~0.23m |
| 截图视觉确认 | 双脚踩进草地（对比修复前悬在草面上方） |

### 已知坑（追加）
61. **Synty 场景可行走地面（草地/土路板块）的 `_Convex` 凸包代理网格比可见地形高 0.3m**，角色会悬空；修复=换用渲染网格+convex=false（静态对象合法），改完必须重烘焙 NavMesh 保持敌人/玩家同高
62. 排查腾空问题的有效路径：①静态扫描"碰撞体 bounds vs 渲染器 bounds"差值 → ②CharacterController.Move 胶囊落点扫描（与运行时同物理路径）→ ③对照点前后对比
63. 一次性大位移 cc.Move（如 8m）会产生穿透/挤压假象（被顶到 y=4），验证必须用小步（0.1m）模拟真实行走
64. 传送后立即读位置是"未落地"状态（重力每帧只走 ~0.04m），必须跨帧读取或用 cc.Move 主动下落后再读
65. **反射调用 NavMeshSurface.BuildNavMesh() 只做内存烘焙，不落盘**（GetAssetPath 为空）；必须把 m_NavMeshData 用 AssetDatabase.CreateAsset 写成资产（路径已存在时先 RenameAsset 旧资产腾位，CreateAsset 静默失败见坑 46）再 SaveAssets + 保存场景。本次发现 8/7 的"NavMesh 300×300 烘焙"同样没落盘（旧资产停留在 8/3 的 10KB），NavMesh 持久化后新资产 769KB（渲染网格更密）
66. NavMesh 烘焙数据密度跟碰撞网格精度走：渲染网格（701 顶点）烘焙出的 NavMesh 比凸包（16 顶点）大 70 倍，属正常

---

## 任务包 4A 完成记录（2026-09-06）

> 编译：0 错误 0 警告 | 场景已保存 | 音效（4B）因 fal.ai Key 未配置顺延，顺序改为 4A → 4C → 4D → 4E → 4F → 阶段5 → 阶段6

### 新增脚本（3 个）

| 文件 | 说明 |
|------|------|
| `Assets/Scripts/Data/GameEvents.cs` | 静态事件总线（决策 D2）：玩家/敌人/表现/流程 4 组事件 + Raise 方法（`Raise{事件名去掉On}`）；事件只增不改签名 |
| `Assets/Scripts/Data/ObjectPool.cs` | 通用对象池（静态类）：Prewarm/Get/Release + PoolTicket 票据组件；池根 DontDestroyOnLoad，场景切换自动清池 |
| `Assets/Scripts/Audio/AudioManager.cs` | 音频单例：2D UI 音源×1 + 3D 音源池×8（轮询复用）；订阅 GameEvents 播音；Clip 字段留空待 4B 填入，空 Clip 静默跳过 |

### 缺陷修复（F1~F4）

| 编号 | 修复 |
|------|------|
| F1 | 切枪刷弹：WeaponManager 增加 `_ammoCache` 字典（槽位→弹匣/备弹），换枪销毁前保存、切回时经 `FirearmWeapon.pendingMagazine/pendingReserve` 挂起值在 Start 恢复（Instantiate 后直接 Set 会被延迟执行的 Start 覆盖——关键坑） |
| F2 | 暂停菜单补 `autoShootToggle`（Toggle），读写走 `GameSettings.SetAutoShoot()`（同步 PlayerPrefs + 运行时缓存）；场景 PauseSettingsPanel 已建控件（FOV 滑块下方，BackButton 下移避让） |
| F3 | EnemyBase 删除每 0.5s 状态日志与每帧追击日志（GetCurrentState 一并移除），保留的攻击/受击日志包 `#if UNITY_EDITOR` |
| F4 | PlayerADSController 的 `GameObject.Find("TPSCamera")` 改为 `[SerializeField] tpsCamera` 序列化引用（场景已赋值），保留空值兜底查找 + 警告 |

### 事件接线（4B/4C 的触发源）

- FirearmWeapon.Shoot → `RaisePlayerShot(枪口位置, weaponData.name)`；ReloadRoutine → `RaiseReloadStart/End`
- EnemyBase：攻击 → `RaiseEnemyShot`；受击 → `RaiseEnemyDamaged`；死亡 → `RaiseEnemyKilled`

### 场景变更（SampleScene 已保存）

- 新增根对象 `AudioManager`（AudioManager 组件，音源池 8）
- Player/PlayerADSController.tpsCamera ← TPSCamera
- Canvas/PauseSettingsPanel/AutoShootToggle（新建 UGUI Toggle，字体 LegacyRuntime）

### 已知坑（追加）

67. **Instantiate 后立即给组件字段赋值会被 Start 覆盖**（Awake/OnEnable 即时、Start 延迟到帧末）——切枪弹药恢复等"实例化后初始化"必须用挂起值模式（pending 字段在 Start 消费）
68. `GameObject.Find` 找不到非激活对象，找隐藏 UI 用 `canvas.transform.Find(...)`
69. 编辑器模式（非 Play）manage_camera 的 ScreenCapture 不可用，回退主相机渲染不含 Overlay UI；Overlay 面板的视觉验证需 Play 模式或 Scene View（大场景中 Overlay 矩形过远，截图效果差）

---

## 任务包 4C 完成记录（2026-09-06~07）

> 编译：0 错误 0 警告 | 场景已保存 | 音效（4B）继续顺延（fal.ai Key 未配置）

### 新增脚本（3 个）

| 文件 | 说明 |
|------|------|
| `Assets/Scripts/Enemy/EnemyPart.cs` | 部位标记：isHead + Owner 懒查找父级 EnemyBase |
| `Assets/Scripts/Combat/BulletTrail.cs` | 曳光组件：LineRenderer 两点连线，存活 lifetime（默认 0.08s）后自动 ObjectPool.Release |
| `Assets/Scripts/Combat/CombatEffectsController.cs` | 战斗表现单例：订阅 GameEvents.OnBulletTrail → 对象池取曳光实例；prefab 引用 + 预热 8 |

### EnemyBase 升级（4C 核心）

| 项 | 内容 |
|----|------|
| 新字段 | `accuracy=0.7`（命中率，散布反向控制）、`corpseDuration=10`、`headshotMultiplier=2`（§7.2 的 bulletTrailPrefab 改挂 CombatEffectsController，场景级单点配置） |
| Raycast 弹道（D1） | AttackPlayer 重做：敌人胸口(+1.5m)→玩家胸口(+1.2m) Raycast（range=attackRange+3），命中 PlayerHealth 才扣血——**隔墙扣血根治**；散布角 =(1-accuracy)×15°；广播 RaiseBulletTrail(起点,终点) |
| 死亡重做 | Die(): RaiseEnemyKilled → `Animator.SetTrigger("Died")` → 禁用 NavMeshAgent+全部 Collider → corpseDuration 后 Destroy（原 0.5s） |
| 部位碰撞体 | Start 时 SetupHeadPart：人形 Avatar 用 `GetBoneTransform(Head)` 挂 SphereCollider(r=0.16)+EnemyPart(isHead)——8 敌人运行时自动，无需场景配置 |
| TakeDamage | 签名 `void → bool`（返回是否被击杀，供爆头击杀判定） |

### FirearmWeapon 命中重做

- `ApplyHit(hit)`：先查 EnemyPart（头/身）再回退 EnemyBase；爆头伤害 ×enemy.headshotMultiplier；命中敌人广播 `RaiseHitConfirmed`（4D HitMarker 订阅），爆头击杀广播 `RaiseHeadshotKilled`

### EnemyAnimator.controller 变更（已磁盘验证）

- Base 层新增 **Die 状态**（motion=`Animations/Humanoid/EquipedAnimations/Death/death.fbx` 的 death 剪辑，3.42s 不循环 Humanoid）+ AnyState→Die 转换（Died:If，duration 0.15，canTransitionToSelf=false）+ Died Trigger 参数
- UpperBody 层不动：死亡后 _isDead 阻断 Update，不会再触发 Fire/Hit

### 新增资产/场景对象

- `Assets/Prefabs/FX_BulletTrail.prefab`（LineRenderer，宽 0.04，Sprites/Default 材质淡黄色）
- SampleScene 新增根对象 `CombatEffects`（CombatEffectsController + prefab 引用）

### 已知坑（追加）

70. **Unity 2022 的 `AnimatorStateMachine.AddState/AddAnyStateTransition` 会自动把子对象注册进 controller 资产**，再手动 `AddObjectToAsset` 会抛 "Adding asset to object failed"；且异常前对象已落盘——验证持久化以 `AssetDatabase.ImportAsset` 后磁盘重载为准（坑 51 需按此更新：手动注册仅对未持久化对象执行 `IsPersistent` 检查后进行）
71. `AnimatorStateTransition` 无 `isAnyState` 属性；AnyState 转换从 `stateMachine.anyStateTransitions` 枚举
72. 编辑器 execute_code（CodeDom C#6）中 `Object` 与 object 二义性，须写 `UnityEngine.Object`
73. **URP 项目中 `Sprites/Default` 着色器渲染洋红**（曳光首版变紫的根因）；且 SaveAsPrefabAsset 对运行时 new 的材质不保证内嵌持久化（sharedMaterial 变 NULL）——正确做法：材质建独立资产存 `Assets/Materials/`（URP/Unlit），经 `LoadPrefabContents → 赋材质 → SaveAsPrefabAsset` 写回预制体

### 4C 热修复（2026-09-07 用户 Play 反馈：曳光洋红）

- `FX_BulletTrail.prefab` 材质重建：Sprites/Default（URP 下洋红）→ **URP/Unlit 淡黄**，独立材质资产 `Assets/Materials/FX_BulletTrail.mat`，LoadPrefabContents 写回预制体，磁盘验证 shader/颜色正确

### 4C 验收清单（待用户 Play 验证）

- [ ] 隔墙/掩体后敌人**不再**扣血（掩体后站立验证）
- [ ] 敌人开枪可见淡黄色曳光一闪（~0.08s）
- [ ] 爆头伤害 = 普通×2（步枪 25→50，更快击杀）
- [ ] 敌人死亡播死亡动画倒地，尸体静置 10s 后消失
- [ ] accuracy 调 0 → 敌人几乎打不中；调 1 → 站桩必中

---

## 任务包 4D 完成记录（2026-09-08）

> 编译：0 错误 0 警告 | 场景已保存 | 音效（4B）继续顺延

### 新增/修改脚本

| 文件 | 内容 |
|------|------|
| `GameEvents.cs` | 新增 `OnSurfaceHit(point, normal, isEnemy)` 事件 + RaiseSurfaceHit（只增不改） |
| `FirearmWeapon.cs` | ApplyHit 广播 RaiseSurfaceHit：打敌人 isEnemy=true（血），打静态物 false（烟+痕） |
| `CombatEffectsController.cs` | 扩展 4D 分支：bloodFxPrefab / impactSmokePrefab / bulletDecalPrefab 三引用；弹痕 50 上限环形队列；包内 FX 的 stopAction Destroy→Disable 兼容池化 |
| `HitFeedbackUI.cs`（新建） | 挂 Canvas：HitMarker（命中放大淡出 0.15s，DOKill 防连发叠加）+ HEADSHOT 提示（弹出 0.12s→停 1s→淡出 0.3s） |

### 新增资产（全部程序化生成）

- `Assets/Textures/HitMarker.png`（64×64 白色 X，Sprite）
- `Assets/Textures/BulletHole.png`（64×64 黑芯+噪声碎裂环）
- `Assets/Materials/FX_BulletDecal.mat`（URP/Unlit + AlphaTest，queue 2450）
- `Assets/Prefabs/FX_BulletDecal.prefab`（Quad 0.14m 无阴影，贴弹孔贴图）
- 复用包内 FX：`BloodSplat_FX.prefab`（敌人命中）、`GunShot_Smoke_FX.prefab`（静态命中）

### 场景变更（SampleScene 已保存）

- Canvas 新增 `HitMarker`（中心 30×30）与 `HeadshotText`（"HEADSHOT!" 屏幕中上部，默认隐藏），HitFeedbackUI 字段已挂接
- CombatEffects 三特效引用已赋值

### 已知坑（追加）

74. **编辑器 execute_code（CodeDom）环境无 `using UnityEngine.UI` / `using System`**：UI 类型写 `UnityEngine.UI.Image/Text` 全限定名；`Random` 与 System.Random 二义须写 `UnityEngine.Random`

### 4D 验收清单（待用户 Play 验证）

- [ ] 打中敌人：溅血 + 准星中心白色 X 闪现
- [ ] 打墙/地面：烟尘 + 黑色弹痕（多打几发弹痕正确贴墙面）
- [ ] 连发射击 HitMarker 不闪烁卡死
- [ ] 爆头击杀出现 "HEADSHOT!" 弹出提示
- [ ] 弹痕超过 50 个后最早的形成循环覆盖（肉眼难验，可不测）

### 4D 热修复 + 全面审查（2026-09-08 用户反馈：HEADSHOT 不显示）

| 问题 | 根因 | 修复 |
|------|------|------|
| 爆头判定/×2 伤害/HEADSHOT 提示整条链从未触发 | **敌人根部 CapsuleCollider（h=2.0，顶到 y=2.0）把头部（y≈1.57）整个包在内部**，射线打头先命中胶囊表面（判为身体），头部 EnemyPart 球体永远轮不到 | SetupHeadPart 同时收缩胶囊体到 0~1.5m（height=1.5, center=0.75）覆盖躯干腿，头部球体 r 加大到 0.18（覆盖 ~1.4~1.75），两者衔接不遮挡 |
| 连续爆头 UI 叠加隐患 | HEADSHOT 的 DOTween Sequence 未存引用，旧序列 OnComplete 会中途隐藏新提示 | 序列存 `_headshotSeq` 字段，触发时先 Kill 旧序列 |

**审查结论（4A/4C/4D 其余项均通过）**：F1 弹药缓存（缓存/挂起值/Start 消费链路正确）；F2 AutoShoot 双向绑定；F3/F4 已修；Raycast 弹道挡弹正确；曳光材质 URP/Unlit 正常；死亡→Died 触发→禁用碰撞→10s 销毁链路正确；对象池跨场景边界（池根 DontDestroyOnLoad + 键随预制体销毁自然重建）安全；HitFeedbackUI 场景引用完好；CombatEffects 四特效引用完好。

**已知坑（追加）**

75. **给角色添加部位碰撞体时必须检查根部胶囊体的遮挡**：默认 2m 胶囊把头部包在内部，Physics.Raycast 永远先命中胶囊，部位判定（爆头）静默失效——部位碰撞体方案必须配套收缩主碰撞体或分层射线掩码
76. **胶囊收缩后仍有二次遮挡 + 球体覆盖不足**（4D 二次实测）：胶囊 1.5 时顶部到世界 ~1.58（含根部离地 0.08），Head 骨骼枢轴在颈部（骨 y≈1.43~1.59 随动画浮动），r=0.18 无偏移的球只覆盖 ~1.5~1.77 窄带——下颌段被胶囊挡、头顶段探不出球；Play 模式多高度射线探针（头部前方 1.2m 水平打 6 个高度）是定位此类问题的有效手段
77. **Play 模式验证 UI 显示可用 ScreenCapture 截图（含 Overlay Canvas，坑 69 只限编辑器模式）**；跨 MCP 调用验证"限时消失"的 UI 需临时延长显示时间（如 headshotShowTime=30）再截图

### 4D 二次热修复（用户复测：爆头击杀仍无 HEADSHOT）

- **Play 模式分段实测**：UI 链路正常（反射广播事件 → HeadshotText 立即激活，游戏视图截图证实渲染正常）；断点在判定段——射线探针证实头部可判区域仅 y≈1.5~1.77 窄带
- **修复**：胶囊 h 1.5→1.4（center 0.7），头球 r 0.18→0.22 + 球心 local (0,0.08,0) 对准头骨（碰撞体随骨骼动画移动跟随头部）；复测 y=1.45/1.50/1.57/1.70 全部判"头"✅，HEADSHOT 截图验证通过

---

## 任务包 4E 完成记录（2026-09-09）

> 编译：0 错误 0 警告 | 场景已保存 | Play 模式链路验证通过 | 音效（4B）继续顺延（爆炸音效接口已留：OnExplosion 事件已广播，4B 时 AudioManager 直接订阅）

### 新增脚本（3 个）

| 文件 | 说明 |
|------|------|
| `Assets/Scripts/Combat/Grenade.cs` | 投掷物逻辑：5s 引信（OnEnable 重置，池化安全）→ Explode：OverlapSphere 半径 5m 范围伤害（敌我都炸，HashSet 按根组件去重——敌人头/身双碰撞体只结算一次），伤害按爆心到目标碰撞体最近点距离线性衰减（边缘系数 0.3）→ 广播 OnExplosion → ObjectPool.Release 回池 |
| `Assets/Scripts/Player/GrenadeThrower.cs` | 挂 Player：G 键（IsGrenadePressed）→ 扣数量广播 OnGrenadeCountChanged → Animator 触发 Toss → 0.9s 出手延迟（Inspector 可调）→ 初速 = 视线×10 + up×7，出手点胸口前移 0.5m 防碰自身胶囊；死亡中断投掷；AddGrenades() 留给阶段5 拾取物 |
| `Assets/Scripts/Player/CameraShaker.cs` | 挂 TPSCamera：Shake(amplitude) 取历史最大 → Update 每帧 Lerp 衰减写入 Cinemachine Perlin 的 AmplitudeGain（归零钳制防噪声常驻） |

### 修改脚本（4 个，均只增不改）

| 文件 | 内容 |
|------|------|
| `GameEvents.cs` | +`OnGrenadeCountChanged(int)` + RaiseGrenadeCountChanged |
| `PlayerInputHandler.cs` | +`_grenadeAction` + `IsGrenadePressed()`（与 Crouch/Heal 同模式） |
| `CombatEffectsController.cs` | +explosionFxPrefab / cameraShaker / shakeBaseAmplitude=0.8 / shakeRange=25；订阅 OnExplosion：池化生成 Explosion_Small_FX + 按主相机距离衰减震屏（>25m 不震） |
| `UIManager.cs` | +grenadeText 字段 + UpdateGrenadeDisplay；订阅 OnGrenadeCountChanged（OnEnable/OnDisable 配对），Start 主动拉取一次防错过初始广播 |

### 输入 / 动画 / 资产

| 类型 | 内容 |
|------|------|
| inputactions | +Grenade 动作（Button）+ `<Keyboard>/g` 绑定（BOM 保留，资产验证通过） |
| PlayerAnimator.controller | UpperBody 层 +Toss 状态（motion=toss grenade.fbx 的 mixamo.com 剪辑，3.2s Humanoid）+ AnyState→Toss（Toss 触发，0.1s，不可自转）+ Toss→Empty（exitTime 0.95）；磁盘重载验证（坑70） |
| `Assets/Prefabs/Grenade_Thrown.prefab` | 包内 SM_Wep_Grenade_01 复制改名：MeshCollider→SphereCollider(r=0.07)+GrenadeBounce 物理材质 + Rigidbody(mass 0.4/ContinuousDynamic/Interpolate) + Grenade(引信5/半径5/伤害100)；无资源包 prefab 关联 |
| `Assets/Materials/GrenadeBounce.physicMaterial` | bounciness 0.45（bounceCombine Maximum）+ 摩擦 0.6 |
| `Assets/Data/ExplosionShake.asset` | CM3 NoiseSettings：位置噪声 amp 0.25/freq 12 + 朝向噪声 amp 2.5°/freq 18（高频短促爆炸抖动） |

### 场景接线（SampleScene 已保存）

- Player +GrenadeThrower（grenadePrefab/aimCamera=TPSCamera/_inputHandler 已赋值）+ 暂停数组追加
- TPSCamera +CinemachineBasicMultiChannelPerlin（NoiseProfile=ExplosionShake，AmplitudeGain=0）+ CameraShaker
- CombatEffects：explosionFxPrefab=包内 Explosion_Small_FX / cameraShaker 已赋值
- Canvas +GrenadeText（右下角锚点复制 MedkitText，其上方 34px）+ UIManager.grenadeText 已赋值

### Play 验证结果（execute_code 实测）

| 项 | 结果 |
|----|------|
| G 键链路 | Grenade 动作+绑定在资产中；TryThrow 计数 3→2、UI 同步 ✅ |
| 投掷物理 | 受控投掷初速 (0,7,10) 正常飞行，无穿透报错 ✅ |
| 引信 | 投出后真实间隔 >5s 时已爆炸回池（多次一致）✅ |
| 敌人伤害 | 敌人脚下爆炸 200→100（贴身满伤 100）✅ |
| 炸自己 | 玩家 4m 处 100→51（衰减公式精确吻合：胶囊最近点 3.5m→falloff 0.3→51）✅ |
| 距离衰减 | 30.8m 外爆炸玩家无伤、不震屏（>25m 范围外，正确）✅ |
| 爆炸特效 | Explosion_Small_FX 池化实例激活播放 ✅ |
| 震屏闭环 | Shake→_amplitude→次帧 Perlin.AmplitudeGain=0.242（衰减中），相机被噪声驱动 ✅ |
| 对象池 | Prewarm 2 + 投掷 1 回收后共 3 循环复用 ✅ |

### 已知坑（追加）

76. **给预制体 Collider 赋物理材质必须用 `sharedMaterial`**：`collider.material` 属性是实例化引用（返回场景实例副本），LoadPrefabContents 里赋值后 SaveAsPrefabAsset 会丢失（验证 mat=NULL）；`sharedMaterial` 直接引资产才落盘
77. **CM3 命名空间是 `Unity.Cinemachine` 而非旧版 `Cinemachine`**，且 CM3 无 `InvalidateComponentPipeline`（组件管线自动重建）；`Rigidbody.linearDamping/angularDamping` 是 Unity 6 字段名，2022.3 仍是 `drag/angularDrag`
78. **execute_code 两次调用间的真实间隔远超体感**（模型回复耗时），Play 时序类验证（引信 5s、动画播完回落）不能以"调用间隔 2s"为准——手雷"2 秒后消失"实为间隔已 >5s 的正常爆炸，池回收计数是判定爆炸发生的可靠证据

### 4E 验收清单（待用户 Play 验证）

- [ ] G 键投掷：角色播 toss 动画（上身），0.9s 后手雷出手（出手时机可在 GrenadeThrower.throwDelay 调）
- [ ] 手雷抛物线飞行、落地弹跳 1~2 次（bounciness 0.45）
- [ ] 5s 后爆炸：爆炸特效 + 近处震屏 + 敌人/玩家均掉血
- [ ] 右下角 HUD「手雷: N」随投掷递减，0 枚时按 G 无效
- [ ] 掩体后扔雷可隔墙炸到敌人（OverlapSphere 无视线检测，属设计预期）

### 4E 热修复（2026-09-10 用户反馈：惯性大弹太远 / 投掷方向不明确）

| 问题 | 根因 | 修复 |
|------|------|------|
| 轻微碰撞弹很远（碰敌人也弹飞） | 物理材质 bounciness 0.45 且 bounceCombine=**Maximum**（恒取两材质最大值，敌方默认材质 0 也不起作用）；无空气阻力；敌人碰撞无特殊处理 | 弹性 0.45→0.3、组合改 **Average**；Grenade.OnCollisionEnter 碰 EnemyBase 时速度 ×0.15、角速 ×0.3（雷落敌人脚边） |
| 落点方向不可读 | 初速取相机 forward（越肩视角与准星视线有夹角）；高抛弧线（up 7/fwd 10）落点难预判；小球无视觉追踪特征 | **瞄点投掷**：从准星（Camera.main 屏幕中心，与 CrosshairController 同源）发射线命中点为投掷目标，所见即所投；弧线改平直（fwd 12/up 5）；预制体加 TrailRenderer（0.35s 淡色拖尾，`FX_GrenadeTrail.mat` URP/Unlit）+ 投掷赋予随机自转（spinSpeed 12），肉眼可追踪 |

**验证**（Play 实测）：瞄点初速与期望值逐位一致（dot>0.99）；拖尾/自转生效；10 m/s 直冲敌人碰撞后剩 0.29 m/s 落于脚边。

**新注意点**：Grenade.OnEnable 会用 fuseTime 字段重置引信——运行时测试改引信必须同时改字段和 `_fuseTimer`（反射），只改字段会被下一次 Get 时的 OnEnable 覆盖。

### 4E 热修复 2（2026-09-10 用户反馈：落地后滑行很远，摩擦/Drag 调参无效）

| 问题 | 根因 | 修复 |
|------|------|------|
| 落地后滚很远，PhysicMaterial 摩擦调到 0.9 + Rigidbody Drag 0.3~0.5 均无效 | **球体滚动不受摩擦衰减**：摩擦只对接触点有相对滑动的"滑动"做功，球落地后很快转为纯滚动（接触点无相对滑动），Unity 也不模拟滚动阻力；Drag 在低速段衰减太慢 | Grenade.cs 加**滚动钳停**：碰撞接触面法线 y>0.5 视为接地（OnCollisionEnter/Stay 置位、FixedUpdate 消费后重置），接地且速度 < groundStopSpeed（默认 1.5，Inspector 可调）→ 速度/角速直接归零 |

**验证**（Play 实测）：4 m/s 贴地水平投出，滑行 4.71m 后速度精确归零停死（旧版会持续滚动至引信耗尽）。

**认知更正**：坑 79 的"物理材质"修正补充——摩擦参数对球体类投掷物的滚动阶段基本无效，调参只影响滑动段；"停得稳"必须靠速度钳停类逻辑。滑行距离感仍偏长时调大 groundStopSpeed（如 2.5）即可缩短。

### 4E 顺手修复（2026-09-10 用户反馈：敌人站桩开枪时模型不面向玩家）

| 问题 | 根因 | 修复 |
|------|------|------|
| 敌人朝玩家开枪时身体不面向玩家（子弹/曳光却正确飞向玩家） | `AttackPlayer()` 用 `isStopped` 停住 NavMeshAgent 后**无任何转向逻辑**——NavMeshAgent 只在沿路径移动时自动转向，站桩后模型朝向冻结在停下的瞬间；而 Raycast 弹道是纯数学（胸口→玩家胸口），与模型面朝无关 | AttackPlayer 每帧水平转向玩家（`Quaternion.RotateTowards`，新字段 `attackTurnSpeed=360°/s`），同时 `agent.updateRotation=false` 接管转向；Patrol/ChasePlayer 恢复 `updateRotation=true` 归还转向权 |

**验证**（Play 实测）：玩家传送至敌人后方 8m 攻击圈内，1 秒内敌人 forward 与"指向玩家"夹角 0.0°、updateRotation=False（接管中）；截图肉眼确认模型正面朝玩家举枪射击。

**注意**：执行_code 里测 `head.position - hips.position` 的 XZ 方向判断视觉朝向不可靠（Polygon 骨骼轴向非标准 + 动画姿态噪声，实测读出 -159° 假象），视觉朝向验证以截图为准。

---

## 会话 5 记录（2026-09-10~09-29：任务包 4E 手雷全程 + 手感热修复 + 敌人攻击转向修复）

> 本段记录 f87ca2d 提交前后的全部工作与结论，供下一轮对话/新 Agent 直接续接。

### 一、4E 交付内容（已提交 f87ca2d，详录见上文任务包 4E 完成记录）

- 新脚本：`Grenade.cs`（引信/范围伤害/池化）、`GrenadeThrower.cs`（G键/瞄点投掷）、`CameraShaker.cs`（震屏）
- 修改：GameEvents（+OnGrenadeCountChanged/复用 OnExplosion）、PlayerInputHandler（+G键）、CombatEffectsController（+爆炸特效/震屏接线）、UIManager（+手雷计数）、inputactions（+Grenade）、PlayerAnimator（UpperBody 层 +Toss 状态）
- 资产：Grenade_Thrown.prefab、GrenadeBounce.physicMaterial、FX_GrenadeTrail.mat、ExplosionShake.asset（CM3 噪声档）
- 新增坑 76~78（sharedMaterial 落盘 / CM3 命名空间 / execute_code 时序）

### 二、f87ca2d 之后的后续修复（**未提交 git**，见下方清单）

| 项 | 内容 | 文件 |
|----|------|------|
| 手雷滑行热修复 | 球体滚动不受摩擦衰减（调摩擦/Drag 无效），Grenade 加**滚动钳停**：接地（接触法线 y>0.5）且速度 < `groundStopSpeed`（默认1.5，预制体 Inspector 可调）→ 速度归零；实测 4m/s 投出滑行 4.7m 停死 | `Grenade.cs` |
| 手雷手感（f87ca2d 已含部分）+ 用户手调 | 用户在 Inspector 手调过 GrenadeBounce 摩擦与预制体 Rigidbody Drag，最终数值以 Inspector 为准；预制体含拖尾 | `Grenade_Thrown.prefab` |
| 敌人攻击转向修复 | 攻击站桩时 NavMeshAgent 不转向（只在移动时转）→ 模型背对玩家开枪；修复：AttackPlayer 每帧 RotateTowards 玩家（`attackTurnSpeed=360°/s`）+ 临时 updateRotation=false，巡逻/追击归还；实测夹角 0.0° | `EnemyBase.cs` |
| 资源包材质自动迁移 | Unity 加载爆炸 FX 时自动同步 URP 字段（无害单向，还原后还会复发，建议下次提交时一并入库） | `PolygonBattleRoyale_FX_Dirt.mat` |

### 三、待开发内容（按计划 §5.6~§5.8 顺序）

| 任务包 | 内容 | 状态 |
|--------|------|------|
| **4B 音效** | generate_audio 生成全套音效；AudioManager Clip 字段已全部留空，事件钩子（射击/换弹/脚步/受击/爆炸 OnExplosion/UI）4A 起就绪，配置 fal.ai Key 后可随时插队 | ⏸ 挂起 |
| **4F 武器手感** | WeaponSway + HeadBob，参照 `K:\Unity\UnityProject\TPS Shooter\Scripts\Entities\Player\Weapon\` 下 WeaponSway.cs / HeadBob.cs 抄参数量级 | ⬜ |
| **阶段5 流程闭环** | GameManager 状态机（接管暂停）/ GameOverPanel 结算（击杀数/波次/存活时间）/ EnemyWaveGenerator + WaveConfig SO / 击杀统计 HUD | ⬜ |
| **阶段6 AI 深度** | FOV+视线+噪音三重感知 / Search 记忆 / 侧移射击 / 敌人血条 / 受击方向指示 / 掩体切角 / UI 美化 / 性能收官（实施前需 architect 详设） | ⬜ |

### 四、下一阶段建议（给用户与新 Agent）

1. **推进顺序**：4F（小包）→ 4B（Key 配好即插队）→ 阶段5 → 阶段6。4F 与 4B 互不依赖，顺序可换。
2. **⚠️ 阶段5 前置依赖（易被忽略）**：场景中的 8 个敌人是**无 prefab 关联的散装实例**（敌人 prefab 在阶段3.5 回滚时已删除）——波次生成器刷怪必须先重建 Enemy_Soldier/Scout/Heavy 预制体（含 EnemyBase/NavMeshAgent/CapsuleCollider/差异化数值/模型），这是阶段5 的第一个实际任务。
3. **阶段5 开工前需用户拍板的 3 个决策**（计划 §10.2 标注）：① 最后一波后无限循环波还是胜利画面；② 现有 8 个固定敌人是编入首波还是保留为巡逻守卫+波次另算；③ 手雷拾取物是否随阶段5 一并做。
4. **4F 实现要点**：武器挂在 hand_R 骨骼下（非直接挂根），Sway/HeadBob 改的是挂点局部偏移，与 PlayerADSController 的 adsPositionOffset 叠加时注意两者都改 WeaponHolder local，需明确叠加顺序防互相覆盖；ADS 时 Sway 幅度应收窄。
5. **工作流**：新 Agent 开工前读本文件「项目概况→会话5」+ DEVELOPMENT_PLAN.md 即可上手；用户说「按流程来」＝方案先行+确认+独立审查，说「直接修」＝自主完成+记 DEVLOG（会话 4 已与用户确认此约定）。
6. **工程纪律**：每个任务包完成立即 git 提交（坑 48 教训）；本次 5 个未提交文件建议新对话开工前先提交。

---

## 任务包 4F 完成记录（2026-09-30）

> 编译：0 错误 0 警告 | 场景已保存 | Play 静止状态验证通过（零漂移/零报错）| 音效（4B）继续顺延

### 新增脚本（2 个）

| 文件 | 说明 |
|------|------|
| `Assets/Scripts/Player/WeaponSway.cs` | 挂 WeaponHolder：按鼠标视角增量对挂点做附加偏移（位置 + 小幅旋转，SmoothDamp 回位，参照 TPS Shooter 参数量级）；**ADS 时幅度按进度收窄**（adsSwayMultiplier=0.3）；玩家死亡时目标归零 |
| `Assets/Scripts/Player/HeadBob.cs` | 挂 CameraTarget：移动时正弦起伏（垂直 2 倍频弹跳 + 半频左右重心横摆），幅度随实际速度缩放；蹲伏 ×0.5、ADS 渐变至 ×0.4；静止/空中/死亡 SmoothDamp 归位 |

### 修改脚本（2 个，均只增不改行为）

| 文件 | 内容 |
|------|------|
| `PlayerController.cs` | +`HorizontalSpeed` / `IsGrounded` 两个只读属性（HeadBob 等表现层读取，替代直接访问私有字段） |
| `PlayerADSController.cs` | **修挂点引用**：+`[SerializeField] weaponHolder` 序列化引用（F4 同模式），兜底改递归查找——原 `transform.Find("WeaponHolder")` 只查直接子级，而 WeaponHolder 实际挂在 hand_R 骨骼下，**ADS 贴枪偏移自会话 3 武器挂到手骨起就静默失效** |

### 叠加架构（核心设计）

```
每帧写入顺序（脚本执行顺序保证）：
1. Update  PlayerADSController 全权重写 WeaponHolder.localPosition/localRotation
           = Lerp(默认姿势, 默认姿势+adsOffset, ADS进度)   ←「贴枪基准」
2. LateUpdate WeaponSway 先减去上一帧自己写的附加量还原基准，
           再叠加平滑后的新附加量（反向于视角旋转，钳制上限） ←「附加偏移」
CameraTarget：PlayerController 只写 localRotation（俯仰视角），
              HeadBob 只写 localPosition（起伏）→ 各占一个通道，与 Cinemachine 不打架
```

- HeadBob 选址结论：**放 CameraTarget 不放 WeaponHolder**——WeaponHolder 在手骨下，bob 会让枪相对手浮动穿帮；CameraTarget 是 Cinemachine 跟随目标，bob 即视角自然起伏，且其 localPosition 无人占用
- Sway 偏移在**玩家坐标系**计算（右/上轴）再经 `parent.worldToLocalMatrix` 转入手骨局部空间写入——手骨朝向任意（localRot 307/100/20），直接写局部轴会沿奇怪方向摇

### 场景接线（SampleScene 已保存）

- WeaponHolder +WeaponSway、CameraTarget +HeadBob（参数均为脚本默认值，Inspector 可调）
- PlayerADSController.weaponHolder ← WeaponHolder（实例赋值）；**adsPositionOffset 归零**——旧值 (-0.12,0.45,0.15) 是按阶段 2 的"Player 直接子级挂点"调的，坐标基准已随挂到手骨而失效，若保留会在修复引用后突然生效把枪甩飞；ADS 贴枪偏移如需要请按手骨局部坐标重新手调
- PauseMenuController.disableOnPause 追加两个新组件（共 8 个）

### Play 验证结果（execute_code 实测）

| 项 | 结果 |
|----|------|
| 组件挂载/启用 | WeaponSway、HeadBob 均挂载且 enabled ✅ |
| ADS 挂点引用 | weaponHolder 序列化引用生效，无"未找到"警告 ✅ |
| 静止漂移 | 两次跨帧读数 WeaponHolder/CameraTarget 局部值逐位一致，附加量无累积 ✅ |
| 控制台 | 0 错误 0 警告 ✅ |
| 截图 | 游戏画面/HUD 正常，静止姿态无位移异常 ✅ |
| 跑/走/蹲/ADS 手感 | ⏳ 待用户实际跑图验收（输入无法程序模拟，坑 28） |

### 已知坑（追加）

80. **`transform.Find` 只查直接子级**：WeaponHolder 挂到手骨（hand_R）后，PlayerADSController 的直接子级查找静默返回 null，整段 ADS 贴枪代码失效且无任何报错——跨层级找挂点用序列化引用 + `GetComponentsInChildren` 兜底，并加空值警告
81. **execute_code 的 CodeDom 编译域持有旧程序集快照**：新编译的脚本类型在 execute_code 里不可直接引用（typeof 同程序集旧类型可用），但**运行时多态可用**——`GetComponents<MonoBehaviour>()` 后按 `GetType().Name` 匹配即可操作新组件；manage_components 在编辑器进程内执行不受此限（需先 refresh_unity 真正完成编译）

### 4F 验收清单（待用户 Play 验证）

- [ ] 原地转视角：武器小幅落后于视角并平滑回位（甩头越快偏移越大，有上限）
- [ ] 跑/走：视角轻微上下起伏 + 左右摆，跑比走明显
- [ ] 蹲伏/ADS：起伏明显变小；ADS 中转视角武器晃动收窄
- [ ] 静止站立：武器与视角完全归位，无残余晃动
- [ ] 手感数值不满意时调 Inspector：WeaponSway（positionAmount/maxPositionOffset）、HeadBob（bobbingSpeed/bobbingAmount）均在挂点组件上

---

## 阶段 5 实施记录（2026-09-30，按 PHASE5_PLAN.md S1→S6 逐步）

### S1：敌人预制体重建（完成）

> 编译 0 错误 0 警告 | 场景未动（8 散装敌人保留供回归）| Play 临时实例化验证通过

**产出**：`Assets/Prefabs/Enemy_Soldier.prefab` / `Enemy_Scout.prefab` / `Enemy_Heavy.prefab`（从场景实例 `PrefabUtility.SaveAsPrefabAsset` 标准路径，未手拼；场景实例保持散装、未建立关联）

**迁移数值实录**（以场景实例为准，逐项核对）：

| 变体 | 血量 | 巡逻速度 | 追击速度 | 伤害 | 冷却 | 爆头倍率 | 代理人参数 |
|------|------|---------|---------|------|------|---------|-----------|
| Soldier | 100 | 3 | 5.5 | 3 | 1.0 | ×2 | 全变体一致：spd3.5/ang120/accel8/stop0.5（EnemyBase 按状态动态改 agent.speed） |
| Scout | 60 | 4 | 6.5 | 2 | 0.8 | ×2 | 同上 |
| Heavy | 200 | 2 | 3.5 | 6 | 1.5 | ×2 | 同上 |

- 公共：accuracy 0.7 / detectRange 20 / attackRange 15 / corpseDuration 10 / attackTurnSpeed 360 / 胶囊 h=2 c=1 r=0.4 / Animator=EnemyAnimator + CharactersAvatar / 模型 MilitaryMale_01・SportyMale_01・MercenaryMale_01
- **patrolPoints 场景引用自动断链置空（数组长度保留、元素全 null）= 预期行为**；prefab 内 0 missing 引用
- 根胶囊保持 h=2.0（运行时 SetupHeadPart 收缩），符合坑 75 逻辑不动

**Play 验证**：三预制体临时实例化 → EnemyBase/NavMeshAgent/胶囊齐全、落 NavMesh（y=0.17）、无路点原地待命 0 报错、SetupHeadPart 自动收缩胶囊至 1.4 + 生成头球 r=0.22（isHead=true）✅；验证后测试实例已销毁

### S2：GameManager 骨架 + PauseMenu 职责迁移（完成）

> 编译 0 错误 0 警告 | Play 链路验证通过 | 截图：S2_pause_panel_migrated.png

**新增脚本**：`Assets/Scripts/Data/GameManager.cs`——状态机 `Playing ⇄ Paused`（GameOver/Victory 留待 S3/S5 接入）；`Pause()/Resume()` 接管 timeScale+disableOnPause（8 组件）+鼠标锁；`Restart()`=重载 SampleScene、`ReturnToMenu()`=加载 MainMenu；`SurvivalTime` 仅 Playing 时累计（timeScale=0 天然不计时）；`KillCount/CurrentWave` 字段就位（S4/S6 接入）

**修改脚本**：`PauseMenuController.cs`——删 `disableOnPause` 字段/timeScale 管理/LoadScene 三项职责（全部改调 GameManager.Instance）；Tab 检测新增 GameOver/Victory 状态屏蔽（本次预留，S3 起生效）；面板切换、设置控件绑定、滑块填充逻辑保留不动

**场景接线**：新增根对象 `GameManager`（GameManager 组件）；disableOnPause 数组 8 组件从 PauseMenuController 迁移赋值（先迁移后删字段，无数据丢失）；场景已保存

**Play 验证**（按钮 onClick / 反射调私有方法，Tab 键输入本身无法模拟但该段代码未变）：

| 项 | 结果 |
|----|------|
| Pause | State=Paused、timeScale=0、鼠标解锁、面板显示、8/8 脚本禁用 ✅ |
| Resume | State=Playing、timeScale=1、鼠标锁定、面板关闭、8/8 脚本恢复 ✅ |
| 设置开/关 | settings 面板 ↔ pause 面板切换正常（与迁移前等价）✅ |
| 回主菜单 | mainMenuButton.onClick → 场景切至 MainMenu ✅ |
| 控制台 | 0 错误 0 警告 ✅ |

### S3：死亡流程 + GameOverPanel 阵亡态（完成）

> 编译 0 错误 0 警告 | Play 全链路验证通过 | 截图：S3_gameover_panel_clean.png

**新增脚本**：`Assets/Scripts/UI/GameOverPanelController.cs`——订阅 OnGameOver/OnGameVictory（OnEnable/OnDisable 配对退订）；标题 YOU DIED 红 / VICTORY 绿、数据三行（击杀/波次 N/Max/时间 mm:ss）从 GameManager 拉取；重玩/回主菜单按钮调 GameManager 并广播 RaiseUIClick（4B 预留）；DOTween 序列存 `_showSeq` 字段、触发前 Kill（4D 教训）

**修改脚本**：
- `GameEvents.cs`：+`OnGameOver` / `OnGameVictory` / `OnKillCountChanged(int)` 三事件 + Raise（只增不改）
- `PlayerHealth.cs`：死亡分支补一行 `GameEvents.RaisePlayerDeath()`（实例事件 OnDeath 保留）
- `GameManager.cs`：订阅 OnPlayerDeath（OnEnable/OnDisable 配对）→ State=GameOver + 禁用 8 组件 + 解锁鼠标（**不置 timeScale=0**）→ 延迟 gameOverDelay(1.5s) 广播 RaiseGameOver；+MaxWave/SetCurrentWave/SetMaxWave（S4 波次生成器用）；死亡后重复 TakeDamage 由 State!=Playing 守卫兜底
- `PauseMenuController.cs`：Pause/Resume 入口补状态守卫（热修：GameOver 态直调 Pause 曾出现暂停面板叠加在结算面板上，输入路径本有 Update 屏蔽、守卫为防御性双保险）

**新增资产/场景**：`Assets/Textures/GameOverBackdrop.png`（程序化 64×64 暗底，RedVignette 同模式）；Canvas 下新建 `GameOverPanel`（暗底 Image + CanvasGroup，默认隐藏）+ TitleText + StatsText + RestartButton/MenuButton（复用 BtnNormal 样式 + LegacyRuntime 字体）；GameOverPanelController 挂 Canvas，引用已接线；场景已保存

**Play 验证**（execute_code 直调 TakeDamage(9999)，坑 28）：

| 项 | 结果 |
|----|------|
| 死亡即时 | State=GameOver、hp=0、8/8 脚本禁用、鼠标解锁、timeScale 保持 1 ✅ |
| 1.5s 后 | 结算面板淡入（alpha 1.00）、YOU DIED 红标题、击杀 0/波次 0/时间正确 ✅ |
| Tab 屏蔽 | GameOver 态调 Pause：状态守卫拒绝，暂停面板不出现 ✅ |
| 重玩 | onClick → 场景重载 → State=Playing、hp=100、手雷=3、KillCount=0、面板隐藏、鼠标锁定 ✅ |
| 控制台 | 0 错误 0 警告 ✅ |

### S4：波次系统核心 + 首波上线（完成）

> 编译 0 错误 0 警告 | Play 全链路验证通过 | 截图：S4_wave1_hunting.png

**新增脚本**：
- `Assets/Scripts/Data/WaveConfig.cs`——SO 混兵种条目数组（§3 修订版：entries[{prefab,count}] + spawnInterval/startDelay）
- `Assets/Scripts/Enemy/EnemyWaveGenerator.cs`——挂 WaveSystem 根对象；状态机 Preparing(10s)→Spawning(1.5s 逐刷)→Fighting(List<EnemyBase> 登记+IsDead 轮询等全灭)→Intermission(startDelay 倒计时)→下一波；末波清空 RaiseGameVictory（S5 接 UI）；出生点=距玩家>20m 随机/无则最远 + NavMesh.SamplePosition 2m 容差（不重烘焙）；刷出即置 `huntPlayer=true`（坑 67 安全：新字段无 Start 重置）；GameOver/Victory 态不推进波次（实测意外验证：玩家死亡后生成器正确停机）；每波开始 AddGrenades(1)（D7，钳制已在 GrenadeThrower.maxGrenades=3）

**修改脚本**：`EnemyBase.cs` 只增三处——①`public bool IsDead => _isDead`（存活判定专用，尸体 10s 销毁期不算存活）②`public bool huntPlayer` 字段 ③Update 分支 `huntPlayer` 时 attackRange 外直追（无视 detectRange）

**新增资产/场景**：`Assets/Data/WaveConfig.asset`（S4 先填 Wave1=士兵4/侦察2/重装2、Wave2=士兵4/侦察4，S5 补满）；场景新增 `WaveSystem` 根对象（WaveGenerator + SpawnPoints/6 个临时出生点，全部 SamplePosition 命中现有 NavMesh，y=0.04~0.25）；waveConfig/spawnPoints/grenadeThrower(Player) 已接线；**Enemies 与 PatrolPoints 分组 SetActive(false) 停用保留**（D6，全链路验证通过后由用户决定删除）；场景已保存

**Play 验证**：

| 项 | 结果 |
|----|------|
| 开场准备 | Preparing 10s 倒计时 → 自动开波 ✅ |
| Wave1 | 8/8 刷出（1.5s 间隔）、huntPlayer 8/8、从远处出生点逼近至 14.2m（截图多方向围攻）✅ |
| 波清空 | 全灭 → Fighting 清零 → RaiseWaveCleared → Intermission ✅ |
| Wave2 | 5s 后 8/8 刷出、huntPlayer 8/8、CurrentWave=2 ✅ |
| 结算停机 | 玩家死亡 → State=GameOver → 生成器停止刷怪（守卫生效）✅ |
| 控制台 | 0 错误 0 警告 ✅ |

**说明**：KillCount 计数按计划留 S6（击杀 HUD 一起接）；波间倒计时 UI/WaveBanner 留 S5。

### S5：胜利流程 + WaveBanner + 5 波表 + 出生点完善（完成）

> 编译 0 错误 0 警告 | Play 加速验证通过 | 截图：S5_victory_panel.png

**新增脚本**：`Assets/Scripts/UI/WaveBannerController.cs`——Canvas 中央大字横幅：波开始 WAVE N / 波清空 WAVE N CLEARED（缩放弹出停 2s 淡出，DOTween 序列存 `_bannerSeq` 字段、触发前 Kill）；波间 Intermission 显示「下一波 Xs」每秒刷新（大字让位后出现）；订阅 OnWaveStarted/OnWaveCleared（OnEnable/OnDisable 配对）

**修改脚本**：
- `GameManager.cs`：+`TriggerVictory()`（§4.1 接口）——State=Victory + 禁操控 + 解锁鼠标（不置 timeScale=0）→ 延迟 victoryDelay(1.0s) 广播 RaiseGameVictory 弹结算；HandlePlayerDeath 更名公开为 `TriggerGameOver()`（§6.4 接口名，订阅入口不变）
- `EnemyWaveGenerator.cs`：末波清空改调 `GameManager.Instance.TriggerVictory()`（无 Instance 时兜底直接广播），不再立即 RaiseGameVictory——保证「延迟 1.0s 弹结算」语义且面板不提前显示

**新增资产/场景**：WaveConfig.asset 波次表填满 5 波（8/8/10/12/14，§3 表）；出生点补至 **10 个**（新增 4 个全部 SamplePosition 命中 NavMesh）；Canvas 新增 `WaveBannerText` + WaveBannerController 挂 Canvas（bannerText/waveGenerator 已接线）；场景已保存

**Play 验证**（加速法：内存临时改波次表为 2 波×1 侦察，见新坑 82）：

| 项 | 结果 |
|----|------|
| 横幅 | WAVE 1 / WAVE 2 弹出并按时淡出 ✅ |
| 波间倒计时 | Intermission 期间显示（被击杀间隔截断未截图，机制经 PhaseName/CountdownRemaining 轮询验证）✅ |
| 胜利链路 | 末波清空 → State=Victory + 8 组件禁用 + 鼠标解锁 → 1s 后 VICTORY 绿标题面板（波次 2/2、时间正确）✅ |
| 暂停面板 | Victory 态不出现 ✅ |
| 控制台 | 0 错误 0 警告 ✅ |

**已知坑（追加）**

82. **Play 模式下用 SerializedObject 修改 SO 资产会持久化落盘**（ApplyModifiedPropertiesWithoutUndo + 退出 Play 时的资产序列化即写盘，无需 SetDirty/SaveAssets）——「内存临时改数据加速测试」对 SO 资产不成立；测试后必须显式还原 + SaveAssets 并复核磁盘值。本次胜利链路测试把 WaveConfig 临时改成 2 波×1，测后已按 §3 表还原并逐波复核（8/8/10/12/14）

### S6：击杀 HUD + GrenadePickup + 每波补雷（完成）

> 编译 0 错误 0 警告 | Play 全链路验证通过 | 截图：S6_kill_hud.png

**修改脚本**：
- `GameManager.cs`：订阅 OnEnemyKilled（OnEnable/OnDisable 配对）→ KillCount++ → 广播 RaiseKillCountChanged
- `UIManager.cs`：+killText 字段 + UpdateKillDisplay，订阅 OnKillCountChanged（OnEnable/OnDisable 配对 + Start 主动拉初始值，4E 手雷计数同模式）

**新增脚本**：`Assets/Scripts/Combat/GrenadePickup.cs`——复制 HealthPickup 模式：Trigger + Player tag 过滤 → AddGrenades(1)（上限 3 钳制在 GrenadeThrower，核查已有无需改）→ RaisePickupTaken（4B 预留）→ Destroy 自身；Awake 自动补 Trigger Collider

**新增资产/场景**：`Assets/Prefabs/GrenadePickup.prefab`（包内 SM_Wep_Grenade_01 + SphereCollider r=2 trigger）；Pickups 组摆 3 处（Dock -45.5,18.3 / Village 1,28.8 / South 20,-40.2，全部 NavMesh 命中）；Canvas 右上角 `KillText`（与血条左上/弹药手雷右下无冲突）+ UIManager.killText 已接线；每波补雷（restockGrenadePerWave=true）S4 起已接线；场景已保存

**Play 验证**：

| 项 | 结果 |
|----|------|
| 击杀 HUD | 击杀测试敌兵 → KillCount 0→1、右上角 HUD 实时显示「击杀: 1」✅ |
| 拾取 +1 | 手雷 1 → 传送到拾取物 → 计数 +1、拾取物销毁 ✅ |
| 上限钳制 | 满仓 3 捡拾取物 → 保持 3、拾取物正常消耗 ✅ |
| 每波补雷 | 波次开始自动 AddGrenades(1)（测试中 1→2 被观测到）✅ |
| 控制台 | 0 错误 0 警告 ✅ |

---

## 阶段 5 收尾状态

> S1~S6 全部完成（2026-10-01）| 编译 0 错误 0 警告 | 各步 Play 自测通过
> **explore 独立审查（2026-10-01，只读子 Agent）：通过**——10 项全过（坑67 huntPlayer 直赋安全/存活判定 List+IsDead/GameEvents 只增不改+订阅配对/Find 零新增/敌人不入池/死亡胜利流程守卫/DOTween 序列管理/只增不改 git diff 核对/逻辑缺陷扫描/场景资产一致性含 WaveConfig 磁盘 5 波复核）；无 P0/P1；2 条 P2 建议留阶段6：① SpawnNext 对缺 EnemyBase 组件的预制体无兜底（当前预制体均含）② WaveBanner 倒计时 0.1s 淡入 tween 未跟踪 Kill（影响可忽略）
> **待办**：用户 Play 终验（§8 清单：5 波全流程/击杀 HUD/死亡与胜利结算/回归项）→ 确认后 git 提交。

### 阶段 5 补充：来袭倒计时 UI（2026-10-01 用户反馈）

> 编译 0 错误 0 警告 | Play 验证通过 | 截图：S5b_countdown_ui.png

- **问题**：开场到 Wave 1 显示前（Preparing 10s）没有任何倒计时 UI；原 WaveBanner 只在波间（Intermission）显示纯文字「下一波 Xs」且无动画
- **修改**：`WaveBannerController.cs` 重写倒计时逻辑——Preparing 与 Intermission 统一显示「第 N 波来袭 Xs」（同一中央上方位置），**DOTween 弹入（OutBack 缩放+淡入）+ 每秒 DOPunchScale 跳动**；倒计时结束/波开始时快速淡出，WAVE N 横幅照常弹出接管
- **tween 纪律**（4D 教训落实）：_bannerSeq/_countdownSeq/_pulseTween/_fadeOutTween 全部存字段引用，触发前 Kill 旧 tween；OnDisable 统一 KillAllTweens；横幅激活期间倒计时让位，横幅弹出时倒计时 tween 全部让位且 alpha 置 1 防残留半透明
- **波号显示**：Preparing 期间=首波号（CurrentWaveNumber），Intermission=当前波+1
- **验证**：Preparing 显示「第 1 波来袭 7s」（scale 1.02 脉冲中）→ 开波后倒计时消失、WAVE 1 横幅正常弹出淡出 → Fighting 无残留；控制台 0 错 0 警

---

## 相机避障记录（2026-10-01 用户反馈：镜头穿墙）

> 编译 0 错误 0 警告 | 场景已保存 | 无脚本改动，纯场景组件配置

**改动**：TPSCamera 新增 `CinemachineDeoccluder`（CM3 组件，Unity.Cinemachine 命名空间），参数：

| 参数 | 值 | 说明 |
|------|-----|------|
| CollideAgainst | Default 层 | 建筑/地形碰撞都在 Default |
| IgnoreTag | Player | 忽略玩家自身胶囊防误拉近 |
| AvoidObstacles.Enabled / Strategy | true / PullCameraForward | 遮挡时沿视线拉近，保持构图 |
| CameraRadius | 0.25 | 相机体半径，防镜头边缘穿墙 |
| DampingWhenOccluded / Damping | 0 / 0.5 | 被挡瞬间拉近（商业手感），脱离后 0.5s 平滑退回 |
| MinimumDistanceFromTarget | 0.5 | 离角色最近不低于 0.5m |

**Play 验证**：玩家背贴 SM_Bld_House_01 东墙 → 相机 5m→2.17m 停在墙前约 0.5m 不穿透 ✅；传送回开阔地 → 恢复 5.39m（与改造前基准一致）✅。

**注意**：`GetCameraDisplacementDistance` 反射调用需传参（CodeDom 崩），验证遮挡直接读「主相机到玩家距离 + 中间射线」即可。CameraDistance 仍为 5（越肩拉近调整用户未应用，待定）。

---

## 阶段 6 开工 + S1 状态机重构记录（2026-10-25）

> 方案基准：[PHASE6_PLAN.md](PHASE6_PLAN.md)（用户已确认：D8 波次敌人接入感知 / D9 状态机重构 / 4B 继续挂起 / D12 掩体切角降为可选）
> 编译 0 错误 0 警告 | Play 回归通过 | 无场景改动（纯脚本）

### S1 变更内容（行为等价迁移，不加新功能）

| 文件 | 内容 |
|------|------|
| `EnemyBase.cs` | **状态机重构**（D9）：enum `EnemyState { Patrol, Hunt, Chase, Attack, Dead }` + switch 主循环，状态切换同帧执行新行为（帧级等价旧 if-else）；**删除 `huntPlayer` 字段**（D8 语义迁移），新增 `SetInitialHuntTarget(Vector3)`（运行时字段无 Start 重置，坑 67 约束保持）+ `State` 只读属性（调试观测）；Patrol/Hunt/Chase/Attack/Die 方法体原样保留 |
| `EnemyWaveGenerator.cs` | `huntPlayer=true` → `SetInitialHuntTarget(玩家出生时刻位置)`（S1 过渡语义下行为等价）；**P2① 修复**：SpawnNext 对缺 EnemyBase 的预制体 LogError 指名并跳过（原先静默吞掉） |
| `WaveBannerController.cs` | **P2② 复核未复现**：四个 tween（_bannerSeq/_countdownSeq/_pulseTween/_fadeOutTween）当前代码全部已存字段引用并在触发前 Kill——explore 所述「0.1s 淡入未纳管」在现版本不存在（淡入 0.2s 且在 _countdownSeq 序列内），判定为基于旧版本结论，不做改动 |

### Play 回归验证（execute_code，坑 81 反射访问新类型）

| 回归项 | 结果 |
|--------|------|
| 散装守卫巡逻 | Play 中临时启用 Enemies 组 → 8/8 状态 Patrol（玩家在远处）✅ |
| 波次围攻链 | Wave1 刷出 8 敌 → 6 个 Hunt→追至玩家身边转 Attack、2 个重装（慢速）保持 Hunt 路上 ✅ |
| 攻击输出 | 玩家被围攻至血量 0（真实弹道伤害生效，意外完成死亡结算链路二次验证）✅ |
| 死亡链 | TakeDamage(9999) → 返回 true、State=Dead、IsDead=true ✅ |
| huntPlayer 残留 | 全局 grep 仅剩 DEVLOG 历史文档记载（代码零残留）；场景 YAML 旧序列化值由 Unity 下次保存自动清理 ✅ |
| 控制台 | 0 错误 0 警告 ✅ |

**注意**：`GameObject.Find` 找不到停用的 Enemies 组（坑 68 复现）——Play 验证停用分组用 `SceneManager.GetActiveScene().GetRootGameObjects()` 遍历。

---

## 阶段 6 S2 感知系统记录（2026-10-25）

> 编译 0 错误 0 警告 | Play 验证通过 | 无场景改动（prefab×3 挂组件）

### S2 变更内容（PHASE6_PLAN §2.2）

| 文件 | 内容 |
|------|------|
| `EnemyPerception.cs` **新建** | FOV 视锥（水平夹角，`fovAngle=80°` 默认）+ 视线 Linecast（敌人胸口+1.5m→玩家胸口+1.2m，首个命中物含 PlayerHealth 即通，被墙/他敌挡住=不可见）+ **0.2s 节流**（防 14 敌 raycast 风暴）；输出 `CanSeePlayer` / `LastKnownPlayerPos` / `TimeSinceSeen`；玩家引用由 EnemyBase.Start 注入（prefab 不持场景引用） |
| `EnemyBase.cs` | 状态机接入视觉判定：Chase/Attack 的进入与维持改为 `SeesPlayer()` 门控（未挂感知组件回退旧版纯距离判定）；Attack 中丢失视线/离开攻击圈 → 退回 Chase 不再站桩隔墙开枪；Chase 丢失视线 → 追 `LastKnownPlayerPos` → 到达后守卫回 Patrol / 波次敌人回 Hunt；新增 `ChaseToPosition()` 共用机动方法；Patrol 环形跳过 null 路点元素（防御 prefab 断链数据形态） |
| 敌人 prefab ×3 | +EnemyPerception：Soldier 20m/80°、**Scout 25m/100°（视野更远更宽）**、Heavy 18m/70°（重装窄视野） |

### S2 发现并修复的 bug

波次敌人（prefab patrolPoints 数组长度保留但元素全 null，S1 迁移实录）在「丢失视线→追到最后位置→进入 Patrol」的一帧执行 `Patrol()` 访问 null 路点元素，抛 `UnassignedReferenceException`（旧代码波次敌人从不走 Patrol 故未暴露）。修复：① 波次敌人到达后回 Hunt 不进 Patrol（语义正确）；② Patrol 推进路点时环形跳过 null 元素，全空视为无路点。复测控制台 0 异常。

### Play 验证（execute_code，坑 81 反射访问）

| 测试 | 结果 |
|------|------|
| A 视锥外 | 守卫面北、玩家背后 10m → CanSee=False、保持 Patrol 待命 ✅ |
| B 视锥内 | 玩家绕至正面 10m → CanSee=True → Chase 追击 → 贴身转 Attack 站桩 ✅ |
| C 掩体遮挡 | 与玩家隔集装箱对峙 → CanSee=False → 退出攻击 → 追至最后已知位置(0,-10) → 回 Patrol ✅（链路在调用间隔真实时间内完整走完） |
| D 波次回归 | Wave 敌人正常刷出，感知组件按变体参数生效（Soldier20/Scout25/Heavy18），Hunt 追击与贴身 Attack 正常 ✅ |
| 控制台 | 修复后 0 错误 0 警告（含 0 UnassignedReferenceException）✅ |

**行为说明**：本步波次敌人 Hunt 仍为直追实时位置的过渡语义（S1 等价），S3 起改冲注入点 + 噪音听觉接管。

---

## 阶段 6 S3 噪音系统 + D8 语义迁移记录（2026-10-25）

> 编译 0 错误 0 警告 | Play 全链路验证通过 | 场景改动：Player +PlayerNoiseEmitter（已保存）
> **起因**：用户反馈「开场敌人直接寻路到玩家面前开枪，躲藏无效」——即 S1/S2 过渡语义（Hunt 直追实时位置）的实际手感问题，S3 的 D8 迁移正是解法

### S3 变更内容（PHASE6_PLAN §2.3/§2.4 前半）

| 文件 | 内容 |
|------|------|
| `PlayerNoiseEmitter.cs` **新建** | 挂 Player：持续噪音（蹲走3/走5/跑7，由 HorizontalSpeed+IsCrouching 计算）+ 瞬时噪音（订阅 OnPlayerShot/OnExplosion → 30m 半径 0.5s 线性衰减）；`CurrentNoiseRadius` 取两者大值 |
| `EnemyPerception.cs` | +听觉：感知 tick 内 `距玩家 < CurrentNoiseRadius → HeardPlayer=true` 并刷新 lastKnown；+`HasIntel` 标记（获得过任何情报） |
| `EnemyBase.cs` | **D8 语义迁移**：Hunt 改冲 `_initialHuntTarget`（出生时刻玩家位置），**不再追踪实时位置**；到达注入点 → **Search 状态**（跑向最后已知位置 → 原地环顾 searchDuration=3s，searchTurnSpeed=90°/s）；听见 → Search 调查（行为分层：看见→Chase/Attack，听到→Search）；搜索超时：波次敌人 reHuntOnSearchTimeout → 复用 SetInitialHuntTarget 向玩家**当前位置**重新进发（防全程隐身卡关），守卫回 Patrol；**到达判定全部改水平（XZ）距离**；Search 无情报时以注入点为锚（防误冲 Vector3.zero） |

### S3 发现并修复的 2 个 bug

1. **到达判定用三维距离**：lastKnown 记录了传送未落地的高处位置（y=2），敌人贴地站在正下方却差 ~1.9m y 值，永远达不到 1.1m 阈值 → 卡死 Chase。修复：Chase/Search 到达判定改 `HorizontalDistance`（Hunt 到达原本已水平）
2. **Search 空情报锚点**：从未看见/听见过玩家的敌人进 Search 后会跑向 Vector3.zero（world origin）。修复：perception +HasIntel 标记，无情报时以注入点为搜索锚点

### Play 验证（execute_code）

| 测试 | 结果 |
|------|------|
| D8 核心 | 敌人刷出后传送玩家 → 8/8 冲向注入点（距玩家 43m+ 仍不跟来）✅ |
| 兜底链 | 注入点 Search 环顾 → reHunt → 找到玩家新位置 → Chase → Attack（8/8 全链路真实完成）✅ |
| 噪音吸引 | 守卫在 21m 视觉外，广播枪声（半径 30m）→ 听见 → Search 循声移动（21m→10.9m）→ 进入视觉 → Attack ✅ |
| Search 锚点修复 | 出生即注入的敌人 Search 阶段原地环顾，未跑向原点 ✅ |
| 瞬时噪音衰减 | 广播后立即读 CurrentNoiseRadius=30 ✅（0.5s 线性衰减；execute_code 调用间隔几十秒，跨调用验证须临时延长 decayTime） |
| 控制台 | 0 错 0 警（Play 中一条 missing-script 警告经编辑模式权威复查为临时测试实例噪声，场景/预制体均干净）✅ |

**待用户体感验证**：蹲走 3m/走 5m/跑 7m 的持续噪音半径手感（程序无法模拟持续移动输入，坑 28）。

### S3 热修复（2026-10-25 用户反馈：躲藏无效、敌人贴脸寻路射击）

用户实测仍「敌人一直寻路找到我然后射击，甚至直接跑到面前」。排查出 **2 个真根因**：

| # | 根因 | 修复 |
|---|------|------|
| 1 | **视线射线打空兜底="视为可见"**：目标取玩家脚底+1.2m，蹲伏胶囊仅 0.08~1.08m——射线从蹲姿头顶掠过打空任何物体 → 兜底判可见 → **蹲下反而必被看见**；站立时隔 1.1m 矮墙的射线也掠过墙顶打中站立胶囊 → 隔矮墙"可见"。此前 S2 测试全部被该兜底掩盖（遮挡测试走的是"打中墙"路径，从未验证"打中玩家"路径） | 目标改**脚底+0.5m**（蹲/站胶囊内必命中）；打空一律判**不可见**（保守） |
| 2 | **reHunt 精确锁定玩家脚下**：搜索超时兜底目标=玩家实时位置 → 敌人贴脸寻路到身上 | 目标改**玩家周围 8~15m 环带随机点**（`reHuntMinOffset/MaxOffset` 可调）+ NavMesh.SamplePosition 吸附——只到区域靠感知交战，不再贴脸 |

**Play 验证**：① 1.1m 实体矮墙（Cinderblock_Wall）敌我分居两侧 → CanSee=false、保持 Patrol ✅；② reHunt 后注入目标距玩家 10.8m（环带内，非 ≈0）✅；控制台 0 错 0 警 ✅。

**铁丝网说明**（排查确认，不改）：SM_Prop_WireFence 碰撞体在框架面板 MeshCollider 上（114/228 有碰撞体），金属丝部分无碰撞体且网格稀疏——**铁丝网本来就不挡视线/子弹**（视觉上也是透的），作掩体需用实体墙/集装箱/建筑。静态碰撞不在本包动（坑 61/65 约束）。

**教训（追加坑 83）**：视线判定射线打空的兜底语义必须保守（判不可见），且目标点必须保证落在目标碰撞体内部——"打空视为可见"会在目标点偏出碰撞体（蹲伏/高度差）时把整个 LOS 逻辑架空成"只查中间遮挡"；测试视线判定必须同时覆盖"打中墙=遮挡/打中玩家=可见/视锥外=不可见"三条路径，只测遮挡路径测不出兜底错误。

### S3 热修复 2（2026-10-25 用户二次反馈：敌人仍持续寻路到面前围堵）

一修后 reHunt 目标虽带 8~15m 偏移，但**锚点仍是玩家实时位置**——每轮 Search 超时（5s）重新锁定一次当前位置，8 敌轮转等价于永久合围，躲藏依旧无效（用户截图：3 敌贴脸围堵）。

| 修复 | 内容 |
|------|------|
| **D8 兜底语义再修订**（PHASE6_PLAN D8 行已同步） | reHunt 锚点改为**最后已知情报位置**（看见/听到过玩家处）；从未获得情报（HasIntel=false）则锚定**出生注入点**——**全链路不再使用玩家实时位置**。敌人失去目标后在其最后接触区域/注入区域驻守游走（环带随机点），玩家重新露头或开枪（=新情报）才会再次触发追击 |
| 波次推进闭环 | 清波必须开枪 → 枪声（30m）天然产生新情报 → 敌人循声而来——"必须交战才能推进"与"藏得住"不再矛盾 |

**Play 验证**：TEST_ANCHOR 注入 (10,-60)、玩家 55m 外静止 → 无情报 reHunt 目标距注入点 14.5m（环带内）、距玩家 28m+，敌人在注入区域驻守游走**不朝玩家去** ✅；hasIntel=false 全程无假视觉 ✅；控制台 0 错 0 警（missing-script 警告经编辑模式复查仍为临时实例噪声）✅。听觉腿已在 TEST_NOISE 端到端验证（21m 枪声→调查→交战）。

**行为语义（最终版，供玩家理解）**：
- 开波：敌人进发到你**开波时刻位置周围 2~5m** 的随机落点（松散，非精确）
- 你隐藏不动：敌人搜查该区域后**驻守**，不再追踪（藏得住）
- 你开枪/暴露：30m 内敌人获得新情报，进发到你**最后已知位置周围 5~10m** 区域
- 你跑动：3~7m 内会被听见
- 清波手段不变：必须主动交战（枪声会暴露你，打完换位）

### S3 热修复 3（2026-10-26 用户三次反馈：目的地松散化「不要寻路到确切位置，而是一个范围」）

用户实测（木屋躲藏截图）：两只敌人**堵在门口**——根因：所有以玩家情报为来源的寻路目的地都是**精确坐标**，NavMesh 会把敌人精确寻路到该点（屋内点的唯一可达路径就是门）。按用户方案改造：

| 改造 | 内容 |
|------|------|
| **松散目的地机制** | 新增 `PickLoosePoint(anchor, minR, maxR)`：锚点周围环带随机取点 + NavMesh.SamplePosition 吸附；**所有**玩家情报来源的寻路必须经此转换，禁止以确切位置为终点 |
| Hunt 进发 | `_huntDestination` = 注入锚点周围 2~5m 随机落点（`huntSpreadMin/Max` 可调），每段 Hunt 只选一次防抖动 |
| Search 接近 | `_searchDestination` = 情报点周围 2~5m 随机落点；情报移动超 3m 自动重选（听到新动静换方向）；Chase 丢失视线/Attack 丢视线**直接转 Search**（原"精确追到 lastKnown 脚下"阶段删除） |
| reHunt 兜底 | `BeginHunt(情报锚点, 5, 10)`——最后已知情报位置周围 5~10m 游走 |

**Play 验证**：① 注入锚点 (20,-60) → 实际寻路目的地偏移 3.9m（环带内）✅；② 三敌同时听见屋内枪声 → 各自搜索落点距玩家 3.4/3.6/4.1m、方向各异（不再挤同一点）✅；控制台 0 错 0 警（missing-script 警告复查仍为临时实例噪声）✅。

**另确认**：用户指出我此前 MCP 验证点把玩家摆在集装箱顶（制高点被全场看见属正确行为），该测试点位无效——后续感知类测试点位一律放地面掩体后。

---

## 阶段 6 S4 交战距离纪律 + 侧移射击记录（2026-10-26）

> 编译 0 错误 0 警告 | Play 木屋场景复测通过 | 无场景/prefab 改动（纯脚本）
> **起因**：用户四次反馈敌人贴脸围堵。带完整观测的复现（木屋+三向进发+视线探针）定位：敌人到达松散落点后**贴墙 3~4m 驻守+环顾旋转**，扫到门窗开口即看见→站桩贴脸射击；另抓到 LOS 射线被自身头球自挡的间歇性 bug（DIAG_1 实录"命中 Head @0.0m"）

### S4 变更内容（PHASE6_PLAN §2.4）

| 文件 | 内容 |
|------|------|
| `EnemyBase.cs` | **Attack 重构为「移动策略+转向开火」两层**：①`AttackUpdate`——距玩家 < 偏好交战距离 → **后撤拉开**（backOffSpeed 2.5）；偏好带内 → **左右横移**（S4 侧移射击，strafeSpeed 2，换向 0.8s+随机防整齐）；②`FaceAndFire`——转向+冷却弹道（与移动解耦，转身/开火不再依赖站桩）；③**每敌随机偏好交战距离 6~13m**（`engageMin/MaxDistance`，Start 随机取值）——交战线自然分散；④开火射线 NonAlloc+跳过自身碰撞体（头球随动画包住起点会自挡） |
| `EnemyPerception.cs` | 视线射线改 `RaycastNonAlloc(8)` + `IsChildOf(self)` 跳过自身——修复动画姿势导致的间歇性自盲 |
| 搜索落点 | searchSpread 2~5m → **4~8m**（驻守位置离建筑更远，减少贴墙环顾） |

### Play 复测（木屋场景，用户同款处境：玩家屋内+枪声暴露）

| 读数 | 结果 |
|------|------|
| 交战距离 | S4_3 看见后于 7.2m 交战（偏好 10m，后撤中）——不再 3~4m 贴墙 ✅ |
| 距离分散 | 三敌偏好交战距离 10.0/6.3/10.0 各不相同 ✅ |
| 丢视线脱离 | 玩家躲回屋内 → S4_3 自动转 Search 撤出交战，血量稳定不再掉 ✅ |
| 惩罚逻辑 | 敌人围屋驻守（4~8m）+扫视开口，**露头才被打**——躲藏有效、暴露受罚 ✅ |
| 控制台 | 0 错 0 警 ✅ |

**行为总结**：木屋门洞/窗是真实视线通道，屋内持续开枪=持续暴露（21 发的代价），这是战术规则不是 bug；但敌人现在**保持 6~13m 交战距离、过近自动后撤**，不会再挤到脸上处决。

---

## 阶段 6 S5 敌人血条 + 受击方向指示器记录（2026-10-26）

> 编译 0 错误 0 警告 | Play 验证通过 | 场景已保存（Canvas+DamageIndicatorUI）

### S5 变更内容（PHASE6_PLAN §2.5/§2.6）

| 文件 | 内容 |
|------|------|
| `EnemyHealthBar.cs` **新建** | 挂三敌人 prefab：**运行时自建**世界空间 Canvas（背景+左对齐红色填充条，0.6×0.07m @头顶 2.2m）——免 prefab 层级手术（坑 55~57 规避）；受击显示 3s 自动隐藏、Billboard 朝主相机（引用失效自动重取）、死亡 HidePermanent（尸体不留血条） |
| `DamageIndicatorUI.cs` **新建** | 挂主 Canvas：4 实例轮换（连续受击不互断），订阅 `OnPlayerDamagedFrom(sourcePos)`——以相机水平朝向为 0°，扇形旋转指向来向（SignedAngle），1s 淡出（手动 alpha，无 DOTween 依赖）；来源 zero 向量忽略 |
| `GameEvents.cs` | +`OnPlayerDamagedFrom(Vector3)` + Raise（只增不改） |
| `PlayerHealth.cs` | TakeDamage 重载 `(amount, sourcePos)`（旧签名转发 zero，调用方零破坏）+ 广播新事件 |
| `EnemyBase.cs` | TakeDamage → `_healthBar.ShowDamage(血量比例)`；Die → `HidePermanent()`；FaceAndFire 命中玩家传 `TakeDamage(damage, transform.position)` |
| `Assets/Textures/DamageIndicator.png` **新增** | 程序化 64×64 径向扇形（尖头朝上，角度/内外缘柔化），Sprite 导入 |
| 敌人 prefab ×3 | +EnemyHealthBar 组件 |

**赋值坑（追加）**：manage_components set_property 对 Sprite 字段两种 guid 格式均报"Failed to convert"——改用 execute_code 的 `SerializedObject.FindProperty("indicatorSprite").objectReferenceValue` 赋值成功（不依赖 CodeDom 识别新类型，坑 81 变通）。

### Play 验证

| 项 | 结果 |
|----|------|
| 血条 | 敌人受 60 伤 → HealthBarCanvas 激活、fill 比例正确（程序读数）✅；位置头顶 2.1m ✅ |
| 方向指示器 | 正面来敌伤害 → 红色扇形出现在屏幕正上方（截图确认）✅；来自侧面时应偏向对应侧（SignedAngle 数学直读）|
| 联动 | 受击同时触发红晕（既有系统）+ 指示器 + 受击动画，互不冲突 ✅ |

**注**：测试玩家在验证期间被波次敌人击杀（YOU DIED，阶段 5 结算流程再次顺带验证）。

---

## 阶段 6 S6 性能收官记录（2026-10-26）

> 编译 0 错误 0 警告 | 池化修复验证通过 | 性能基线已建档

### S6 变更内容（PHASE6_PLAN §2.8）

| 文件 | 内容 |
|------|------|
| `PoolParticleRelease.cs` **新建** | 粒子播完自动回池组件：`OnParticleSystemStopped` → `ObjectPool.Release`（Unity 官方池化回调流） |
| `CombatEffectsController.cs` | **修复池泄漏**：旧实现 stopAction=Disable 仅就地停用、从未 Release 回队列，池退化为"每次新建+孤儿堆积"；改为根粒子 Callback + PoolParticleRelease 回池，子系统保持 Disable |
| `FirearmWeapon.cs` | **枪口火焰池化**：旧实现每次射击 Instantiate+Destroy（600RPM≈10 次/秒分配）；改为 ObjectPool.Get + Callback 回池 + Start 预热 4；删除 `_muzzleFlash` 字段 |

### 对象池审计结论（全量扫描 Instantiate/Destroy）

| 调用点 | 结论 |
|--------|------|
| 曳光/手雷/血特效/烟尘/爆炸/枪口火焰 | 全部走池 ✅（血/烟/爆炸的池泄漏本次修复） |
| 武器模型实例化（WeaponManager/FirearmWeapon.Start） | 一次性装备行为，不池化（合理） |
| 波次敌人生成 | Instantiate 直出（D4 设计：尸体 10s 延迟销毁使池化不值） |
| 拾取物/尸体销毁 | 一次性生命周期 Destroy（合理） |

### 性能基线（编辑器 Play，Wave 交战中采集，FrameTimingManager）

| 指标 | 数值 |
|------|------|
| CPU 帧耗时 | 8.46ms（≈118 FPS，目标 ≥60 达成） |
| GPU 帧耗时 | 4.44ms |
| 主线程/渲染线程 | 3.34ms / 2.03ms |
| 可见蒙皮网格 | 5 |
| Draw Call | 此 API 读取为 0（计数器需编辑器 Stats 窗口激活，暂不可用——后续优化时用 Profiler/Stats 窗人工读取） |

**渲染配置档案**：URP-HighFidelity（MSAA×4、主光 4096 阴影、每像素附加光）——后续如需提帧先降此档。

**UI 美化说明**：按方案"克制"原则本步未动 HUD 布局——具体样式需用户意向（描边/底板/配色），待用户提出后单独小包处理。

---

## 阶段 6 S7 敌人搜索行为 v2：扩展环搜索 + 开波距离纪律（2026-10-26）

> 用户反馈：开场敌人依旧精准寻路到玩家面前；丢失目标后搜索范围太小太频繁，围着玩家位置小圈打转

### 根因分析

| 现象 | 根因 |
|------|------|
| 开场贴脸 | 波次生成器注入玩家出生点为 Hunt 锚点，预制体 `huntSpreadMin/Max=2/5`——敌人寻路到距锚点仅 2~5m |
| 围着玩家打小转 | 搜索超时 reHunt 偏移固定 5~10m 且锚点每轮漂移，永远在情报点 ~10m 内循环；近距离持续听觉（蹲 3m/走 5m）每 tick 把搜索中心钉回玩家实时位置 |
| 显得频繁 | 搜索移动沿用追击速度（5.5~6.5m/s），观感是"冲刺打转"而非"搜索" |
| （实测新发现）原地站立卡死 | 移动阶段每帧 SetDestination 反复重排队路径计算，pathPending 长期为真 → 到达判定 `!pathPending` 永不触发（rem=0/vel=0 但 pend=true，经典 Unity 陷阱） |

### 变更内容

| 文件 | 内容 |
|------|------|
| `EnemyBase.cs` | ① **扩展环搜索**：Search 确立 `_searchCenter`+`_searchRing`，首圈贴情报点（searchSpread 4~8m），每轮环顾超时环半径 +7m（`reHuntExpansionStep`），封顶 32m（`reHuntMaxRadius`）回卷内圈往复扫掠——替代旧"reHunt 固定偏移"模型（`reHuntMin/MaxOffset` 字段删除）② **新声源搬迁门控**：搜索中听见动静仅当声源距当前中心 >6m 才整体搬迁重搜，防持续听觉钉死 ③ **搜索分速** `searchSpeed=4`（MoveToPosition 拆分，追击/搜索分速）④ **开波 huntSpread 默认 2~5→10~16**：进发到锚点外 10~16m 即停转搜索，不贴脸 ⑤ **SetDestination 门控**：目的地变化 >0.5m 或路径丢失才重设（修每帧重排队卡死）⑥ **部分路径自愈**：`PathPartial` 且速度归零视为本轮到达，防孤立 NavMesh 落点永久卡 Hunt/Search ⑦ PickLoosePoint 吸附容差递增重试（2/4/6/8m），全失败回退锚点本身（必可达） |
| `Enemy_Soldier/Scout/Heavy.prefab` | huntSpreadMin 2→10、huntSpreadMax 5→16、searchDuration 5→6、删除 reHuntMin/MaxOffset 残留序列化行 |

### Play 验证（两轮，快进波次计时器 + T+10s 反射禁用感知模拟玩家完全隐身）

| 检查点 | 结果 |
|--------|------|
| 开波距离 | 全员 Hunt 落点距玩家 ≥9.2m，无人进入 8m 内（旧版此时已贴脸）✅ |
| 环推进 | 0→7→14→21→28（封顶）→回卷 0 完整周期；Soldier_1 距离轨迹 15.7→7.0→25.3→36.6m（扫掠路过即离开，不驻留）✅ |
| 卡点 | 第二轮修复后 80s 全程 stall=0（修复前 8 敌中 6 个 pend=true 原地站立）✅ |
| 分速 | Search 移动 v=4.0（searchSpeed）✅ |
| 视觉发现 | 隐身前看见过玩家的敌人，搜索中心=真实目击点，回卷扫掠路过该点（正确搜索教义）✅ |
| 截图 | `Assets/Screenshots/阶段6S7_扩展环搜索验证.png`、`阶段6S7_扩展环搜索验证_终.png` |

---

## 下一阶段

> **规划基准已迁移至 [DEVELOPMENT_PLAN.md](file:///k:/Unity/UnityProject/AI%20Test/Assets/DEVELOPMENT_PLAN.md)（2026-09-06 创建）**——后续开发以该文档为准，包含完整差距分析、已确认决策（D1 Raycast弹道 / D2 轻量静态事件 / D3 波次生存模式 / D4 任务包顺序）、接口定义、进度计划与测试策略。


执行顺序：**4A 基础设施**（GameEvents/AudioManager/ObjectPool + 缺陷F1~F4修复）→ **4B 音效**（generate_audio）→ **4C 敌人升级**（死亡动画/Raycast弹道+曳光/部位伤害）→ **4D 命中反馈**（粒子/HitMarker/弹痕）→ **4E 手雷** → **4F 武器手感**（Sway/HeadBob）→ **阶段5 流程闭环**（GameManager/波次生成/结算界面）→ **阶段6 AI深度**（三重感知/Search/侧移/血条/UI美化/性能）

> **阶段5 实施基准（2026-09-30 已确认）**：[PHASE5_PLAN.md](file:///k:/Unity/UnityProject/AI%20Test/Assets/PHASE5_PLAN.md)——含决策 D5~D7（胜利画面/编入首波/手雷补给一并做）、敌人预制体重建方案、WaveConfig 数据设计、组件详设与接线、S1~S6 实施步骤及每步验收标准、接手 Agent 提示词（文档附录）。阶段5 完成前本节执行顺序中后续项冻结。
