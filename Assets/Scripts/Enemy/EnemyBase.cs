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

    private float _currentHealth;
    private NavMeshAgent _navAgent;
    private Transform _playerTransform;
    private PlayerHealth _playerHealth;
    private int _currentPatrolIndex;
    private float _lastAttackTime;
    private bool _isDead;
    private float _lastLogTime; // 状态日志节流计时（每 0.5 秒输出一次）

    private void Start()
    {
        _currentHealth = maxHealth;
        _navAgent = GetComponent<NavMeshAgent>();

        // 初始化为负值，避免首帧攻击要等 1 秒冷却
        _lastAttackTime = -attackCooldown;

        // 玩家查找依赖 "Player" Tag，主流程会给玩家设置该 Tag
        var player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            _playerTransform = player.transform;
            _playerHealth = player.GetComponent<PlayerHealth>();
        }
    }

    private void Update()
    {
        if (_isDead)
        {
            return;
        }

        // 状态日志每 0.5 秒输出一次，避免刷屏；攻击/追击日志仍每次输出
        if (Time.time - _lastLogTime >= 0.5f)
        {
            _lastLogTime = Time.time;
            float dist = _playerTransform != null ? Vector3.Distance(transform.position, _playerTransform.position) : -1f;
            Debug.Log($"[{gameObject.name}] 距离玩家={dist:F1}m | 状态={GetCurrentState(dist)} | 血量={_currentHealth}");
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

    /// <summary>根据与玩家距离返回当前行为状态字符串，用于日志</summary>
    private string GetCurrentState(float dist)
    {
        if (_playerTransform == null || dist < 0f)
        {
            return "无目标";
        }
        if (dist <= attackRange)
        {
            return "攻击";
        }
        if (dist <= detectRange)
        {
            return "追击";
        }
        return "巡逻";
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
        _navAgent.speed = chaseSpeed;
        _navAgent.SetDestination(_playerTransform.position);

        float dist = Vector3.Distance(transform.position, _playerTransform.position);
        Debug.Log($"[{gameObject.name}] 追击玩家！距离={dist:F1}m");
    }

    /// <summary>攻击玩家：攻击时停下，冷却结束后对玩家造成伤害</summary>
    private void AttackPlayer()
    {
        if (_navAgent != null)
        {
            // 用 isStopped 停住 NavMeshAgent，代替 SetDestination(自身)，避免路径重算阻塞
            _navAgent.isStopped = true;
            _navAgent.velocity = Vector3.zero;
        }

        if (Time.time - _lastAttackTime < attackCooldown)
        {
            return;
        }

        // 玩家已死亡时停止攻击（血量归零后敌人不再输出攻击日志）
        if (_playerHealth == null || _playerHealth.CurrentHealth <= 0f)
        {
            return;
        }

        _lastAttackTime = Time.time;

        // 上面已确认 _playerHealth 非空且存活，直接造成伤害
        _playerHealth.TakeDamage(attackDamage);
        Debug.Log($"[{gameObject.name}] 攻击！造成 {attackDamage} 伤害 | 玩家剩余血量={_playerHealth.GetHealthPercent() * 100:F0}%");
    }

    /// <summary>受击扣血，血量归零触发死亡</summary>
    public void TakeDamage(float amount)
    {
        if (_isDead)
        {
            return;
        }

        _currentHealth -= amount;
        Debug.Log($"<color=red>💥 {gameObject.name} 受到 {amount} 伤害！剩余血量 {_currentHealth}</color>");

        if (_currentHealth <= 0f)
        {
            Die();
        }
    }

    /// <summary>死亡：记录日志并延迟销毁</summary>
    private void Die()
    {
        if (_isDead)
        {
            return;
        }

        _isDead = true;
        Debug.Log("敌人死亡");

        if (_navAgent != null)
        {
            _navAgent.ResetPath();
        }

        Destroy(gameObject, 0.5f);
    }
}
