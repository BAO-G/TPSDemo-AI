using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// 波次横幅 + 来袭倒计时（阶段5，用户反馈补充：首波前也要有倒计时 UI）
/// 横幅：波开始 WAVE N / 波清空 WAVE N CLEARED（缩放弹出停 2s 淡出）
/// 倒计时：Preparing（首波 10s）与 Intermission（波间 5s）期间中央上方显示「第 N 波来袭 Xs」
///         每秒跳动一次（DOPunchScale），进入时弹入（OutBack）
/// 纪律（4D 热修复教训）：所有 Sequence/Tween 存字段引用，触发前 Kill 旧 tween 防叠加
/// </summary>
public class WaveBannerController : MonoBehaviour
{
    [Header("引用")]
    [SerializeField] private Text bannerText;
    [SerializeField] private EnemyWaveGenerator waveGenerator;   // 读阶段与倒计时

    [Header("节奏")]
    [SerializeField] private float bannerDuration = 2f;   // 横幅停留时长
    [SerializeField] private float popScale = 0.6f;       // 弹出起始缩放
    [SerializeField] private float tickPunch = 0.12f;     // 每秒跳动脉冲幅度

    private Sequence _bannerSeq;          // 横幅序列（存字段引用，触发前 Kill）
    private Sequence _countdownSeq;       // 倒计时弹入序列
    private Tween _pulseTween;            // 每秒跳动
    private Tween _fadeOutTween;          // 倒计时结束淡出
    private bool _countdownShowing;
    private string _lastCountdownText;

    private void OnEnable()
    {
        GameEvents.OnWaveStarted += HandleWaveStarted;
        GameEvents.OnWaveCleared += HandleWaveCleared;
    }

    private void OnDisable()
    {
        GameEvents.OnWaveStarted -= HandleWaveStarted;
        GameEvents.OnWaveCleared -= HandleWaveCleared;
        KillAllTweens();
    }

    private void HandleWaveStarted(int wave) => ShowBanner("WAVE " + wave);

    private void HandleWaveCleared(int wave) => ShowBanner("WAVE " + wave + " CLEARED");

    private void ShowBanner(string message)
    {
        if (bannerText == null) return;
        if (_bannerSeq != null && _bannerSeq.IsActive()) _bannerSeq.Kill();
        KillCountdownTweens();
        _countdownShowing = false;
        _lastCountdownText = null;

        // 倒计时可能留在任意 alpha，横幅从全亮弹出
        var c = bannerText.color;
        c.a = 1f;
        bannerText.color = c;
        bannerText.text = message;
        bannerText.transform.localScale = Vector3.one * popScale;

        _bannerSeq = DOTween.Sequence().SetUpdate(true);
        _bannerSeq.Append(bannerText.transform.DOScale(1f, 0.25f).SetEase(Ease.OutBack));
        _bannerSeq.AppendInterval(bannerDuration);
        _bannerSeq.Append(bannerText.DOFade(0f, 0.4f));
    }

    private void Update()
    {
        if (bannerText == null || waveGenerator == null) return;

        // 倒计时窗口：首波准备(Preparing)与波间(Intermission)均显示
        string phase = waveGenerator.PhaseName;
        bool counting = (phase == "Preparing" || phase == "Intermission")
                        && waveGenerator.CountdownRemaining > 0f;

        if (!counting)
        {
            if (_countdownShowing)
                EndCountdown();
            return;
        }

        // 大字横幅（WAVE N CLEARED 等）播放中让位，结束后再弹入倒计时
        if (_bannerSeq != null && _bannerSeq.IsActive()) return;

        if (!_countdownShowing)
        {
            StartCountdown();
            return; // 弹入帧不跳动
        }

        // 每秒刷新文本 + 跳动脉冲
        string text = "第 " + NextWaveNumber() + " 波来袭 " + Mathf.CeilToInt(waveGenerator.CountdownRemaining) + "s";
        if (text != _lastCountdownText)
        {
            _lastCountdownText = text;
            bannerText.text = text;
            if (_pulseTween != null && _pulseTween.IsActive()) _pulseTween.Kill();
            _pulseTween = bannerText.transform.DOPunchScale(Vector3.one * tickPunch, 0.25f, 1, 0.5f).SetUpdate(true);
        }
    }

    /// <summary>倒计时弹入：缩放 OutBack + 淡入</summary>
    private void StartCountdown()
    {
        KillCountdownTweens();
        _countdownShowing = true;
        _lastCountdownText = null;

        bannerText.transform.localScale = Vector3.one * popScale;
        _countdownSeq = DOTween.Sequence().SetUpdate(true);
        _countdownSeq.Append(bannerText.DOFade(1f, 0.2f));
        _countdownSeq.Join(bannerText.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack));
    }

    /// <summary>倒计时结束：脉冲停、快速淡出；横幅已接管时不淡出避免覆盖</summary>
    private void EndCountdown()
    {
        _countdownShowing = false;
        _lastCountdownText = null;
        KillCountdownTweens();

        bool bannerActive = _bannerSeq != null && _bannerSeq.IsActive();
        if (!bannerActive)
        {
            _fadeOutTween = bannerText.DOFade(0f, 0.25f).SetUpdate(true);
        }
    }

    /// <summary>下一波波号：Preparing 期间 CurrentWaveNumber 即首波；Intermission 为当前波 +1</summary>
    private int NextWaveNumber()
    {
        return waveGenerator.PhaseName == "Intermission"
            ? waveGenerator.CurrentWaveNumber + 1
            : waveGenerator.CurrentWaveNumber;
    }

    private void KillCountdownTweens()
    {
        if (_countdownSeq != null && _countdownSeq.IsActive()) _countdownSeq.Kill();
        if (_pulseTween != null && _pulseTween.IsActive()) _pulseTween.Kill();
        if (_fadeOutTween != null && _fadeOutTween.IsActive()) _fadeOutTween.Kill();
    }

    private void KillAllTweens()
    {
        if (_bannerSeq != null && _bannerSeq.IsActive()) _bannerSeq.Kill();
        KillCountdownTweens();
    }
}
