using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using DG.Tweening;

/// <summary>
/// 暂停菜单：Tab 键切换暂停/继续
/// 功能：继续游戏、设置（灵敏度/FOV/音量/自动射击）、返回主菜单
/// 暂停时 timeScale=0、解锁鼠标、禁用游戏脚本；恢复时还原
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
    public Button backButton;

    [Header("暂停时禁用的游戏脚本")]
    public MonoBehaviour[] disableOnPause;

    private bool _isPaused;
    private bool _inSettings;

    private void Start()
    {
        BindEvents();
        SetupSliderFills();
    }

    private void Update()
    {
        // Tab 键切换（设置面板内按 Tab 返回暂停菜单）
        var input = FindAnyObjectByType<PlayerInputHandler>();
        bool pausePressed = input != null ? input.IsPausePressed()
            : (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame);

        if (pausePressed)
        {
            if (_inSettings)
                CloseSettings();
            else if (_isPaused)
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
        _isPaused = true;
        Time.timeScale = 0f;

        // 暂停菜单显示（alpha 恢复为可见，使用不受 timeScale 影响的动画）
        pausePanel.gameObject.SetActive(true);
        pausePanel.alpha = 0f;
        pausePanel.interactable = true;
        pausePanel.blocksRaycasts = true;
        pausePanel.DOFade(1f, 0.2f).SetUpdate(true);

        // 加载当前设置值
        LoadSettings();

        // 解锁鼠标
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 禁用游戏脚本（防止暂停时移动/射击）
        foreach (var comp in disableOnPause)
            if (comp != null)
                comp.enabled = false;
    }

    private void Resume()
    {
        _isPaused = false;
        Time.timeScale = 1f;

        pausePanel.gameObject.SetActive(false);
        settingsPanel.gameObject.SetActive(false);
        _inSettings = false;

        // 重新锁定鼠标
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // 恢复游戏脚本
        foreach (var comp in disableOnPause)
            if (comp != null)
                comp.enabled = true;
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
    }

    private void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }
}
