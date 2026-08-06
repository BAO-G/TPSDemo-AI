using UnityEngine;

/// <summary>
/// 玩家手部 IK 控制器：武器挂在右手骨骼（hand_R）时，右手跟随武器自然握枪
/// 左手用 Animator IK 吸附到武器护木挂点（LeftHandIk），确保左手贴合护木
/// 换弹时左手 IK 权重归零，让换弹动画正常驱动
/// 注意：此脚本必须挂在 Animator 所在的 GameObject 上（OnAnimatorIK 由该对象的 Animator 触发）
/// </summary>
public class PlayerIKController : MonoBehaviour
{
    private Animator _animator;
    private Transform _leftHandIk;
    private int _reloadLayerIndex;
    public static int IkCallCount; // 诊断：统计 OnAnimatorIK 调用次数

    private void Start()
    {
        _animator = GetComponent<Animator>();
        if (_animator != null)
            _reloadLayerIndex = _animator.GetLayerIndex("Reload");
        RefreshWeaponRefs();
    }

    private void Update()
    {
        // 切枪后刷新武器 IK 挂点引用
        if (_leftHandIk == null)
            RefreshWeaponRefs();
    }

    private void RefreshWeaponRefs()
    {
        _leftHandIk = null;

        // 从 Player 根节点递归查找当前武器（FirearmWeapon 组件），获取护木挂点
        var weapon = transform.root.GetComponentInChildren<FirearmWeapon>();
        if (weapon != null)
        {
            _leftHandIk = weapon.transform.Find("LeftHandIk");
        }
    }

    private void OnAnimatorIK(int layerIndex)
    {
        IkCallCount++;
        if (_animator == null) return;

        // 换弹时左手让位给换弹动画
        var weapon = transform.root.GetComponentInChildren<FirearmWeapon>();
        bool isReloading = weapon != null && weapon.IsReloading;
        float leftWeight = isReloading ? 0f : 1f;

        // 左手：位置吸附到护木（旋转保留动画自然朝向，避免手部扭曲）
        if (_leftHandIk != null)
        {
            _animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, leftWeight);
            _animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0f);
            if (leftWeight > 0f)
            {
                _animator.SetIKPosition(AvatarIKGoal.LeftHand, _leftHandIk.position);
            }
        }
        else
        {
            _animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0f);
            _animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0f);
        }
    }
}
