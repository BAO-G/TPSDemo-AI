using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 手雷投掷物（4E）：Rigidbody 抛物线 + 落地弹跳 + 引信计时 → 范围伤害
/// 只负责逻辑（引信/伤害/回池）；爆炸特效与震屏由 CombatEffectsController 订阅 OnExplosion 表现
/// 走 ObjectPool 池化；爆炸半径内敌人/玩家均受伤害（按距离线性衰减）
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class Grenade : MonoBehaviour
{
    [Header("引信")]
    public float fuseTime = 5f;                     // 投出后到爆炸的时间（秒）

    [Header("范围伤害")]
    public float explosionRadius = 5f;              // 爆炸半径
    public float maxDamage = 100f;                  // 爆心最大伤害
    [Range(0f, 1f)] public float edgeDamageFactor = 0.3f;  // 半径边缘伤害系数（线性衰减到边缘）

    private float _fuseTimer;
    private bool _exploded;
    private Rigidbody _rb;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        // 从池中取出时重置引信与拖尾（池化对象状态必须干净）
        _fuseTimer = fuseTime;
        _exploded = false;
        var trail = GetComponent<TrailRenderer>();
        if (trail != null) trail.Clear();
    }

    private void Update()
    {
        if (_exploded) return;
        _fuseTimer -= Time.deltaTime;
        if (_fuseTimer <= 0f)
            Explode();
    }

    /// <summary>
    /// 碰撞手感：碰到敌人快速卸力，让手雷落在敌人脚边而非弹飞；
    /// 一般碰撞的低弹性由 GrenadeBounce 物理材质（bounciness 0.3 / Average）控制
    /// </summary>
    private void OnCollisionEnter(Collision collision)
    {
        if (_rb == null) return;
        var enemy = collision.collider.GetComponentInParent<EnemyBase>();
        if (enemy != null)
        {
            _rb.velocity *= 0.15f;
            _rb.angularVelocity *= 0.3f;
        }
    }

    /// <summary>爆炸：范围伤害（敌/我，去重）→ 广播 OnExplosion → 回池</summary>
    public void Explode()
    {
        if (_exploded) return;
        _exploded = true;
        Vector3 pos = transform.position;

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

        // 表现层（特效/震屏/音效 4B）订阅此事件，逻辑层不直接依赖
        GameEvents.RaiseExplosion(pos);

        ObjectPool.Release(gameObject);
    }
}
