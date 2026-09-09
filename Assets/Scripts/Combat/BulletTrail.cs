using UnityEngine;

/// <summary>
/// 曳光弹道视觉（4C，决策 D1）：LineRenderer 两点连线，存活 lifetime 后自动回对象池
/// 由 CombatEffectsController 响应 GameEvents.OnBulletTrail 时经 ObjectPool 取用
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class BulletTrail : MonoBehaviour
{
    [Header("存活时长（秒）")]
    public float lifetime = 0.08f;

    private LineRenderer _line;
    private float _timer;

    private void Awake()
    {
        _line = GetComponent<LineRenderer>();
    }

    /// <summary>显示一条从 start 到 end 的曳光（世界坐标）</summary>
    public void Show(Vector3 start, Vector3 end)
    {
        if (_line == null) _line = GetComponent<LineRenderer>();
        _line.positionCount = 2;
        _line.SetPosition(0, start);
        _line.SetPosition(1, end);
        _timer = lifetime;
        enabled = true;
    }

    private void Update()
    {
        _timer -= Time.deltaTime;
        if (_timer <= 0f)
            ObjectPool.Release(gameObject);
    }
}
