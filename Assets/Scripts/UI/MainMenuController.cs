using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

/// <summary>
/// 主菜单控制器：主菜单/设置面板切换、设置持久化（PlayerPrefs）、Loading 读条、DOTween 动画
/// 设置项：鼠标灵敏度、主音量、音效音量、FOV、自动射击
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("面板")]
    public CanvasGroup menuPanel;
    public CanvasGroup settingsPanel;
    public CanvasGroup loadingPanel;

    [Header("主菜单按钮")]
    public Button playButton;
    public Button settingsButton;
    public Button exitButton;

    [Header("设置控件")]
    public Slider sensitivitySlider;
    public Slider masterVolumeSlider;
    public Slider sfxVolumeSlider;
    public Slider fovSlider;
    public Toggle autoShootToggle;
    public Button backButton;

    [Header("Loading")]
    public Slider loadingProgress;
    public Text loadingProgressText;
    public Text loadingTitleText;

    [Header("动画参数")]
    public float panelFadeDuration = 0.35f;
    public float hoverScale = 1.06f;

    // PlayerPrefs 键（游戏场景用相同键读取）
    public const string KeySensitivity = "Settings.Sensitivity";
    public const string KeyMasterVolume = "Settings.MasterVolume";
    public const string KeySFXVolume = "Settings.SFXVolume";
    public const string KeyFOV = "Settings.FOV";
    public const string KeyAutoShoot = "Settings.AutoShoot";

    private void Start()
    {
        LoadSettings();
        BindEvents();
        SetupHoverAnimations();

        // 入场动画：主菜单淡入
        menuPanel.alpha = 0f;
        menuPanel.DOFade(1f, panelFadeDuration + 0.2f);

        // LOADING 标题呼吸动效（循环）
        if (loadingTitleText != null)
            loadingTitleText.DOFade(0.35f, 0.6f).SetLoops(-1, LoopType.Yoyo);
    }

    private void BindEvents()
    {
        if (playButton != null)
            playButton.onClick.AddListener(OnPlay);
        if (settingsButton != null)
            settingsButton.onClick.AddListener(OnSettings);
        if (exitButton != null)
            exitButton.onClick.AddListener(OnExit);
        if (backButton != null)
            backButton.onClick.AddListener(OnBack);

        if (sensitivitySlider != null)
            SetupSliderFill(sensitivitySlider);
        if (masterVolumeSlider != null)
            SetupSliderFill(masterVolumeSlider);
        if (sfxVolumeSlider != null)
            SetupSliderFill(sfxVolumeSlider);
        if (fovSlider != null)
            SetupSliderFill(fovSlider);
        if (loadingProgress != null)
            SetupSliderFill(loadingProgress);

        if (sensitivitySlider != null)
            sensitivitySlider.onValueChanged.AddListener(v => PlayerPrefs.SetFloat(KeySensitivity, v));
        if (masterVolumeSlider != null)
            masterVolumeSlider.onValueChanged.AddListener(v => { PlayerPrefs.SetFloat(KeyMasterVolume, v); AudioListener.volume = v; });
        if (sfxVolumeSlider != null)
            sfxVolumeSlider.onValueChanged.AddListener(v => PlayerPrefs.SetFloat(KeySFXVolume, v));
        if (fovSlider != null)
            fovSlider.onValueChanged.AddListener(v => PlayerPrefs.SetFloat(KeyFOV, v));
        if (autoShootToggle != null)
            autoShootToggle.onValueChanged.AddListener(v => PlayerPrefs.SetInt(KeyAutoShoot, v ? 1 : 0));
    }

    /// <summary>手动控制滑块填充（代码创建的 Slider 自动 Fill 不生效，改为监听值变化更新 fillRect）</summary>
    private void SetupSliderFill(Slider slider)
    {
        slider.onValueChanged.AddListener(_ => UpdateSliderFill(slider));
        UpdateSliderFill(slider);
    }

    private void UpdateSliderFill(Slider slider)
    {
        if (slider.fillRect != null)
        {
            slider.fillRect.anchorMin = new Vector2(0, 0);
            slider.fillRect.anchorMax = new Vector2(slider.normalizedValue, 1);
            slider.fillRect.anchoredPosition = Vector2.zero;
            slider.fillRect.sizeDelta = Vector2.zero;
            slider.fillRect.pivot = new Vector2(0, 0.5f);
        }
    }

    /// <summary>读取 PlayerPrefs 到控件（含默认值）</summary>
    private void LoadSettings()
    {
        if (sensitivitySlider != null)
            sensitivitySlider.value = PlayerPrefs.GetFloat(KeySensitivity, 2f);
        if (masterVolumeSlider != null)
            masterVolumeSlider.value = PlayerPrefs.GetFloat(KeyMasterVolume, 0.8f);
        if (sfxVolumeSlider != null)
            sfxVolumeSlider.value = PlayerPrefs.GetFloat(KeySFXVolume, 1f);
        if (fovSlider != null)
            fovSlider.value = PlayerPrefs.GetFloat(KeyFOV, 75f);
        if (autoShootToggle != null)
            autoShootToggle.isOn = PlayerPrefs.GetInt(KeyAutoShoot, 0) == 1;
        AudioListener.volume = PlayerPrefs.GetFloat(KeyMasterVolume, 0.8f);
    }

    /// <summary>按钮悬停缩放动画（DOTween）</summary>
    private void SetupHoverAnimations()
    {
        AddHover(playButton);
        AddHover(settingsButton);
        AddHover(exitButton);
        AddHover(backButton);
    }

    private void AddHover(Button btn)
    {
        if (btn == null) return;
        var trigger = btn.gameObject.AddComponent<EventTrigger>();

        var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enter.callback.AddListener(_ => btn.transform.DOScale(hoverScale, 0.12f));
        trigger.triggers.Add(enter);

        var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exit.callback.AddListener(_ => btn.transform.DOScale(1f, 0.12f));
        trigger.triggers.Add(exit);
    }

    // ===== 面板切换 =====
    private void SetPanel(CanvasGroup show, CanvasGroup hide)
    {
        if (hide != null)
        {
            hide.interactable = false;
            hide.blocksRaycasts = false;
            hide.DOFade(0f, panelFadeDuration).OnComplete(() => hide.gameObject.SetActive(false));
        }
        if (show != null)
        {
            show.gameObject.SetActive(true);
            show.alpha = 0f;
            show.interactable = true;
            show.blocksRaycasts = true;
            show.DOFade(1f, panelFadeDuration);
        }
    }

    private void OnSettings()
    {
        SetPanel(settingsPanel, menuPanel);
    }

    private void OnBack()
    {
        SetPanel(menuPanel, settingsPanel);
    }

    private void OnPlay()
    {
        // 显示 Loading 并异步加载游戏场景
        SetPanel(loadingPanel, menuPanel);
        if (loadingProgress != null)
            loadingProgress.value = 0f;
        if (loadingProgressText != null)
            loadingProgressText.text = "0%";
        StartCoroutine(LoadGameSceneAsync());
    }

    private IEnumerator LoadGameSceneAsync()
    {
        var op = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("SampleScene");
        op.allowSceneActivation = false;

        // 进度条模拟动画（TPS Shooter 同款：约 1 秒平滑走完）+ 百分比实时刷新
        if (loadingProgress != null)
        {
            loadingProgress.DOValue(1f, 1f)
                .SetEase(Ease.OutCubic)
                .OnUpdate(() =>
                {
                    if (loadingProgressText != null)
                        loadingProgressText.text = Mathf.RoundToInt(loadingProgress.value * 100f) + "%";
                });
        }

        // 等待场景加载完成（真实进度达到 0.9 即完成）
        while (op.progress < 0.9f)
            yield return null;

        // 动画收尾停留后激活场景
        yield return new WaitForSeconds(0.3f);
        if (loadingProgress != null)
            loadingProgress.value = 1f;
        if (loadingProgressText != null)
            loadingProgressText.text = "100%";
        yield return new WaitForSeconds(0.2f);
        op.allowSceneActivation = true;
    }

    private void OnExit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
