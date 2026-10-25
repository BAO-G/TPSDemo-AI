using UnityEngine;

/// <summary>
/// 玩家噪音发射器（阶段6 S3，PHASE6_PLAN §2.3）：敌人听觉的噪音来源
/// 持续噪音按移动状态取半径（蹲走/走/跑），瞬时噪音（开枪/爆炸）取大半径线性衰减
/// 敌人感知 tick 内查询 CurrentNoiseRadius：距离 < 半径 → 听见（更新最后已知位置 → Search 调查）
/// 挂 Player 根物体
/// </summary>
public class PlayerNoiseEmitter : MonoBehaviour
{
    [Header("持续噪音半径（米）")]
    public float walkNoise = 5f;        // 走路
    public float sprintNoise = 7f;      // 奔跑
    public float crouchNoise = 3f;      // 蹲走
    [Header("瞬时噪音")]
    public float gunshotNoise = 30f;    // 开枪（DEVELOPMENT_PLAN §5.8）
    public float explosionNoise = 30f;  // 爆炸
    public float transientDecayTime = 0.5f;  // 瞬时噪音线性衰减时长（秒）

    private PlayerController _playerController;
    private PlayerCoverController _cover;
    private float _transientRadius;
    private float _transientTimer;

    /// <summary>当前噪音半径（持续与瞬时取大者；敌人距离 < 此值即听见）</summary>
    public float CurrentNoiseRadius
    {
        get
        {
            float continuous = 0f;
            if (_playerController != null)
            {
                float speed = _playerController.HorizontalSpeed;
                if (speed > 0.1f)
                {
                    if (_cover != null && _cover.IsCrouching)
                        continuous = crouchNoise;
                    else if (speed > _playerController.walkSpeed + 0.5f)
                        continuous = sprintNoise;
                    else
                        continuous = walkNoise;
                }
            }
            float transient = _transientTimer > 0f
                ? Mathf.Lerp(0f, _transientRadius, _transientTimer / transientDecayTime)
                : 0f;
            return Mathf.Max(continuous, transient);
        }
    }

    private void Start()
    {
        _playerController = GetComponent<PlayerController>();
        _cover = GetComponent<PlayerCoverController>();
    }

    private void OnEnable()
    {
        GameEvents.OnPlayerShot += HandlePlayerShot;
        GameEvents.OnExplosion += HandleExplosion;
    }

    private void OnDisable()
    {
        GameEvents.OnPlayerShot -= HandlePlayerShot;
        GameEvents.OnExplosion -= HandleExplosion;
    }

    private void HandlePlayerShot(Vector3 pos, string weaponName)
    {
        // 枪声源就是玩家自身，忽略事件携带的位置
        _transientRadius = gunshotNoise;
        _transientTimer = transientDecayTime;
    }

    private void HandleExplosion(Vector3 pos)
    {
        _transientRadius = explosionNoise;
        _transientTimer = transientDecayTime;
    }

    private void Update()
    {
        if (_transientTimer > 0f)
            _transientTimer -= Time.deltaTime;
    }
}
