using UnityEngine;

/// <summary>
/// 武器摇摆：按鼠标视角输入对武器挂点做附加偏移（SmoothDamp 回位）
/// 挂载在 WeaponHolder 上
/// 叠加规则：PlayerADSController 在 Update 写入「贴枪基准」局部位置/旋转，
/// 本脚本在 LateUpdate 用「基准 + 附加偏移」方式叠加并记录本帧附加量，
/// 下一帧先减去上帧附加量还原基准，因此与 ADS 互不覆盖、也不累积漂移
/// </summary>
public class WeaponSway : MonoBehaviour
{
    [Header("位置摇摆")]
    [SerializeField] private float _positionAmount = 0.001f;    // 每度视角旋转产生的位移（米）
    [SerializeField] private float _maxPositionOffset = 0.035f; // 位移钳制上限（米）
    [SerializeField] private float _positionSmooth = 6f;        // 回位平滑（参照实现量级：越大越快）

    [Header("旋转摇摆")]
    [SerializeField] private float _rotationAmount = 0.15f;     // 每度视角旋转产生的转角（度）
    [SerializeField] private float _maxRotationOffset = 3f;     // 转角钳制上限（度）
    [SerializeField] private float _rotationSmooth = 8f;

    [Header("ADS 收窄")]
    [SerializeField] private float _adsSwayMultiplier = 0.3f;   // 完全 ADS 时摇摆幅度倍率

    private Vector3 _positionOffset;                    // 当前平滑后的位置附加量（挂点局部空间）
    private Vector3 _positionVelocity;
    private Vector2 _rotationOffsetEuler;               // 当前平滑后的旋转附加量（俯仰/偏航，度）
    private Vector2 _rotationVelocity;
    private Vector3 _lastAppliedPositionDelta;          // 上一帧实际写入的位置附加量（还原本帧基准用）
    private Quaternion _lastAppliedRotationDelta;       // 上一帧实际写入的旋转附加量（局部空间）
    private PlayerInputHandler _inputHandler;
    private PlayerADSController _adsController;
    private PlayerController _playerController;

    private void Start()
    {
        _inputHandler = GetComponentInParent<PlayerInputHandler>();
        _adsController = GetComponentInParent<PlayerADSController>();
        _playerController = GetComponentInParent<PlayerController>();
    }

    private void LateUpdate()
    {
        if (_inputHandler == null) return;

        // 还原本帧基准：当前局部值减去上一帧的附加量 = ADS/动画写入的原始值
        Vector3 basePosition = transform.localPosition - _lastAppliedPositionDelta;
        Quaternion baseRotation = Quaternion.Inverse(_lastAppliedRotationDelta) * transform.localRotation;

        // 玩家死亡时摇摆归零（与 ADS 控制器一致的健康检查）
        bool alive = _playerController == null || _playerController.CurrentHealth > 0f;

        // 目标附加量：反向于视角旋转量（武器落后于视角），ADS 时按进度收窄
        Vector2 lookDelta = alive ? _inputHandler.GetLookDelta() : Vector2.zero;
        float swayScale = 1f;
        if (_adsController != null)
            swayScale = Mathf.Lerp(1f, _adsSwayMultiplier, _adsController.ADSProgress);

        float moveX = Mathf.Clamp(-lookDelta.x * _positionAmount, -_maxPositionOffset, _maxPositionOffset);
        float moveY = Mathf.Clamp(-lookDelta.y * _positionAmount, -_maxPositionOffset, _maxPositionOffset);
        Vector3 targetPosition = new Vector3(moveX, moveY, 0f) * swayScale;

        float rotX = Mathf.Clamp(-lookDelta.y * _rotationAmount, -_maxRotationOffset, _maxRotationOffset);
        float rotY = Mathf.Clamp(lookDelta.x * _rotationAmount, -_maxRotationOffset, _maxRotationOffset);
        Vector2 targetRotation = new Vector2(rotX, rotY) * swayScale;

        // 平滑逼近目标（SmoothDamp 系数随帧时长缩放，与参照实现同式）
        _positionOffset = Vector3.SmoothDamp(_positionOffset, targetPosition, ref _positionVelocity, _positionSmooth * Time.deltaTime);
        _rotationOffsetEuler = Vector2.SmoothDamp(_rotationOffsetEuler, targetRotation, ref _rotationVelocity, _rotationSmooth * Time.deltaTime);

        // 挂点父级是手骨（任意朝向+缩放），先把偏移量转换到挂点局部空间再写入，
        // 保证鼠标左右/上下对应屏幕方向的可感摇摆，而不是沿骨骼奇怪轴向
        Transform parent = transform.parent;
        Vector3 playerRight = _playerController != null ? _playerController.transform.right : Vector3.right;
        Vector3 worldPositionDelta = playerRight * _positionOffset.x + Vector3.up * _positionOffset.y;
        Vector3 localPositionDelta = parent != null
            ? parent.worldToLocalMatrix.MultiplyVector(worldPositionDelta)
            : _positionOffset;

        // 世界旋转摇摆（俯仰绕玩家右轴、偏航绕世界上轴）转成挂点局部旋转增量
        Quaternion worldRotationDelta = Quaternion.AngleAxis(_rotationOffsetEuler.x, playerRight)
                                       * Quaternion.AngleAxis(_rotationOffsetEuler.y, Vector3.up);
        Quaternion parentRot = parent != null ? parent.rotation : Quaternion.identity;
        Quaternion localRotationDelta = Quaternion.Inverse(parentRot) * worldRotationDelta * parentRot;

        _lastAppliedPositionDelta = localPositionDelta;
        _lastAppliedRotationDelta = localRotationDelta;

        transform.localPosition = basePosition + localPositionDelta;
        transform.localRotation = localRotationDelta * baseRotation;
    }
}
