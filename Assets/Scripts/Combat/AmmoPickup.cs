using UnityEngine;

/// <summary>
/// 弹药拾取物：玩家靠近时自动拾取，补充备弹池
/// 挂载在弹药包场景物体上，需要 Collider(Trigger)
/// </summary>
public class AmmoPickup : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        var weaponManager = other.GetComponent<WeaponManager>();
        if (weaponManager == null || weaponManager.weaponHolder == null) return;

        var firearm = weaponManager.weaponHolder.GetComponentInChildren<FirearmWeapon>();
        if (firearm == null) return;

        // 从 WeaponData 读取拾取量
        int amount = (firearm.weaponData != null) ? firearm.weaponData.ammoPickupAmount : 30;
        firearm.AddReserveAmmo(amount);
        Destroy(gameObject);
    }

    private void Awake()
    {
        // 确保有 Collider 组件且为 Trigger
        var col = GetComponent<Collider>();
        if (col == null)
        {
            var sphereCol = gameObject.AddComponent<SphereCollider>();
            sphereCol.isTrigger = true;
            sphereCol.radius = 2f;
        }
        else
        {
            col.isTrigger = true;
        }
    }
}
