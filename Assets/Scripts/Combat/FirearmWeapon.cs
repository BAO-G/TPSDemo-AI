using System.Collections;
using UnityEngine;

/// <summary>
/// 挂载在武器 GameObject 上，处理射击与换弹逻辑
/// </summary>
public class FirearmWeapon : MonoBehaviour
{
    [Header("武器配置")]
    public WeaponData weaponData;      // WeaponData 资产引用
    public Transform firePoint;        // 枪口位置（为空则用自身 Transform）

    private int _currentAmmo;          // 当前弹匣弹药
    private bool _isReloading;         // 是否正在换弹
    private float _nextFireTime;       // 下次可射击时间
    private GameObject _muzzleFlash;   // 枪口特效实例
    private PlayerController _playerController; // 缓存的玩家控制器，避免每次射击查找

    private void Start()
    {
        _playerController = FindAnyObjectByType<PlayerController>();

        if (weaponData == null)
        {
            Debug.LogError("FirearmWeapon 缺少 WeaponData 配置", this);
            return;
        }

        _currentAmmo = weaponData.magazineSize;

        // 若自身是空挂载点（无模型）且配置了武器预制体，则实例化模型作为子物体
        if (weaponData.weaponPrefab != null && GetComponentInChildren<MeshRenderer>() == null)
        {
            Instantiate(weaponData.weaponPrefab, transform);
        }
    }

    /// <summary>尝试射击：有弹药、冷却结束、未在换弹</summary>
    public void Shoot()
    {
        if (!CanShoot())
        {
            return;
        }

        Transform origin = firePoint != null ? firePoint : transform;

        // 子弹方向跟随主相机（准星）：WeaponHolder 挂在 Player 根下不跟随俯仰，
        // 若用枪口 forward 会导致玩家低头瞄准时子弹水平打出打不中目标
        Vector3 direction = Camera.main != null ? Camera.main.transform.forward : origin.forward;

        // 应用子弹散布：在射向上叠加随机偏移角
        if (weaponData.bulletSpread > 0f)
        {
            float spreadDeg = weaponData.bulletSpread * Mathf.Rad2Deg;
            direction = Quaternion.Euler(
                Random.Range(-spreadDeg, spreadDeg),
                Random.Range(-spreadDeg, spreadDeg),
                0f
            ) * direction;
        }

        if (Physics.Raycast(origin.position, direction, out RaycastHit hit, weaponData.maxRange))
        {
            // 命中敌人：造成伤害并输出显眼日志，便于确认射击生效
            var enemy = hit.collider.GetComponentInParent<EnemyBase>();
            if (enemy != null)
            {
                enemy.TakeDamage(weaponData.damage);
                Debug.Log($"<color=red>🔫 命中 {hit.collider.name}！伤害 {weaponData.damage}，敌人剩余血量将会减少</color>");
            }
            else
            {
                Debug.Log($"命中 {hit.collider.name}, 伤害 {weaponData.damage}");
            }

            // 命中玩家：造成伤害（测试用）
            var player = hit.collider.GetComponentInParent<PlayerHealth>();
            if (player != null)
            {
                player.TakeDamage(weaponData.damage);
            }
        }
        else
        {
            Debug.Log("射击未命中");
        }

        // 扣弹药、计算冷却
        _currentAmmo--;
        _nextFireTime = Time.time + (60f / weaponData.fireRate);

        PlayMuzzleFlash();

        // 射击后应用后坐力：让玩家相机上跳
        if (_playerController != null)
        {
            _playerController.ApplyRecoil(weaponData.recoilAmount);
        }
    }

    /// <summary>换弹：已在换弹或弹药已满则忽略</summary>
    public void Reload()
    {
        if (weaponData == null || _isReloading || _currentAmmo >= weaponData.magazineSize)
        {
            return;
        }

        StartCoroutine(ReloadRoutine());
    }

    private IEnumerator ReloadRoutine()
    {
        _isReloading = true;
        yield return new WaitForSeconds(weaponData.reloadTime);
        _currentAmmo = weaponData.magazineSize;
        _isReloading = false;
    }

    /// <summary>是否允许射击</summary>
    public bool CanShoot()
    {
        return weaponData != null
            && _currentAmmo > 0
            && Time.time >= _nextFireTime
            && !_isReloading;
    }

    public int GetCurrentAmmo()
    {
        return _currentAmmo;
    }

    public int GetMaxAmmo()
    {
        return weaponData != null ? weaponData.magazineSize : 0;
    }

    /// <summary>在枪口生成特效实例，1 秒后销毁</summary>
    private void PlayMuzzleFlash()
    {
        if (weaponData.muzzleFlashPrefab == null)
        {
            return;
        }

        Transform origin = firePoint != null ? firePoint : transform;
        if (_muzzleFlash != null)
        {
            Destroy(_muzzleFlash);
        }
        _muzzleFlash = Instantiate(weaponData.muzzleFlashPrefab, origin.position, origin.rotation);
        Destroy(_muzzleFlash, 1f);
    }
}
