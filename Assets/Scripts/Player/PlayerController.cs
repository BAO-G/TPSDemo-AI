using UnityEngine;

/// <summary>
/// 第三人称玩家控制器：CharacterController 移动 + 跳跃 + 重力 + 鼠标旋转视角
/// 阶段2新增：ADS/掩体减速整合
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("移动参数")]
    public float walkSpeed = 5f;
    public float sprintSpeed = 8f;
    public float jumpHeight = 2f;
    public float gravity = -9.81f;

    [Header("视角参数")]
    public float mouseSensitivity = 2f;
    public float pitchMin = -30f;
    public float pitchMax = 60f;

    private CharacterController _characterController;
    private PlayerInputHandler _inputHandler;
    private Transform _cameraTarget;
    private float _verticalVelocity;
    private float _cameraPitch;
    private PlayerHealth _playerHealth;
    private PlayerADSController _adsController;
    private PlayerCoverController _coverController;

    /// <summary>当前血量，供 ADS/Cover 控制器读取</summary>
    public float CurrentHealth => _playerHealth != null ? _playerHealth.CurrentHealth : 100f;

    private void Start()
    {
        _characterController = GetComponent<CharacterController>();
        _inputHandler = GetComponent<PlayerInputHandler>();
        _playerHealth = GetComponent<PlayerHealth>();
        _adsController = GetComponent<PlayerADSController>();
        _coverController = GetComponent<PlayerCoverController>();

        _cameraTarget = transform.Find("CameraTarget");

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        HandleMovement();
        HandleGravity();
        HandleLook();
    }

    private void HandleMovement()
    {
        if (_playerHealth != null && _playerHealth.CurrentHealth <= 0f) return;
        if (_inputHandler == null || _characterController == null) return;

        Vector2 moveInput = _inputHandler.GetMoveInput();
        Vector3 moveDirection = transform.TransformDirection(new Vector3(moveInput.x, 0f, moveInput.y));

        // 基础速度
        float currentSpeed = _inputHandler.IsSprintHeld() ? sprintSpeed : walkSpeed;

        // ADS 减速
        if (_adsController != null)
            currentSpeed *= 1f - _adsController.ADSProgress * (1f - _adsController.adsMoveSpeedMultiplier);

        // 掩体/蹲伏减速
        if (_coverController != null && _coverController.IsCrouching)
            currentSpeed *= _coverController.crouchSpeedMultiplier;

        if (moveInput.magnitude > 0.1f)
        {
            _characterController.Move(moveDirection.normalized * currentSpeed * Time.deltaTime);
        }
    }

    private void HandleGravity()
    {
        if (_characterController == null) return;

        if (_characterController.isGrounded)
            _verticalVelocity = -2f;

        if (_inputHandler != null && _inputHandler.IsJumpPressed() && _characterController.isGrounded)
            _verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);

        _verticalVelocity += gravity * Time.deltaTime;
        _characterController.Move(new Vector3(0f, _verticalVelocity, 0f) * Time.deltaTime);
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
