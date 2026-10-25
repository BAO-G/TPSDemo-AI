using UnityEngine;

/// <summary>
/// 敌人感知组件（阶段6 S2，PHASE6_PLAN §2.2）：FOV 视锥 + 视线 Linecast + 节流轮询
/// 与 EnemyBase 同物体挂载，由 EnemyBase.Start 注入玩家引用（prefab 不能持场景引用，禁新增 Find）
/// 行为分层约定（D8）：看见 → Chase/Attack；听到 → Search（S3/S4 接入）
/// </summary>
public class EnemyPerception : MonoBehaviour
{
    [Header("视觉参数")]
    public float sightRange = 20f;                    // 视觉距离（米）
    [Range(10f, 180f)] public float fovAngle = 80f;   // 视锥全角（度，以自身 forward 为轴）

    [Header("节流")]
    public float perceptionTick = 0.2f;               // 感知计算间隔（秒），防 14 敌 raycast 风暴

    private readonly RaycastHit[] _hitBuffer = new RaycastHit[8];   // 非分配射线缓冲（防 GC）

    private Transform _player;
    private PlayerNoiseEmitter _noise;   // 玩家噪音发射器（Init 注入，可为空=无听觉）
    private float _timer;
    private bool _visible;
    private Vector3 _lastKnownPos;
    private float _lastSeenTime;
    private bool _hasSeen;
    private bool _heard;
    private bool _hasIntel;   // 是否获得过任何情报（看见或听到）——Search 锚点选择用，防从未接触时误以 Vector3.zero 为目标

    /// <summary>当前是否看见玩家（视锥内 + 视线通）</summary>
    public bool CanSeePlayer => _visible;
    /// <summary>本 tick 是否听见玩家（距离 < 玩家噪音半径；行为分层：听见→Search，看见→Chase）</summary>
    public bool HeardPlayer => _heard;
    /// <summary>是否获得过任何情报（看见或听到过玩家）</summary>
    public bool HasIntel => _hasIntel;
    /// <summary>最后已知玩家位置（可见或听到时每 tick 刷新）</summary>
    public Vector3 LastKnownPlayerPos => _lastKnownPos;
    /// <summary>上次看见玩家至今的秒数（从未见过为 ∞）</summary>
    public float TimeSinceSeen => _hasSeen ? Time.time - _lastSeenTime : float.PositiveInfinity;

    /// <summary>由 EnemyBase.Start 注入玩家引用与噪音发射器</summary>
    public void Init(Transform player, PlayerNoiseEmitter noise)
    {
        _player = player;
        _noise = noise;
    }

    private void Update()
    {
        if (_player == null)
            return;

        _timer -= Time.deltaTime;
        if (_timer > 0f)
            return;
        _timer = perceptionTick;

        // 听觉：与玩家距离 < 当前噪音半径即听见（含开枪/爆炸的瞬时大半径）
        _heard = false;
        if (_noise != null)
        {
            float noiseRadius = _noise.CurrentNoiseRadius;
            if (noiseRadius > 0f &&
                Vector3.Distance(transform.position, _player.position) < noiseRadius)
            {
                _heard = true;
                _hasIntel = true;
                _lastKnownPos = _player.position;
            }
        }

        _visible = CheckSight();
        if (_visible)
        {
            _lastKnownPos = _player.position;
            _lastSeenTime = Time.time;
            _hasSeen = true;
            _hasIntel = true;
        }
    }

    /// <summary>视觉判定：距离 ≤ sightRange 且玩家在 FOV 视锥内（水平夹角）且视线无遮挡</summary>
    private bool CheckSight()
    {
        Vector3 toPlayer = _player.position - transform.position;
        toPlayer.y = 0f;   // 水平化，避免俯仰角干扰视锥判定
        float dist = toPlayer.magnitude;
        if (dist > sightRange)
            return false;

        if (dist > 0.01f)
        {
            float angle = Vector3.Angle(transform.forward, toPlayer.normalized);
            if (angle > fovAngle * 0.5f)
                return false;
        }

        // 视线：敌人胸口(+1.5m) → 玩家低胸位(脚底+0.5m)。
        // 目标取低位的原因：蹲伏时玩家胶囊仅 0.08~1.08m，取 +1.2m 会从蹲姿头顶掠过打空；
        // 射线用 RaycastNonAlloc + 跳过自身碰撞体——动画中头球可能包住射线起点，
        // 普通.Raycast 会"打中自己头部@0m"而误判遮挡（诊断实录 DIAG_1），必须排除自身
        Vector3 origin = transform.position + Vector3.up * 1.5f;
        Vector3 target = _player.position + Vector3.up * 0.5f;
        Vector3 dir = target - origin;
        float losDist = dir.magnitude;
        if (losDist < 0.05f)
            return true;

        int count = Physics.RaycastNonAlloc(origin, dir / losDist, _hitBuffer, losDist);
        for (int i = 0; i < count; i++)
        {
            if (_hitBuffer[i].collider.transform.IsChildOf(transform))
                continue;   // 自身碰撞体（头球/胶囊）不算遮挡
            return _hitBuffer[i].collider.GetComponentInParent<PlayerHealth>() != null;
        }
        // 没有任何非自身命中：目标点在玩家胶囊内的前提下正常必命中——异常几何保守判不可见
        return false;
    }
}
