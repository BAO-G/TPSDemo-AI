using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 敌人基类：管理血量、NavMesh 移动、简单巡逻与追击攻击
/// 挂载时需同时添加 NavMeshAgent 组件，场景需有烘焙的 NavMesh
/// </summary>
public class EnemyBase : MonoBehaviour
{
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

    private float _currentHealth;
    private NavMeshAgent _navAgent;
    private Transform _playerTransform;
    private PlayerHealth _playerHealth;
    private int _currentPatrolIndex;
    private float _lastAttackTime;
    private bool _isDead;
    private Animator _animator; // 动画驱动器（可为空，阶段3接入）

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

    private void Update()
    {
        if (_isDead)
        {
            return;
        }

        // 未找到玩家（未设置 Tag）时保持巡逻
        if (_playerTransform == null)
        {
            Patrol();
            return;
        }

        float distance = Vector3.Distance(transform.position, _playerTransform.position);
        if (distance <= attackRange)
        {
            AttackPlayer();
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
            _navAgent.SetDestination(patrolPoints[_currentPatrolIndex].position);
            _currentPatrolIndex = (_currentPatrolIndex + 1) % patrolPoints.Length;
        }
    }

    /// <summary>追击玩家</summary>
    private void ChasePlayer()
    {
        if (_navAgent == null)
        {
            return;
        }

        // 从攻击状态恢复移动（攻击时 isStopped=true，若不移除会一直卡在原地）
        _navAgent.isStopped = false;
        _navAgent.updateRotation = true;    // 把转向权还给 NavMeshAgent
        _navAgent.speed = chaseSpeed;
        _navAgent.SetDestination(_playerTransform.position);
    }

    /// <summary>攻击玩家：停下 → 转身面向玩家 → 冷却结束后从胸口向玩家胸口 Raycast（决策 D1），命中才扣血（掩体天然挡弹）</summary>
    private void AttackPlayer()
    {
        if (_navAgent != null)
        {
            // 用 isStopped 停住 NavMeshAgent，代替 SetDestination(自身)，避免路径重算阻塞
            _navAgent.isStopped = true;
            _navAgent.velocity = Vector3.zero;
            // NavMeshAgent 只在移动时自动转向；站桩攻击必须接管转向，否则模型背对玩家开枪
            if (_navAgent.updateRotation)
                _navAgent.updateRotation = false;
        }

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

        // Raycast 即时弹道：胸口 → 玩家胸口，accuracy 越低弹道偏转越大
        Vector3 origin = transform.position + Vector3.up * 1.5f;
        Vector3 target = _playerTransform.position + Vector3.up * 1.2f;
        Vector3 dir = (target - origin).normalized;
        float spreadDeg = (1f - Mathf.Clamp01(accuracy)) * 15f;
        dir = Quaternion.Euler(
            Random.Range(-spreadDeg, spreadDeg),
            Random.Range(-spreadDeg, spreadDeg),
            0f) * dir;

        float range = attackRange + 3f;
        if (Physics.Raycast(origin, dir, out RaycastHit hit, range))
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
            GameEvents.RaiseBulletTrail(origin, origin + dir * range);
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
