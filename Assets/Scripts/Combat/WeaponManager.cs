using System;
using System.Collections.Generic;
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
    private Animator _animator;        // 驱动 ChangeWeapon 切枪动画

    // 弹药变化事件（弹匣当前/弹匣最大），供 HUD 监听
    public event Action<int, int> OnAmmoChanged;

    // 备弹变化事件，供 HUD 监听
    public event Action<int> OnReserveAmmoChanged;

    private int _lastAmmo;
    private int _lastReserve;

    // F1 修复：各槽位弹药缓存（切枪销毁前保存、切回时恢复，不再刷满弹）
    private readonly Dictionary<int, (int mag, int reserve)> _ammoCache = new();

    private void Start()
    {
        if (weaponHolder == null)
            weaponHolder = transform.Find("WeaponHolder");
        _inputHandler = GetComponent<PlayerInputHandler>();
        _animator = GetComponentInChildren<Animator>();

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

        // 自动射击：设置开启且准星指向敌人时自动开火
        if (GameSettings.AutoShoot && IsCrosshairOnEnemy())
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
    /// <summary>准星是否指向敌人（自动射击用，查找场景中的 CrosshairController）</summary>
    private bool IsCrosshairOnEnemy()
    {
        var crosshair = FindAnyObjectByType<CrosshairController>();
        return crosshair != null && crosshair.IsOnEnemy;
    }

    public void TrySwitchWeapon(int index)
    {
        if (weapons == null || index < 0 || index >= weapons.Length || weapons[index] == null)
            return;
        if (index == _currentWeaponIndex && _currentWeapon != null)
            return;

        int oldIndex = _currentWeaponIndex;
        _currentWeaponIndex = index;
        EquipWeapon(weapons[index], oldIndex);
        // 触发切枪动画（ChangeWeapon 层）
        _animator?.SetTrigger("ChangeWeapon");
    }

    /// <summary>装备武器；oldSlotIndex 为换下武器的槽位（用于保存其弹药缓存）</summary>
    public void EquipWeapon(GameObject weaponPrefab, int oldSlotIndex = -1)
    {
        if (weaponPrefab == null || weaponHolder == null) return;

        // F1：换下武器前把当前弹药存入对应槽位缓存
        if (_currentWeapon != null && oldSlotIndex >= 0)
            _ammoCache[oldSlotIndex] = (_currentWeapon.GetCurrentAmmo(), _currentWeapon.GetReserveAmmo());

        if (_currentWeapon != null)
            Destroy(_currentWeapon.gameObject);

        var weapon = Instantiate(weaponPrefab, weaponHolder);
        // 位置/旋转使用武器预制体内保存的挂枪姿势（不再重置，否则切枪后武器偏离双手）

        _currentWeapon = weapon.GetComponent<FirearmWeapon>();
        if (_currentWeapon == null)
        {
            Debug.LogError("武器预制体缺少 FirearmWeapon 组件", weapon);
        }
        else
        {
            // F1：槽位有缓存则写入挂起值，武器 Start 时恢复（Instantiate 后 Start 延迟执行，直接 Set 会被覆盖）
            if (_ammoCache.TryGetValue(_currentWeaponIndex, out var cached))
            {
                _currentWeapon.pendingMagazine = cached.mag;
                _currentWeapon.pendingReserve = cached.reserve;
            }
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
