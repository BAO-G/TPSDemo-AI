using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD 管理：订阅玩家血量/弹药事件，驱动血条和弹药文字的实时更新
/// 弹药文字用 UnityEngine.UI.Text（项目未导入 TMP Essentials，无字体资源）
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("HUD 组件绑定")]
    public Slider healthSlider; // 血条
    public Text ammoText;       // 弹药文字

    private PlayerHealth _playerHealth;
    private WeaponManager _weaponManager;

    private void Awake()
    {
        // 提前定位玩家并订阅事件，避免 Start 执行顺序导致漏订阅
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            _playerHealth = player.GetComponent<PlayerHealth>();
            _weaponManager = player.GetComponent<WeaponManager>();
            if (_playerHealth != null) _playerHealth.OnHealthChanged += UpdateHealthBar;
            if (_weaponManager != null) _weaponManager.OnAmmoChanged += UpdateAmmoDisplay;
        }
    }

    private void Start()
    {
        // 初始化显示：满血；弹药优先取武器管理器真实值，武器未接管时用默认值兜底
        if (_playerHealth != null)
        {
            UpdateHealthBar(_playerHealth.maxHealth, _playerHealth.maxHealth);
        }

        int maxAmmo = _weaponManager != null ? _weaponManager.GetMaxAmmo() : 0;
        UpdateAmmoDisplay(maxAmmo > 0 ? _weaponManager.GetCurrentAmmo() : 30, maxAmmo > 0 ? maxAmmo : 30);

        // 延迟一帧再同步一次弹药，确保 WeaponManager.Start 已接管场景预置武器
        StartCoroutine(SyncAmmoAfterWeaponReady());
    }

    private void Update()
    {
        // 兜底：每 0.2 秒主动读取血量刷新 HUD，防止事件订阅时机问题
        if (_playerHealth != null && Time.frameCount % 12 == 0)
        {
            UpdateHealthBar(_playerHealth.CurrentHealth, _playerHealth.maxHealth);
        }
    }

    private void OnDestroy()
    {
        // 取消订阅，防止对象销毁后事件仍被调用
        if (_playerHealth != null)
        {
            _playerHealth.OnHealthChanged -= UpdateHealthBar;
        }
        if (_weaponManager != null)
        {
            _weaponManager.OnAmmoChanged -= UpdateAmmoDisplay;
        }
    }

    private IEnumerator SyncAmmoAfterWeaponReady()
    {
        yield return null;
        if (_weaponManager != null)
        {
            UpdateAmmoDisplay(_weaponManager.GetCurrentAmmo(), _weaponManager.GetMaxAmmo());
        }
    }

    private void UpdateHealthBar(float current, float max)
    {
        if (healthSlider == null)
        {
            return;
        }
        healthSlider.maxValue = max;
        healthSlider.value = current;
    }

    private void UpdateAmmoDisplay(int current, int max)
    {
        if (ammoText == null)
        {
            return;
        }
        ammoText.text = $"{current} / {max}";
    }
}
