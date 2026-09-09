using UnityEngine;

/// <summary>
/// 敌人部位标记（4C 部位伤害）：挂在头/身体碰撞体上，FirearmWeapon 命中时区分倍率
/// Owner 懒查找父级 EnemyBase
/// </summary>
public class EnemyPart : MonoBehaviour
{
    /// <summary>是否为头部（爆头倍率判定）</summary>
    public bool isHead;

    private EnemyBase _owner;

    /// <summary>所属敌人（首次访问时向上查找并缓存）</summary>
    public EnemyBase Owner
    {
        get
        {
            if (_owner == null)
                _owner = GetComponentInParent<EnemyBase>();
            return _owner;
        }
    }
}
