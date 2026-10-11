using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 设置界面：音效音量 / 背景音乐音量 / 分辨率 / 全屏。
///
/// 【设计】做成 DontDestroyOnLoad 的常驻面板：
///   在 Menu 场景建一次，切到 Battle 场景也还在（不会重复创建）。
///   这样只需要维护一套 UI，两个场景都能用。
///
/// 【为什么不直接改 AudioManager 的字段】
///   Inspector 里的 [Range] 滑条只改字段值，不会触发方法。
///   必须调用 SetSfxVolume / SetBgmVolume 才会真正生效并写进 PlayerPrefs。
/// </summary>
public class SettingsPanel : MonoBehaviour
{
    private const string KEY_FULLSCREEN = "settings.fullscreen";
    private const string KEY_WIDTH = "settings.width";
    private const string KEY_HEIGHT = "settings.height";

    [Header("界面")]
    [Tooltip("整个面板的根（默认隐藏；脚本挂在它外面时填这里）")]
    public GameObject PanelRoot;

    [Tooltip("音效音量滑条")]
    public Slider SfxSlider;

    [Tooltip("音效音量百分比文字")]
    public Text SfxValueText;

    [Tooltip("背景音乐音量滑条")]
    public Slider BgmSlider;

    [Tooltip("背景音乐音量百分比文字")]
    public Text BgmValueText;

    [Tooltip("全屏切换按钮（按钮上的文字会在「全屏 / 窗口化」之间切换）")]
    public Button FullscreenButton;

    [Tooltip("当前分辨率显示文字")]
    public Text ResolutionText;

    [Tooltip("关闭按钮")]
    public Button CloseButton;

    [Header("分辨率档位（按 16:10，和设计分辨率 2560x1600 同比例）")]
    public Vector2Int[] Resolutions = new Vector2Int[]
    {
        new Vector2Int(2560, 1600),
        new Vector2Int(1920, 1200),
        new Vector2Int(1600, 1000),
        new Vector2Int(1280, 800)
    };

    [Header("设置")]
    [Tooltip("打开设置时是否暂停游戏")]
    public bool PauseGameWhenOpen = true;

    [Tooltip("允许按 ESC 开关设置界面")]
    public bool AllowEscapeKey = true;

    /// <summary>面板是否打开。</summary>
    public bool IsOpen { get { return PanelRoot != null && PanelRoot.activeSelf; } }

    // =====================================================================
    //  初始化
    // =====================================================================

    private void Awake()
    {
        if (PanelRoot == null) PanelRoot = gameObject;

        // 常驻：切场景不销毁（只在第一个场景需要建，之后自动保留）
        DontDestroyOnLoad(gameObject);

        // ---------- 音量滑条 ----------
        if (SfxSlider != null)
        {
            SfxSlider.minValue = 0f;
            SfxSlider.maxValue = 1f;
            SfxSlider.wholeNumbers = false;
            SfxSlider.value = AudioManager.Instance.SfxVolume;
            SfxSlider.onValueChanged.RemoveAllListeners();
            SfxSlider.onValueChanged.AddListener(OnSfxChanged);
        }

        if (BgmSlider != null)
        {
            BgmSlider.minValue = 0f;
            BgmSlider.maxValue = 1f;
            BgmSlider.wholeNumbers = false;
            BgmSlider.value = AudioManager.Instance.BgmVolume;
            BgmSlider.onValueChanged.RemoveAllListeners();
            BgmSlider.onValueChanged.AddListener(OnBgmChanged);
        }

        // ---------- 按钮 ----------
        if (FullscreenButton != null)
        {
            FullscreenButton.onClick.RemoveAllListeners();
            FullscreenButton.onClick.AddListener(ToggleFullscreen);
        }

        if (CloseButton != null)
        {
            CloseButton.onClick.RemoveAllListeners();
            CloseButton.onClick.AddListener(Close);
        }

        PanelRoot.SetActive(false);      // 默认隐藏
    }

    private void OnDestroy()
    {
        // 保险：面板还开着就退出播放时，恢复时间缩放，否则编辑器会一直停在暂停状态
        if (IsOpen) Time.timeScale = 1f;
    }

    private void Update()
    {
        if (!AllowEscapeKey) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (IsOpen) Close();
            else Open();
        }
    }

    // =====================================================================
    //  打开 / 关闭
    // =====================================================================

    public void Open()
    {
        if (PanelRoot == null || PanelRoot.activeSelf) return;

        PanelRoot.SetActive(true);
        if (PauseGameWhenOpen) Time.timeScale = 0f;

        RefreshUI();
        Debug.Log("[设置] 已打开" + (PauseGameWhenOpen ? "（游戏已暂停）" : ""));

        // 播放一个点击音作为反馈（面板里的按钮也靠这个）
        AudioManager.Instance.PlayClick();
    }

    public void Close()
    {
        if (PanelRoot == null) return;

        PanelRoot.SetActive(false);
        Time.timeScale = 1f;          // 一定要恢复，否则游戏卡死
        AudioManager.Instance.PlayClick();

        Debug.Log("[设置] 已关闭");
    }

    public void Toggle()
    {
        if (IsOpen) Close();
        else Open();
    }

    // =====================================================================
    //  音量
    // =====================================================================

    private void OnSfxChanged(float value)
    {
        AudioManager.Instance.SetSfxVolume(value);
        if (SfxValueText != null) SfxValueText.text = Mathf.RoundToInt(value * 100f) + "%";
    }

    private void OnBgmChanged(float value)
    {
        AudioManager.Instance.SetBgmVolume(value);
        if (BgmValueText != null) BgmValueText.text = Mathf.RoundToInt(value * 100f) + "%";
    }

    // =====================================================================
    //  分辨率 / 全屏
    // =====================================================================

    /// <summary>切到第 index 个分辨率（在 Inspector 里给按钮连这个）。</summary>
    public void SetResolution(int index)
    {
        if (Resolutions == null || index < 0 || index >= Resolutions.Length) return;

        Vector2Int size = Resolutions[index];
        Screen.SetResolution(size.x, size.y, FullScreenMode.Windowed);

        PlayerPrefs.SetInt(KEY_FULLSCREEN, 0);
        PlayerPrefs.SetInt(KEY_WIDTH, size.x);
        PlayerPrefs.SetInt(KEY_HEIGHT, size.y);
        PlayerPrefs.Save();

        AudioManager.Instance.PlayClick();
        RefreshUI();

        Debug.Log("[设置] 分辨率改为 " + size.x + " x " + size.y);
    }

    /// <summary>在全屏 / 窗口化之间切换。</summary>
    public void ToggleFullscreen()
    {
        bool goFullscreen = !Screen.fullScreen;

        if (goFullscreen)
        {
            Resolution native = Screen.currentResolution;
            Screen.SetResolution(native.width, native.height, FullScreenMode.FullScreenWindow);
            PlayerPrefs.SetInt(KEY_FULLSCREEN, 1);
            PlayerPrefs.SetInt(KEY_WIDTH, native.width);
            PlayerPrefs.SetInt(KEY_HEIGHT, native.height);
            Debug.Log("[设置] 切换为全屏 " + native.width + " x " + native.height);
        }
        else
        {
            Vector2Int size = (Resolutions != null && Resolutions.Length > 0)
                ? Resolutions[0] : new Vector2Int(1920, 1200);
            Screen.SetResolution(size.x, size.y, FullScreenMode.Windowed);
            PlayerPrefs.SetInt(KEY_FULLSCREEN, 0);
            PlayerPrefs.SetInt(KEY_WIDTH, size.x);
            PlayerPrefs.SetInt(KEY_HEIGHT, size.y);
            Debug.Log("[设置] 切换为窗口化 " + size.x + " x " + size.y);
        }

        PlayerPrefs.Save();
        AudioManager.Instance.PlayClick();
        RefreshUI();
    }

    /// <summary>刷新界面上的数值显示。</summary>
    private void RefreshUI()
    {
        AudioManager am = AudioManager.Instance;

        if (SfxSlider != null && !Mathf.Approximately(SfxSlider.value, am.SfxVolume))
            SfxSlider.value = am.SfxVolume;
        if (BgmSlider != null && !Mathf.Approximately(BgmSlider.value, am.BgmVolume))
            BgmSlider.value = am.BgmVolume;

        if (SfxValueText != null) SfxValueText.text = Mathf.RoundToInt(am.SfxVolume * 100f) + "%";
        if (BgmValueText != null) BgmValueText.text = Mathf.RoundToInt(am.BgmVolume * 100f) + "%";

        if (ResolutionText != null)
        {
            ResolutionText.text = (Screen.fullScreen ? "全屏 " : "窗口化 ")
                                  + Screen.width + " x " + Screen.height;
        }

        if (FullscreenButton != null)
        {
            Text label = FullscreenButton.GetComponentInChildren<Text>();
            if (label != null) label.text = Screen.fullScreen ? "窗口化" : "全屏";
        }
    }

    /// <summary>启动时应用上次保存的分辨率（放在主菜单的 Start 里调一次）。</summary>
    public static void ApplySavedResolution()
    {
        if (PlayerPrefs.GetInt(KEY_FULLSCREEN, 0) == 1)
        {
            Resolution native = Screen.currentResolution;
            Screen.SetResolution(native.width, native.height, FullScreenMode.FullScreenWindow);
        }
        else
        {
            int w = PlayerPrefs.GetInt(KEY_WIDTH, 1600);
            int h = PlayerPrefs.GetInt(KEY_HEIGHT, 1000);
            Screen.SetResolution(w, h, FullScreenMode.Windowed);
        }
    }
}