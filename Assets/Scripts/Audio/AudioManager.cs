using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 音频管理单例：事件驱动播音，只听事件不主动查询
/// 2D UI 音源 ×1（走 MasterVolume）+ 3D 世界音源池 ×8（走 SFXVolume × MasterVolume，SpatialBlend=1）
/// 音频资产在 4B 生成后由 Inspector 拖入；资产为空时静默跳过
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("枪声（按武器名匹配）")]
    public AudioClip playerShotRifle;
    public AudioClip playerShotSMG;
    public AudioClip playerShotPistol;

    [Header("换弹")]
    public AudioClip reloadStartClip;
    public AudioClip reloadEndClip;

    [Header("受击/死亡")]
    public AudioClip playerHurtClip;
    public AudioClip enemyHurtClip;
    public AudioClip enemyDeathClip;
    public AudioClip enemyShotClip;

    [Header("交互/表现")]
    public AudioClip pickupClip;
    public AudioClip explosionClip;
    public AudioClip uiClickClip;

    [Header("脚步变体（随机抽取 + 随机 pitch）")]
    public AudioClip[] footstepClips;

    [Header("音源池配置")]
    public int sfxSourceCount = 8;

    private AudioSource _uiSource;                 // 2D UI 音源
    private readonly List<AudioSource> _sfxSources = new();
    private int _nextSfxIndex;

    private void Awake()
    {
        // 单例守卫：场景内重复挂载时保留首个
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        SetupSources();
    }

    private void OnEnable()
    {
        // 全部走 GameEvents（音效表见 DEVELOPMENT_PLAN §5.2）
        GameEvents.OnPlayerShot += HandlePlayerShot;
        GameEvents.OnReloadStart += HandleReloadStart;
        GameEvents.OnReloadEnd += HandleReloadEnd;
        GameEvents.OnPlayerDamaged += HandlePlayerDamaged;
        GameEvents.OnEnemyDamaged += HandleEnemyDamaged;
        GameEvents.OnEnemyKilled += HandleEnemyKilled;
        GameEvents.OnEnemyShot += HandleEnemyShot;
        GameEvents.OnPickupTaken += HandlePickupTaken;
        GameEvents.OnExplosion += HandleExplosion;
        GameEvents.OnUIClick += HandleUIClick;
    }

    private void OnDisable()
    {
        // 订阅方必须在 OnDisable/OnDestroy 退订
        GameEvents.OnPlayerShot -= HandlePlayerShot;
        GameEvents.OnReloadStart -= HandleReloadStart;
        GameEvents.OnReloadEnd -= HandleReloadEnd;
        GameEvents.OnPlayerDamaged -= HandlePlayerDamaged;
        GameEvents.OnEnemyDamaged -= HandleEnemyDamaged;
        GameEvents.OnEnemyKilled -= HandleEnemyKilled;
        GameEvents.OnEnemyShot -= HandleEnemyShot;
        GameEvents.OnPickupTaken -= HandlePickupTaken;
        GameEvents.OnExplosion -= HandleExplosion;
        GameEvents.OnUIClick -= HandleUIClick;
    }

    /// <summary>创建 2D UI 音源 + 3D 音源池</summary>
    private void SetupSources()
    {
        _uiSource = gameObject.AddComponent<AudioSource>();
        _uiSource.spatialBlend = 0f;
        _uiSource.playOnAwake = false;

        for (int i = 0; i < sfxSourceCount; i++)
        {
            var src = gameObject.AddComponent<AudioSource>();
            src.spatialBlend = 1f;          // 3D 世界音
            src.playOnAwake = false;
            _sfxSources.Add(src);
        }

        ApplyVolumes();
    }

    /// <summary>应用音量设置：UI 音源走 Master，3D 音源按播放时乘 SFX × Master</summary>
    public void ApplyVolumes()
    {
        GameSettings.ApplyAll();            // AudioListener.volume = MasterVolume（已有逻辑）
        if (_uiSource != null)
            _uiSource.volume = 1f;          // Master 由 AudioListener 统一承担
    }

    // ===== 事件处理 =====

    private void HandlePlayerShot(Vector3 pos, string weaponName) => PlayShot(pos, weaponName);

    private void HandleReloadStart()
    {
        if (_currentWeaponPosition == null) return;
        PlayReload(_currentWeaponPosition.Value, false);
    }

    private void HandleReloadEnd()
    {
        if (_currentWeaponPosition == null) return;
        PlayReload(_currentWeaponPosition.Value, true);
    }

    // 换弹音跟随玩家武器位置（换弹事件无位置参数，缓存最近一次开枪位置）
    private Vector3? _currentWeaponPosition;

    private void HandlePlayerDamaged() => Play2D(playerHurtClip);

    private void HandleEnemyDamaged(Vector3 pos) => Play3D(enemyHurtClip, pos);

    private void HandleEnemyKilled(Vector3 pos) => Play3D(enemyDeathClip, pos);

    private void HandleEnemyShot(Vector3 pos) => Play3D(enemyShotClip, pos);

    private void HandlePickupTaken() => Play2D(pickupClip);

    private void HandleExplosion(Vector3 pos) => Play3D(explosionClip, pos);

    private void HandleUIClick() => Play2D(uiClickClip);

    // ===== 公开播放接口 =====

    /// <summary>按武器名选枪声音效播放（3D，位置=枪口）</summary>
    public void PlayShot(Vector3 pos, string weaponName)
    {
        _currentWeaponPosition = pos;
        AudioClip clip;
        if (!string.IsNullOrEmpty(weaponName))
        {
            string n = weaponName.ToLowerInvariant();
            if (n.Contains("smg")) clip = playerShotSMG;
            else if (n.Contains("pistol")) clip = playerShotPistol;
            else clip = playerShotRifle;    // 步枪为默认兜底
        }
        else
        {
            clip = playerShotRifle;
        }
        Play3D(clip, pos);
    }

    /// <summary>换弹音（3D；isEnd=false 开始 / true 结束）</summary>
    public void PlayReload(Vector3 pos, bool isEnd) => Play3D(isEnd ? reloadEndClip : reloadStartClip, pos);

    /// <summary>脚步音（3D，随机变体 + pitch 0.9~1.1；speed 备用）</summary>
    public void PlayFootstep(Vector3 pos, float speed)
    {
        if (footstepClips == null || footstepClips.Length == 0) return;
        var clip = footstepClips[Random.Range(0, footstepClips.Length)];
        if (clip == null) return;
        var src = AcquireSfxSource(pos);
        if (src == null) return;
        src.pitch = Random.Range(0.9f, 1.1f);
        src.PlayOneShot(clip);
    }

    // ===== 内部播放 =====

    private void Play2D(AudioClip clip)
    {
        if (clip == null || _uiSource == null) return;
        _uiSource.PlayOneShot(clip);
    }

    private void Play3D(AudioClip clip, Vector3 pos)
    {
        if (clip == null) return;
        var src = AcquireSfxSource(pos);
        if (src == null) return;
        src.PlayOneShot(clip);
    }

    /// <summary>轮询取一个空闲 3D 音源，全部占用时复用最早的（ Newest wins 交给 PlayOneShot 叠加）</summary>
    private AudioSource AcquireSfxSource(Vector3 pos)
    {
        if (_sfxSources.Count == 0) return null;
        var src = _sfxSources[_nextSfxIndex];
        _nextSfxIndex = (_nextSfxIndex + 1) % _sfxSources.Count;
        src.transform.position = pos;
        // gameplay 音量 = SFX × Master（Master 由 AudioListener 承担）
        src.volume = GameSettings.SFXVolume;
        return src;
    }
}
