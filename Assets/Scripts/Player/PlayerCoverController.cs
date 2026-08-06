using UnityEngine;

/// <summary>
/// 掩体/蹲伏控制器：管理 C 键蹲伏时的角色高度减半、移动减速、判定缩小
/// 挂载在 Player 上
/// </summary>
public class PlayerCoverController : MonoBehaviour
{
    [Header("蹲伏参数")]
    public float crouchHeight = 1f;                  // 蹲伏时角色高度
    public float crouchSpeedMultiplier = 0.35f;      // 蹲伏时移速倍率（蹲走≈1.05m/s，蹲跑≈2.1m/s）
    public float crouchTransitionSpeed = 15f;        // 高度过渡速度

    private CharacterController _characterController;
    private PlayerInputHandler _inputHandler;
    private PlayerController _playerController;
    private Animator _animator; // 驱动 IsCrouch 动画参数

    private float _standHeight;
    private Vector3 _standCenter;
    private Vector3 _crouchCenter;
    private float _targetHeight;
    private bool _isCrouching;

    /// <summary>是否正在蹲伏</summary>
    public bool IsCrouching => _isCrouching;

    private void Start()
    {
        _characterController = GetComponent<CharacterController>();
        _inputHandler = GetComponent<PlayerInputHandler>();
        _playerController = GetComponent<PlayerController>();
        _animator = GetComponentInChildren<Animator>();

        if (_characterController != null)
        {
            _standHeight = _characterController.height;
            _standCenter = _characterController.center;
            _crouchCenter = new Vector3(_standCenter.x, crouchHeight * 0.5f, _standCenter.z);
            _targetHeight = _standHeight;
        }
    }

    private void Update()
    {
        if (_playerController == null || _inputHandler == null) return;
        if (_playerController.CurrentHealth <= 0f) return;
        if (_characterController == null) return;

        // 切换式蹲伏：按一下蹲下，再按一下站起（不要求一直按住）
        if (_inputHandler.IsCrouchPressed())
            _isCrouching = !_isCrouching;

        _targetHeight = _isCrouching ? crouchHeight : _standHeight;

        // 平滑过渡高度和中心
        float newHeight = Mathf.Lerp(_characterController.height, _targetHeight, crouchTransitionSpeed * Time.deltaTime);
        _characterController.height = newHeight;

        Vector3 targetCenter = _isCrouching ? _crouchCenter : _standCenter;
        _characterController.center = Vector3.Lerp(_characterController.center, targetCenter, crouchTransitionSpeed * Time.deltaTime);

        // 驱动蹲伏动画状态（Crouch 状态，IsCrouch 参数）
        if (_animator != null)
            _animator.SetBool("IsCrouch", _isCrouching);
    }
}
