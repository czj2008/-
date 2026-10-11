using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 战后奖励面板：显示 3 张可选卡 + 金币 + 跳过按钮。
///
/// 【复用卡牌 Prefab】展示用的卡牌就是战斗里那张 CardView.prefab，
/// 所以奖励的卡面和你手牌长得完全一样，不用维护两套美术。
///
/// 【面板结构】
///   CardRewardPanel            (Image 全屏底 + CanvasGroup + 本脚本)
///   ├─ Title        (Text)     "战斗胜利  选择一张牌"
///   ├─ GoldText     (Text)     "+30 金币"
///   ├─ CardRoot     (空物体)   卡牌生成在这里（加 Horizontal Layout Group）
///   └─ SkipButton   (Button)   "跳过"
/// </summary>
public class CardRewardPanel : MonoBehaviour
{
    [Header("引用")]
    public RunState Run;
    public CardManager Cards;

    [Header("界面")]
    public GameObject RewardRoot;
    public RectTransform CardRoot;
    public Text TitleText;
    public Text GoldText;
    public Button SkipButton;

    [Header("卡牌 Prefab（用战斗里那个 CardView.prefab）")]
    public CardView CardPrefab;

    [Header("掉落池")]
    [Tooltip("可以掉落的卡牌（权重控制出现率）")]
    public List<RewardOption> Pool = new List<RewardOption>();

    [Header("设置")]
    [Tooltip("每次给几个选项")]
    public int OptionCount = 3;

    /// <summary>面板是否正在显示。</summary>
    public bool IsOpen { get { return RewardRoot != null && RewardRoot.activeSelf; } }

    /// <summary>玩家选完之后的通知（GameFlow 订阅它，用来回选关界面）。</summary>
    public System.Action Closed;

    private readonly List<CardView> _spawned = new List<CardView>();
    private Coroutine _routine;

    private void Awake()
    {
        if (RewardRoot == null) RewardRoot = gameObject;

        if (SkipButton != null)
        {
            SkipButton.onClick.RemoveAllListeners();
            SkipButton.onClick.AddListener(OnSkip);
        }
    }

    // 靠 Inspector 里默认取消勾选来隐藏
    // 如果一定要代码隐藏，用 Start（但注意 Start 也只在物体激活后才跑）

    // =====================================================================
    //  显示
    // =====================================================================

    /// <summary>
    /// 显示奖励面板。
    /// </summary>
    /// <param name="goldReward">这一关的金币奖励（领奖时就加进 RunState）</param>
    /// <param name="title">标题文字（比如"战斗胜利"）</param>
    public void Show(int goldReward, string title)
    {
        // ★ 第一步：激活【脚本自己所在的物体】（这是 StartCoroutine 的前提）
        gameObject.SetActive(true);

        // RewardRoot 只是"兼容旧配置"，有就顺手激活一下
        if (RewardRoot != null && RewardRoot != gameObject) RewardRoot.SetActive(true);

        // 发金币
        Run.Gold += goldReward;
        Run.Save();

        // 标题和金币文字
        if (TitleText != null) TitleText.text = title;
        if (GoldText != null) GoldText.text = "获得 " + goldReward + " 金币";

        // 抽候选牌
        BuildCards();

        // 弹出动画（此时物体一定是激活的）
        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(PopIn());
    }

    private void BuildCards()
    {
        // 清掉上次的
        for (int i = 0; i < _spawned.Count; i++)
        {
            if (_spawned[i] != null) Destroy(_spawned[i].gameObject);
        }
        _spawned.Clear();

        if (Cards == null || CardPrefab == null || CardRoot == null)
        {
            Debug.LogError("[奖励] Cards / Card Prefab / Card Root 没拖，无法显示奖励卡。", this);
            return;
        }

        List<CardDef> options = Cards.DrawRandomRewardCards(Pool, OptionCount);
        if (options.Count == 0)
        {
            Debug.LogWarning("[奖励] 掉落池是空的，没有牌可选。", this);
            return;
        }

        for (int i = 0; i < options.Count; i++)
        {
            CardDef card = options[i];
            CardView view = Instantiate(CardPrefab, CardRoot);
            view.Setup(card, null);            // manager 传 null：奖励卡不参与出牌逻辑

            // 点这张卡 = 选择它
            Button button = view.GetComponent<Button>();
            if (button == null) button = view.gameObject.AddComponent<Button>();

            CardDef captured = card;           // 闭包捕获
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => OnCardChosen(captured));

            _spawned.Add(view);
        }

        Debug.Log("[奖励] 给出 " + options.Count + " 张候选牌");
    }

    private IEnumerator PopIn()
    {
        // 整个面板从 0.9 倍弹到 1 倍（用 unscaled，暂停时也能播）
        float t = 0f;
        const float duration = 0.18f;
        Vector3 from = Vector3.one * 0.9f;

        transform.localScale = from;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            transform.localScale = Vector3.LerpUnclamped(from, Vector3.one, t / duration);
            yield return null;
        }
        transform.localScale = Vector3.one;
    }

    // =====================================================================
    //  选择 / 跳过
    // =====================================================================

    private void OnCardChosen(CardDef card)
    {
        if (card == null) return;

        Run.Deck.Add(card);            // ★ 加进牌组（会跟着存档延续）
        Run.Save();

        Debug.Log("[奖励] 玩家选择了「" + card.Title + "」，牌组现在 " + Run.Deck.Count + " 张");

        Close();
    }

    private void OnSkip()
    {
        Debug.Log("[奖励] 玩家跳过了选牌");
        Close();
    }

    private void Close()
    {
        // 清掉展示用的卡牌
        for (int i = 0; i < _spawned.Count; i++)
        {
            if (_spawned[i] != null) Destroy(_spawned[i].gameObject);
        }
        _spawned.Clear();

        // 隐藏自己
        gameObject.SetActive(false);

        if (Closed != null) Closed();
    }
}