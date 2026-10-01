using UnityEngine;

/// <summary>
/// 波次配置 SO（阶段5）：混兵种条目数组（D6：首波 4/2/2 三兵种混编）
/// 数据随时可调；测试期可临时调低再还原
/// </summary>
[CreateAssetMenu(fileName = "WaveConfig", menuName = "TPS/WaveConfig")]
public class WaveConfig : ScriptableObject
{
    public Wave[] waves;

    [System.Serializable]
    public class Wave
    {
        public WaveEntry[] entries;         // 混兵种条目
        public float spawnInterval = 1.5f;  // 波内生成间隔（秒）
        public float startDelay = 5f;       // 波间准备时间（首波由生成器覆盖为 10s）
    }

    [System.Serializable]
    public class WaveEntry
    {
        public GameObject enemyPrefab;
        public int count;
    }
}
