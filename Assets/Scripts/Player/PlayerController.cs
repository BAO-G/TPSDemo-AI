using UnityEngine;

/// <summary>
/// 第三人称玩家控制器：CharacterController 移动 + 跳跃 + 重力 + 鼠标旋转视角
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

    private void Start()
    {
        _characterController = GetComponent<CharacterController>();
        _inputHandler = GetComponent<PlayerInputHandler>();
        _playerHealth = GetComponent<PlayerHealth>();

        // 俯仰旋转挂在 CameraTarget 上，主相机作为其子物体跟随
        _cameraTarget = transform.Find("CameraTarget");

        // 锁定鼠标，隐藏光标，便于视角控制
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        HandleMovement();
        HandleGravity();
        HandleLook();
    }

    /// <summary>读取移动输入，把本地方向转为世界方向后移动</summary>
    private void HandleMovement()
    {
        // 死亡后禁止移动
        if (_playerHealth != null && _playerHealth.CurrentHealth <= 0f)
        {
            return;
        }

        if (_inputHandler == null || _characterController == null)
        {
            return;
        }

        Vector2 moveInput = _inputHandler.GetMoveInput();
        // 输入 (x=左右, y=前后) 映射到本地空间，再转世界方向
        Vector3 moveDirection = transform.TransformDirection(new Vector3(moveInput.x, 0f, moveInput.y));
        // 按住 Shift 时用冲刺速度，否则用步行速度
        float currentSpeed = (_inputHandler != null && _inputHandler.IsSprintHeld()) ? sprintSpeed : walkSpeed;
        if (moveInput.magnitude > 0.1f)
        {
            _characterController.Move(moveDirection.normalized * currentSpeed * Time.deltaTime);
        }
    }

    /// <summary>重力叠加，落地时重置，检测跳跃给初速度</summary>
    private void HandleGravity()
    {
        if (_characterController == null)
        {
            return;
        }

        if (_characterController.isGrounded)
        {
            // 落地后压制竖直速度，避免反复累积
            _verticalVelocity = -2f;
        }

        // 跳跃：给一个向上的初速度
        if (_inputHandler != null && _inputHandler.IsJumpPressed() && _characterController.isGrounded)
        {
            _verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        _verticalVelocity += gravity * Time.deltaTime;
        _characterController.Move(new Vector3(0f, _verticalVelocity, 0f) * Time.deltaTime);
    }

    /// <summary>鼠标控制视角：水平旋转 Player Y 轴，垂直旋转 CameraTarget X 轴并限制角度</summary>
    private void HandleLook()
    {
        // 死亡后禁止旋转视角
        if (_playerHealth != null && _playerHealth.CurrentHealth <= 0f)
        {
            return;
        }

        if (_inputHandler == null || _cameraTarget == null)
        {
            return;
        }

        Vector2 lookDelta = _inputHandler.GetLookDelta();

        // 水平：绕世界 Y 轴旋转角色
        transform.Rotate(0f, lookDelta.x * mouseSensitivity, 0f);

        // 垂直：绕本地 X 轴旋转相机目标，限制俯仰角
        _cameraPitch -= lookDelta.y * mouseSensitivity;
        _cameraPitch = Mathf.Clamp(_cameraPitch, pitchMin, pitchMax);
        _cameraTarget.localRotation = Quaternion.Euler(_cameraPitch, 0f, 0f);
    }

    /// <summary>应用后坐力：抬升枪口（减小俯仰角），并限制在允许范围内</summary>
    public void ApplyRecoil(float amount)
    {
        _cameraPitch -= amount; // 向上抬枪口
        _cameraPitch = Mathf.Clamp(_cameraPitch, pitchMin, pitchMax);
        if (_cameraTarget != null)
        {
            _cameraTarget.localRotation = Quaternion.Euler(_cameraPitch, 0f, 0f);
        }
    }
}
