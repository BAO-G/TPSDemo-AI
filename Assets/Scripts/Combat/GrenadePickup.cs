using UnityEngine;

/// <summary>
/// 手雷拾取物（阶段5，D7）：玩家靠近自动拾取，AddGrenades(1)（上限 3 钳制在 GrenadeThrower）
/// 复制 HealthPickup 模式：Trigger + Player tag 过滤；拾取广播 OnPickupTaken（4B 音效预留）后销毁自身
/// </summary>
public class GrenadePickup : MonoBehaviour
{
    [Header("拾取参数")]
    public int grenadeAmount = 1;                    // 增加手雷数量

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        var thrower = other.GetComponent<GrenadeThrower>();
        if (thrower == null) return;

        thrower.AddGrenades(grenadeAmount);
        GameEvents.RaisePickupTaken();
        Destroy(gameObject);
    }

    private void Awake()
    {
        // 确保有 Trigger Collider（HealthPickup 同模式）
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
