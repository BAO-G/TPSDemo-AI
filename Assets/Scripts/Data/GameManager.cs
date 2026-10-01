using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 游戏流程状态机（阶段5）：接管原 PauseMenuController 的 timeScale/脚本禁用/场景加载职责
/// Playing ⇄ Paused（Tab 暂停链路）；GameOver / Victory 由死亡/胜利流程接入（S3/S5）
/// 死亡/胜利不置 timeScale=0（世界保持活动：Died 动画、敌人走动正常播放），
/// 只禁用玩家操控脚本 + 解锁鼠标
/// </summary>
public class GameManager : MonoBehaviour
{
    public enum GameState { Playing, Paused, GameOver, Victory }

    public static GameManager Instance { get; private set; }

    [Header("暂停/结算时禁用的玩家操控脚本（自 PauseMenuController 迁移，场景拖入）")]
    [SerializeField] private MonoBehaviour[] disableOnPause;

    [Header("场景名（重玩=重载游戏场景；返回=主菜单）")]
    [SerializeField] private string gameplaySceneName = "SampleScene";
    [SerializeField] private string menuSceneName = "MainMenu";

    [Header("结算弹窗延迟（秒）")]
    [SerializeField] private float gameOverDelay = 1.5f;
    [SerializeField] private float victoryDelay = 1.0f;

    public GameState State { get; private set; } = GameState.Playing;
    public int KillCount { get; private set; }
    public int CurrentWave { get; private set; }
    public int MaxWave { get; private set; }
    public float SurvivalTime { get; private set; }
    public float GameOverDelay => gameOverDelay;
    public float VictoryDelay => victoryDelay;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
        GameEvents.OnPlayerDeath += TriggerGameOver;
        GameEvents.OnEnemyKilled += HandleEnemyKilled;
    }

    private void OnDisable()
    {
        GameEvents.OnPlayerDeath -= TriggerGameOver;
        GameEvents.OnEnemyKilled -= HandleEnemyKilled;
    }

    /// <summary>击杀计数（§4.1）：累加后广播 KillCountChanged（HUD 订阅）</summary>
    private void HandleEnemyKilled(Vector3 position)
    {
        KillCount++;
        GameEvents.RaiseKillCountChanged(KillCount);
    }

    private void Update()
    {
        // 存活时间仅在 Playing 时累计（暂停 timeScale=0 时 deltaTime=0，天然不计时）
        if (State == GameState.Playing)
            SurvivalTime += Time.deltaTime;
    }

    /// <summary>玩家死亡（OnPlayerDeath 订阅入口）：立即进入 GameOver（不置 timeScale=0，世界保持活动），延迟后广播结算</summary>
    public void TriggerGameOver()
    {
        // 死亡后重复 TakeDamage 会重复触发 OnDeath，状态守卫兜底
        if (State != GameState.Playing) return;
        State = GameState.GameOver;
        SetScriptsEnabled(false);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        StartCoroutine(GameOverRoutine());
    }

    /// <summary>胜利（末波清空，波次生成器调用，D5）：立即禁操控+解锁鼠标，延迟 victoryDelay 后广播结算</summary>
    public void TriggerVictory()
    {
        if (State != GameState.Playing) return;
        State = GameState.Victory;
        SetScriptsEnabled(false);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        StartCoroutine(VictoryRoutine());
    }

    private System.Collections.IEnumerator GameOverRoutine()
    {
        yield return new WaitForSeconds(gameOverDelay);
        GameEvents.RaiseGameOver();
    }

    private System.Collections.IEnumerator VictoryRoutine()
    {
        yield return new WaitForSeconds(victoryDelay);
        GameEvents.RaiseGameVictory();
    }

    /// <summary>波次生成器设置当前/最大波次（结算界面显示 N/Max）</summary>
    public void SetCurrentWave(int wave) => CurrentWave = wave;

    public void SetMaxWave(int maxWave) => MaxWave = maxWave;

    /// <summary>暂停：timeScale=0 + 禁用玩家操控脚本 + 解锁鼠标（原 PauseMenu 职责）</summary>
    public void Pause()
    {
        if (State != GameState.Playing) return;
        State = GameState.Paused;
        Time.timeScale = 0f;
        SetScriptsEnabled(false);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// <summary>恢复：还原 timeScale 与脚本，重新锁定鼠标</summary>
    public void Resume()
    {
        if (State != GameState.Paused) return;
        State = GameState.Playing;
        Time.timeScale = 1f;
        SetScriptsEnabled(true);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    /// <summary>批量禁用/恢复玩家操控脚本（暂停与死亡/胜利结算共用）</summary>
    public void SetScriptsEnabled(bool enabled)
    {
        if (disableOnPause == null) return;
        foreach (var comp in disableOnPause)
            if (comp != null)
                comp.enabled = enabled;
    }

    /// <summary>重玩：重载游戏场景，状态随场景重载全清（订阅者随对象销毁退订）</summary>
    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(gameplaySceneName);
    }

    /// <summary>回主菜单</summary>
    public void ReturnToMenu()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneManager.LoadScene(menuSceneName);
    }

    // S3/S5 接入：TriggerGameOver / TriggerVictory / KillCount 计数 / SetCurrentWave
}
