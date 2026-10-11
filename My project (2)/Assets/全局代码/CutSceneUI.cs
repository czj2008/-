using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 结局演出：一屏文字 + 一张 CG，点击推进。
///
/// 【设计】整个演出是一条"步骤列表"，每步要么是台词、要么是换 CG：
///   Dialogue → 显示一句台词（可以一次显示多行）
///   ShowCG   → 显示一张 CG（淡入）
///   HideCG   → 隐藏 CG（淡出）
///
/// 【为什么用列表而不是"每步写一个协程"】
/// 剧本有 40 多句，写死成代码没法改。用数据列表，改剧本 = 改 Inspector。
///
/// 【用法】GameFlow 通关最后一关时：yield return StartCoroutine(cutscene.Play());
/// </summary>
public class CutsceneUI : MonoBehaviour
{
    /// <summary>演出步骤类型。</summary>
    public enum StepType
    {
        Dialogue,   // 显示台词
        ShowCG,     // 显示 CG
        HideCG      // 隐藏 CG
    }

    /// <summary>一个演出步骤。</summary>
    [System.Serializable]
    public class CutsceneStep
    {
        public StepType Type = StepType.Dialogue;

        [Tooltip("Dialogue 用：这一屏要显示的文字（多行用回车分隔）")]
        [TextArea(2, 8)]
        public string Text = "";

        [Tooltip("ShowCG 用：要显示的 CG 图")]
        public Sprite CG;
    }

    [Header("界面")]
    [Tooltip("整个演出面板的根（默认隐藏；脚本挂在它外面）")]
    public GameObject CutsceneRoot;

    [Tooltip("CG 显示区（RawImage 或 Image，需要没有子物体遮挡）")]
    public Image CGScreen;

    [Tooltip("台词文字")]
    public Text DialogueText;

    [Tooltip("继续提示，每次显示台词时闪一下")]
    public GameObject ContinueHint;

    [Tooltip("跳过按钮（可选）")]
    public Button SkipButton;

    [Tooltip("结束时淡出用的黑幕（可选）")]
    public Image FadeOverlay;

    [Header("剧本")]
    public List<CutsceneStep> Steps = new List<CutsceneStep>();

    [Header("参数")]
    [Tooltip("CG 淡入 / 淡出时间")]
    public float CGFadeDuration = 0.6f;

    [Tooltip("文字打出来的速度（每秒几个字；0 = 直接全部显示）")]
    public float TypewriterSpeed = 45f;

    [Tooltip("允许点击跳过整个演出")]
    public bool AllowSkip = true;

    [Header("结束流程")]
    public string MenuSceneName = "Menu";
    public bool DeleteSaveOnFinish = true;
    public float FadeOutDuration = 1.0f;

    // 运行时状态
    private int _index;
    private bool _advanceRequested;   // 玩家点了一下
    private bool _skipRequested;      // 玩家要跳过整个演出

    private void Awake()
    {
        if (CutsceneRoot != null) CutsceneRoot.SetActive(false);
        if (SkipButton != null)
        {
            SkipButton.onClick.RemoveAllListeners();
            SkipButton.onClick.AddListener(() => { _skipRequested = true; _advanceRequested = true; });
        }
    }

    private void Update()
    {
        if (CutsceneRoot == null || !CutsceneRoot.activeSelf) return;

        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
        {
            _advanceRequested = true;
        }
        if (AllowSkip && Input.GetKeyDown(KeyCode.Escape))
        {
            _skipRequested = true;
            _advanceRequested = true;
        }
    }

    // =====================================================================
    //  播放
    // =====================================================================

    /// <summary>播放整个演出。GameFlow 会 yield 等它跑完。</summary>
    public IEnumerator Play()
    {
        if (Steps == null || Steps.Count == 0)
        {
            Debug.LogWarning("[结局] 剧本是空的，直接结束");
            yield return StartCoroutine(Finish());
            yield break;
        }

        // 显示面板
        if (CutsceneRoot != null) CutsceneRoot.SetActive(true);
        if (CGScreen != null) SetAlpha(CGScreen, 0f);
        if (DialogueText != null) DialogueText.text = "";
        if (ContinueHint != null) ContinueHint.SetActive(false);

        Debug.Log("[结局] 演出开始，共 " + Steps.Count + " 步");

        for (_index = 0; _index < Steps.Count; _index++)
        {
            if (_skipRequested) break;

            CutsceneStep step = Steps[_index];

            switch (step.Type)
            {
                case StepType.ShowCG:
                    yield return StartCoroutine(ShowCGRoutine(step.CG));
                    break;

                case StepType.HideCG:
                    yield return StartCoroutine(HideCGRoutine());
                    break;

                case StepType.Dialogue:
                    yield return StartCoroutine(DialogueRoutine(step.Text));
                    break;
            }
        }

        Debug.Log("[结局] 演出结束" + (_skipRequested ? "（玩家跳过）" : ""));
        yield return StartCoroutine(Finish());
    }

    // =====================================================================
    //  各类步骤
    // =====================================================================

    /// <summary>显示一句台词：逐字打出 → 等玩家点击 → 清空。</summary>
    private IEnumerator DialogueRoutine(string text)
    {
        if (DialogueText == null) yield break;

        _advanceRequested = false;

        // 逐字显示
        if (TypewriterSpeed > 0f && !string.IsNullOrEmpty(text))
        {
            DialogueText.text = "";
            float perChar = 1f / TypewriterSpeed;
            float timer = 0f;
            int shown = 0;

            while (shown < text.Length)
            {
                // 玩家点击 / 跳过 → 立刻显示全部
                if (_advanceRequested || _skipRequested)
                {
                    DialogueText.text = text;
                    break;
                }

                timer += Time.deltaTime;
                while (timer >= perChar && shown < text.Length)
                {
                    timer -= perChar;
                    shown++;
                }
                DialogueText.text = text.Substring(0, shown);
                yield return null;
            }
        }
        else
        {
            DialogueText.text = text;
        }

        // 等玩家点击推进
        if (ContinueHint != null) ContinueHint.SetActive(true);
        _advanceRequested = false;
        while (!_advanceRequested && !_skipRequested) yield return null;
        if (ContinueHint != null) ContinueHint.SetActive(false);

        DialogueText.text = "";
    }

    /// <summary>显示一张 CG（淡入）。</summary>
    private IEnumerator ShowCGRoutine(Sprite cg)
    {
        if (CGScreen == null || cg == null) yield break;

        // 已经是同一张就不重播
        if (CGScreen.sprite == cg && CGScreen.color.a > 0.99f) yield break;

        CGScreen.sprite = cg;
        CGScreen.enabled = true;

        float t = 0f;
        float from = CGScreen.color.a;
        while (t < CGFadeDuration)
        {
            if (_skipRequested) { SetAlpha(CGScreen, 1f); yield break; }
            t += Time.deltaTime;
            SetAlpha(CGScreen, Mathf.Lerp(from, 1f, t / CGFadeDuration));
            yield return null;
        }
        SetAlpha(CGScreen, 1f);
    }

    /// <summary>隐藏 CG（淡出）。</summary>
    private IEnumerator HideCGRoutine()
    {
        if (CGScreen == null) yield break;

        float t = 0f;
        float from = CGScreen.color.a;
        while (t < CGFadeDuration)
        {
            if (_skipRequested) break;
            t += Time.deltaTime;
            SetAlpha(CGScreen, Mathf.Lerp(from, 0f, t / CGFadeDuration));
            yield return null;
        }
        SetAlpha(CGScreen, 0f);
        CGScreen.sprite = null;
        CGScreen.enabled = false;
    }

    // =====================================================================
    //  收尾：淡出 → 删存档 → 回主菜单
    // =====================================================================

    private IEnumerator Finish()
    {
        if (ContinueHint != null) ContinueHint.SetActive(false);
        if (SkipButton != null) SkipButton.gameObject.SetActive(false);

        // 淡出
        if (FadeOverlay != null && FadeOutDuration > 0f)
        {
            FadeOverlay.gameObject.SetActive(true);
            float t = 0f;
            Color c = FadeOverlay.color;
            c.a = 0f;
            FadeOverlay.color = c;

            while (t < FadeOutDuration)
            {
                t += Time.unscaledDeltaTime;
                c.a = Mathf.Clamp01(t / FadeOutDuration);
                FadeOverlay.color = c;
                yield return null;
            }
        }

        // 删存档
        if (DeleteSaveOnFinish) DeleteRunSave();

        Time.timeScale = 1f;      // 防止设置界面暂停导致卡死

        Debug.Log("[结局] 回到主菜单：" + MenuSceneName);
        SceneManager.LoadScene(MenuSceneName);
    }

    private void DeleteRunSave()
    {
        PlayerPrefs.DeleteKey("run.cleared");
        PlayerPrefs.DeleteKey("run.unlocked");
        PlayerPrefs.DeleteKey("run.gold");
        PlayerPrefs.DeleteKey("run.hp");
        PlayerPrefs.DeleteKey("run.maxHp");
        PlayerPrefs.DeleteKey("run.deck");
        PlayerPrefs.Save();
        Debug.Log("[结局] 存档已删除");
    }

    private static void SetAlpha(Image img, float a)
    {
        if (img == null) return;
        Color c = img.color;
        c.a = Mathf.Clamp01(a);
        img.color = c;
    }
}
