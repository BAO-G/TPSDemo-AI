using UnityEngine;

/// <summary>
/// 武器静态数据资产：伤害、射速、弹匣、换弹、散布等参数
/// </summary>
[CreateAssetMenu(fileName = "WeaponData", menuName = "Combat/WeaponData")]
public class WeaponData : ScriptableObject
{
    public string weaponName = "默认步枪";
    public float damage = 25f;
    public float fireRate = 600f;        // RPM（每分钟射速）
    public int magazineSize = 30;
    public float reloadTime = 2f;        // 秒
    public float maxRange = 200f;
    public float bulletSpread = 0.02f;   // 散布角度（弧度）
    public float recoilAmount = 1.5f;    // 后坐力幅度
    public GameObject weaponPrefab;      // 武器模型预制体引用
    public GameObject muzzleFlashPrefab; // 枪口特效（可选）
}
