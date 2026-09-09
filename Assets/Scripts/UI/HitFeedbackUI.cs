using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 命中反馈 UI（4D）：准星中心 HitMarker（命中闪现 0.15s）+ 爆头 HEADSHOT 提示（淡入弹出淡出）
/// 挂 Canvas 上，Image/Text 引用场景中赋值（均为程序化生成控件）
/// </summary>
public class HitFeedbackUI : MonoBehaviour
{
    [Header("命中标记（准星中心 X 形）")]
    public Image hitMarkerImage;

    [Header("爆头提示文本")]
    public Text headshotText;

    [Header("参数")]
    public float markerShowTime = 0.15f;
    public float markerScale = 1.3f;        // 出现瞬间放大倍率
    public float headshotShowTime = 1.0f;

    private void OnEnable()
    {
        GameEvents.OnHitConfirmed += HandleHitConfirmed;
        GameEvents.OnHeadshotKilled += HandleHeadshotKilled;
    }

    private void OnDisable()
    {
        GameEvents.OnHitConfirmed -= HandleHitConfirmed;
        GameEvents.OnHeadshotKilled -= HandleHeadshotKilled;
    }

    /// <summary>命中：标记放大淡出（连发时先杀旧动画防叠加闪烁）</summary>
    private void HandleHitConfirmed()
    {
        if (hitMarkerImage == null) return;
        hitMarkerImage.DOKill();
        hitMarkerImage.gameObject.SetActive(true);
        hitMarkerImage.color = Color.white;
        hitMarkerImage.transform.localScale = Vector3.one * markerScale;
        hitMarkerImage.DOFade(0f, markerShowTime).OnComplete(() =>
        {
            if (hitMarkerImage != null)
                hitMarkerImage.gameObject.SetActive(false);
        });
    }

    private Sequence _headshotSeq;   // 连续爆头时先杀旧序列，防止旧 OnComplete 中途隐藏新提示

    /// <summary>爆头击杀：HEADSHOT 弹出+停留+淡出</summary>
    private void HandleHeadshotKilled()
    {
        if (headshotText == null) return;
        if (_headshotSeq != null && _headshotSeq.IsActive())
            _headshotSeq.Kill();
        headshotText.gameObject.SetActive(true);
        var c = headshotText.color;
        c.a = 1f;
        headshotText.color = c;
        headshotText.transform.localScale = Vector3.one * 0.6f;

        _headshotSeq = DOTween.Sequence();
        _headshotSeq.Append(headshotText.transform.DOScale(1.15f, 0.12f).SetEase(Ease.OutBack));
        _headshotSeq.AppendInterval(headshotShowTime);
        _headshotSeq.Append(headshotText.DOFade(0f, 0.3f).OnComplete(() =>
        {
            if (headshotText != null)
                headshotText.gameObject.SetActive(false);
        }));
    }
}
