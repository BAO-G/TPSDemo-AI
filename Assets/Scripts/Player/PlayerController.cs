using UnityEngine;

/// <summary>
/// 第三人称玩家控制器：CharacterController 移动 + 跳跃 + 重力 + 鼠标旋转视角
/// 阶段2新增：ADS/掩体减速整合
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("移动参数")]
    public float walkSpeed = 3f;
    public float sprintSpeed = 6f;
    public float jumpHeight = 1.0f;
    public float gravity = -9.81f;
    public float acceleration = 14f;      // 水平加速度（m/s²）：起步平滑，值越大起步越快
    public float deceleration = 18f;      // 水平减速度（m/s²）：急停平滑，值越大刹车越快
    public float animSpeedMin = 0.5f;     // 动画速度因子下限（加速中动画放慢的最低倍率）
    public float animSpeedMax = 1.3f;     // 动画速度因子上限（超速时动画加快的最高倍率）

    [Header("视角参数")]
    public float mouseSensitivity = 2f;
    public float pitchMin = -30f;
    public float pitchMax = 60f;

    private CharacterController _characterController;
    private PlayerInputHandler _inputHandler;
    private Transform _cameraTarget;
    private float _verticalVelocity;
    private Vector3 _horizontalVelocity; // 本帧水平速度（供动画驱动，也用于合并单次 Move）
    private float _cameraPitch;
    private PlayerHealth _playerHealth;
    private PlayerADSController _adsController;
    private PlayerCoverController _coverController;
    private Animator _animator; // 角色动画驱动（阶段3接入）

    /// <summary>当前血量，供 ADS/Cover 控制器读取</summary>
    public float CurrentHealth => _playerHealth != null ? _playerHealth.CurrentHealth : 100f;

    private void Start()
    {
        _characterController = GetComponent<CharacterController>();
        _inputHandler = GetComponent<PlayerInputHandler>();
        _playerHealth = GetComponent<PlayerHealth>();
        _adsController = GetComponent<PlayerADSController>();
        _coverController = GetComponent<PlayerCoverController>();
        _animator = GetComponentInChildren<Animator>();
        if (_animator != null)
            _animator.applyRootMotion = false; // 移动由 CharacterController 接管

        _cameraTarget = transform.Find("CameraTarget");

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        HandleMovement();
        HandleGravity();
        ApplyCombinedMove();
        HandleLook();
    }

    /// <summary>水平速度+垂直速度合并为单次 Move：多次 Move 会导致 velocity 只计最后一次，动画取不到水平速度</summary>
    private void ApplyCombinedMove()
    {
        if (_characterController == null) return;
        _characterController.Move((_horizontalVelocity + new Vector3(0f, _verticalVelocity, 0f)) * Time.deltaTime);
    }

    /// <summary>用本帧水平速度驱动动画 Blend Tree（Speed / ForwardSpeed / StrafeSpeed / IsRun 参数）</summary>
    private void LateUpdate()
    {
        if (_animator == null) return;
        // 世界速度转角色本地速度，用于 2D Blend Tree 方向判断
        Vector3 localVelocity = transform.InverseTransformDirection(_horizontalVelocity);
        _animator.SetFloat("Speed", _horizontalVelocity.magnitude);
        // 归一化到 -1~1（除以目标速度，匹配 BlendTree 节点 ±1.0 坐标）
        float normSpeed = _targetSpeed > 0.01f ? _targetSpeed : walkSpeed;
        _animator.SetFloat("ForwardSpeed", localVelocity.z / normSpeed);
        _animator.SetFloat("StrafeSpeed", localVelocity.x / normSpeed);
        // 冲刺切换 Running Locomotion 状态（TPS 设计：只有向前才进入 Running，后退/横移用 Walking）
        bool wantRun = _inputHandler != null && _inputHandler.IsSprintHeld() && localVelocity.z > 0.3f;
        if (_coverController != null && _coverController.IsCrouching)
            wantRun = false;
        _animator.SetBool("IsRun", wantRun);
        // 动画速度因子：实际速度 / 目标速度，让腿的步频与位移同步，消除起步/急停滑步
        // 仅在有移动输入时生效（_targetSpeedValid=true），无输入时保持 1（Idle 正常速度）
        if (_targetSpeedValid && _targetSpeed > 0.01f)
        {
            float factor = Mathf.Clamp(_horizontalVelocity.magnitude / _targetSpeed, animSpeedMin, animSpeedMax);
            _animator.SetFloat("AnimSpeed", factor);
        }
        else
        {
            _animator.SetFloat("AnimSpeed", 1f);
        }
        // 通知动画状态机是否在地面，用于跳跃落地后退出 Jump 状态
        _animator.SetBool("Grounded", _characterController.isGrounded);
    }

    private float _targetSpeed;          // 本帧目标速度（供动画速度因子计算）
    private bool _targetSpeedValid;      // 是否有有效移动输入

    private void HandleMovement()
    {
        if (_playerHealth != null && _playerHealth.CurrentHealth <= 0f) return;
        if (_inputHandler == null || _characterController == null) return;

        Vector2 moveInput = _inputHandler.GetMoveInput();
        Vector3 moveDirection = transform.TransformDirection(new Vector3(moveInput.x, 0f, moveInput.y));

        // 仅在有移动输入时更新目标速度，否则保持 Idle 正常速度
        _targetSpeedValid = moveInput.magnitude > 0.1f;
        if (_targetSpeedValid)
        {
            // TPS 设计：只有向前移动时才允许冲刺加速（后退/横移不加速）
            bool isForward = moveInput.y > 0.3f;
            _targetSpeed = (_inputHandler.IsSprintHeld() && isForward) ? sprintSpeed : walkSpeed;

            // ADS 减速
            if (_adsController != null)
                _targetSpeed *= 1f - _adsController.ADSProgress * (1f - _adsController.adsMoveSpeedMultiplier);

            // 掩体/蹲伏减速
            if (_coverController != null && _coverController.IsCrouching)
                _targetSpeed *= _coverController.crouchSpeedMultiplier;
        }

        // 目标速度向量（无输入时归零）
        Vector3 targetVelocity = moveInput.magnitude > 0.1f
            ? moveDirection.normalized * _targetSpeed
            : Vector3.zero;

        // 速度平滑：用加速度/减速度向目标速度逼近，避免瞬间启停造成滑步
        float maxDelta = (targetVelocity.sqrMagnitude >= _horizontalVelocity.sqrMagnitude
            ? acceleration : deceleration) * Time.deltaTime;
        _horizontalVelocity = Vector3.MoveTowards(_horizontalVelocity, targetVelocity, maxDelta);
    }

    private void HandleGravity()
    {
        if (_characterController == null) return;

        if (_characterController.isGrounded)
            _verticalVelocity = -2f;

        if (_inputHandler != null && _inputHandler.IsJumpPressed() && _characterController.isGrounded)
        {
            _verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);

            // 动态计算动画播放速度：让完整跳跃动画精确匹配实际空中时间
            float animDuration = 0.6f; // rifle jump.fbx 时长（18帧/30fps）
            float airTime = 2f * Mathf.Sqrt(2f * jumpHeight / Mathf.Abs(gravity));
            float jumpSpeed = animDuration / airTime;
            if (_animator != null)
            {
                _animator.SetFloat("JumpSpeed", jumpSpeed);
                _animator.SetTrigger("Jump");
            }
        }

        _verticalVelocity += gravity * Time.deltaTime;
    }

    private void HandleLook()
    {
        if (_playerHealth != null && _playerHealth.CurrentHealth <= 0f) return;
        if (_inputHandler == null || _cameraTarget == null) return;

        Vector2 lookDelta = _inputHandler.GetLookDelta();
        transform.Rotate(0f, lookDelta.x * mouseSensitivity, 0f);

        _cameraPitch -= lookDelta.y * mouseSensitivity;
        _cameraPitch = Mathf.Clamp(_cameraPitch, pitchMin, pitchMax);
        _cameraTarget.localRotation = Quaternion.Euler(_cameraPitch, 0f, 0f);
    }

    public void ApplyRecoil(float amount)
    {
        _cameraPitch -= amount;
        _cameraPitch = Mathf.Clamp(_cameraPitch, pitchMin, pitchMax);
        if (_cameraTarget != null)
            _cameraTarget.localRotation = Quaternion.Euler(_cameraPitch, 0f, 0f);
    }
}
