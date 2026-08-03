using System;
using UnityEngine;

/// <summary>
/// 玩家武器管理：持有当前武器，将输入转发给武器
/// </summary>
public class WeaponManager : MonoBehaviour
{
    public Transform weaponHolder; // 指向 Player/WeaponHolder

    private FirearmWeapon _currentWeapon; // 当前装备的武器
    private PlayerInputHandler _inputHandler;

    // 弹药变化事件（当前/最大），供 HUD 等监听
    public event Action<int, int> OnAmmoChanged;

    private int _lastAmmo; // 上次上报的弹药数，用于检测变化

    private void Start()
    {
        // 未手动指定时按子物体路径查找 WeaponHolder
        if (weaponHolder == null)
        {
            weaponHolder = transform.Find("WeaponHolder");
        }
        _inputHandler = GetComponent<PlayerInputHandler>();

        // 接管 WeaponHolder 下已放置的武器（场景预放置时无需调用 EquipWeapon）
        if (weaponHolder != null)
        {
            _currentWeapon = weaponHolder.GetComponentInChildren<FirearmWeapon>();
            if (_currentWeapon != null)
            {
                _lastAmmo = _currentWeapon.GetCurrentAmmo();
            }
        }
    }

    private void Update()
    {
        if (_currentWeapon == null || _inputHandler == null)
        {
            return;
        }

        // 按住射击键持续射击，按换弹键执行换弹
        if (_inputHandler.IsShootPressed())
        {
            _currentWeapon.Shoot();
        }

        if (_inputHandler.IsReloadPressed())
        {
            _currentWeapon.Reload();
        }

        // 弹药变化时触发事件
        int current = _currentWeapon.GetCurrentAmmo();
        if (current != _lastAmmo)
        {
            _lastAmmo = current;
            OnAmmoChanged?.Invoke(current, _currentWeapon.GetMaxAmmo());
        }
    }

    /// <summary>装备武器：在 weaponHolder 下实例化预制体并获取 FirearmWeapon</summary>
    public void EquipWeapon(GameObject weaponPrefab)
    {
        if (weaponPrefab == null || weaponHolder == null)
        {
            Debug.LogWarning("EquipWeapon 失败：预制体或武器挂点为空");
            return;
        }

        // 先清掉旧武器
        if (_currentWeapon != null)
        {
            Destroy(_currentWeapon.gameObject);
        }

        var weapon = Instantiate(weaponPrefab, weaponHolder);
        weapon.transform.localPosition = Vector3.zero;
        weapon.transform.localRotation = Quaternion.identity;

        _currentWeapon = weapon.GetComponent<FirearmWeapon>();
        if (_currentWeapon == null)
        {
            Debug.LogError("武器预制体缺少 FirearmWeapon 组件", weapon);
        }
        else
        {
            _lastAmmo = _currentWeapon.GetCurrentAmmo();
            OnAmmoChanged?.Invoke(_lastAmmo, _currentWeapon.GetMaxAmmo());
        }
    }

    public int GetCurrentAmmo()
    {
        return _currentWeapon != null ? _currentWeapon.GetCurrentAmmo() : 0;
    }

    public int GetMaxAmmo()
    {
        return _currentWeapon != null ? _currentWeapon.GetMaxAmmo() : 0;
    }
}
