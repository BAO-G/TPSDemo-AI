using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// 结算界面（GameOver/Victory 共用面板）：暗底 + 标题 + 数据三行 + 重玩/回主菜单
/// 订阅 OnGameOver / OnGameVictory 显示；数据从 GameManager 拉取
/// DOTween 淡入 SetUpdate(true)（死亡不置 timeScale=0，但保持与暂停动画同模式防冲突）
/// </summary>
public class GameOverPanelController : MonoBehaviour
{
    [Header("面板引用")]
    [SerializeField] private CanvasGroup panel;
    [SerializeField] private Text titleText;
    [SerializeField] private Text statsText;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button menuButton;

    [Header("文案与配色")]
    [SerializeField] private string deathTitle = "YOU DIED";
    [SerializeField] private string victoryTitle = "VICTORY";
    [SerializeField] private Color deathColor = new Color(0.85f, 0.15f, 0.15f);
    [SerializeField] private Color victoryColor = new Color(0.2f, 0.85f, 0.3f);

    private Sequence _showSeq;   // 存字段引用，触发前 Kill 旧序列（4D 热修复教训：防连续触发叠加）

    private void OnEnable()
    {
        GameEvents.OnGameOver += ShowGameOver;
        GameEvents.OnGameVictory += ShowVictory;
        if (restartButton != null) restartButton.onClick.AddListener(OnRestartClicked);
        if (menuButton != null) menuButton.onClick.AddListener(OnMenuClicked);
    }

    private void OnDisable()
    {
        GameEvents.OnGameOver -= ShowGameOver;
        GameEvents.OnGameVictory -= ShowVictory;
        if (restartButton != null) restartButton.onClick.RemoveListener(OnRestartClicked);
        if (menuButton != null) menuButton.onClick.RemoveListener(OnMenuClicked);
        if (_showSeq != null && _showSeq.IsActive()) _showSeq.Kill();
    }

    private void ShowGameOver() => Show(deathTitle, deathColor);

    private void ShowVictory() => Show(victoryTitle, victoryColor);

    private void Show(string title, Color color)
    {
        if (panel == null) return;

        if (titleText != null)
        {
            titleText.text = title;
            titleText.color = color;
        }

        // 数据从 GameManager 拉取（重玩后场景重载，面板状态随之全清）
        int kills = 0, wave = 0, maxWave = 0;
        float survival = 0f;
        if (GameManager.Instance != null)
        {
            kills = GameManager.Instance.KillCount;
            wave = GameManager.Instance.CurrentWave;
            maxWave = GameManager.Instance.MaxWave;
            survival = GameManager.Instance.SurvivalTime;
        }
        if (statsText != null)
            statsText.text = string.Format("击杀数：{0}\n存活波次：{1}/{2}\n存活时间：{3:00}:{4:00}",
                kills, wave, maxWave, (int)(survival / 60f), (int)(survival % 60f));

        panel.gameObject.SetActive(true);
        panel.alpha = 0f;
        panel.interactable = true;
        panel.blocksRaycasts = true;

        if (_showSeq != null && _showSeq.IsActive()) _showSeq.Kill();
        _showSeq = DOTween.Sequence().SetUpdate(true);
        _showSeq.Append(panel.DOFade(1f, 0.5f));
        if (titleText != null)
        {
            titleText.transform.localScale = Vector3.one * 0.6f;
            _showSeq.Join(titleText.transform.DOScale(1f, 0.35f).SetEase(Ease.OutBack));
        }
    }

    private void OnRestartClicked()
    {
        GameEvents.RaiseUIClick(); // 4B 音效预留
        if (GameManager.Instance != null)
            GameManager.Instance.Restart();
    }

    private void OnMenuClicked()
    {
        GameEvents.RaiseUIClick();
        if (GameManager.Instance != null)
            GameManager.Instance.ReturnToMenu();
    }
}
