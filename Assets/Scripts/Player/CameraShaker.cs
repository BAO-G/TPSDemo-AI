using UnityEngine;

/// <summary>
/// 相机震屏（4E）：挂在 TPSCamera 上，驱动 Cinemachine 噪声组件（Perlin）实现震动
/// CombatEffectsController 订阅 OnExplosion 后按距离衰减调用 Shake
/// 幅度指数衰减到 0 后钳零，避免噪声常驻
/// </summary>
public class CameraShaker : MonoBehaviour
{
    [Header("衰减参数")]
    public float decaySpeed = 6f;                   // 幅度衰减速度（越大震得越快停）

    private Unity.Cinemachine.CinemachineBasicMultiChannelPerlin _noise;
    private float _amplitude;

    private void Awake()
    {
        _noise = GetComponent<Unity.Cinemachine.CinemachineBasicMultiChannelPerlin>();
        if (_noise == null)
            _noise = gameObject.AddComponent<Unity.Cinemachine.CinemachineBasicMultiChannelPerlin>();
        _noise.AmplitudeGain = 0f;
    }

    private void Update()
    {
        if (_amplitude <= 0.001f) return;

        _amplitude = Mathf.Lerp(_amplitude, 0f, Time.deltaTime * decaySpeed);
        if (_amplitude <= 0.001f)
        {
            _amplitude = 0f;
            _noise.AmplitudeGain = 0f;
        }
        else
        {
            _noise.AmplitudeGain = _amplitude;
        }
    }

    /// <summary>触发震动（幅度取历史最大，避免连爆时互相覆盖变小）</summary>
    public void Shake(float amplitude)
    {
        _amplitude = Mathf.Max(_amplitude, amplitude);
    }
}
