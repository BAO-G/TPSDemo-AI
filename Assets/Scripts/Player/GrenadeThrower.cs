using System.Collections;
using UnityEngine;

/// <summary>
/// 手雷投掷（4E）：G 键 → 播 toss 动画（UpperBody 层 Toss 状态）→ 出手延迟后生成手雷并赋初速
/// 初速 = 视线方向×10 + 向上×7（计划 §5.5 参照项目验证参数）
/// 初始 3 枚；数量变化广播 GameEvents.OnGrenadeCountChanged（UI 订阅显示）
/// 手雷走 ObjectPool 池化
/// </summary>
public class GrenadeThrower : MonoBehaviour
{
    [Header("手雷配置")]
    public GameObject grenadePrefab;                // 手雷预制体（Grenade 组件 + Rigidbody）
    public int initialGrenades = 3;                 // 初始手雷数
    public int maxGrenades = 3;                     // 携带上限（阶段5 拾取物用）

    [Header("投掷参数")]
    public float throwDelay = 0.9f;                 // toss 起手到出手的延迟（3.2s 动画中出手瞬间，见 DEVLOG）
    public float throwForwardSpeed = 12f;           // 初速：瞄点方向分量
    public float throwUpSpeed = 5f;                 // 初速：向上分量（弧线平直，方向可读）
    public float throwOriginHeight = 1.5f;          // 出手点高度（默认玩家胸口）
    public float aimRaycastRange = 60f;             // 准星瞄点射线距离（未命中取该距离处为瞄点）
    public float spinSpeed = 12f;                   // 飞行自转速度（视觉追踪）

    [Header("引用（场景序列化，禁止 Find 系列新增）")]
    public Transform aimCamera;                     // TPSCamera，取视线方向
    [SerializeField] private PlayerInputHandler _inputHandler;

    /// <summary>当前手雷数量（UI 读取）</summary>
    public int GrenadeCount { get; private set; }

    private Animator _animator;
    private Coroutine _throwRoutine;
    private bool _isDead;

    private void Start()
    {
        GrenadeCount = initialGrenades;
        _animator = GetComponentInChildren<Animator>();

        if (_inputHandler == null)
            _inputHandler = GetComponent<PlayerInputHandler>();

        var health = GetComponent<PlayerHealth>();
        if (health != null)
            health.OnDeath += OnPlayerDeath;

        if (grenadePrefab != null)
            ObjectPool.Prewarm(grenadePrefab, 2);

        // 通知 UI 初始数量
        GameEvents.RaiseGrenadeCountChanged(GrenadeCount);
    }

    private void OnDestroy()
    {
        var health = GetComponent<PlayerHealth>();
        if (health != null)
            health.OnDeath -= OnPlayerDeath;
    }

    private void Update()
    {
        if (_isDead) return;
        if (_inputHandler != null && _inputHandler.IsGrenadePressed())
            TryThrow();
    }

    /// <summary>投掷：扣数量 → 播动画 → 延迟出手；投掷中不可连投</summary>
    public void TryThrow()
    {
        if (GrenadeCount <= 0 || _throwRoutine != null) return;

        GrenadeCount--;
        GameEvents.RaiseGrenadeCountChanged(GrenadeCount);

        if (_animator != null)
            _animator.SetTrigger("Toss");

        _throwRoutine = StartCoroutine(ThrowRoutine());
    }

    /// <summary>增加手雷（拾取物/阶段5 用），广播数量变化</summary>
    public void AddGrenades(int count)
    {
        GrenadeCount = Mathf.Min(GrenadeCount + count, maxGrenades);
        GameEvents.RaiseGrenadeCountChanged(GrenadeCount);
    }

    private IEnumerator ThrowRoutine()
    {
        yield return new WaitForSeconds(throwDelay);
        Throw();
        _throwRoutine = null;
    }

    private void Throw()
    {
        if (grenadePrefab == null) return;

        Vector3 origin = transform.position + Vector3.up * throwOriginHeight;

        // 瞄点投掷（所见即所投）：与 CrosshairController 同源，从准星（主相机屏幕中心）发射线，
        // 命中点即投掷目标；未命中（朝天）取视线 60m 处。参考 CS 的准星指向投掷
        Vector3 aimDir = aimCamera != null ? aimCamera.forward : transform.forward;
        Vector3 target = origin + aimDir * aimRaycastRange;
        var cam = Camera.main;
        if (cam != null)
        {
            var ray = new Ray(cam.transform.position, cam.transform.forward);
            if (Physics.Raycast(ray, out var hit, aimRaycastRange, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                target = hit.point;
        }
        Vector3 dir = (target - origin).normalized;

        // 出手点沿投掷方向前移 0.5m，避免与自身胶囊碰撞
        origin += dir * 0.5f;

        var go = ObjectPool.Get(grenadePrefab, origin, Quaternion.identity);
        if (go == null) return;

        var rb = go.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = dir * throwForwardSpeed + Vector3.up * throwUpSpeed;
            rb.angularVelocity = UnityEngine.Random.insideUnitSphere * spinSpeed;  // 自转便于肉眼追踪
        }
    }

    private void OnPlayerDeath()
    {
        _isDead = true;
        if (_throwRoutine != null)
        {
            StopCoroutine(_throwRoutine);
            _throwRoutine = null;
        }
    }
}
