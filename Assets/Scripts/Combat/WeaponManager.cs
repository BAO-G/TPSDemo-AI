using System;
using UnityEngine;

/// <summary>
/// 玩家武器管理：持有当前武器，将输入转发给武器
/// 阶段2新增：备弹事件暴露
/// </summary>
public class WeaponManager : MonoBehaviour
{
    public Transform weaponHolder;

    [Header("武器槽位（按 1/2/3 切换：0=步枪 1=冲锋枪 2=手枪）")]
    public GameObject[] weapons;

    private FirearmWeapon _currentWeapon;
    private PlayerInputHandler _inputHandler;
    private int _currentWeaponIndex;   // 当前武器槽位索引

    // 弹药变化事件（弹匣当前/弹匣最大），供 HUD 监听
    public event Action<int, int> OnAmmoChanged;

    // 备弹变化事件，供 HUD 监听
    public event Action<int> OnReserveAmmoChanged;

    private int _lastAmmo;
    private int _lastReserve;

    private void Start()
    {
        if (weaponHolder == null)
            weaponHolder = transform.Find("WeaponHolder");
        _inputHandler = GetComponent<PlayerInputHandler>();

        if (weaponHolder != null)
        {
            _currentWeapon = weaponHolder.GetComponentInChildren<FirearmWeapon>();
            if (_currentWeapon != null)
            {
                _lastAmmo = _currentWeapon.GetCurrentAmmo();
                _lastReserve = _currentWeapon.GetReserveAmmo();
            }
        }
    }

    private void Update()
    {
        if (_inputHandler == null) return;

        // 切枪检测（武器不存在时也允许切换）
        int switchIndex = _inputHandler.GetWeaponSwitchIndex();
        if (switchIndex >= 0)
            TrySwitchWeapon(switchIndex);

        if (_currentWeapon == null) return;

        if (_inputHandler.IsShootPressed())
            _currentWeapon.Shoot();

        if (_inputHandler.IsReloadPressed())
            _currentWeapon.Reload();

        // 检测弹匣变化
        int current = _currentWeapon.GetCurrentAmmo();
        if (current != _lastAmmo)
        {
            _lastAmmo = current;
            OnAmmoChanged?.Invoke(current, _currentWeapon.GetMaxAmmo());
        }

        // 检测备弹变化
        int reserve = _currentWeapon.GetReserveAmmo();
        if (reserve != _lastReserve)
        {
            _lastReserve = reserve;
            OnReserveAmmoChanged?.Invoke(reserve);
        }
    }

    /// <summary>按槽位索引切枪：越界/空槽/重复切换均忽略</summary>
    public void TrySwitchWeapon(int index)
    {
        if (weapons == null || index < 0 || index >= weapons.Length || weapons[index] == null)
            return;
        if (index == _currentWeaponIndex && _currentWeapon != null)
            return;

        _currentWeaponIndex = index;
        EquipWeapon(weapons[index]);
    }

    public void EquipWeapon(GameObject weaponPrefab)
    {
        if (weaponPrefab == null || weaponHolder == null) return;

        if (_currentWeapon != null)
            Destroy(_currentWeapon.gameObject);

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
            _lastReserve = _currentWeapon.GetReserveAmmo();
            OnAmmoChanged?.Invoke(_lastAmmo, _currentWeapon.GetMaxAmmo());
            OnReserveAmmoChanged?.Invoke(_lastReserve);
        }
    }

    public int GetCurrentAmmo() => _currentWeapon != null ? _currentWeapon.GetCurrentAmmo() : 0;

    public int GetMaxAmmo() => _currentWeapon != null ? _currentWeapon.GetMaxAmmo() : 0;

    public int GetReserveAmmo() => _currentWeapon != null ? _currentWeapon.GetReserveAmmo() : 0;
}
