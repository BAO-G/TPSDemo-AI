# TPS 战术射击游戏 — 开发日志

> 最后更新：2026-08-03 | 当前阶段：阶段 1 完成 → 待进入阶段 2

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
@architect 请读取 Assets/DEVLOG.md 了解项目现状，然后规划阶段 2 战术系统
```

日常开发：
```
@architect 规划XXX → 自动派 @coder 执行
@coder 实现XXX
@explore 审查XXX
```

每个 coder 完成必须 `read_console` 检查编译错误。
