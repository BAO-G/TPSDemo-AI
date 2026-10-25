using UnityEngine;

/// <summary>
/// 粒子播完自动回池（S6）：配合 ParticleSystemStopAction.Callback 使用——粒子停止时 Unity
/// 回调 OnParticleSystemStopped，将整个 FX 实例 Release 回对象池。
/// 挂在携带 ParticleSystem 的物体上，rootToRelease 指向池实例根物体。
/// 修复背景：此前 stopAction=Disable 只就地停用、从未 Release 回队列，池退化成"每次新建+孤儿堆积"
/// </summary>
public class PoolParticleRelease : MonoBehaviour
{
    [HideInInspector] public GameObject rootToRelease;

    public void OnParticleSystemStopped()
    {
        ObjectPool.Release(rootToRelease != null ? rootToRelease : gameObject);
    }
}
