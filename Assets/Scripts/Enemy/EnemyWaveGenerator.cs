using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 敌人波次生成器（阶段5）：WaveConfig 驱动刷怪
/// 状态机：Preparing(首波10s) → Spawning(interval 逐刷) → Fighting(等全灭) → Intermission(startDelay 倒计时) → 下一波 | Victory
/// 敌人 Instantiate 直出（不入对象池：尸体 10s 延迟销毁使池化复杂度不值）
/// 出生点：距玩家 > minSpawnDistance 的点随机，无满足则取最远；NavMesh.SamplePosition 容差微调（不重烘焙）
/// 存活判定：List 登记 + IsDead 轮询（不能用 activeInHierarchy——尸体延迟销毁期间仍 active）
/// </summary>
public class EnemyWaveGenerator : MonoBehaviour
{
    private enum WavePhase { Preparing, Spawning, Fighting, Intermission, Finished }

    [Header("波次配置（SO）")]
    [SerializeField] private WaveConfig waveConfig;

    [Header("出生点（岛屿边缘，拖入 Transform）")]
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float minSpawnDistance = 20f;   // 距玩家小于此距离的点不选（除非全不满足取最远）
    [SerializeField] private float spawnSampleRadius = 2f;   // NavMesh.SamplePosition 容差（米）

    [Header("节奏")]
    [SerializeField] private float firstWaveDelay = 10f;     // 首波准备时间（秒）

    [Header("补给（D7）")]
    [SerializeField] private bool restockGrenadePerWave = true; // 每波开始补 1 雷（上限钳制在 GrenadeThrower）
    [SerializeField] private GrenadeThrower grenadeThrower;     // 玩家手雷组件引用（兼作玩家位置来源）

    private WavePhase _phase = WavePhase.Preparing;
    private int _waveIndex;                     // 0-based
    private float _phaseTimer;
    private float _spawnTimer;
    private int _spawnedCount;
    private readonly List<GameObject> _spawnQueue = new List<GameObject>();
    private readonly List<EnemyBase> _aliveEnemies = new List<EnemyBase>();

    /// <summary>当前波号（1-based）</summary>
    public int CurrentWaveNumber => _waveIndex + 1;
    /// <summary>总波数</summary>
    public int TotalWaves => waveConfig != null && waveConfig.waves != null ? waveConfig.waves.Length : 0;
    /// <summary>当前阶段名（WaveBanner/调试用）</summary>
    public string PhaseName => _phase.ToString();
    /// <summary>准备/波间倒计时剩余秒数（WaveBanner 显示用）</summary>
    public float CountdownRemaining => (_phase == WavePhase.Preparing || _phase == WavePhase.Intermission) ? _phaseTimer : 0f;

    private void Start()
    {
        _phaseTimer = firstWaveDelay;
        if (GameManager.Instance != null)
            GameManager.Instance.SetMaxWave(TotalWaves);
    }

    private void Update()
    {
        if (waveConfig == null || waveConfig.waves == null || waveConfig.waves.Length == 0) return;
        // 非 Playing（暂停/结算）不推进波次（暂停 timeScale=0 时 deltaTime=0 天然冻结，此处防结算后仍刷怪）
        if (GameManager.Instance != null && GameManager.Instance.State != GameManager.GameState.Playing) return;

        switch (_phase)
        {
            case WavePhase.Preparing:
                _phaseTimer -= Time.deltaTime;
                if (_phaseTimer <= 0f)
                    StartWave();
                break;

            case WavePhase.Spawning:
                _spawnTimer -= Time.deltaTime;
                if (_spawnTimer <= 0f && _spawnQueue.Count > 0)
                {
                    SpawnNext();
                    _spawnTimer = waveConfig.waves[_waveIndex].spawnInterval;
                }
                if (_spawnQueue.Count == 0)
                    _phase = WavePhase.Fighting;
                break;

            case WavePhase.Fighting:
                // 清理已销毁/已死亡条目（尸体 Destroy 后为 null；IsDead 期间也不计入存活）
                _aliveEnemies.RemoveAll(e => e == null || e.IsDead);
                if (_aliveEnemies.Count == 0)
                    OnWaveCleared();
                break;

            case WavePhase.Intermission:
                _phaseTimer -= Time.deltaTime;
                if (_phaseTimer <= 0f)
                {
                    _waveIndex++;
                    StartWave();
                }
                break;
        }
    }

    /// <summary>开始当前波：广播 WaveStarted + 每波补雷（D7）+ 展开刷怪队列</summary>
    private void StartWave()
    {
        var wave = waveConfig.waves[_waveIndex];
        _spawnQueue.Clear();
        if (wave.entries != null)
        {
            foreach (var entry in wave.entries)
                for (int i = 0; i < entry.count; i++)
                    _spawnQueue.Add(entry.enemyPrefab);
        }

        _spawnedCount = 0;
        _spawnTimer = 0f;
        _aliveEnemies.Clear();

        if (GameManager.Instance != null)
            GameManager.Instance.SetCurrentWave(CurrentWaveNumber);
        GameEvents.RaiseWaveStarted(CurrentWaveNumber);
        if (restockGrenadePerWave && grenadeThrower != null)
            grenadeThrower.AddGrenades(1);

        _phase = WavePhase.Spawning;
    }

    /// <summary>从队列刷一个敌人：选出生点 → NavMesh 微调 → 直出并置 huntPlayer 寻敌模式</summary>
    private void SpawnNext()
    {
        var prefab = _spawnQueue[0];
        _spawnQueue.RemoveAt(0);
        if (prefab == null) return;

        var pos = PickSpawnPosition();
        var go = Instantiate(prefab, pos, Quaternion.identity);
        go.name = prefab.name + "_W" + CurrentWaveNumber + "_" + (++_spawnedCount);

        var enemy = go.GetComponent<EnemyBase>();
        if (enemy != null)
        {
            // 坑 67：huntPlayer 是无 Start 重置的新字段，Instantiate 后直赋安全
            enemy.huntPlayer = true;
            _aliveEnemies.Add(enemy);
        }
    }

    /// <summary>选出生点：距玩家 > minSpawnDistance 的点随机；无满足则取最远；SamplePosition 贴合 NavMesh</summary>
    private Vector3 PickSpawnPosition()
    {
        if (grenadeThrower == null)
            return transform.position;

        Vector3 playerPos = grenadeThrower.transform.position;
        Transform farthest = null;
        float farthestDist = -1f;
        List<Transform> candidates = new List<Transform>();
        if (spawnPoints != null)
        {
            foreach (var p in spawnPoints)
            {
                if (p == null) continue;
                float d = Vector3.Distance(p.position, playerPos);
                if (d > farthestDist) { farthestDist = d; farthest = p; }
                if (d > minSpawnDistance) candidates.Add(p);
            }
        }
        Transform pick = candidates.Count > 0
            ? candidates[Random.Range(0, candidates.Count)]
            : farthest;
        if (pick == null)
            return transform.position;

        // 容差内吸附到 NavMesh（不重烘焙，坑 61/65 约束）
        Vector3 pos = pick.position;
        if (NavMesh.SamplePosition(pos, out NavMeshHit hit, spawnSampleRadius, NavMesh.AllAreas))
            pos = hit.position;
        return pos;
    }

    /// <summary>波清空：广播 WaveCleared；末波 → Victory（D5），否则进入波间倒计时</summary>
    private void OnWaveCleared()
    {
        GameEvents.RaiseWaveCleared(CurrentWaveNumber);

        if (_waveIndex >= waveConfig.waves.Length - 1)
        {
            _phase = WavePhase.Finished;
            // D5：末波清空 → 胜利流程（GameManager 延迟 1s 后广播 OnGameVictory 弹结算）
            if (GameManager.Instance != null)
                GameManager.Instance.TriggerVictory();
            else
                GameEvents.RaiseGameVictory();
        }
        else
        {
            _phaseTimer = waveConfig.waves[_waveIndex].startDelay;
            _phase = WavePhase.Intermission;
        }
    }
}
