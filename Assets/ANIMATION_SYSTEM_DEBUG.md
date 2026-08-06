# 动画系统排查文档（2026-08-06 会话整理）

> 用途：供后续独立对话直接续接，快速理解动画系统现状、已修复问题、遗留坑与排查方法。
> 关联：`Assets/DEVLOG.md`（阶段 3/3.5 历史）、`Assets/ANIMATION_SYSTEM_DEBUG.md`（本文档）。

---

## 1. 项目背景

| 项 | 值 |
|----|----|
| 引擎 | Unity 2022.3.62f2c1 LTS，URP |
| 主场景 | `Assets/Scenes/SampleScene.unity` |
| 玩家模型 | **P08**（`Assets/P08_Federica/Model_Data/fbx/P08_Federica_forUnity_250729.fbx`），Polygon 骨骼（Hips 绑定 270°），自带 Humanoid Avatar |
| 敌人模型 | PolygonBattleRoyale `Characters.fbx`，共享 CharactersAvatar |
| 玩家动画 | **PlayerAnimator.controller**（TPS Shooter 移植精简版） |
| 敌人动画 | **EnemyAnimator.controller**（老套 BSP 版） |
| 动画资产 | TPS：`Assets/Animations/Humanoid/EquipedAnimations/`（与 TPS Shooter 项目 guid 一致）；BSP：`Assets/Animations/Basic Shooter Pack/` |

---

## 2. 动画状态机架构

### 2.1 PlayerAnimator.controller（玩家，5 层）

```
Base Layer:
  Idle (rifle aiming idle.fbx)
    ←→ Locomotion (2D Freeform Directional, 12 节点)
    ├→ Jump (rifle jump.fbx, JumpSpeed 驱动) → Grounded 退出
    ├→ Crouch (2D Freeform 9 节点, AnimSpeed 驱动)
    └→ Die (death.fbx, AnyState + Died 触发)

Aiming Layer (mask=UpperBodyMask):  Empty ←IsAiming→ Aiming (Aiming.anim)
Reload Layer (mask=UpperBodyMask):   Empty ←AnyState+Reload→ Reload (reload.fbx, exitTime 退出)
ChangeWeapon Layer (mask=UpperBodyMask): Empty ←AnyState+ChangeWeapon→ WeaponEquip (RifleEquip.fbx, exitTime 退出)
UpperBody Layer (mask=UpperBodyMask): Empty ←AnyState+Fire/Hit→ Fire_Upper / HitReaction
```

**参数（14 个）**：`Speed, ForwardSpeed, StrafeSpeed, Grounded, Jump, JumpSpeed, Fire, Reload, Hit, IsAiming, IsCrouch, Died, ChangeWeapon, AnimSpeed`

**Locomotion2D 节点布局**（参数范围用实际速度值 0~7）：
| 位置 | 动画 | 说明 |
|---|---|---|
| (-3,3)(3,3)(-3,-3)(3,-3) | walk_45_left / walk_45_right | 对角 |
| (0,3)(-3,0)(3,0)(0,-3) | walk_fwd / walk_left / walk_right / walk_bwd | 四向 |
| (0,0) | idle aiming | 中心 |
| (-3,7)(0,7)(3,7) | run_45_left / run / run_45_right | 快跑 |

**Crouch2D 节点布局**（9 节点，坐标 ±2.1 = 蹲跑速度）：
crouch_idle + walk crouching forward/backward/right/left + 4 对角（坐标 ±2.1）

> 注意：walk.fbx 内含 6 个 clip（walk_45_left/right/bwd/fwd/left/right），run.fbx 含 3 个（run/run_45_left/run_45_right）。**必须按名称精确引用**，不能取第一个 clip。

### 2.2 EnemyAnimator.controller（敌人，2 层）

```
Base Layer:
  Idle ←→ Locomotion (2D Freeform, 8 节点，全部 BSP 动画)
  Jump (rifle jump.fbx) → Grounded 退出
UpperBody Layer (mask=UpperBodyMask): Empty ←AnyState+Fire/Reload/Hit→ Fire_Upper/Reload_Upper/HitReaction
```

**参数（9 个）**：`Speed, Fire, Reload, Jump, Grounded, JumpSpeed, ForwardSpeed, StrafeSpeed, Hit`
敌人 Locomotion 8 节点**全部用 BSP 动画**（前向 walking.fbx / rifle run.fbx，其余 BSP）。

---

## 3. 脚本驱动链

| 脚本 | 文件 | 驱动参数 |
|---|---|---|
| PlayerController | `Scripts/Player/PlayerController.cs` | Speed/ForwardSpeed/StrafeSpeed/Grounded/Jump/JumpSpeed/AnimSpeed |
| PlayerADSController | `Scripts/Player/PlayerADSController.cs` | IsAiming（_adsProgress>0.5）|
| PlayerCoverController | `Scripts/Player/PlayerCoverController.cs` | IsCrouch（切换式）|
| PlayerHealth | `Scripts/Player/PlayerHealth.cs` | Hit、Died（死亡时）|
| WeaponManager | `Scripts/Combat/WeaponManager.cs` | ChangeWeapon（切枪时）|
| FirearmWeapon | `Scripts/Combat/FirearmWeapon.cs` | Fire、Reload |
| EnemyBase | `Scripts/Enemy/EnemyBase.cs` | Speed/Grounded/Fire/Hit |
| FootstepReceiver | `Scripts/Player/FootstepReceiver.cs` | 动画事件接收（脚步+TPS 换弹/切枪空事件）|

**关键逻辑**：
- PlayerController `LateUpdate`：世界速度 `InverseTransformDirection` 转本地 → ForwardSpeed=z、StrafeSpeed=x
- `_horizontalVelocity` 用 acceleration/deceleration 平滑（MoveTowards），非瞬间启停
- AnimSpeed = clamp(实际速度/目标速度, animSpeedMin, animSpeedMax)，绑定在 Locomotion 和 Crouch 状态的 speedParameter

---

## 4. 场景状态（当前已保存）

- 玩家 = `Player`（根）→ 子物体 `P08_Federica_Plain_C Variant`（Animator + FootstepReceiver）
- 玩家 Animator: PlayerAnimator.controller + P08 自带 Avatar + applyRootMotion=false
- 8 个敌人：EnemyBase + NavMeshAgent + CapsuleCollider，模型子物体 Animator=EnemyAnimator + CharactersAvatar
- 已创建 `Assets/Prefabs/Player.prefab`
- 玩家速度：walk=3 / sprint=6 / crouchMultiplier=0.35 / acceleration=14 / deceleration=18 / animSpeed 0.5~1.3
- FOV：defaultFOV=55 / adsFOV=40 / TPSCamera Lens=55

---

## 5. 本次已修复的 Bug（重要排查参考）

### 5.1 "Statemachine for layer 'Aiming' is missing"（创建 prefab 报错）
- **根因**：PlayerAnimator.controller 的 4 个上层（Aiming/Reload/ChangeWeapon/UpperBody）的 `m_StateMachine: {fileID: 0}`。用 `controller.AddLayer(layer)`（传对象）不会注册 stateMachine，YAML 里 fileID=0。
- **修复**：用 `controller.AddLayer("名称")`（传字符串）→ Unity 自动创建并注册 stateMachine；mask 设置需 `controller.layers = layers` 回写才持久化。
- **副作用**：重建 controller 后 guid 变化 → 场景 Animator 引用变 null，需重新绑定 `anim.runtimeAnimatorController = PlayerAnimator` 并保存场景。

### 5.2 动画"只播放一小段就停住"（玩家/敌人）
- **根因**：BlendTree 节点引用了 `__preview__mixamo.com`（Unity 预览 clip），不是真实动画 clip。
- **修复**：`LoadClip` 必须排除 `name.StartsWith("__preview__")`，并按精确 clip 名加载（尤其 walk.fbx 6 clip / run.fbx 3 clip / crouch 各 FBX 单 clip 但需排除 preview）。

### 5.3 蹲下动画跟不上速度
- **根因**：Crouch 状态最初未绑定 AnimSpeed 参数（只绑了 Locomotion）；Crouch2D 节点坐标 ±3 与蹲速（1.05~2.1）不匹配。
- **修复**：Crouch 状态绑定 `speedParameter=AnimSpeed`；Crouch2D 节点坐标改为 ±2.1。

### 5.4 PlayFootstepSound / TPS 动画事件无接收者报错
- **修复**：`FootstepReceiver.cs` 增加 `PlayFootstepSound` + `FinishedReloading/StartChangingWeapon/UnequipEvent/FinishChangingWeapon` 空方法，挂到玩家+8 敌人模型。

### 5.5 鼠标离开 Game 视图视角仍转（Editor）
- **根因**：`Application.isFocused` 在 Editor 恒为 true。
- **修复**：`PlayerInputHandler.GetLookDelta` 中 `IsMouseInGameView()`——用 `GUIUtility.GUIToScreenPoint` 把 GameView GUI 矩形转屏幕物理像素矩形，与 `Input.mousePosition`（屏幕物理像素，左下原点）比较。
- **坑**：`Screen.height` 是 Game 视图分辨率，不是物理屏幕高度；`Mouse.current.position` 单位/坐标系与 GUI 不一致。必须用 `GUIToScreenPoint` + `Input.mousePosition` 同源对比。

### 5.6 蹲下改为切换式
- `PlayerCoverController`：`IsCrouchPressed()`（WasPressedThisFrame）切换，不再按住。

---

## 6. 已知坑（排查时必读）

1. **Polygon 角色 + Mixamo/TPS 动画 Humanoid retarget 系统性偏差**：P08 Hips 绑定 270°，CharactersAvatar/TPS 动画均有 -47.7°~60° 偏差（历史遗留，本会话未根治，DEVLOG 阶段 3.5 有详述）。
2. **速度权属**：场景 Inspector 序列化值是权威值，改脚本默认值后必须同步场景并保存。
3. **代码创建 BlendTree** 后必须确认参数范围/节点坐标与实际速度匹配。
4. **loopTime** 修复必须用 SerializedObject 改 `m_ClipAnimations[].loopTime` 再 SaveAndReimport，改 meta 顶层 loop 不生效。
5. **`__preview__` clip**：FBX 有多个 clip 时，`LoadAllAssetsAtPath` 第一个可能是预览 clip，必须排除。
6. **重建 controller 会变 guid**：场景/预制体引用会 broken，需重新绑定。
7. **`controller.layers[i].xxx = value` 直接改不持久化**，需 `var l = controller.layers; l[i].xxx = v; controller.layers = l;`。
8. Editor 模拟按键会被 PlayerLoop 重置，验证 BlendTree 用 SetFloat 参数法。

---

## 7. 排查建议流程（供新对话参考）

1. 先 `read_console` 看报错 → 判断是编译错 / 动画事件无接收者 / stateMachine 缺失。
2. `execute_code` 加载对应 controller，遍历 layers 检查 stateMachine 名、states、motion 引用是否 `__preview__`。
3. 进 Play，`SetFloat`/`SetTrigger` 驱动参数，读 `GetCurrentAnimatorStateInfo` + `GetCurrentAnimatorClipInfo` 验证状态/权重/clip.isLooping。
4. 验证 Clip 真实名：`LoadAllAssetsAtPath` 打印所有 clip（排除 `@` 与 `__preview__`）。
5. 修改后 `SaveAssets` + 重新绑定场景引用 + 保存场景 + 截图。

---

## 8. 关键资源路径速查

| 资源 | 路径 |
|---|---|
| 玩家 controller | `Assets/AnimControllers/PlayerAnimator.controller` |
| 敌人 controller | `Assets/AnimControllers/EnemyAnimator.controller` |
| 旧通用 controller | `Assets/AnimControllers/CharacterAnimator.controller`（仍存在，未被引用）|
| mask | `Assets/AnimControllers/UpperBodyMask.mask` |
| TPS 动画 | `Assets/Animations/Humanoid/EquipedAnimations/` |
| BSP 动画 | `Assets/Animations/Basic Shooter Pack/` |
| 玩家模型 | `Assets/P08_Federica/Model_Data/fbx/P08_Federica_forUnity_250729.fbx` |
| 玩家预制体 | `Assets/Prefabs/Player.prefab`（已创建）|
| 输入 | `Assets/Input/PlayerInput.inputactions` |
| 武器数据 | `Assets/Data/RifleData.asset / SMGData.asset / PistolData.asset` |
