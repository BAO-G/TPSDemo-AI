using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// ADS 瞄准控制器：管理右键瞄准时的 FOV 变化、武器贴枪、移动减速
/// 挂载在 Player 上
/// </summary>
public class PlayerADSController : MonoBehaviour
{
    [Header("ADS 参数")]
    public float defaultFOV = 60f;
    public float adsFOV = 40f;
    public float adsTransitionSpeed = 10f;
    public float adsMoveSpeedMultiplier = 0.5f;     // ADS时移速倍率

    [Header("武器贴枪偏移（相对挂点默认位置的偏移量）")]
    public Vector3 adsPositionOffset = new Vector3(-0.12f, 0.45f, 0.15f); // 抬至眼线并对齐屏幕中线
    public Vector3 adsRotationOffset = Vector3.zero;

    private CinemachineCamera _cinemachineCamera;
    private PlayerInputHandler _inputHandler;
    private PlayerController _playerController;
    private Transform _weaponHolder;
    private Vector3 _defaultWeaponPosition;
    private Quaternion _defaultWeaponRotation;

    private float _adsProgress;                      // 0=腰射, 1=完全ADS

    /// <summary>是否处于 ADS 状态（进度过半）</summary>
    public bool IsADS => _adsProgress > 0.5f;

    /// <summary>ADS 过渡进度 0~1</summary>
    public float ADSProgress => _adsProgress;

    private void Start()
    {
        _inputHandler = GetComponent<PlayerInputHandler>();
        _playerController = GetComponent<PlayerController>();

        // 查找 Cinemachine 相机
        var tpsCamera = GameObject.Find("TPSCamera");
        if (tpsCamera != null)
            _cinemachineCamera = tpsCamera.GetComponent<CinemachineCamera>();

        // 缓存武器挂点默认位置
        var weaponHolderObj = transform.Find("WeaponHolder");
        if (weaponHolderObj != null)
        {
            _weaponHolder = weaponHolderObj;
            _defaultWeaponPosition = _weaponHolder.localPosition;
            _defaultWeaponRotation = _weaponHolder.localRotation;
        }
    }

    private void Update()
    {
        if (_playerController == null || _inputHandler == null) return;
        if (_playerController.CurrentHealth <= 0f) return;

        bool wantADS = _inputHandler.IsAimHeld();
        float target = wantADS ? 1f : 0f;
        _adsProgress = Mathf.MoveTowards(_adsProgress, target, adsTransitionSpeed * Time.deltaTime);

        // 平滑过渡 FOV
        if (_cinemachineCamera != null)
        {
            _cinemachineCamera.Lens.FieldOfView = Mathf.Lerp(defaultFOV, adsFOV, _adsProgress);
        }

        // 武器贴枪：ADS 时武器从默认挂点向屏幕中线/眼线偏移
        if (_weaponHolder != null)
        {
            Vector3 adsTarget = _defaultWeaponPosition + adsPositionOffset;
            Quaternion adsRotTarget = _defaultWeaponRotation * Quaternion.Euler(adsRotationOffset);
            _weaponHolder.localPosition = Vector3.Lerp(_defaultWeaponPosition, adsTarget, _adsProgress);
            _weaponHolder.localRotation = Quaternion.Slerp(_defaultWeaponRotation, adsRotTarget, _adsProgress);
        }
    }
}
