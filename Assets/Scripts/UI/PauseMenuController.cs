using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using DG.Tweening;

/// <summary>
/// 暂停菜单（纯 UI）：Tab 键切换暂停/继续
/// 功能：继续游戏、设置（灵敏度/FOV/音量/自动射击）、返回主菜单
/// timeScale/脚本禁用/场景加载职责已迁移 GameManager（阶段5），本类只管面板切换与设置控件
/// </summary>
public class PauseMenuController : MonoBehaviour
{
    [Header("面板")]
    public CanvasGroup pausePanel;
    public CanvasGroup settingsPanel;

    [Header("暂停菜单按钮")]
    public Button resumeButton;
    public Button settingsButton;
    public Button mainMenuButton;

    [Header("设置控件")]
    public Slider sensitivitySlider;
    public Slider masterVolumeSlider;
    public Slider sfxVolumeSlider;
    public Slider fovSlider;
    public Toggle autoShootToggle;   // F2：与主菜单对称的自动射击开关
    public Button backButton;

    private bool _inSettings;

    private void Start()
    {
        BindEvents();
        SetupSliderFills();
    }

    private void Update()
    {
        // 结算状态（GameOver/Victory）下 Tab 不再响应暂停
        if (GameManager.Instance != null
            && (GameManager.Instance.State == GameManager.GameState.GameOver
                || GameManager.Instance.State == GameManager.GameState.Victory))
            return;

        // Tab 键切换（设置面板内按 Tab 返回暂停菜单）
        var input = FindAnyObjectByType<PlayerInputHandler>();
        bool pausePressed = input != null ? input.IsPausePressed()
            : (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame);

        if (pausePressed)
        {
            if (_inSettings)
                CloseSettings();
            else if (GameManager.Instance != null && GameManager.Instance.State == GameManager.GameState.Paused)
                Resume();
            else
                Pause();
        }
    }

    private void BindEvents()
    {
        if (resumeButton != null)
            resumeButton.onClick.AddListener(Resume);
        if (settingsButton != null)
            settingsButton.onClick.AddListener(OpenSettings);
        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(ReturnToMainMenu);
        if (backButton != null)
            backButton.onClick.AddListener(CloseSettings);

        if (sensitivitySlider != null)
            sensitivitySlider.onValueChanged.AddListener(v => PlayerPrefs.SetFloat("Settings.Sensitivity", v));
        if (masterVolumeSlider != null)
            masterVolumeSlider.onValueChanged.AddListener(v => { PlayerPrefs.SetFloat("Settings.MasterVolume", v); AudioListener.volume = v; });
        if (sfxVolumeSlider != null)
            sfxVolumeSlider.onValueChanged.AddListener(v => PlayerPrefs.SetFloat("Settings.SFXVolume", v));
        if (fovSlider != null)
            fovSlider.onValueChanged.AddListener(v => PlayerPrefs.SetFloat("Settings.FOV", v));
        if (autoShootToggle != null)
            autoShootToggle.onValueChanged.AddListener(v => GameSettings.SetAutoShoot(v));
    }

    /// <summary>滑块填充手动控制（代码创建的 Slider 自动 Fill 不生效）</summary>
    private void SetupSliderFills()
    {
        if (sensitivitySlider != null) SetupSliderFill(sensitivitySlider);
        if (masterVolumeSlider != null) SetupSliderFill(masterVolumeSlider);
        if (sfxVolumeSlider != null) SetupSliderFill(sfxVolumeSlider);
        if (fovSlider != null) SetupSliderFill(fovSlider);
    }

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

    private void Pause()
    {
        // 状态守卫：非 Playing（GameOver/Victory/已 Paused）不响应，面板不得叠加在结算面板上
        if (GameManager.Instance == null || GameManager.Instance.State != GameManager.GameState.Playing) return;
        GameManager.Instance.Pause();

        // 暂停菜单显示（alpha 恢复为可见，使用不受 timeScale 影响的动画）
        pausePanel.gameObject.SetActive(true);
        pausePanel.alpha = 0f;
        pausePanel.interactable = true;
        pausePanel.blocksRaycasts = true;
        pausePanel.DOFade(1f, 0.2f).SetUpdate(true);

        // 加载当前设置值
        LoadSettings();
    }

    private void Resume()
    {
        if (GameManager.Instance == null || GameManager.Instance.State != GameManager.GameState.Paused) return;
        GameManager.Instance.Resume();

        pausePanel.gameObject.SetActive(false);
        settingsPanel.gameObject.SetActive(false);
        _inSettings = false;
    }

    private void OpenSettings()
    {
        _inSettings = true;
        pausePanel.gameObject.SetActive(false);
        settingsPanel.gameObject.SetActive(true);
        settingsPanel.alpha = 0f;
        settingsPanel.interactable = true;
        settingsPanel.blocksRaycasts = true;
        settingsPanel.DOFade(1f, 0.2f).SetUpdate(true);
        LoadSettings();
    }

    private void CloseSettings()
    {
        _inSettings = false;
        settingsPanel.gameObject.SetActive(false);
        pausePanel.gameObject.SetActive(true);
        pausePanel.alpha = 0f;
        pausePanel.interactable = true;
        pausePanel.blocksRaycasts = true;
        pausePanel.DOFade(1f, 0.2f).SetUpdate(true);
    }

    /// <summary>读取 PlayerPrefs 到设置控件</summary>
    private void LoadSettings()
    {
        if (sensitivitySlider != null)
            sensitivitySlider.value = PlayerPrefs.GetFloat("Settings.Sensitivity", 2f);
        if (masterVolumeSlider != null)
            masterVolumeSlider.value = PlayerPrefs.GetFloat("Settings.MasterVolume", 0.8f);
        if (sfxVolumeSlider != null)
            sfxVolumeSlider.value = PlayerPrefs.GetFloat("Settings.SFXVolume", 1f);
        if (fovSlider != null)
            fovSlider.value = PlayerPrefs.GetFloat("Settings.FOV", 75f);
        if (autoShootToggle != null)
            autoShootToggle.isOn = PlayerPrefs.GetInt("Settings.AutoShoot", 0) == 1;
    }

    private void ReturnToMainMenu()
    {
        // timeScale/鼠标/场景加载全部由 GameManager 处理
        if (GameManager.Instance != null)
            GameManager.Instance.ReturnToMenu();
    }
}
