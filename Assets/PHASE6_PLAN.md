# 阶段 6「AI 深度 + 收官」实施方案

> **文档版本**：v1.0（草案，待用户确认） | **起草日期**：2026-10-25 | **性质**：本任务包唯一实施基准
> **上游文档**：[DEVELOPMENT_PLAN.md](DEVELOPMENT_PLAN.md)（总规划，D1~D4） | [DEVLOG.md](DEVLOG.md)（开发历史与坑库，重点：会话 5、阶段 5 记录、坑 61/65/67/75~82）
> **任务包范围**：①EnemyBase 状态机重构（前置） ②感知系统（FOV+视线+噪音） ③Search 状态+侧移射击 ④敌人血条+受击方向指示 ⑤性能与 UI 收官；掩体切角降级为可选包
> **4B 音效不在本包**（用户确认：fal.ai Key 未配置，继续挂起；AudioManager 空 Clip 静默逻辑勿动）

---

## 0. 决策（并入总规划 D 表）

| 编号 | 决策项 | 结论 | 状态 |
|------|--------|------|------|
| D8 | 波次敌人与感知的关系 | **接入感知**：`huntPlayer` 全知模式废除，语义改为「出生时注入一次玩家当前位置（Hunt 初始目标）」，到达后由感知/搜索接管；**兜底**：波次敌人 Search 超时后重新向玩家当前位置进发（防玩家全程隐身导致波次永不结算卡关） | ✅ 用户已确认（2026-10-25） |
| D9 | EnemyBase 重构幅度 | **轻量状态机**：enum + switch，不引入框架/类库（符合 D2 克制风格）；现有行为等价迁移 + 新增状态一次到位 | ✅ 用户已确认（2026-10-25） |
| D10 | 敌人血条实现 | 世界空间 Canvas（prefab 头顶子物体）+ Image fillAmount，受击显示 3s 自动隐藏，每帧 Billboard 朝向主相机；用 UGUI Image 不用 TMP（项目未导入 TMP Essentials） | 随本方案确认 |
| D11 | 受击方向指示器 | 玩家屏幕空间径向扇形 Image，按攻击来源方向旋转，1s 淡出，同屏上限 4 个循环复用；需新增事件 `OnPlayerDamagedFrom(Vector3 sourcePos)`（只增不改） | 随本方案确认 |
| D12 | 掩体切角 | **降级为可选包 S7**（默认不做）：与状态机/侧移耦合度高、收益低，时间富余再议 | 随本方案确认 |
| D13 | 性能目标 | 编辑器 Game 视图 Wave5 峰值（14 敌）FPS ≥ 60；记录 Draw Call 基线数字入 DEVLOG（不设硬指标，先建档再优化） | 随本方案确认 |

上游决策 D1（Raycast 弹道）/ D2（轻量静态事件）/ D3（波次生存）/ D4~D7 继续有效。

---

## 1. 现状关键结论（方案依据，coder 实施前须知晓）

| 接线点 | 现状（已核对源码） | 对本方案的影响 |
|---|---|---|
| `EnemyBase.Update`（321 行） | if-else 距离驱动：attackRange→攻击 / huntPlayer→直追 / detectRange→追击 / 否则巡逻；**无状态机** | S1 重构对象；重构必须行为等价，回归清单见 §4 S1 |
| `EnemyBase.huntPlayer` | 波次生成器实例化后直赋 true（无 Start 重置，坑 67 安全） | D8 改语义后，生成器改调 `SetInitialHuntTarget(pos)`——**新字段同样不得有 Start 重置逻辑** |
| 敌人视觉/听觉 | 完全没有——detectRange 纯距离判定，隔墙也"看见" | 全部新建 |
| 玩家噪音源 | `PlayerController.HorizontalSpeed` / `IsGrounded` 只读属性已就绪（4F）；蹲伏状态在 `PlayerCoverController`；开枪事件 `OnPlayerShot(pos, weaponName)`、爆炸 `OnExplosion(pos)` 已存在 | 噪音发射器只需订阅+轮询，零新增输入依赖 |
| `GameEvents` | 24 个事件全部带 Raise 方法；订阅方 OnEnable/OnDisable 配对退订 | 只增不改：+`OnPlayerDamagedFrom(Vector3)` |
| 敌人攻击链 | `AttackPlayer()`：isStopped 停桩 + RotateTowards 转向 + Raycast 弹道 + 曳光 | S4 侧移在"停桩"处改为横移，攻击逻辑本身不动 |
| 死亡链 | Die() 禁用 NavMeshAgent+全部 Collider → 10s 销毁 | 血条须在 Die 时隐藏（尸体不留血条） |
| explore 审查 P2 ×2 | ① SpawnNext 对缺 EnemyBase 的预制体无兜底；② WaveBanner 倒计时 0.1s 淡入 tween 未纳入 Kill 管理 | S1 顺手修复 |
| 敌人预制体 ×3 | Enemy_Soldier/Scout/Heavy（阶段 5 重建），头部球体碰撞体是**运行时** SetupHeadPart 生成 | 血条挂 prefab 根下固定偏移（不挂 Head 骨骼，避开运行时加部件） |

---

## 2. 组件详设

### 2.1 EnemyBase 状态机重构（S1，前置）

```
enum EnemyState { Patrol, Hunt, Chase, Attack, Search, Dead }

Update 主循环（switch）：
  Patrol   = 现有 Patrol()（路点循环/原地待命），仅散装守卫用（场景已停用，保留兼容）
  Hunt     = 新增：奔向 _initialHuntTarget（出生注入点），到达（remaining ≤ stoppingDistance+1m）→ Search
  Chase    = 现有 ChasePlayer()，追实时玩家位置（视觉持续确认时）
  Attack   = 现有 AttackPlayer()（S1 原样迁移；S4 加侧移）
  Search   = S4 实现：奔向最后已知位置 → 到达后环顾 searchDuration → 驻守/兜底进发
  Dead     = 现有 Die() 之后的静置期
```

- 状态切换**只发生在感知结果驱动下**（见 2.2），不再每帧按距离直接分流
- **迁移等价性对照表**（S1 验收基准）：

| 旧行为 | 新路径 |
|---|---|
| huntPlayer=true 且 distance>attackRange → 直追 | Hunt（冲注入点）→ 感知看见 → Chase |
| distance ≤ attackRange → 攻击 | 感知确认可见（或 Hunt/Chase 中进入攻击圈）→ Attack |
| 距离 ≤ detectRange → 追击 | 感知 CanSee → Chase |
| 否则巡逻 | 感知未见 → Patrol（守卫）/ Search（波次敌人） |

- 公开接口：`SetInitialHuntTarget(Vector3 pos)`；`huntPlayer` 字段**删除**（三预制体与生成器同步改，避免半套兼容两套语义）；`IsDead` 保留
- 关键约束：`_initialHuntTarget` 等新字段**不得加 Start 重置**（坑 67：生成器实例化后同帧直赋，Start 重置会覆盖）

### 2.2 EnemyPerception（新组件，挂三敌人 prefab）

- **视觉判定** `CanSeePlayer`：距离 ≤ `sightRange`(=现 detectRange 20，Inspector 可调) **且** 玩家在 FOV 视锥内 `fovAngle=80°`（以自身 forward 为轴，水平夹角）**且** Linecast 无遮挡（胸口 +1.5m → 玩家胸口 +1.2m；命中物含 PlayerHealth 判定到达，被墙挡判不可见）
- **节流**：感知每 `perceptionTick=0.2s` 计算一次（14 敌 × 5 次/秒 × 1 raycast，开销可忽略；忌每帧 raycast）
- **输出**：`CanSeePlayer`（bool）、`LastKnownPlayerPos`（可见时每 tick 刷新）、`LastSeenTime`
- 距离环按 DEVELOPMENT_PLAN §5.8 简化：不做 Inner/Outer 双环，单一 sightRange + FOV 即可（Inspector 可调，实测后如需再增）
- 视线 Linecast 层：Default（同 Deoccluder 配置），起点自身需排除（从胸口发出天然避开自身胶囊；命中敌人头球时判遮挡——敌人互相挡视线是合理行为，不做特判）

### 2.3 PlayerNoiseEmitter（新组件，挂 Player）

- **持续噪音**：每帧计算 `noiseRadius`：蹲移动 3 / 走 5 / 跑 7 / 站立 0（读 HorizontalSpeed + PlayerCoverController 蹲伏状态；数值 Inspector 可调）
- **瞬时噪音**：订阅 `OnPlayerShot` → noiseRadius 置 30 并 0.5s 线性衰减；订阅 `OnExplosion` → 同上（手雷炸响引敌）
- **消费方式**：敌人感知 tick 内查询 `PlayerNoiseEmitter.CurrentNoiseRadius` + 发射器位置：距离 < 半径 → 听见 → 更新 LastKnownPlayerPos（不直接触发 Chase，交给状态机按"听到→Search"处理；**看见→Chase，听到→Search** 是本方案的行为分层）

### 2.4 Search 状态 + 侧移射击（S4）

- **Search**：感知丢失超过 `lostSightDelay=0.5s` 从 Chase/Attack 进入 → 奔向 LastKnownPlayerPos → 到达后环顾（`searchTurnSpeed=90°/s` 原地转 `searchDuration=3s`）→ 期间看见则回 Chase；超时后：守卫（有路点）回 Patrol；**波次敌人兜底进发**（D8）：重新向玩家当前位置 Hunt（`reHuntOnSearchTimeout=true`，Inspector 可关）
- **侧移射击**：Attack 状态内、冷却等待期执行——沿「玩家方向 × 垂直轴」横移（NavMeshAgent 不停桩改移动，`strafeSpeed=2`，`strafeSwitchInterval=0.8s` 随机换向）；距玩家 <4m 停止侧移防贴脸；开火瞬间与转向逻辑不动（转向仍是每帧 RotateTowards）

### 2.5 EnemyHealthBar（新组件 + prefab 子物体，S5）

- 三敌人 prefab 根下加 World Space Canvas 子物体（头顶 +2.2m，含背景 Image + 红色填充 Image，尺寸 0.6×0.07m），默认隐藏
- `EnemyHealthBar`：订阅自身 EnemyBase 受击（直接在 TakeDamage 里调用 `healthBar.Show()`，不走全局事件）→ 显示 3s 淡出；fillAmount = 当前血量/上限；LateUpdate Billboard 朝主相机；Die 时永久隐藏
- 血条脚本持引用由 EnemyBase SerializeField 注入（禁 Find）

### 2.6 DamageIndicatorUI（新组件，挂 Canvas，S5）

- 订阅 `OnPlayerDamagedFrom(sourcePos)`（新增事件；`PlayerHealth.TakeDamage` 增加可选来源参数，玩家自身掉血处广播）与 `OnEnemyShot`（可预判开火方向？**不做**——只对实际受击生效，避免噪声）
- 计算：攻击者相对玩家水平方位角 → Canvas 中心为轴旋转扇形 Image 指向来向 → 1s DOTween 淡出；4 个实例环形队列复用（PoolTicket 模式或直接预挂 4 个子物体轮换）
- 素材：程序化生成 64×64 径向扇形纹理（RedVignette/HitMarker 同模式）

### 2.7 P2 修复（S1 顺手）

- ① `EnemyWaveGenerator.SpawnNext`：`GetComponent<EnemyBase>()` 判空 → LogError 指名预制体 + 跳过该条目
- ② `WaveBannerController`：倒计时淡入 tween 引用纳入字段管理，触发前 Kill（与 _bannerSeq 同纪律）

### 2.8 性能 + UI 收官（S6）

- Profiler 记录 Wave5 峰值基线（FPS / Draw Call / GC Alloc），数字入 DEVLOG；确认 SRP Batcher 开启
- 感知节流已在 2.2；血条 3s 自动隐藏控制 world canvas 存活数
- UI 美化范围**克制**：HUD 文字描边/底板统一、结算面板按钮样式统一，不重排布局（大改留到有美术资源时）
- 对象池覆盖核查：曳光/弹痕/特效/手雷确认无 Instantiate 直出残留

---

## 3. 增量变更清单

| 文件 | 变更 | 性质 |
|---|---|---|
| `EnemyBase.cs` | 状态机重构（§2.1）；TakeDamage 加来源位置参数与回调钩子；**删除 huntPlayer** | ⚠️ 改造（方案内唯一大改，迁移对照表见 §2.1） |
| `EnemyPerception.cs` | **新建**：FOV+视线+节流+噪音查询 | 新增 |
| `PlayerNoiseEmitter.cs` | **新建**：持续/瞬时噪音半径 | 新增 |
| `EnemyHealthBar.cs` | **新建**：血条显示/Billboard | 新增 |
| `DamageIndicatorUI.cs` | **新建**：受击方向指示 | 新增 |
| `GameEvents.cs` | +`OnPlayerDamagedFrom(Vector3)` + Raise（只增不改） | 增量 |
| `PlayerHealth.cs` | TakeDamage 增加可选来源参数并广播新事件（现有签名重载，旧调用不破坏） | 增量 |
| `EnemyWaveGenerator.cs` | huntPlayer=true → SetInitialHuntTarget(玩家当前位置)；P2① 兜底 | 增量 |
| `WaveBannerController.cs` | P2② tween 管理 | 增量 |
| 敌人 prefab ×3 | +EnemyPerception 组件；+血条 Canvas 子物体；Inspector 参数按变体微调（Scout 视野更远等，实施时定） | 资产 |
| Player prefab/场景 | +PlayerNoiseEmitter；Canvas +DamageIndicator（4 实例）；新纹理资产 | 资产 |
| `inputactions` | **无变更** | — |
| NavMesh/静态碰撞 | **不动**（坑 61/65） | — |

---

## 4. 实施步骤与验收（S1→S6 顺序，每步独立可编译可玩）

| 步 | 内容 | 验收标准 |
|----|------|----------|
| **S1** | 状态机重构（行为等价迁移）+ P2×2 修复 | 编译 0 错；**回归清单全过**：散装守卫巡逻/追击/攻击、波次围攻至结算、死亡链、爆头、隔墙不扣血、曳光正常； huntPlayer 删除后无残留引用（全局搜索确认） |
| **S2** | EnemyPerception（FOV+视线）接入状态机 | 掩体后/视锥外敌人**不**开火不追击；绕到侧后敌人转向发现你；感知 tick 节流生效（Profiler 无 raycast 尖峰）；Play 射线探针验证视锥边界 |
| **S3** | PlayerNoiseEmitter + 噪音听觉 + huntPlayer 语义迁移（D8） | 蹲行接近 20m 外敌人不被察觉；开一枪 30m 内敌人转向 Search；手雷爆炸引敌；**波次开局敌人仍从出生点冲向玩家**（Hunt 注入生效）；全程蹲住不动敌人最终仍会兜底进发（波次可结算） |
| **S4** | Search 状态完善 + 侧移射击 | 丢失视线 → 敌奔向最后已知位置 → 环顾 3s → 驻守/兜底；攻击时敌人左右横移、贴脸停止侧移、开火节奏不变 |
| **S5** | 敌人血条 + 受击方向指示器 | 受击显示血条 3s 消失、朝向相机、死亡不显示；指示器指向攻击来向、1s 淡出、连续受击不叠加错乱 |
| **S6** | 性能基线 + UI 收官 | Wave5 峰值 FPS≥60（编辑器）；Draw Call 基线数字入 DEVLOG；HUD 样式统一；对象池核查无直出 |
| *(S7 可选)* | 掩体切角（D12 默认不做） | 另行详设 |

**每步收尾铁律**（同阶段 5）：read_console 0 错 0 警 → 对照验收自测（Play 实测，输入不可模拟用 execute_code 直调，坑 28）→ manage_camera 截图 → DEVLOG 追加记录 → git 提交 → 下一步。

---

## 5. 风险与坑对照

| 风险 | 对策 |
|---|---|
| 坑 67 Start 覆盖 | `_initialHuntTarget` 等新字段无 Start 重置；**coder 若需在 EnemyBase.Start 加重置必须停下回报** |
| S1 重构回归面大 | 迁移对照表逐项 Play 实测；重构步不加新功能（行为等价原则）；旧 Update 逻辑保留在 git 历史可随时 diff 对照 |
| 感知 raycast 性能 | 0.2s tick 节流 + 单次 raycast；禁每帧 Physics queries |
| 噪音/感知数据竞争 | 感知读、发射器写，单线程 Unity 无锁问题；发射器 OnEnable/OnDisable 配对，场景重载自清 |
| 血条 world canvas 数量 | 3s 自动隐藏 + 死亡隐藏；峰值同屏可见 ≤ 存活敌数（≤14），可接受 |
| 指示器 DOTween 叠加 | 每实例单 tween 字段引用、触发前 Kill（4D 教训） |
| 坑 61/65 | 本包不碰静态碰撞、不重烘焙 NavMesh |
| 坑 75/76 头部碰撞体 | 视线 Linecast 会命中敌人头球（判遮挡）——合理行为不特判；血条挂根不挂 Head 骨骼 |
| 侧移走出 NavMesh | NavMeshAgent 移动天然贴网格；侧移方向贴边时 agent 自动截断，无需特判 |
| execute_code 坑 70~74/81 | CodeDom：UnityEngine.Object 全限定 / Random 全限定 / 新类型经 GetType().Name 匹配 / Play 改 SO 会落盘（坑 82）测后还原 |
| 兜底进发误伤玩法 | reHuntOnSearchTimeout 默认 true 保波次推进；Inspector 可关，留测试开关 |

---

## 6. 整体验收清单（用户 Play 终验）

- [ ] 波次敌人开局从出生点冲向你（Hunt 注入）；蹲掩体静默移动不被远处敌人察觉
- [ ] 敌人视锥/视线成立：掩体后探头即被发现，绕后偷袭可行；开枪会引来 30m 内敌人
- [ ] 丢失视线后敌人搜到你的最后位置并环顾，而不是原地瞬移回巡逻
- [ ] 攻击中的敌人左右横移，不再站桩挨打
- [ ] 敌人受击头顶出血条；玩家被击中时屏幕边缘扇形指示来向
- [ ] 回归：射击/换弹/切枪/ADS/蹲伏/治疗/手雷/暂停/结算/重玩全通过
- [ ] Wave5 峰值流畅（编辑器 ≥60 FPS）；DEVLOG 有 Draw Call 基线
- [ ] 编译 0 错 0 警；DEVLOG 记录完整；explore 独立审查通过

---

## 附录：接手 Agent 提示词（复制即用）

```text
你现在的角色是 coder（Flash 开发者），职责：接收已确认方案 → 实现代码。禁止自行规划、跳过依赖、修改方案。

项目根目录：k:\Unity\UnityProject\AI Test（Unity 2022.3.62 LTS + URP + 新 Input System + Cinemachine 3，波次生存 TPS）。

【开工前必读（按顺序）】
1. AGENTS.md —— 角色权限边界
2. Assets/DEVLOG.md —— 重点：会话 5、阶段 5 实施记录、坑 61/65/67/70~82
3. Assets/DEVELOPMENT_PLAN.md —— 决策 D1~D13、GameEvents 约定（§6.1）、代码规范（§8）
4. Assets/PHASE6_PLAN.md —— 本包唯一实施基准

【当前进度】阶段 5 全部完成并入库；4B 音效挂起（勿动 AudioManager 空 Clip 逻辑）。

【任务】按 PHASE6_PLAN.md §4 的 S1→S6 顺序实施。每步：read_console 0 错 0 警 → 验收自测（Play 实测/execute_code 直调，坑 28）→ 截图 → DEVLOG 追加 → git 提交。

【硬性约束】
1. 决策 D1~D13 不可更改；GameEvents 只增不改签名；订阅 OnEnable/OnDisable 配对退订
2. 禁止新增 Find 系列调用；新引用一律 [SerializeField]
3. S1 重构必须行为等价（§2.1 迁移对照表逐项回归）；重构步不加新功能
4. EnemyBase 新字段不得加 Start 重置（坑 67）；huntPlayer 删除后全局搜索确认无残留
5. 感知 raycast 必须 0.2s tick 节流；DOTween 序列存字段触发前 Kill
6. 不动静态碰撞、不重烘焙 NavMesh（坑 61/65）
7. 每步截图（manage_camera）交用户确认后再进下一步
```
