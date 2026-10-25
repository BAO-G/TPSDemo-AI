using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 敌人基类（阶段6 状态机）：血量、NavMesh 移动、状态机驱动的行为（PHASE6_PLAN §2.1/§2.4）
///   Patrol  巡逻路点/原地待命（散装守卫用）
///   Hunt    波次寻敌（D8）：只冲出生时注入的目标点，到达后进 Search；不追踪玩家实时位置
///   Chase   追击玩家（视觉确认中）
///   Attack  交战距离纪律（S4）：过近后撤/偏好带内侧移/超程追击 + 转向开火（决策 D1）；视觉门控（掩体后不开枪）
///   Search  调查：跑向最后已知位置 → 原地环顾 searchDuration → 期间听见/看见恢复追击；超时守卫回 Patrol、波次敌人兜底重新进发（reHuntOnSearchTimeout）
///   Dead    死亡静置期
/// 行为分层（D8）：看见→Chase/Attack；听到（噪音半径内）→Search 调查
/// 挂载时需同时添加 NavMeshAgent 组件，场景需有烘焙的 NavMesh
/// </summary>
public class EnemyBase : MonoBehaviour
{
    /// <summary>敌人状态（阶段6 轻量状态机，D9）</summary>
    public enum EnemyState { Patrol, Hunt, Chase, Attack, Search, Dead }

    [Header("血量参数")]
    public float maxHealth = 100f;

    [Header("战斗参数")]
    public float detectRange = 20f;      // 检测玩家范围
    public float attackRange = 15f;      // 攻击范围
    public float attackDamage = 3f;      // 每次攻击伤害
    public float attackCooldown = 1f;    // 攻击间隔（秒）

    [Header("巡逻参数")]
    public Transform[] patrolPoints;     // 巡逻路点数组（可为空，空则原地待命）
    public float patrolSpeed = 3f;       // 巡逻速度
    public float chaseSpeed = 6f;        // 追击速度

    [Header("远程攻击(4C新增)")]
    [Range(0f, 1f)] public float accuracy = 0.7f;  // 命中率（越低散布角越大）
    public float corpseDuration = 10f;             // 尸体保留时长（秒）
    public float attackTurnSpeed = 360f;           // 攻击站桩时朝向玩家的转速（度/秒）

    [Header("部位(4C新增)")]
    public float headshotMultiplier = 2f;          // 爆头伤害倍率（FirearmWeapon 读取）

    [Header("搜索参数(阶段6 S3/S4)")]
    public float searchDuration = 5f;              // 到达搜索区域后的环顾时长（秒）
    public float searchTurnSpeed = 90f;            // 环顾转速（度/秒）
    public bool reHuntOnSearchTimeout = true;      // 波次敌人兜底（D8）：搜索超时后向最后已知情报区域重新进发，防全程隐身卡关
    public float reHuntMinOffset = 5f;             // reHunt 目标距情报锚点的最小偏移（米）
    public float reHuntMaxOffset = 10f;            // reHunt 目标距情报锚点的最大偏移（米）

    [Header("交战参数(阶段6 S4)")]
    public float engageMinDistance = 6f;           // 偏好交战距离下限（每敌 Start 随机取值，交战距离分散治贴脸）
    public float engageMaxDistance = 13f;          // 偏好交战距离上限
    public float strafeSpeed = 2f;                 // 交战侧移速度
    public float strafeSwitchInterval = 0.8f;      // 侧移换向间隔（秒）
    public float backOffSpeed = 2.5f;              // 过近后撤速度

    [Header("寻路范围参数(阶段6 用户反馈：目的地松散化)")]
    public float huntSpreadMin = 2f;               // 开波进发：注入点周围随机落点最小半径（米）
    public float huntSpreadMax = 5f;               // 开波进发：注入点周围随机落点最大半径（米）
    public float searchSpreadMin = 4f;             // 搜索接近：情报点周围随机落点最小半径（米）
    public float searchSpreadMax = 8f;             // 搜索接近：情报点周围随机落点最大半径（米）

    private float _currentHealth;
    private NavMeshAgent _navAgent;
    private Transform _playerTransform;
    private PlayerHealth _playerHealth;
    private int _currentPatrolIndex;
    private float _lastAttackTime;
    private bool _isDead;
    private Animator _animator; // 动画驱动器（可为空，阶段3接入）

    // ===== 阶段6 状态机（D8/D9）=====
    // 初始目标为纯运行时字段且无 Start 重置（坑 67：生成器 Instantiate 后同帧直赋，Start 重置会覆盖）
    private EnemyState _state = EnemyState.Patrol;
    private Vector3 _initialHuntTarget;    // Hunt 情报锚点：波次出生时刻的玩家位置（D8；兜底时刷新为最后已知情报位置）
    private Vector3 _huntDestination;      // Hunt 实际寻路目的地（锚点周围松散落点，用户反馈：不寻路到确切位置）
    private bool _hasInitialTarget;
    private EnemyPerception _perception;   // 感知组件（S2；prefab 未挂时回退纯距离判定，保持兼容）
    private bool _searchLooking;           // Search 子阶段：false=跑向搜索区域，true=原地环顾
    private float _searchTimer;
    private Vector3 _searchDestination;    // Search 实际寻路目的地（情报点周围松散落点）
    private Vector3 _searchDestAnchor;     // 搜索目的地对应的情报锚点（情报移动过大时重选落点）

    // ===== S4 交战距离纪律（每敌随机，运行时生成无注入问题）=====
    private float _preferredEngage;        // 本敌偏好交战距离
    private int _strafeDir = 1;            // 侧移方向
    private float _strafeTimer;
    private readonly RaycastHit[] _fireHitBuffer = new RaycastHit[8];   // 开火射线非分配缓冲（跳过自身）

    /// <summary>当前状态（调试/测试观测用）</summary>
    public EnemyState State => _state;
    /// <summary>是否已死亡（波次生成器存活判定用——尸体延迟销毁期间 activeInHierarchy 仍为 true，不可用）</summary>
    public bool IsDead => _isDead;

    /// <summary>
    /// 波次生成器注入 Hunt 情报锚点（阶段6 D8，替代已删除的 huntPlayer 字段）：实例化后同帧直赋安全。
    /// 语义：敌人进发到锚点周围 huntSpread 环带的松散落点（不寻路到确切位置），到达后进 Search 环顾；
    /// 搜索超时按 reHuntOnSearchTimeout 向最后已知情报区域重新进发。
    /// </summary>
    public void SetInitialHuntTarget(Vector3 pos)
    {
        BeginHunt(pos, huntSpreadMin, huntSpreadMax);
    }

    /// <summary>开始一段 Hunt：锚点周围随机松散落点为寻路目的地（用户反馈：目的地范围化）</summary>
    private void BeginHunt(Vector3 anchor, float minR, float maxR)
    {
        _initialHuntTarget = anchor;
        _huntDestination = PickLoosePoint(anchor, minR, maxR);
        _hasInitialTarget = true;
        if (!_isDead)
            _state = EnemyState.Hunt;
    }

    private void Start()
    {
        _currentHealth = maxHealth;
        _navAgent = GetComponent<NavMeshAgent>();
        _animator = GetComponentInChildren<Animator>();

        // 初始化为负值，避免首帧攻击要等 1 秒冷却
        _lastAttackTime = -attackCooldown;

        // 玩家查找依赖 "Player" Tag，主流程会给玩家设置该 Tag
        var player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            _playerTransform = player.transform;
            _playerHealth = player.GetComponent<PlayerHealth>();
        }

        // 感知组件（S2/S3）：玩家引用与噪音发射器由代码注入（prefab 不能持场景引用）
        _perception = GetComponent<EnemyPerception>();
        if (_perception != null && _playerTransform != null)
            _perception.Init(_playerTransform, _playerTransform.GetComponent<PlayerNoiseEmitter>());

        // S4：每敌随机偏好交战距离（交战距离分散，杜绝全员贴脸）
        _preferredEngage = Random.Range(engageMinDistance, engageMaxDistance);
        _strafeTimer = strafeSwitchInterval;

        SetupHeadPart();
    }

    /// <summary>头部部位碰撞体（4C 部位伤害）：人形 Avatar 时在 Head 骨骼挂球形碰撞体 + EnemyPart 标记
    /// 同时收缩根部胶囊体——默认 2m 胶囊把头部包在内部，射线先命中胶囊导致爆头判定失效（4D 热修复）
    /// 二次修复（4D 实测）：胶囊 1.4 + 头球 r=0.22 球心偏移 +0.08 对准视觉头骨（首版 r=0.18 无偏移时，
    /// 下颌段被胶囊遮挡、头顶段超出球体，可判头区域仅 ~1.5~1.77 一条窄带，实战几乎无法触发爆头）</summary>
    private void SetupHeadPart()
    {
        var bodyCap = GetComponent<CapsuleCollider>();
        if (bodyCap != null)
        {
            bodyCap.height = 1.4f;
            bodyCap.center = new Vector3(0f, 0.7f, 0f);
        }

        if (_animator == null || _animator.avatar == null || !_animator.avatar.isHuman)
            return;
        var head = _animator.GetBoneTransform(HumanBodyBones.Head);
        if (head == null || head.GetComponent<EnemyPart>() != null)
            return;
        var col = head.gameObject.AddComponent<SphereCollider>();
        col.center = new Vector3(0f, 0.08f, 0f);   // Head 骨骼枢轴在颈部，球心上移对准头骨
        col.radius = 0.22f;
        var part = head.gameObject.AddComponent<EnemyPart>();
        part.isHead = true;
    }

    /// <summary>是否看见玩家：有感知组件用视觉判定（视锥+视线），未挂时回退旧版纯距离判定保持兼容</summary>
    private bool SeesPlayer(float distance)
    {
        return _perception != null ? _perception.CanSeePlayer : distance <= detectRange;
    }

    private void Update()
    {
        if (_isDead)
        {
            return;
        }

        // 未找到玩家（未设置 Tag）时保持巡逻（与旧版一致）
        if (_playerTransform == null)
        {
            _state = EnemyState.Patrol;
            Patrol();
            return;
        }

        float distance = Vector3.Distance(transform.position, _playerTransform.position);
        bool sees = SeesPlayer(distance);
        bool heard = _perception != null && _perception.HeardPlayer;

        // 状态机主循环：行为分层（D8）——看见→Chase/Attack；听到→Search 调查；波次敌人 Hunt 只冲注入点
        switch (_state)
        {
            case EnemyState.Patrol:
                if (distance <= attackRange && sees)
                {
                    SetState(EnemyState.Attack);
                    AttackUpdate(distance);
                }
                else if (sees)
                {
                    SetState(EnemyState.Chase);
                    ChasePlayer();
                }
                else if (heard)
                {
                    // 听到动静：前往声源最后已知位置调查（不直接追击）
                    EnterSearch();
                    ChaseToPosition(_perception.LastKnownPlayerPos);
                }
                else if (_hasInitialTarget)
                {
                    // 波次敌人兜底：绝不滞留巡逻（S3 起为冲注入点语义）
                    SetState(EnemyState.Hunt);
                    ChaseToPosition(_initialHuntTarget);
                }
                else
                {
                    Patrol();
                }
                break;

            case EnemyState.Hunt:
                if (distance <= attackRange && sees)
                {
                    SetState(EnemyState.Attack);
                    AttackUpdate(distance);
                }
                else if (sees)
                {
                    SetState(EnemyState.Chase);
                    ChasePlayer();
                }
                else if (heard)
                {
                    // 听到动静：转入搜索（松散接近声源区域）
                    EnterSearch();
                }
                else
                {
                    // D8：只进发到注入点周围的松散落点（_huntDestination），不追踪玩家实时位置
                    ChaseToPosition(_huntDestination);
                    if (!_navAgent.pathPending &&
                        HorizontalDistance(transform.position, _huntDestination) <= _navAgent.stoppingDistance + 1.5f)
                        EnterSearch();
                }
                break;

            case EnemyState.Chase:
                if (distance <= attackRange && sees)
                {
                    SetState(EnemyState.Attack);
                    AttackUpdate(distance);
                }
                else if (sees)
                {
                    ChasePlayer();
                }
                else if (_perception == null)
                {
                    // 无感知组件回退：脱离检测距离即回巡逻（等价旧版）
                    SetState(EnemyState.Patrol);
                    Patrol();
                }
                else
                {
                    // 丢失视线：立即转入搜索（松散接近最后已知区域，不再精确寻路到脚下）
                    EnterSearch();
                }
                break;

            case EnemyState.Attack:
                if (!sees)
                {
                    // 丢失视线（掩体后）→ 直接转搜索，不再站桩隔墙开枪、也不精确寻路追脚下
                    EnterSearch();
                }
                else if (distance > attackRange)
                {
                    SetState(EnemyState.Chase);
                    ChasePlayer();
                }
                else
                {
                    AttackUpdate(distance);
                }
                break;

            case EnemyState.Search:
                if (sees)
                {
                    // 环顾/途中再发现玩家 → 恢复追击链
                    _searchLooking = false;
                    if (distance <= attackRange)
                    {
                        SetState(EnemyState.Attack);
                        AttackUpdate(distance);
                    }
                    else
                    {
                        SetState(EnemyState.Chase);
                        ChasePlayer();
                    }
                    break;
                }
                if (heard)
                {
                    // 搜索期间听到新动静：重新跑向新声源区域（锚点偏移超阈值时移动阶段会重选落点）
                    _searchLooking = false;
                    _searchTimer = searchDuration;
                }

                if (!_searchLooking)
                {
                    // 跑向搜索区域（情报点周围松散落点，用户反馈：目的地范围化）
                    Vector3 anchor = GetSearchAnchor();
                    if (HorizontalDistance(anchor, _searchDestAnchor) > 3f)
                    {
                        _searchDestAnchor = anchor;
                        _searchDestination = PickLoosePoint(anchor, searchSpreadMin, searchSpreadMax);
                    }
                    ChaseToPosition(_searchDestination);
                    if (!_navAgent.pathPending &&
                        HorizontalDistance(transform.position, _searchDestination) <= _navAgent.stoppingDistance + 0.6f)
                    {
                        _searchLooking = true;
                        _searchTimer = searchDuration;
                    }
                }
                else
                {
                    // 到达：原地环顾（站桩慢速旋转）
                    if (_navAgent != null && !_navAgent.isStopped)
                    {
                        _navAgent.isStopped = true;
                        _navAgent.velocity = Vector3.zero;
                        _navAgent.updateRotation = false;   // 接管转向做环顾
                    }
                    transform.Rotate(0f, searchTurnSpeed * Time.deltaTime, 0f);
                    _searchTimer -= Time.deltaTime;
                    if (_searchTimer <= 0f)
                    {
                        _searchLooking = false;
                        if (_hasInitialTarget && reHuntOnSearchTimeout)
                        {
                            // D8 兜底：向最后已知情报区域重新进发（锚点周围 5~10m 松散落点）
                            BeginHunt(GetSearchAnchor(), reHuntMinOffset, reHuntMaxOffset);
                        }
                        else
                        {
                            SetState(EnemyState.Patrol);
                            Patrol();
                        }
                    }
                }
                break;
        }
    }

    /// <summary>搜索锚点：有情报=最后已知玩家位置；无情报=当前位置（就近搜索，绝不使用实时位置/世界原点）</summary>
    private Vector3 GetSearchAnchor()
    {
        return _perception != null && _perception.HasIntel ? _perception.LastKnownPlayerPos : transform.position;
    }

    /// <summary>进入搜索：跑向情报点周围松散落点，到达后原地环顾 searchDuration 秒</summary>
    private void EnterSearch()
    {
        SetState(EnemyState.Search);
        _searchLooking = false;
        _searchTimer = searchDuration;
        _searchDestAnchor = GetSearchAnchor();
        _searchDestination = PickLoosePoint(_searchDestAnchor, searchSpreadMin, searchSpreadMax);
    }

    /// <summary>
    /// 松散落点：锚点周围 minR~maxR 环带随机取点并吸附 NavMesh（失败则用原始偏移点）。
    /// 所有以玩家情报为来源的寻路目的地都必须经此转换——禁止以确切位置为终点（用户反馈：堵门/贴脸根因）
    /// </summary>
    private Vector3 PickLoosePoint(Vector3 anchor, float minR, float maxR)
    {
        Vector2 circle = UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(minR, maxR);
        Vector3 raw = anchor + new Vector3(circle.x, 0f, circle.y);
        if (NavMesh.SamplePosition(raw, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            return hit.position;
        return raw;
    }

    private void SetState(EnemyState next)
    {
        if (_state == next)
            return;
        _state = next;
    }

    /// <summary>水平（XZ）距离：到达判定专用——lastKnown/注入点可能记录在高处（传送未落地等），y 差不应阻塞到达</summary>
    private static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        var d = a - b;
        d.y = 0f;
        return d.magnitude;
    }

    /// <summary>用实际移动速度驱动动画 Blend Tree（Speed 参数）</summary>
    private void LateUpdate()
    {
        if (_animator != null && _navAgent != null)
        {
            _animator.SetFloat("Speed", _isDead ? 0f : _navAgent.velocity.magnitude);
            // NavMeshAgent 始终贴地，直接置为 true 让 Jump 状态正常退出
            _animator.SetBool("Grounded", true);
        }
    }

    /// <summary>循环遍历巡逻路点；无路点或没有 NavMeshAgent 时原地待命</summary>
    private void Patrol()
    {
        if (_navAgent == null)
        {
            return;
        }

        // 从攻击状态恢复移动
        _navAgent.isStopped = false;
        _navAgent.updateRotation = true;    // 把转向权还给 NavMeshAgent

        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            // 无路点：停下等待
            if (_navAgent.hasPath)
            {
                _navAgent.ResetPath();
            }
            return;
        }

        _navAgent.speed = patrolSpeed;

        // 路径计算中时跳过，避免重复设点抖动
        if (_navAgent.pathPending)
        {
            return;
        }

        // 无路径（出生或刚到达）时切到下一个路点，实现循环巡逻
        if (!_navAgent.hasPath || _navAgent.remainingDistance <= _navAgent.stoppingDistance)
        {
            // 环形跳过 null 路点元素（防御 prefab 断链置空的数据形态），全空视为无路点
            Transform next = null;
            for (int i = 0; i < patrolPoints.Length; i++)
            {
                var cand = patrolPoints[(_currentPatrolIndex + i) % patrolPoints.Length];
                if (cand != null)
                {
                    next = cand;
                    _currentPatrolIndex = (_currentPatrolIndex + i + 1) % patrolPoints.Length;
                    break;
                }
            }
            if (next == null)
            {
                if (_navAgent.hasPath)
                {
                    _navAgent.ResetPath();
                }
                return;
            }
            _navAgent.SetDestination(next.position);
        }
    }

    /// <summary>追击玩家</summary>
    private void ChasePlayer()
    {
        ChaseToPosition(_playerTransform.position);
    }

    /// <summary>向指定点机动（追击/追最后已知位置共用）：恢复移动 + 把转向权还给 NavMeshAgent</summary>
    private void ChaseToPosition(Vector3 target)
    {
        if (_navAgent == null)
        {
            return;
        }

        // 从攻击状态恢复移动（攻击时 isStopped=true，若不移除会一直卡在原地）
        _navAgent.isStopped = false;
        _navAgent.updateRotation = true;    // 把转向权还给 NavMeshAgent
        _navAgent.speed = chaseSpeed;
        _navAgent.SetDestination(target);
    }

    /// <summary>攻击行为（S4 交战距离纪律）：过近后撤、偏好带内侧移（不再站桩贴脸），转向与开火与移动解耦</summary>
    private void AttackUpdate(float distance)
    {
        if (_navAgent != null)
        {
            if (_navAgent.updateRotation)
                _navAgent.updateRotation = false;   // 朝向由 FaceAndFire 手动控制

            if (distance < _preferredEngage - 0.5f)
            {
                // 过近：后撤拉开距离（用户反馈核心——敌人不再赖在脸上）
                Vector3 away = transform.position - _playerTransform.position;
                away.y = 0f;
                away = away.sqrMagnitude > 0.001f ? away.normalized : Vector3.back;
                _navAgent.isStopped = false;
                _navAgent.speed = backOffSpeed;
                _navAgent.SetDestination(transform.position + away * 3f);
            }
            else
            {
                // 偏好带内：左右横移（S4 侧移射击），换向带随机防整齐划一
                _strafeTimer -= Time.deltaTime;
                if (_strafeTimer <= 0f)
                {
                    _strafeDir = -_strafeDir;
                    _strafeTimer = strafeSwitchInterval + Random.Range(0f, 0.4f);
                }
                Vector3 toPlayer = _playerTransform.position - transform.position;
                toPlayer.y = 0f;
                Vector3 side = toPlayer.sqrMagnitude > 0.001f
                    ? Vector3.Cross(Vector3.up, toPlayer.normalized) * _strafeDir
                    : Vector3.right * _strafeDir;
                _navAgent.isStopped = false;
                _navAgent.speed = strafeSpeed;
                _navAgent.SetDestination(transform.position + side * 2f);
            }
        }

        FaceAndFire();
    }

    /// <summary>转向玩家 + 冷却冷却弹道开火（原 AttackPlayer 非移动部分）；射线跳过自身碰撞体</summary>
    private void FaceAndFire()
    {
        // 每帧水平转向玩家（在冷却判断之前，保证转动连续）
        if (_playerTransform != null)
        {
            Vector3 toPlayer = _playerTransform.position - transform.position;
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(toPlayer);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, attackTurnSpeed * Time.deltaTime);
            }
        }

        if (Time.time - _lastAttackTime < attackCooldown)
        {
            return;
        }

        // 玩家已死亡时停止攻击（血量归零后敌人不再输出）
        if (_playerHealth == null || _playerHealth.CurrentHealth <= 0f)
        {
            return;
        }

        _lastAttackTime = Time.time;

        // 播放射击动画
        if (_animator != null)
        {
            _animator.SetTrigger("Fire");
        }

        // 广播敌人开枪事件（音效/受击方向订阅）
        GameEvents.RaiseEnemyShot(transform.position);

        // Raycast 即时弹道（决策 D1）：胸口 → 玩家低胸位，accuracy 越低散布越大；
        // NonAlloc + 跳过自身碰撞体——动画中头球可能包住射线起点，普通 Raycast 会自挡（诊断实录）
        Vector3 origin = transform.position + Vector3.up * 1.5f;
        Vector3 target = _playerTransform.position + Vector3.up * 0.5f;
        Vector3 dir = target - origin;
        float maxDist = attackRange + 3f;
        if (dir.magnitude < 0.05f)
            return;
        dir.Normalize();
        float spreadDeg = (1f - Mathf.Clamp01(accuracy)) * 15f;
        dir = Quaternion.Euler(
            Random.Range(-spreadDeg, spreadDeg),
            Random.Range(-spreadDeg, spreadDeg),
            0f) * dir;

        int count = Physics.RaycastNonAlloc(origin, dir, _fireHitBuffer, maxDist);
        RaycastHit hit = default(RaycastHit);
        bool hasHit = false;
        for (int i = 0; i < count; i++)
        {
            if (_fireHitBuffer[i].collider.transform.IsChildOf(transform))
                continue;   // 自身碰撞体不算命中
            hit = _fireHitBuffer[i];
            hasHit = true;
            break;
        }

        if (hasHit)
        {
            // 命中：曳光到命中点，命中玩家才扣血（打墙/掩体无效）
            GameEvents.RaiseBulletTrail(origin, hit.point);
            var hitPlayer = hit.collider.GetComponentInParent<PlayerHealth>();
            if (hitPlayer != null)
                hitPlayer.TakeDamage(attackDamage);
        }
        else
        {
            // 未命中任何物体：曳光打到射程尽头
            GameEvents.RaiseBulletTrail(origin, origin + dir * maxDist);
        }
    }

    /// <summary>受击扣血，血量归零触发死亡；返回是否被击杀（供爆头判定）</summary>
    public bool TakeDamage(float amount)
    {
        if (_isDead)
        {
            return false;
        }

        _currentHealth -= amount;

        // 广播敌人受击事件（音效订阅）
        GameEvents.RaiseEnemyDamaged(transform.position);

        // 受击时播放 hit reaction 动画（UpperBody 层 HitReaction 状态）
        _animator?.SetTrigger("Hit");
#if UNITY_EDITOR
        Debug.Log($"<color=red>💥 {gameObject.name} 受到 {amount} 伤害！剩余血量 {_currentHealth}</color>");
#endif

        if (_currentHealth <= 0f)
        {
            Die();
            return true;
        }
        return false;
    }

    /// <summary>死亡：播放死亡动画，禁用导航/碰撞，尸体保留 corpseDuration 后销毁</summary>
    private void Die()
    {
        if (_isDead)
        {
            return;
        }

        _isDead = true;
        SetState(EnemyState.Dead);

        // 广播敌人死亡事件（波次生成/击杀统计订阅）
        GameEvents.RaiseEnemyKilled(transform.position);

        // 死亡动画（EnemyAnimator Base 层 Die 状态，Died 触发器）
        if (_animator != null)
        {
            _animator.SetFloat("Speed", 0f);
            _animator.SetTrigger("Died");
        }

        // 禁用导航与全部碰撞体，尸体静置不参与玩法
        if (_navAgent != null)
        {
            _navAgent.isStopped = true;
            _navAgent.enabled = false;
        }
        foreach (var col in GetComponentsInChildren<Collider>())
            col.enabled = false;

        Destroy(gameObject, corpseDuration);
    }
}
