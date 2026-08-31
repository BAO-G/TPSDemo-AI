using UnityEngine;
using UnityEngine.InputSystem;

#if UNITY_EDITOR
using UnityEditor;
#endif

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
    private InputAction _pauseAction;
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
        _pauseAction = _playerInput.actions.FindAction("Pause");
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
        // 窗口失去焦点（鼠标移出游戏窗口/被按 Esc 释放）时忽略视角旋转
        if (!Application.isFocused) return Vector2.zero;
        // Editor 中额外检测：鼠标不在 Game 视图内时不转视角
        // （Application.isFocused 在 Editor 里恒为 true，鼠标移到 Hierarchy/Console/Inspector 时仍会驱动视角）
        if (!IsMouseInGameView()) return Vector2.zero;
        return _lookAction != null ? _lookAction.ReadValue<Vector2>() : Vector2.zero;
    }

    /// <summary>Editor 中判断鼠标是否位于 Game 视图窗口内（构建版恒为 true）</summary>
    private bool IsMouseInGameView()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying) return true;
        try
        {
            // 查找 GameView 编辑器窗口
            EditorWindow gameView = null;
            foreach (var w in Resources.FindObjectsOfTypeAll<EditorWindow>())
            {
                if (w.GetType().Name == "GameView") { gameView = w; break; }
            }
            if (gameView == null) return true;

            // GameView 屏幕矩形（GUI 坐标，左上原点逻辑点）→ 屏幕物理像素矩形（左下原点）
            Rect guiRect = gameView.position;
            Vector2 screenMin = GUIUtility.GUIToScreenPoint(new Vector2(guiRect.x, guiRect.y));
            Vector2 screenMax = GUIUtility.GUIToScreenPoint(new Vector2(guiRect.x + guiRect.width, guiRect.y + guiRect.height));
            Rect screenRect = new Rect(screenMin.x, screenMin.y, screenMax.x - screenMin.x, screenMax.y - screenMin.y);

            // Input.mousePosition 与 screenRect 同为屏幕物理像素坐标（左下原点），可直接比较
            Vector3 mouseScreen = Input.mousePosition;
            return screenRect.Contains(new Vector2(mouseScreen.x, mouseScreen.y));
        }
        catch
        {
            return true; // 异常时保守放行，避免卡视角
        }
#else
        return true;
#endif
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

    /// <summary>蹲伏键是否本帧按下（用于切换式蹲伏）</summary>
    public bool IsCrouchPressed()
    {
        return _crouchAction != null && _crouchAction.WasPressedThisFrame();
    }

    /// <summary>治疗键是否本帧按下</summary>
    public bool IsHealPressed()
    {
        return _healAction != null && _healAction.WasPressedThisFrame();
    }

    /// <summary>暂停键是否本帧按下（Tab）</summary>
    public bool IsPausePressed()
    {
        return _pauseAction != null && _pauseAction.WasPressedThisFrame();
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
