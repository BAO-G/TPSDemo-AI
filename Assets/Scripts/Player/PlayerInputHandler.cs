using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 封装 InputAction，为其他脚本提供统一的输入数据接口
/// </summary>
public class PlayerInputHandler : MonoBehaviour
{
    private PlayerInput _playerInput;
    private InputAction _moveAction;
    private InputAction _lookAction;
    private InputAction _shootAction;
    private InputAction _reloadAction;
    private InputAction _jumpAction;
    private InputAction _aimAction;
    private InputAction _crouchAction;
    private InputAction _healAction;
    private InputAction _switchWeapon1Action;
    private InputAction _switchWeapon2Action;
    private InputAction _switchWeapon3Action;

    private void Awake()
    {
        _playerInput = GetComponent<PlayerInput>();
        if (_playerInput == null || _playerInput.actions == null)
        {
            Debug.LogError("PlayerInputHandler 需要 PlayerInput 组件及其 InputActionAsset");
            return;
        }

        // 从 PlayerInput 绑定的 InputActionAsset 中按名字查找各 Action
        _moveAction = _playerInput.actions.FindAction("Move");
        _lookAction = _playerInput.actions.FindAction("Look");
        _shootAction = _playerInput.actions.FindAction("Shoot");
        _reloadAction = _playerInput.actions.FindAction("Reload");
        _jumpAction = _playerInput.actions.FindAction("Jump");
        _aimAction = _playerInput.actions.FindAction("Aim");
        _crouchAction = _playerInput.actions.FindAction("Crouch");
        _healAction = _playerInput.actions.FindAction("Heal");
        _switchWeapon1Action = _playerInput.actions.FindAction("SwitchWeapon1");
        _switchWeapon2Action = _playerInput.actions.FindAction("SwitchWeapon2");
        _switchWeapon3Action = _playerInput.actions.FindAction("SwitchWeapon3");

        // 确保 ActionMap 已启用（修复 PlayerInput 未自动激活的问题）
        _playerInput.actions.FindActionMap("Player")?.Enable();
    }

    /// <summary>移动方向输入 (WASD/摇杆)</summary>
    public Vector2 GetMoveInput()
    {
        return _moveAction != null ? _moveAction.ReadValue<Vector2>() : Vector2.zero;
    }

    /// <summary>鼠标/右摇杆视角增量</summary>
    public Vector2 GetLookDelta()
    {
        return _lookAction != null ? _lookAction.ReadValue<Vector2>() : Vector2.zero;
    }

    /// <summary>射击键是否按下</summary>
    public bool IsShootPressed()
    {
        return _shootAction != null && _shootAction.IsPressed();
    }

    /// <summary>换弹键是否本帧按下</summary>
    public bool IsReloadPressed()
    {
        return _reloadAction != null && _reloadAction.WasPressedThisFrame();
    }

    /// <summary>跳跃键是否本帧按下</summary>
    public bool IsJumpPressed()
    {
        return _jumpAction != null && _jumpAction.WasPressedThisFrame();
    }

    /// <summary>瞄准键是否按住</summary>
    public bool IsAimHeld()
    {
        return _aimAction != null && _aimAction.IsPressed();
    }

    /// <summary>蹲伏键是否按住</summary>
    public bool IsCrouchHeld()
    {
        return _crouchAction != null && _crouchAction.IsPressed();
    }

    /// <summary>治疗键是否本帧按下</summary>
    public bool IsHealPressed()
    {
        return _healAction != null && _healAction.WasPressedThisFrame();
    }

    /// <summary>切枪键是否本帧按下，返回武器槽位索引（0/1/2），无按下返回 -1</summary>
    public int GetWeaponSwitchIndex()
    {
        if (_switchWeapon1Action != null && _switchWeapon1Action.WasPressedThisFrame()) return 0;
        if (_switchWeapon2Action != null && _switchWeapon2Action.WasPressedThisFrame()) return 1;
        if (_switchWeapon3Action != null && _switchWeapon3Action.WasPressedThisFrame()) return 2;
        return -1;
    }

    /// <summary>冲刺键是否按住（直接检测键盘，不依赖 InputActionAsset）</summary>
    public bool IsSprintHeld()
    {
        return Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed;
    }
}
