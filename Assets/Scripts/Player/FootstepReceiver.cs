using UnityEngine;

/// <summary>
/// 动画事件接收器：接收角色动画（Mixamo/BSP/TPS）中回调的动画事件
/// 挂在角色模型（Animator 所在物体）上，消除"事件无接收者"报错
/// </summary>
public class FootstepReceiver : MonoBehaviour
{
    [Tooltip("脚步音源（可选，为空则静默接收事件）")]
    public AudioSource footstepAudio;

    /// <summary>动画事件回调：Mixamo/BSP 动画每步触发一次</summary>
    public void PlayFootstepSound()
    {
        if (footstepAudio != null && footstepAudio.clip != null)
            footstepAudio.Play();
    }

    // 以下为 TPS Shooter 动画自带事件的空接收器（换弹/切枪完成由各自协程控制，事件本身不使用）

    /// <summary>TPS 换弹动画结束事件（换弹完成由 FirearmWeapon.ReloadRoutine 控制，此处忽略）</summary>
    public void FinishedReloading() { }

    /// <summary>TPS 切枪动画：开始切换</summary>
    public void StartChangingWeapon() { }

    /// <summary>TPS 切枪动画：卸下旧武器</summary>
    public void UnequipEvent() { }

    /// <summary>TPS 切枪动画：完成切换</summary>
    public void FinishChangingWeapon() { }
}
