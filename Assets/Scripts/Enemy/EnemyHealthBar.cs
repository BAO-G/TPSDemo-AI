using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 敌人头顶血条（阶段6 S5，PHASE6_PLAN §2.5）：受击显示 3s 自动隐藏，Billboard 朝向主相机，死亡永久隐藏
/// 运行时构建世界空间 Canvas（免 prefab 层级手术，坑 55~57 规避），挂敌人根物体
/// </summary>
public class EnemyHealthBar : MonoBehaviour
{
    [Header("血条参数")]
    public float barHeight = 2.2f;         // 头顶高度（米）
    public float barWidth = 0.6f;
    public float barThickness = 0.07f;
    public float showDuration = 3f;        // 受击后显示时长（秒）

    private Canvas _canvas;
    private Image _fill;
    private float _hideTimer;
    private bool _dead;
    private Transform _cam;

    private void Awake()
    {
        // 世界空间 Canvas（作为敌人子物体跟随移动）
        var go = new GameObject("HealthBarCanvas");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = Vector3.up * barHeight;
        _canvas = go.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.WorldSpace;
        var cam = Camera.main;
        if (cam != null) _canvas.worldCamera = cam;
        var rt = _canvas.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(barWidth, barThickness);

        // 背景底板
        var bgGo = new GameObject("Bg", typeof(Image));
        bgGo.transform.SetParent(go.transform, false);
        var bgRt = bgGo.GetComponent<RectTransform>();
        bgRt.sizeDelta = new Vector2(barWidth, barThickness);
        bgGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

        // 填充条（左对齐，改 sizeDelta 显示血量比例）
        var fillGo = new GameObject("Fill", typeof(Image));
        fillGo.transform.SetParent(bgGo.transform, false);
        var fillRt = fillGo.GetComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.zero;
        fillRt.pivot = new Vector2(0f, 0.5f);
        fillRt.anchoredPosition = new Vector2(-barWidth / 2f, 0f);
        fillRt.sizeDelta = new Vector2(barWidth, barThickness);
        _fill = fillGo.GetComponent<Image>();
        _fill.color = new Color(0.85f, 0.15f, 0.15f, 1f);

        go.SetActive(false);   // 默认隐藏，受击才显示
    }

    /// <summary>受击时调用：显示血条并按比例填充</summary>
    public void ShowDamage(float ratio)
    {
        if (_dead || _canvas == null) return;
        RefreshCam();
        _canvas.gameObject.SetActive(true);
        if (_fill != null)
            _fill.rectTransform.sizeDelta = new Vector2(barWidth * Mathf.Clamp01(ratio), barThickness);
        _hideTimer = showDuration;
    }

    /// <summary>死亡永久隐藏（尸体不留血条）</summary>
    public void HidePermanent()
    {
        _dead = true;
        if (_canvas != null)
            _canvas.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (_dead || _canvas == null || !_canvas.gameObject.activeSelf) return;
        _hideTimer -= Time.deltaTime;
        if (_hideTimer <= 0f)
            _canvas.gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        if (_dead || _canvas == null || !_canvas.gameObject.activeSelf) return;
        RefreshCam();
        if (_cam != null)
            _canvas.transform.rotation = _cam.rotation;   // Billboard 朝向相机
    }

    /// <summary>场景重载后主相机引用可能失效，延迟重取</summary>
    private void RefreshCam()
    {
        if (_cam == null && Camera.main != null)
        {
            _cam = Camera.main.transform;
            _canvas.worldCamera = _cam.GetComponent<Camera>();
        }
    }
}
