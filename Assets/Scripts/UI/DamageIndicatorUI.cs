using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 受击方向指示器（阶段6 S5，PHASE6_PLAN §2.6）：屏幕中央径向扇形指向攻击来向，1s 淡出，4 实例轮换
/// 挂主 Canvas；扇形贴图 Inspector 指定（程序化生成 DamageIndicator.png，尖头朝上=正前方）
/// </summary>
public class DamageIndicatorUI : MonoBehaviour
{
    [SerializeField] private Sprite indicatorSprite;   // 扇形贴图（尖头朝上）
    [SerializeField] private int instanceCount = 4;    // 同屏实例数（连续受击不互相打断）
    [SerializeField] private float indicatorDistance = 130f;  // 距屏幕中心像素
    [SerializeField] private float fadeDuration = 1f;  // 淡出时长

    private RectTransform[] _instances;
    private float[] _fadeTimers;
    private int _next;

    private void Awake()
    {
        _instances = new RectTransform[instanceCount];
        _fadeTimers = new float[instanceCount];
        for (int i = 0; i < instanceCount; i++)
        {
            var go = new GameObject("Indicator" + i, typeof(Image));
            go.transform.SetParent(transform, false);
            var img = go.GetComponent<Image>();
            img.sprite = indicatorSprite;
            img.raycastTarget = false;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);   // 锚定屏幕中心
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(96f, 96f);
            go.SetActive(false);
            _instances[i] = rt;
        }
    }

    private void OnEnable()
    {
        GameEvents.OnPlayerDamagedFrom += HandleDamagedFrom;
    }

    private void OnDisable()
    {
        GameEvents.OnPlayerDamagedFrom -= HandleDamagedFrom;
    }

    /// <summary>受击：以相机水平朝向为 0°（屏幕正上），扇形旋转指向攻击来向</summary>
    private void HandleDamagedFrom(Vector3 sourcePos)
    {
        if (sourcePos == Vector3.zero) return;   // 来源未知不显示
        var cam = Camera.main;
        if (cam == null) return;

        Vector3 toSource = sourcePos - cam.transform.position;
        toSource.y = 0f;
        if (toSource.sqrMagnitude < 0.01f) return;
        float angle = Vector3.SignedAngle(cam.transform.forward, toSource, Vector3.up);

        var rt = _instances[_next];
        int idx = _next;
        _next = (_next + 1) % instanceCount;

        // 位置=来向方向 × 距中心距离；旋转=贴图尖头(上)对准来向（屏幕顺时针为正角）
        float rad = angle * Mathf.Deg2Rad;
        rt.anchoredPosition = new Vector2(Mathf.Sin(rad), Mathf.Cos(rad)) * indicatorDistance;
        rt.localRotation = Quaternion.Euler(0f, 0f, -angle);

        rt.gameObject.SetActive(true);
        _fadeTimers[idx] = fadeDuration;
        var img = rt.GetComponent<Image>();
        var c = img.color;
        c.a = 1f;
        img.color = c;
    }

    private void Update()
    {
        for (int i = 0; i < _instances.Length; i++)
        {
            if (!_instances[i].gameObject.activeSelf) continue;
            _fadeTimers[i] -= Time.deltaTime;
            var img = _instances[i].GetComponent<Image>();
            var c = img.color;
            c.a = Mathf.Clamp01(_fadeTimers[i] / fadeDuration);
            img.color = c;
            if (_fadeTimers[i] <= 0f)
                _instances[i].gameObject.SetActive(false);
        }
    }
}
