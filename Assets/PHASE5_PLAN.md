# 阶段 5「流程闭环」实施方案

> **文档版本**：v1.0（已确认） | **确认日期**：2026-09-30 | **性质**：本任务包唯一实施基准
> **上游文档**：[DEVELOPMENT_PLAN.md](file:///k:/Unity/UnityProject/AI%20Test/Assets/DEVELOPMENT_PLAN.md)（总规划，D1~D4） | [DEVLOG.md](file:///k:/Unity/UnityProject/AI%20Test/Assets/DEVLOG.md)（开发历史与坑库）
> **任务包范围**：①敌人预制体重建（前置） ②GameManager 状态机 ③GameOverPanel 结算界面 ④EnemyWaveGenerator + WaveConfig ⑤击杀统计 HUD ＋ 手雷补给（D7）
> **阶段 6（AI 深度）不在本包**，实施前需 architect 另行详设。

---

## 0. 已确认决策（并入总规划 D 表，coder 不可更改）

| 编号 | 决策项 | 结论 | 确认日期 |
|------|--------|------|----------|
| D5 | 终局玩法 | **胜利画面**：打完 WaveConfig 配置的全部波次 → VICTORY 结算 → 重玩/回主菜单 | 2026-09-30 |
| D6 | 存量 8 敌人 | **编入首波**：场景散装实例（士兵×4/侦察×2/重装×2）停用，首波由生成器按同比例刷出 | 2026-09-30 |
| D7 | 手雷补给 | **一并做**：GrenadePickup 拾取物 + 每波开始自动补 1 枚（上限 3） | 2026-09-30 |

上游决策 D1（Raycast 弹道）/ D2（轻量静态事件）/ D3（波次生存）/ D4（任务包顺序）见 DEVELOPMENT_PLAN §2.2，继续有效。

---

## 1. 现状关键结论（方案依据，coder 实施前须知晓）

| 接线点 | 现状 | 对本方案的影响 |
|---|---|---|
| `PlayerHealth.cs` 死亡分支 | 只触发实例事件 `OnDeath`，未广播总线 | 需补 `GameEvents.RaisePlayerDeath()` |
| `EnemyBase.patrolPoints` | 引用场景 Transform（WP_* 路点） | 预制体不能持有场景引用 → 预制体留空；波次敌人改主动寻敌 |
| `EnemyBase.Update` 距离分支 | 距离 > detectRange(20m) 一律 Patrol | 800m 地图上出生点在 20m 外的波次敌人会**永久站桩** → 必须新增 `huntPlayer` 寻敌模式 |
| `EnemyBase.Die()` | `Destroy(gameObject, corpseDuration=10s)` 延迟销毁 | 存活判定不能用 `activeInHierarchy`（死后 10s 内仍 active）→ 需 `IsDead` 属性 + 登记列表轮询 |
| `PauseMenuController` | 自管 timeScale + disableOnPause 数组（8 组件）+ LoadScene | 三项职责全部迁移 GameManager，PauseMenu 退化为纯 UI |
| `GameEvents` | `OnWaveStarted/OnWaveCleared` 已定义未使用 | 直接启用；另新增 3 个事件（见 §5） |
| 场景敌人 | 8 个散装实例无 prefab 关联（阶段 3.5 回滚时 prefab 已删） | 前置任务① 重建三预制体（D6） |

---

## 2. 前置任务①：敌人预制体重建（S1）

**产出**：`Assets/Prefabs/Enemy_Soldier.prefab` / `Enemy_Scout.prefab` / `Enemy_Heavy.prefab`

**制作方式**——从场景现有散装实例直接 `PrefabUtility.SaveAsPrefabAsset`（标准路径，不手拼）：

1. 每变体选一个场景实例 → 先记录 Inspector 全量数值（EnemyBase 血量/速度/伤害/accuracy、NavMeshAgent 参数、模型层级、Animator 引用）→ 再存为预制体
2. `patrolPoints` 的场景引用会被 Unity 自动断链置空——**预期行为**，预制体巡逻点留空
3. 数值以场景实例为准迁移（DEVLOG 阶段 3 记载追击速度：士兵 5.5 / 侦察 6.5 / 重装 3.5），逐项核对并记录到 DEVLOG
4. 根部 CapsuleCollider **保持 height=2.0 默认**——运行时 `SetupHeadPart` 动态收缩至 1.5（坑 75 逻辑不动）
5. 验证：临时实例化到空场景，组件齐全、无路点时原地待命不报错、爆头判定正常

**场景处置（S4 步执行，S1/S2/S3 不动）**：`Enemies` 与 `PatrolPoints` 分组 SetActive(false) **停用保留**（沿用旧 Ground 先例），全链路验证通过后由用户决定是否删除。

---

## 3. WaveConfig 数据设计（总规划 §7.3 的修订版）

修订点：原设计单预制体/波 → **混兵种条目数组**（D6 要求首波 4/2/2 三兵种混编）。此修订已经用户确认（随本方案整体确认）。

```csharp
// Assets/Scripts/Data/WaveConfig.cs
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

**初始波次表**（`Assets/Data/WaveConfig.asset`，SO 随时可调，测试期可临时调低再还原）：

| 波次 | 配置 | 总数 | 备注 |
|------|------|------|------|
| Wave 1 | 士兵×4 + 侦察×2 + 重装×2 | 8 | 等价原场景守卫（D6） |
| Wave 2 | 士兵×4 + 侦察×4 | 8 | |
| Wave 3 | 士兵×5 + 侦察×3 + 重装×2 | 10 | |
| Wave 4 | 士兵×6 + 侦察×4 + 重装×2 | 12 | |
| Wave 5（终波） | 士兵×5 + 侦察×5 + 重装×4 | 14 | 清空 → VICTORY（D5） |

---

## 4. 组件详设

### 4.1 GameManager（`Assets/Scripts/Data/GameManager.cs`，场景根对象）

```
状态机：
  Playing ⇄ Paused   （Tab，走原暂停链路）
  Playing → GameOver （OnPlayerDeath → 延迟 1.5s 弹结算）
  Playing → Victory  （OnGameVictory → 延迟 1.0s 弹结算）
```

- 接口按总规划 §6.4（State/KillCount/CurrentWave/SurvivalTime/Pause/Resume/TriggerGameOver/Restart/ReturnToMenu），另加 `TriggerVictory()`
- **迁移自 PauseMenuController**：`Time.timeScale` 管理、`disableOnPause` 数组（现 8 组件，含 4F 新增 2 个）、`ReturnToMainMenu` 场景加载
- 死亡/胜利**不置 timeScale=0**（世界保持活动：Died 动画、敌人走动正常播放），改为禁用 disableOnPause 数组 + 解锁鼠标
- `SurvivalTime` 仅 `State==Playing` 时以 deltaTime 累计（timeScale=0 时 deltaTime 为 0，暂停天然不计时）
- `KillCount`：订阅 `OnEnemyKilled` 累加 → 广播 `RaiseKillCountChanged`
- `Restart()` = `SceneManager.LoadScene(当前场景)`（场景重载即状态全清；GameEvents 订阅者随场景对象销毁退订）
- GameOver/Victory 后 `PauseMenuController` 的 Tab 不再响应（查 `GameManager.Instance.State`）

### 4.2 GameOverPanel（`Assets/Scripts/UI/GameOverPanelController.cs` + Canvas 子物体）

- 结构：全屏半透明暗底（程序化纹理，参照 MainMenu 模式）+ 标题（**YOU DIED** 红 / **VICTORY** 绿）+ 三行数据（击杀数 / 存活波次 `CurrentWave/MaxWave` / 存活时间 mm:ss）+ 重玩 / 回主菜单按钮
- 订阅 `OnGameOver` / `OnGameVictory` 显示；数据从 `GameManager.Instance` 拉取；DOTween 淡入 `SetUpdate(true)`（不受 timeScale 影响）
- 按钮：`Restart()` / `ReturnToMenu()` 调 GameManager；点击广播 `RaiseUIClick`（4B 音效预留）
- 显示时解锁鼠标（Cursor.lockState=None + visible）

### 4.3 EnemyWaveGenerator（`Assets/Scripts/Enemy/EnemyWaveGenerator.cs`，场景根对象 WaveSystem）

内部状态机：`Preparing(首波10s) → Spawning(interval 逐刷) → Fighting(等全灭) → Intermission(startDelay 倒计时) → 下一波 | Victory`

| 设计点 | 结论 |
|---|---|
| 刷怪方式 | `Instantiate` 直出（**敌人不入对象池**——尸体 10s 延迟销毁使池化复杂度不值） |
| 出生点选择 | 候选 = 距玩家 > `minSpawnDistance`(20m) 的点随机；无满足则取最远；布置 8~10 个岛屿边缘点（码头/村口/桥/山脊），布置时以 NavMesh 可视化确保在网格上，运行时 `NavMesh.SamplePosition` 2m 容差微调 |
| 敌人初始化 | 生成后设 `enemyBase.huntPlayer = true`（坑 67 注意：huntPlayer 为无 Start 重置的新字段，直赋安全） |
| 存活判定 | `List<EnemyBase>` 登记 + 每帧清理 `null \|\| IsDead` → Fighting 中清零 → `RaiseWaveCleared(n)`；末波清空 → `RaiseGameVictory()` |
| 波次广播 | 开波 `RaiseWaveStarted(n)`；每波开始 `GrenadeThrower.AddGrenades(1)`（D7，开关 `restockGrenadePerWave=true`，上限 3 钳制在 GrenadeThrower 内） |

### 4.4 WaveBanner（`Assets/Scripts/UI/WaveBannerController.cs` + Canvas 子物体）

- Canvas 中央大字：波开始弹出「WAVE N」2s；波清空显示「WAVE N CLEARED」；Intermission 期间同面板显示「下一波 Xs」每秒刷新
- DOTween 缩放弹出（HitFeedbackUI 模式）；**Sequence 存字段引用，触发前 Kill 旧序列**（4D 热修复教训：防连续触发叠加）

### 4.5 击杀统计 HUD

- Canvas 右上角 `KillText`「击杀 N」（血条左上/弹药手雷右下，无冲突）
- UIManager 新增 `killText` 字段，订阅 `OnKillCountChanged`（OnEnable/OnDisable 配对 + Start 拉取初始值——4E 手雷计数同模式）

### 4.6 GrenadePickup（`Assets/Scripts/Combat/GrenadePickup.cs` + 预制体）

- 复制 HealthPickup 模式：Trigger + Player tag 过滤 → `AddGrenades(1)`（GrenadeThrower 内钳上限 3）→ `RaisePickupTaken` → Destroy 自身；Awake 自动补 Trigger Collider
- 模型用包内 `SM_Wep_Grenade_01`；场景摆 2~3 处（码头/仓库/村口）

---

## 5. 增量变更清单（全部"只增不改"，coder 逐项对照）

| 文件 | 变更 |
|---|---|
| `GameEvents.cs` | **+3 事件**：`OnKillCountChanged(int)` / `OnGameOver` / `OnGameVictory` + 配套 Raise 方法（命名 `Raise{事件名去掉On}`） |
| `PlayerHealth.cs` | 死亡分支补一行 `GameEvents.RaisePlayerDeath()`（实例事件 OnDeath 保留不动） |
| `EnemyBase.cs` | ① `public bool IsDead => _isDead;` ② `[SerializeField] bool huntPlayer`（true 时 Update 走"attackRange 内攻击，否则直接追击"分支，无视 detectRange） |
| `PauseMenuController.cs` | 删 timeScale/disableOnPause/LoadScene 职责 → 全部改调 GameManager；UI 面板切换与设置控件绑定保留；Tab 检测加 `State==GameOver/Victory` 屏蔽 |
| `UIManager.cs` | +killText 字段与订阅（4E 手雷计数同模式） |
| `GrenadeThrower.cs` | `AddGrenades` 补上限钳制（若现无） |

---

## 6. 实施步骤与验收（S1→S6 顺序执行，每步独立可编译可玩）

| 步 | 内容 | 验收标准 |
|----|------|----------|
| **S1** | 三敌人预制体重建（**场景不动**，散装敌人保留供回归） | 预制体磁盘存在且引用完好；临时实例化组件齐全、待命不报错；read_console 0 错误；原场景玩法无回归 |
| **S2** | GameManager 骨架 + PauseMenu 职责迁移 | Tab 暂停/恢复/设置/回主菜单与迁移前**全功能等价**；disableOnPause 8/8 生效；timeScale 切换正常 |
| **S3** | 死亡流程：PlayerHealth 广播 + GameOver 状态 + GameOverPanel（阵亡态） | execute_code 直调 `TakeDamage` 致死 → 1.5s 后结算显示（击杀 0/波次 0/时间）→ 重玩状态全清（弹药/手雷/医疗包/击杀/波次）→ 回主菜单正常；死亡后 Tab 无效 |
| **S4** | WaveConfig SO + WaveGenerator 核心 + EnemyBase 增量（IsDead/huntPlayer）+ **停用散装 Enemies/PatrolPoints** + 首波上线 | 开场 10s → Wave1 8 敌从远处出生点**主动冲向玩家**（huntPlayer 生效）→ 全灭 → 5s → Wave2；敌人行为不依赖巡逻点 |
| **S5** | 胜利流程 + WaveBanner + 波次表填满 5 波 + 出生点完善（8~10 个） | 打完 Wave5 → VICTORY 结算；波间倒计时 UI 正确；测试期可临时调低敌人血量/波次数量加速验证，**测完还原并记录 DEVLOG** |
| **S6** | 击杀 HUD + GrenadePickup + 每波补雷 | 击杀数实时递增；捡雷 +1 且上限 3；每波开始手雷 +1 |

**每步收尾动作（铁律）**：read_console 0 错误 0 警告 → 截图（manage_camera）交用户确认 → DEVLOG.md 追加记录（格式参照「任务包 4X 完成记录」）→ 下一步。

---

## 7. 风险与坑对照

| 风险 | 对策 |
|---|---|
| 坑 55~57 预制体操作 | S1 用场景实例 SaveAsPrefabAsset 标准路径；patrolPoints 断链是预期行为 |
| 坑 67 实例化后 Start 覆盖 | huntPlayer 无 Start 重置逻辑，直赋安全；**coder 若需在 EnemyBase.Start 加重置逻辑必须停下回报 architect** |
| 坑 28 输入模拟不可靠 | 死亡流程测试用 execute_code 直调 `PlayerHealth.TakeDamage`，不模拟射击 |
| 坑 61/65 NavMesh | 本包**不动静态碰撞、不重烘焙 NavMesh**；出生点仅确保落在现有 NavMesh 网格上 |
| 坑 70/72/74/81 execute_code | CodeDom 环境：`UnityEngine.Object` 全限定、`UnityEngine.Random`、UI 类型全限定、新编译类型经 `GetType().Name` 匹配操作 |
| GameEvents 残留订阅 | 新组件全部 OnEnable/OnDisable 配对退订；Restart 靠场景重载 + 销毁退订 |
| 敌人并发峰值（Wave5 = 14 敌） | Instantiate 直出可承受；帧率异常则记录数据留阶段 6 性能收官处理，不在本包优化 |
| DOTween 序列叠加 | WaveBanner/GameOverPanel 的 Sequence 存字段引用，触发前 Kill 旧序列 |

---

## 8. 整体验收清单（用户 Play 终验）

- [ ] 开场 10s 准备倒计时 → Wave 1（士兵4/侦察2/重装2）从多个远离玩家的出生点刷出并主动围攻
- [ ] 波次递进 8/8/10/12/14，波间 5s 倒计时 UI，波开始/清空 Banner 提示
- [ ] 死亡 → 1.5s → YOU DIED 结算（击杀/波次/时间）→ 重玩后状态全清
- [ ] 打完 Wave 5 → VICTORY 绿色结算
- [ ] 击杀 HUD 实时递增；GameOver/Victory 后 Tab 暂停无效
- [ ] 手雷：拾取物 +1（上限 3）、每波开始自动补 1
- [ ] 回归：暂停/设置/主菜单循环、射击/换弹/切枪/ADS/蹲伏/治疗/手雷投掷全通过
- [ ] 编译 0 错误 0 警告；DEVLOG 记录完整；explore 审查通过

---

## 附录：接手 Agent 提示词（复制即用）

```text
你现在的角色是 coder（Flash 开发者），职责：接收已确认方案 → 实现代码。
禁止自行规划、跳过依赖、修改方案；遇到方案未覆盖的问题，停下向用户回报，不要自行发挥。
项目根目录：k:\Unity\UnityProject\AI Test（Unity 2022.3.62 LTS + URP + 新 Input System + Cinemachine 3 的第三人称战术射击，游戏模式为波次生存）。

【开工前必读（按顺序，全部读完再动手）】
1. AGENTS.md —— 角色权限边界与工作流铁律
2. Assets/DEVLOG.md —— 开发历史总库；重点：文末「会话 5 记录」「任务包 4A~4F 完成记录」、已知坑 61/65/67~81
3. Assets/DEVELOPMENT_PLAN.md —— 项目总规划：决策 D1~D4、GameEvents 接口约定（§6.1）、代码规范（§8）
4. Assets/PHASE5_PLAN.md —— 本任务包唯一实施基准：方案详设（§2~§4）、增量变更清单（§5）、步骤与验收（§6）、风险对照（§7）

【当前进度】阶段 4 已全部完成（4B 音效因 fal.ai Key 挂起，与本包无关，勿动 AudioManager 的空 Clip 逻辑）。
阶段 5「流程闭环」方案已经用户确认，新增决策 D5~D7（胜利画面 / 存量敌人编入首波 / 手雷补给一并做）。

【任务】按 PHASE5_PLAN.md §6 的 S1 → S2 → S3 → S4 → S5 → S6 顺序逐步实施。每步完成必须：
1. read_console 检查编译：0 错误 0 警告（铁律，改任何脚本后必查）
2. 对照该步验收标准自测（Play 验证用 execute_code，方法见 PHASE5_PLAN §6/§7）
3. manage_camera 截图交用户确认后再进下一步
4. 在 DEVLOG.md 追加该步记录（格式参照既有「任务包 4X 完成记录」：新增/修改脚本表、场景接线、验证结果、新坑）
5. 六步全部完成后：提醒用户安排 explore 独立审查 + Play 终验（PHASE5_PLAN §8 清单）

【硬性约束】
1. 决策 D1~D7 不可更改（DEVELOPMENT_PLAN §2.2 + PHASE5_PLAN §0）
2. 增量变更严格按 PHASE5_PLAN §5 清单"只增不改"；GameEvents 事件只增不改签名
3. 新增事件订阅必须在 OnEnable/OnDisable（或 OnDestroy）配对退订
4. 禁止新增 Find 系列调用，新引用一律 [SerializeField] 序列化引用
5. 敌人不入对象池（Instantiate 直出）；特效/曳光/弹痕/手雷沿用现有 ObjectPool
6. 本包不动场景静态碰撞、不重烘焙 NavMesh（坑 61/65）；出生点只确保落在现有 NavMesh 上
7. 修改场景/预制体后用 refresh_unity 刷新；新组件使用前确认 isCompiling=false

【本包最容易踩的坑（详见 PHASE5_PLAN §7，动手前重读一遍）】
- 坑 67：Instantiate 后给组件字段赋值可能被 Start 覆盖——huntPlayer 是无 Start 重置的新字段才可直赋；若需在 EnemyBase.Start 加重置逻辑，必须停下回报
- 敌人存活判定用 List<EnemyBase> 登记 + IsDead 轮询，不能查 activeInHierarchy（尸体 10s 延迟销毁期间仍为 active）
- S1 建预制体时 patrolPoints 的场景引用自动断链置空是预期行为，不是 bug
- PauseMenuController 职责迁移后，Tab 键检测必须屏蔽 GameOver/Victory 状态（查 GameManager.Instance.State）
- 坑 28：编辑器内模拟按键不可靠——死亡流程测试用 execute_code 直调 PlayerHealth.TakeDamage
- DOTween 的 Sequence 存字段引用、触发前 Kill 旧序列（4D 热修复教训，WaveBanner/结算面板都要遵守）

【完成定义】PHASE5_PLAN §8 整体验收清单全部满足 + 编译 0 错误 0 警告 + DEVLOG 记录完整 + explore 审查通过 + 用户 Play 确认。任何一步验收不通过，修复后重跑该步全部验收项，不带病进下一步。
```
