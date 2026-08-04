using System.Collections;
using UnityEngine;

/// <summary>
/// 挂载在武器 GameObject 上，处理射击、换弹、弹药管理
/// 阶段2新增：备弹池、ADS散布倍率
/// </summary>
public class FirearmWeapon : MonoBehaviour
{
    [Header("武器配置")]
    public WeaponData weaponData;
    public Transform firePoint;

    private int _currentAmmo;                       // 当前弹匣弹药
    private int _reserveAmmo;                       // 备弹池弹药
    private bool _isReloading;
    private float _nextFireTime;
    private GameObject _muzzleFlash;
    private PlayerController _playerController;
    private PlayerADSController _adsController;
    private Animator _playerAnimator; // 玩家角色动画（阶段3接入，驱动射击/换弹动作）

    private void Start()
    {
        _playerController = FindAnyObjectByType<PlayerController>();
        _adsController = FindAnyObjectByType<PlayerADSController>();

        // 武器挂在玩家子层级，从根节点向下找角色 Animator
        _playerAnimator = transform.root.GetComponentInChildren<Animator>();

        if (weaponData == null)
        {
            Debug.LogError("FirearmWeapon 缺少 WeaponData 配置", this);
            return;
        }

        _currentAmmo = weaponData.magazineSize;
        _reserveAmmo = weaponData.reserveAmmoMax;

        if (weaponData.weaponPrefab != null && GetComponentInChildren<MeshRenderer>() == null)
        {
            Instantiate(weaponData.weaponPrefab, transform);
        }
    }

    /// <summary>尝试射击：有弹药、冷却结束、未在换弹</summary>
    public void Shoot()
    {
        if (!CanShoot()) return;

        Transform origin = firePoint != null ? firePoint : transform;
        Vector3 direction = Camera.main != null ? Camera.main.transform.forward : origin.forward;

        // 应用散布（ADS时散布缩小）
        if (weaponData.bulletSpread > 0f)
        {
            float adsFactor = 1f;
            if (_adsController != null)
                adsFactor = 1f - _adsController.ADSProgress * (1f - weaponData.adsSpreadMultiplier);

            float spreadDeg = weaponData.bulletSpread * adsFactor * Mathf.Rad2Deg;
            direction = Quaternion.Euler(
                Random.Range(-spreadDeg, spreadDeg),
                Random.Range(-spreadDeg, spreadDeg),
                0f
            ) * direction;
        }

        if (Physics.Raycast(origin.position, direction, out RaycastHit hit, weaponData.maxRange))
        {
            var enemy = hit.collider.GetComponentInParent<EnemyBase>();
            if (enemy != null) enemy.TakeDamage(weaponData.damage);

            var player = hit.collider.GetComponentInParent<PlayerHealth>();
            if (player != null) player.TakeDamage(weaponData.damage);
        }

        _currentAmmo--;
        _nextFireTime = Time.time + (60f / weaponData.fireRate);
        PlayMuzzleFlash();

        if (_playerAnimator != null)
            _playerAnimator.SetTrigger("Fire");

        if (_playerController != null)
            _playerController.ApplyRecoil(weaponData.recoilAmount);
    }

    /// <summary>换弹：从备弹池补充弹匣</summary>
    public void Reload()
    {
        if (weaponData == null || _isReloading || _currentAmmo >= weaponData.magazineSize)
            return;
        if (_reserveAmmo <= 0)
            return;
        StartCoroutine(ReloadRoutine());
    }

    private IEnumerator ReloadRoutine()
    {
        _isReloading = true;
        if (_playerAnimator != null)
            _playerAnimator.SetTrigger("Reload");
        yield return new WaitForSeconds(weaponData.reloadTime);

        int needed = weaponData.magazineSize - _currentAmmo;
        int toReload = Mathf.Min(needed, _reserveAmmo);
        _currentAmmo += toReload;
        _reserveAmmo -= toReload;

        _isReloading = false;
    }

    public bool CanShoot()
    {
        return weaponData != null && _currentAmmo > 0
            && Time.time >= _nextFireTime && !_isReloading;
    }

    /// <summary>是否可以换弹（备弹>0且弹匣未满）</summary>
    public bool CanReload()
    {
        return weaponData != null && _currentAmmo < weaponData.magazineSize
            && _reserveAmmo > 0 && !_isReloading;
    }

    /// <summary>补充备弹（弹药拾取物调用）</summary>
    public void AddReserveAmmo(int amount)
    {
        int max = weaponData != null ? weaponData.reserveAmmoMax : 999;
        _reserveAmmo = Mathf.Min(_reserveAmmo + amount, max);
    }

    public int GetCurrentAmmo() => _currentAmmo;

    public int GetReserveAmmo() => _reserveAmmo;

    public int GetMaxAmmo() => weaponData != null ? weaponData.magazineSize : 0;

    private void PlayMuzzleFlash()
    {
        if (weaponData.muzzleFlashPrefab == null) return;
        Transform origin = firePoint != null ? firePoint : transform;
        if (_muzzleFlash != null) Destroy(_muzzleFlash);
        _muzzleFlash = Instantiate(weaponData.muzzleFlashPrefab, origin.position, origin.rotation);
        Destroy(_muzzleFlash, 1f);
    }
}
