using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 总调度：选关界面 ↔ 战斗 ↔ 商店。
///
/// 【三个界面同一时刻只显示一个】
///   StartLevel()  → 隐藏选关、显示战斗、开始这一关
///   OnBattleEnded → 打赢则记录通关 + 解锁，然后回选关界面
///   OpenShop()    → 显示商店
///   CloseShop()   → 回选关界面
/// </summary>
public class GameFlow : MonoBehaviour
{
    [Header("引用")]
    public RunState Run;
    public LevelGraph Graph;
    public BattleManager Battle;
    public EnemyBrain EnemyAI;          // 场景里那只敌人的 AI

    [Header("界面")]
    public GameObject LevelSelectRoot;
    public GameObject BattleRoot;
    public GameObject ShopRoot;

    [Header("控制器（做了就拖）")]
    public LevelSelectController LevelSelect;
    public ShopController Shop;

    [Header("初始设置")]
    public int PlayerMaxHealth = 75;
    public int StartingGold = 50;

    [Header("战斗结果与奖励")]
    [Tooltip("胜负美术图展示（留在战斗界面上）")]
    public BattleResultView ResultView;

    [Tooltip("战后奖励面板（3 选 1）")]
    public CardRewardPanel RewardPanel;

    [Header("通关结局")]
    [Tooltip("结局演出（做好 CutsceneUI 后拖上）")]
    public CutsceneUI Ending;

    [Header("背景")]
    [Tooltip("战斗背景控制器")]
    public BackgroundView Background;

    /// <summary>当前正在打哪一关。</summary>
    private LevelDef _currentLevel;

    // =====================================================================
    //  初始化
    // =====================================================================
    public void StartNewGame()
    {
        Run.ClearSave();
        Run.StartNewRun(PlayerMaxHealth, StartingGold);
        Run.Save();                    // ★ 新开局立刻存一次
        ShowLevelSelect();
    }
    private void Start()
    {
        if (Run == null) { Debug.LogError("[Flow] RunState 没拖。", this); enabled = false; return; }

        // 1) 有存档就读，没有就开新局
        if (Run.HasSave()) Run.Load();
        else Run.StartNewRun(PlayerMaxHealth, StartingGold);

        // 2) 战斗结束 → 处理通关
        if (Battle != null)
        {
            Battle.BattleEnded -= OnBattleEnded;
            Battle.BattleEnded += OnBattleEnded;
        }

        // 3) 开局显示选关界面
        ShowLevelSelect();
    }

    private void OnDestroy()
    {
        if (Battle != null) Battle.BattleEnded -= OnBattleEnded;
    }

    // =====================================================================
    //  界面切换
    // =====================================================================

    private void HideAll()
    {
        if (LevelSelectRoot != null) LevelSelectRoot.SetActive(false);
        if (BattleRoot != null) BattleRoot.SetActive(false);
        if (ShopRoot != null) ShopRoot.SetActive(false);
    }

    public void ShowLevelSelect()
    {
        HideAll();
        if (LevelSelectRoot != null) LevelSelectRoot.SetActive(true);
        if (LevelSelect != null) LevelSelect.Show();
    }

    public void OpenShop()
    {
        if (Shop == null) { Debug.LogWarning("[Flow] 商店没接上。"); return; }
        HideAll();
        Shop.Open();
    }

    public void CloseShop()
    {
        ShowLevelSelect();
    }

    // =====================================================================
    //  进入关卡 ★
    // =====================================================================

    /// <summary>进入某一关：配置敌人 → 隐藏选关 → 显示战斗 → 开打。</summary>
    [Header("敌人生成")]
    public EnemySpawner Spawner;

    public void StartLevel(LevelDef level)
    {
        if (level == null) { Debug.LogWarning("[Flow] 空的关卡配置。"); return; }
        if (Battle == null) { Debug.LogError("[Flow] Battle 没拖。", this); return; }

        _currentLevel = level;
        Debug.Log("[Flow] 进入关卡：" + level.LevelName);

        if (ResultView != null) ResultView.HideImmediate();

        if (Background != null) Background.SetBackground(level.Background);

        // ---------- ① 生成这一关的敌人 ----------
        if (Spawner != null)
        {
            Spawner.SpawnWave(level.Enemies);
            Battle.SetEnemies(Spawner.SpawnedEnemies);      // ★ 把新敌人交给战斗系统
        }

        // ---------- ② 切界面 ----------
        HideAll();
        if (BattleRoot != null) BattleRoot.SetActive(true);

        // ---------- ③ 准备玩家并开打 ----------
        Battle.PreparePlayer(Run.PlayerMaxHealth, Run.PlayerHealth, Run.Deck);
        Battle.BeginBattle();
    }



    // =====================================================================
    //  战斗结束 ★
    // =====================================================================

    private void OnBattleEnded(bool playerWon)
    {
        StartCoroutine(BattleEndRoutine(playerWon));
    }

    private IEnumerator BattleEndRoutine(bool playerWon)
    {
        // 1) 播"胜利 / 失败"美术图（等它播完）
        if (ResultView != null)
        {
            yield return StartCoroutine(ResultView.Play(playerWon));
        }

        // 2) 失败：回满血，直接回选关界面（不发奖励）
        if (!playerWon)
        {
            Run.PlayerHealth = Run.PlayerMaxHealth;
            Run.Save();
            Debug.Log("[Flow] 挑战失败，血量已恢复。");

            _currentLevel = null;
            ShowLevelSelect();
            yield break;
        }

        // 3) 胜利：记录通关 + 解锁后续关卡（奖励的金币由奖励面板发）
        if (Battle != null && Battle.Player != null) Run.PlayerHealth = Battle.Player.Data.Health;

        if (_currentLevel != null)
        {
            Run.MarkLevelCleared(_currentLevel.LevelId, 0);      // ★ 金币传 0：奖励面板会发
            Debug.Log("[Flow] 通关「" + _currentLevel.LevelName + "」");
        }
        Run.Save();

        if (_currentLevel != null && _currentLevel.IsFinalLevel)
        {
            Debug.Log("[Flow] ★ 这是最终关，进入结局演出（跳过卡牌奖励）");

            if (Ending != null)
            {
                yield return StartCoroutine(Ending.Play());
                // Play() 内部会：淡出 → 删除原存档 → 回主菜单
                yield break;
            }
            else
            {
                Debug.LogWarning("[Flow] Ending 没拖（CutsceneUI 没接上），直接回选关界面");
            }
        }

        // ---------- 5) 非最终关：显示奖励面板（3 选 1 + 金币）----------
        if (RewardPanel != null && _currentLevel != null)
        {
            bool finished = false;
            System.Action onClosed = () => finished = true;
            RewardPanel.Closed += onClosed;

            RewardPanel.Show(_currentLevel.GoldReward, "战斗胜利  选择一张牌");

            while (!finished) yield return null;

            RewardPanel.Closed -= onClosed;
        }
        else
        {
            if (_currentLevel != null) Run.Gold += _currentLevel.GoldReward;
            Run.Save();
        }

        // ---------- 6) 回选关界面 ----------
        _currentLevel = null;
        Run.Save();
        ShowLevelSelect();
    }

    // =====================================================================
    //  调试
    // =====================================================================

    [ContextMenu("调试：给 200 金币")]
    private void DebugAddGold() { Run.Gold += 200; Run.Save(); Debug.Log("[Flow] 金币 " + Run.Gold); }

    [ContextMenu("调试：清空存档")]
    private void DebugClearSave() { Run.ClearSave(); Debug.Log("[Flow] 存档已清空。"); }
}