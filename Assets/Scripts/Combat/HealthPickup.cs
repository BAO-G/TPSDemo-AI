using UnityEngine;

/// <summary>
/// 医疗包拾取物：玩家靠近时自动拾取，增加医疗包数量
/// 挂载在医疗包场景物体上，需要 Collider(Trigger)
/// </summary>
public class HealthPickup : MonoBehaviour
{
    [Header("拾取参数")]
    public int medkitAmount = 1;                     // 恢复医疗包数量

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        var playerHealth = other.GetComponent<PlayerHealth>();
        if (playerHealth == null) return;

        playerHealth.AddMedkit(medkitAmount);
        Destroy(gameObject);
    }

    private void Awake()
    {
        // 确保有 Trigger Collider
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
