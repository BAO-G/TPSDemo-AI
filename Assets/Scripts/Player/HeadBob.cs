using UnityEngine;

/// <summary>
/// 头部起伏：移动时对相机跟随目标做正弦起伏，静止/空中/死亡平滑归位
/// 挂载在 Player/CameraTarget 上（Cinemachine 跟随目标）
/// 只改 localPosition：PlayerController 每帧写入 CameraTarget 的 localRotation（俯仰视角），
/// 两者各管一个通道，不与 Cinemachine 相机控制打架
/// </summary>
public class HeadBob : MonoBehaviour
{
    [Header("起伏参数")]
    [SerializeField] private float _bobbingSpeed = 7f;        // 侧摆角速度（rad/s），垂直起伏为其 2 倍频
    [SerializeField] private float _bobbingAmount = 0.05f;    // 满步速垂直起伏幅度（米）
    [SerializeField] private float _lateralMultiplier = 0.4f; // 横向摆动幅度占垂直幅度的比例
    [SerializeField] private float _returnSmooth = 8f;        // 静止归位平滑

    [Header("状态幅度倍率")]
    [SerializeField] private float _crouchMultiplier = 0.5f;  // 蹲伏起伏倍率
    [SerializeField] private float _adsMultiplier = 0.4f;     // 完全 ADS 时起伏倍率

    private float _timer;
    private Vector3 _bobOffset;                     // 当前起伏偏移（局部空间）
    private Vector3 _offsetVelocity;
    private Vector3 _restLocalPosition;
    private PlayerController _playerController;
    private PlayerADSController _adsController;
    private PlayerCoverController _coverController;

    private void Start()
    {
        _playerController = GetComponentInParent<PlayerController>();
        _adsController = GetComponentInParent<PlayerADSController>();
        _coverController = GetComponentInParent<PlayerCoverController>();
        _restLocalPosition = transform.localPosition;
    }

    private void LateUpdate()
    {
        if (_playerController == null) return;

        float speed = _playerController.HorizontalSpeed;
        bool bobbing = _playerController.IsGrounded
                       && speed > 0.1f
                       && _playerController.CurrentHealth > 0f;

        Vector3 targetOffset;
        if (bobbing)
        {
            _timer += _bobbingSpeed * Time.deltaTime;
            if (_timer > Mathf.PI * 2f)
                _timer -= Mathf.PI * 2f;

            // 幅度随实际速度缩放（走小跑大），蹲伏/ADS 再乘衰减倍率
            float amplitude = _bobbingAmount * Mathf.Clamp01(speed / _playerController.walkSpeed);
            if (_coverController != null && _coverController.IsCrouching)
                amplitude *= _crouchMultiplier;
            if (_adsController != null)
                amplitude *= Mathf.Lerp(1f, _adsMultiplier, _adsController.ADSProgress);

            // 垂直弹跳为步频（2 倍频正弦），左右重心横摆为半频
            targetOffset = new Vector3(
                Mathf.Sin(_timer) * amplitude * _lateralMultiplier,
                Mathf.Sin(_timer * 2f) * amplitude,
                0f);
            _bobOffset = targetOffset; // 正弦本身连续，移动中直接跟随
        }
        else
        {
            _timer = 0f;
            targetOffset = Vector3.zero;
            // 静止/空中：从当前偏移平滑归位，避免生硬回弹
            _bobOffset = Vector3.SmoothDamp(_bobOffset, targetOffset, ref _offsetVelocity, _returnSmooth * Time.deltaTime);
        }

        transform.localPosition = _restLocalPosition + _bobOffset;
    }
}
