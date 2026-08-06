using UnityEngine;

/// <summary>
/// 游戏设置统一读取：从 PlayerPrefs 读取主菜单保存的设置（与 MainMenuController 键一致）
/// 在游戏场景 Awake 时调用 ApplyAll() 应用
/// </summary>
public static class GameSettings
{
    // 与 MainMenuController 相同的键
    public const string KeySensitivity = "Settings.Sensitivity";
    public const string KeyMasterVolume = "Settings.MasterVolume";
    public const string KeySFXVolume = "Settings.SFXVolume";
    public const string KeyFOV = "Settings.FOV";
    public const string KeyAutoShoot = "Settings.AutoShoot";

    public static float Sensitivity { get; private set; } = 2f;
    public static float MasterVolume { get; private set; } = 0.8f;
    public static float SFXVolume { get; private set; } = 1f;
    public static float FOV { get; private set; } = 75f;
    public static bool AutoShoot { get; private set; }

    private static bool _loaded;

    /// <summary>读取全部设置（只读一次，之后缓存）</summary>
    public static void Load()
    {
        if (_loaded) return;
        Sensitivity = PlayerPrefs.GetFloat(KeySensitivity, 2f);
        MasterVolume = PlayerPrefs.GetFloat(KeyMasterVolume, 0.8f);
        SFXVolume = PlayerPrefs.GetFloat(KeySFXVolume, 1f);
        FOV = PlayerPrefs.GetFloat(KeyFOV, 75f);
        AutoShoot = PlayerPrefs.GetInt(KeyAutoShoot, 0) == 1;
        _loaded = true;
    }

    /// <summary>应用全部设置到运行时</summary>
    public static void ApplyAll()
    {
        Load();
        AudioListener.volume = MasterVolume;
    }
}
