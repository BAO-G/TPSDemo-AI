using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 战斗表现控制器（4C/4D）：监听 GameEvents 表现类事件，从对象池生成视觉对象
/// 场景级单例（每场景一个），预制体在 Inspector 拖入
/// 曳光（4C）/ 血特效+命中烟尘+弹痕（4D）
/// </summary>
public class CombatEffectsController : MonoBehaviour
{
    public static CombatEffectsController Instance { get; private set; }

    [Header("曳光预制体（LineRenderer + BulletTrail）")]
    public GameObject bulletTrailPrefab;

    [Header("曳光预热数量")]
    public int trailPrewarmCount = 8;

    [Header("命中敌人血特效（PolygonBattleRoyale BloodSplat_FX）")]
    public GameObject bloodFxPrefab;

    [Header("命中静态物烟尘（PolygonBattleRoyale GunShot_Smoke_FX）")]
    public GameObject impactSmokePrefab;

    [Header("弹痕预制体（Quad + 弹孔贴图）")]
    public GameObject bulletDecalPrefab;

    [Header("弹痕上限（环形覆盖）")]
    public int maxDecals = 50;

    [Header("4E 爆炸特效（PolygonBattleRoyale Explosion_Small_FX）")]
    public GameObject explosionFxPrefab;

    [Header("4E 震屏（TPSCamera 上的 CameraShaker）")]
    public CameraShaker cameraShaker;
    public float shakeBaseAmplitude = 0.8f;         // 爆心震屏幅度
    public float shakeRange = 25f;                  // 震屏有效范围（米，超出不震）

    // 弹痕环形队列：超上限回收最早的复用
    private readonly Queue<GameObject> _decals = new();

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (bulletTrailPrefab != null)
            ObjectPool.Prewarm(bulletTrailPrefab, trailPrewarmCount);
        if (bloodFxPrefab != null)
            ObjectPool.Prewarm(bloodFxPrefab, 4);
        if (impactSmokePrefab != null)
            ObjectPool.Prewarm(impactSmokePrefab, 4);
        if (explosionFxPrefab != null)
            ObjectPool.Prewarm(explosionFxPrefab, 2);
    }

    private void OnEnable()
    {
        GameEvents.OnBulletTrail += HandleBulletTrail;
        GameEvents.OnSurfaceHit += HandleSurfaceHit;
        GameEvents.OnExplosion += HandleExplosion;
    }

    private void OnDisable()
    {
        GameEvents.OnBulletTrail -= HandleBulletTrail;
        GameEvents.OnSurfaceHit -= HandleSurfaceHit;
        GameEvents.OnExplosion -= HandleExplosion;
    }

    /// <summary>爆炸表现：爆炸特效 + 按距离衰减震屏（音效由 4B AudioManager 接）</summary>
    private void HandleExplosion(Vector3 pos)
    {
        SpawnFx(explosionFxPrefab, pos, Vector3.up);

        if (cameraShaker != null)
        {
            // 震屏距离以主相机（玩家视点）为准
            var cam = Camera.main;
            float dist = cam != null ? Vector3.Distance(cam.transform.position, pos) : float.MaxValue;
            float falloff = Mathf.Clamp01(1f - dist / shakeRange);
            if (falloff > 0f)
                cameraShaker.Shake(shakeBaseAmplitude * falloff);
        }
    }

    private void HandleBulletTrail(Vector3 start, Vector3 end)
    {
        if (bulletTrailPrefab == null) return;
        var go = ObjectPool.Get(bulletTrailPrefab, Vector3.zero, Quaternion.identity);
        if (go == null) return;
        var trail = go.GetComponent<BulletTrail>();
        if (trail != null)
            trail.Show(start, end);
    }

    /// <summary>命中表面表现：打中敌人溅血；打中静态物烟尘+弹痕（贴法线，环形队列）</summary>
    private void HandleSurfaceHit(Vector3 point, Vector3 normal, bool isEnemy)
    {
        if (isEnemy)
        {
            SpawnFx(bloodFxPrefab, point, normal);
            return;
        }

        SpawnFx(impactSmokePrefab, point, normal);
        SpawnDecal(point, normal);
    }

    /// <summary>从对象池取特效实例，朝向按命中法线；粒子 stopAction 统一改 Disable 以兼容池化</summary>
    private void SpawnFx(GameObject prefab, Vector3 point, Vector3 normal)
    {
        if (prefab == null) return;
        var rot = Quaternion.LookRotation(normal);
        var go = ObjectPool.Get(prefab, point, rot);
        if (go == null) return;
        go.transform.position = point;
        go.transform.rotation = rot;
        ConfigurePooledParticles(go);
        go.SetActive(true);
    }

    /// <summary>包内 FX 预制体默认播完 Destroy（破坏池化）；统一改为 Disable 停止后自动隐藏回池</summary>
    private void ConfigurePooledParticles(GameObject go)
    {
        var systems = go.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in systems)
        {
            var main = ps.main;
            if (main.stopAction == ParticleSystemStopAction.Destroy)
                main.stopAction = ParticleSystemStopAction.Disable;
        }
    }

    /// <summary>弹痕：贴命中面（沿法线外偏 0.01m 防闪面），超上限回收最早实例循环覆盖</summary>
    private void SpawnDecal(Vector3 point, Vector3 normal)
    {
        if (bulletDecalPrefab == null) return;

        GameObject decal;
        if (_decals.Count >= maxDecals)
        {
            // 复用最早的弹痕（可能已被手动销毁，跳过空引用）
            do
            {
                decal = _decals.Dequeue();
            } while (decal == null && _decals.Count > 0);
        }
        else
        {
            decal = ObjectPool.Get(bulletDecalPrefab, point, Quaternion.identity);
        }

        if (decal == null) return;
        decal.transform.position = point + normal * 0.01f;
        decal.transform.rotation = Quaternion.LookRotation(normal);
        decal.SetActive(true);
        _decals.Enqueue(decal);
    }
}
