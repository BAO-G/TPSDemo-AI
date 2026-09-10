using System;
using UnityEngine;

/// <summary>
/// 全局静态事件总线（决策 D2：轻量静态事件）
/// 全游戏唯一跨系统消息通道；事件只增不改签名（破坏性变更需 architect 批准）
/// 订阅方必须在 OnDisable/OnDestroy 退订
/// </summary>
public static class GameEvents
{
    // ===== 玩家 =====
    /// <summary>玩家受击</summary>
    public static event Action OnPlayerDamaged;
    /// <summary>玩家死亡（GameManager/结算订阅）</summary>
    public static event Action OnPlayerDeath;
    /// <summary>玩家开枪（枪口位置 + 武器名，音效/噪音）</summary>
    public static event Action<Vector3, string> OnPlayerShot;
    /// <summary>换弹开始</summary>
    public static event Action OnReloadStart;
    /// <summary>换弹结束</summary>
    public static event Action OnReloadEnd;

    // ===== 敌人 =====
    /// <summary>敌人死亡（波次/计分订阅）</summary>
    public static event Action<Vector3> OnEnemyKilled;
    /// <summary>爆头击杀（UI 提示）</summary>
    public static event Action OnHeadshotKilled;
    /// <summary>敌人受击（音效）</summary>
    public static event Action<Vector3> OnEnemyDamaged;
    /// <summary>敌人开枪（音效/受击方向）</summary>
    public static event Action<Vector3> OnEnemyShot;

    // ===== 战斗表现 =====
    /// <summary>曳光（起点,终点）</summary>
    public static event Action<Vector3, Vector3> OnBulletTrail;
    /// <summary>爆炸（手雷）</summary>
    public static event Action<Vector3> OnExplosion;
    /// <summary>命中反馈（HitMarker）</summary>
    public static event Action OnHitConfirmed;
    /// <summary>拾取</summary>
    public static event Action OnPickupTaken;
    /// <summary>子弹命中表面（命中点,法线,是否命中敌人）——4D 新增：血特效/命中烟尘/弹痕共用</summary>
    public static event Action<Vector3, Vector3, bool> OnSurfaceHit;

    /// <summary>手雷数量变化（4E：HUD 显示剩余手雷）</summary>
    public static event Action<int> OnGrenadeCountChanged;

    // ===== 流程（阶段5） =====
    /// <summary>波次开始（波号）</summary>
    public static event Action<int> OnWaveStarted;
    /// <summary>波次清空（波号）</summary>
    public static event Action<int> OnWaveCleared;
    /// <summary>UI 点击音</summary>
    public static event Action OnUIClick;

    // ===== Raise 方法（内部 null 检查，命名 Raise{事件名去掉On}） =====

    public static void RaisePlayerDamaged() => OnPlayerDamaged?.Invoke();

    public static void RaisePlayerDeath() => OnPlayerDeath?.Invoke();

    public static void RaisePlayerShot(Vector3 pos, string weaponName) => OnPlayerShot?.Invoke(pos, weaponName);

    public static void RaiseReloadStart() => OnReloadStart?.Invoke();

    public static void RaiseReloadEnd() => OnReloadEnd?.Invoke();

    public static void RaiseEnemyKilled(Vector3 pos) => OnEnemyKilled?.Invoke(pos);

    public static void RaiseHeadshotKilled() => OnHeadshotKilled?.Invoke();

    public static void RaiseEnemyDamaged(Vector3 pos) => OnEnemyDamaged?.Invoke(pos);

    public static void RaiseEnemyShot(Vector3 pos) => OnEnemyShot?.Invoke(pos);

    public static void RaiseBulletTrail(Vector3 start, Vector3 end) => OnBulletTrail?.Invoke(start, end);

    public static void RaiseExplosion(Vector3 pos) => OnExplosion?.Invoke(pos);

    public static void RaiseHitConfirmed() => OnHitConfirmed?.Invoke();

    public static void RaisePickupTaken() => OnPickupTaken?.Invoke();

    public static void RaiseSurfaceHit(Vector3 point, Vector3 normal, bool isEnemy) => OnSurfaceHit?.Invoke(point, normal, isEnemy);

    public static void RaiseWaveStarted(int wave) => OnWaveStarted?.Invoke(wave);

    public static void RaiseWaveCleared(int wave) => OnWaveCleared?.Invoke(wave);

    public static void RaiseUIClick() => OnUIClick?.Invoke();

    public static void RaiseGrenadeCountChanged(int count) => OnGrenadeCountChanged?.Invoke(count);
}
