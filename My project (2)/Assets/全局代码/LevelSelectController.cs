using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 选关界面控制器。
///
/// 【和"代码生成按钮"版本的区别】
///   关卡按钮【在场景里手摆】，每个挂一个 LevelButton 脚本、填好自己的 levelId 和三张状态图。
///   本脚本只做三件事：收集按钮 → 按 id 关联关卡 → 按进度换图。
///
/// 好处：位置完全由你在 Scene 视图里拖，做"地图上散落的关卡"这种布局很自然。
/// </summary>
public class LevelSelectController : MonoBehaviour
{
    [Header("引用")]
    public RunState Run;
    public LevelGraph Graph;
    public GameFlow Flow;

    [Header("界面")]
    [Tooltip("选关面板的根（整体显示/隐藏）")]
    public GameObject SelectRoot;

    [Tooltip("所有关卡按钮的父物体（手摆的按钮都放它下面）")]
    public Transform ButtonRoot;

    [Tooltip("金币显示（可留空）")]
    public Text GoldText;

    private readonly List<LevelButton> _buttons = new List<LevelButton>();

    // =====================================================================
    //  初始化
    // =====================================================================

    private void Start()
    {
        CollectButtons();
        Refresh();
    }

    /// <summary>收集场景里所有关卡按钮（包括未激活的）。</summary>
    private void CollectButtons()
    {
        _buttons.Clear();

        if (ButtonRoot == null)
        {
            Debug.LogError("[选关] Button Root 没拖，找不到关卡按钮。", this);
            return;
        }

        LevelButton[] found = ButtonRoot.GetComponentsInChildren<LevelButton>(true);
        for (int i = 0; i < found.Length; i++)
        {
            LevelButton button = found[i];

            // 检查 id 有没有对应关卡（配错了要早点发现）
            if (Graph != null && Graph.GetLevel(button.LevelId) == null)
            {
                Debug.LogError("[选关] 按钮「" + button.name + "」的 LevelId「" + button.LevelId +
                               "」在 LevelGraph 里找不到对应关卡。", button);
                continue;
            }

            string levelId = button.LevelId;                 // 闭包捕获
            button.Button.onClick.RemoveAllListeners();
            button.Button.onClick.AddListener(() => OnLevelClicked(levelId));

            _buttons.Add(button);
        }

        Debug.Log("[选关] 收集到 " + _buttons.Count + " 个关卡按钮。");
    }

    // =====================================================================
    //  刷新（按进度换图）
    // =====================================================================

    public void Refresh()
    {
        if (Run == null || Graph == null) return;

        // "当前进度"= 第一个还没通关的关卡，用来显示箭头之类的指示
        string currentId = FindCurrentLevelId();

        for (int i = 0; i < _buttons.Count; i++)
        {
            LevelButton button = _buttons[i];
            LevelDef level = Graph.GetLevel(button.LevelId);
            if (level == null) continue;

            bool cleared = Run.IsLevelCleared(level.LevelId);
            bool playable = Graph.IsPlayable(level, Run);

            // 可点性：只有"已解锁且没通关"的能点
            button.Button.interactable = playable;

            // 换图 + 显示/隐藏标记
            button.ApplyState(cleared, playable, level.LevelId == currentId);
        }

        if (GoldText != null) GoldText.text = "金币 " + Run.Gold;
    }

    /// <summary>
    /// 找出"当前该打哪一关"：玩家已经解锁、但还没通关的那一关。
    /// 用来给按钮显示一个"你在这"的指示。
    /// </summary>
    private string FindCurrentLevelId()
    {
        for (int i = 0; i < _buttons.Count; i++)
        {
            LevelButton button = _buttons[i];
            LevelDef level = Graph.GetLevel(button.LevelId);
            if (level == null) continue;

            if (Run.IsLevelUnlocked(level.LevelId) && !Run.IsLevelCleared(level.LevelId))
            {
                return level.LevelId;
            }
        }
        return null;
    }

    // =====================================================================
    //  交互
    // =====================================================================

    private void OnLevelClicked(string levelId)
    {
        LevelDef level = Graph.GetLevel(levelId);
        if (level == null) return;

        // 二次校验（按钮状态可能没刷新）
        if (Run.IsLevelCleared(levelId)) { Debug.Log("[选关] 这一关已经打过了。"); return; }
        if (!Run.IsLevelUnlocked(levelId)) { Debug.Log("[选关] 这一关还没解锁。"); return; }

        Flow.StartLevel(level);
    }

    public void Show()
    {
        if (SelectRoot != null) SelectRoot.SetActive(true);
        Refresh();
    }

    public void Hide()
    {
        if (SelectRoot != null) SelectRoot.SetActive(false);
    }
}