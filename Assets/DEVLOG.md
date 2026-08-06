# TPS 战术射击游戏 — 开发日志

> 最后更新：2026-08-07 | 当前阶段：阶段 3.6 移动/武器/准星完善完成 → 待进入阶段 4（打磨）

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

## 下一阶段
阶段 4：打磨 — 音效（射击/换弹/受伤/脚步）、命中粒子特效、敌人死亡动画、手雷功能（toss grenade）、掩体切角（turn 动画挂载点）、UI 美化、性能优化
