using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD 管理：血条、弹药（弹匣/备弹）、受伤红屏、治疗按键、医疗包数
/// 阶段2新增：红屏受伤反馈、备弹显示、治疗逻辑
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("HUD 组件绑定")]
    public Slider healthSlider;
    public Text ammoText;
    public Text medkitText;             // 医疗包数量文字
    public Image redVignetteImage;      // 径向渐变受伤晕影

    [Header("受伤反馈参数")]
    public float redFlashPeakAlpha = 0.4f;
    public float redFlashDuration = 0.35f;

    private PlayerHealth _playerHealth;
    private WeaponManager _weaponManager;
    private PlayerInputHandler _inputHandler;

    private Coroutine _redFlashCoroutine;

    private void Awake()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            _playerHealth = player.GetComponent<PlayerHealth>();
            _weaponManager = player.GetComponent<WeaponManager>();
            _inputHandler = player.GetComponent<PlayerInputHandler>();

            if (_playerHealth != null)
            {
                _playerHealth.OnHealthChanged += UpdateHealthBar;
                _playerHealth.OnDamaged += OnPlayerDamaged;
                _playerHealth.OnMedkitChanged += UpdateMedkitDisplay;
            }
            if (_weaponManager != null)
            {
                _weaponManager.OnAmmoChanged += UpdateAmmoDisplay;
                _weaponManager.OnReserveAmmoChanged += UpdateReserveDisplay;
            }
        }

        // 初始隐藏受伤晕影
        if (redVignetteImage != null)
        {
            var c = redVignetteImage.color;
            c.a = 0f;
            redVignetteImage.color = c;
        }
    }

    private void Start()
    {
        // 初始化血量显示
        if (_playerHealth != null)
            UpdateHealthBar(_playerHealth.CurrentHealth, _playerHealth.maxHealth);

        // 初始化弹药显示
        int maxAmmo = _weaponManager != null ? _weaponManager.GetMaxAmmo() : 0;
        UpdateAmmoDisplay(maxAmmo > 0 ? _weaponManager.GetCurrentAmmo() : 30, maxAmmo > 0 ? maxAmmo : 30);

        // 初始化医疗包显示
        if (_playerHealth != null)
            UpdateMedkitDisplay(_playerHealth.MedkitCount);

        StartCoroutine(SyncAmmoAfterWeaponReady());
    }

    private void Update()
    {
        // 兜底刷新血条
        if (_playerHealth != null && Time.frameCount % 12 == 0)
            UpdateHealthBar(_playerHealth.CurrentHealth, _playerHealth.maxHealth);

        // 检测治疗按键
        if (_inputHandler != null && _inputHandler.IsHealPressed())
        {
            _playerHealth?.TryHeal();
        }
    }

    private void OnDestroy()
    {
        if (_playerHealth != null)
        {
            _playerHealth.OnHealthChanged -= UpdateHealthBar;
            _playerHealth.OnDamaged -= OnPlayerDamaged;
            _playerHealth.OnMedkitChanged -= UpdateMedkitDisplay;
        }
        if (_weaponManager != null)
        {
            _weaponManager.OnAmmoChanged -= UpdateAmmoDisplay;
            _weaponManager.OnReserveAmmoChanged -= UpdateReserveDisplay;
        }
    }

    private IEnumerator SyncAmmoAfterWeaponReady()
    {
        yield return null;
        if (_weaponManager != null)
            UpdateAmmoDisplay(_weaponManager.GetCurrentAmmo(), _weaponManager.GetMaxAmmo());
    }

    // ===== 血条 =====
    private void UpdateHealthBar(float current, float max)
    {
        if (healthSlider == null) return;
        healthSlider.maxValue = max;
        healthSlider.value = current;
    }

    // ===== 受伤红屏反馈 =====
    private void OnPlayerDamaged(float damage)
    {
        if (redVignetteImage == null) return;

        if (_redFlashCoroutine != null)
            StopCoroutine(_redFlashCoroutine);
        _redFlashCoroutine = StartCoroutine(RedFlashRoutine());
    }

    private IEnumerator RedFlashRoutine()
    {
        // 瞬间红色峰值
        SetVignetteAlpha(redFlashPeakAlpha);

        // 渐变消失
        float elapsed = 0f;
        while (elapsed < redFlashDuration)
        {
            elapsed += Time.deltaTime;
            SetVignetteAlpha(Mathf.Lerp(redFlashPeakAlpha, 0f, elapsed / redFlashDuration));
            yield return null;
        }
        SetVignetteAlpha(0f);
    }

    // 对晕影 Image 应用透明度
    private void SetVignetteAlpha(float alpha)
    {
        if (redVignetteImage == null) return;
        var c = redVignetteImage.color;
        c.a = alpha;
        redVignetteImage.color = c;
    }

    // ===== 弹药显示 =====
    private void UpdateAmmoDisplay(int current, int max)
    {
        if (ammoText == null) return;
        int reserve = _weaponManager != null ? _weaponManager.GetReserveAmmo() : 0;
        ammoText.text = $"弹药: {current} / {reserve}";
    }

    private void UpdateReserveDisplay(int reserve)
    {
        if (ammoText == null) return;
        int current = _weaponManager != null ? _weaponManager.GetCurrentAmmo() : 0;
        ammoText.text = $"弹药: {current} / {reserve}";
    }

    // ===== 医疗包显示 =====
    private void UpdateMedkitDisplay(int count)
    {
        if (medkitText != null)
            medkitText.text = $"医疗包: {count}";
    }
}
