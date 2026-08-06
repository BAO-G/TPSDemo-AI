using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 准星控制器：屏幕中心准星 + 敌人识别变色（A+B 方案）
/// 每帧从相机屏幕中心发射射线，命中敌人（EnemyBase）时准星变红，否则白色
/// 挂载在 Canvas 上
/// </summary>
public class CrosshairController : MonoBehaviour
{
    [Header("准星引用")]
    public Image crosshairImage;

    [Header("颜色")]
    public Color normalColor = Color.white;
    public Color enemyColor = Color.red;

    [Header("射线参数")]
    public float maxDistance = 200f;
    public LayerMask raycastLayers = ~0; // 默认所有层

    private void Update()
    {
        if (crosshairImage == null) return;
        if (Camera.main == null) return;

        // 屏幕中心射线
        Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f));
        bool onEnemy = false;

        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, raycastLayers))
        {
            if (hit.collider.GetComponentInParent<EnemyBase>() != null)
                onEnemy = true;
        }

        crosshairImage.color = onEnemy ? enemyColor : normalColor;
    }
}
