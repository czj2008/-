using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 手牌管理：抽牌 → 扇形排版 → 点击出牌 → 弃牌。
///
/// 分工：
///   CardManager   —— 只管"牌"的流程（抽/排/打/弃）
///   BattleManager —— 只管"回合"和"伤害结算"
///   CardView      —— 只管"把 CardDef 画成一张卡面图 + 接点击"
///
/// 卡牌对象用对象池复用（打出去的牌 SetActive(false) 收起来，下次抽牌再拿出来），
/// 避免反复 Instantiate/Destroy 产生 GC 卡顿。
/// </summary>
public class CardManager : MonoBehaviour
{
    [Header("界面引用")]
    [Tooltip("卡牌 Prefab（手牌都是它的实例）")]
    public CardView CardPrefab;

    [Tooltip("手牌容器（卡牌会挂到它下面）")]
    public RectTransform HandArea;

    [Tooltip("抽牌堆数量文字（可留空）")]
    public Text DrawPileText;

    [Tooltip("弃牌堆数量文字（可留空）")]
    public Text DiscardPileText;

    [Header("引用")]
    public BattleManager Battle;
    public CardLibrary Library;

    [Header("排版参数（按 1920x1080 设计分辨率）")]
    [Tooltip("卡牌宽度，要和 Prefab 的宽度一致")]
    public float CardWidth = 170f;

    [Tooltip("卡牌之间的间距")]
    public float CardSpacing = 30f;

    [Tooltip("手牌底边距屏幕底部的像素")]
    public float HandBaseY = 20f;

    [Header("规则")]
    [Tooltip("每回合抽几张牌")]
    public int DrawPerTurn = 5;

    [Tooltip("手牌上限")]
    public int MaxHandSize = 10;

    private Deck _deck;
    private readonly List<CardView> _hand = new List<CardView>();
    private readonly List<CardView> _pool = new List<CardView>();

    /// <summary>正在出牌动画中（此时不接受新的点击）。</summary>
    private bool _busy;

    public int HandCount { get { return _hand.Count; } }

    // =====================================================================
    //  初始化
    // =====================================================================

    private void Start()
    {
        if (CardPrefab == null || HandArea == null || Library == null)
        {
            Debug.LogError("[CardManager] 缺少引用，卡牌系统停止工作："
                + " CardPrefab=" + (CardPrefab != null)
                + " HandArea=" + (HandArea != null)
                + " Library=" + (Library != null), this);
            enabled = false;
            return;
        }

        _deck = new Deck();
        _deck.Initialize(Library.BuildStartingDeck());
        RefreshPileText();
    }

    // =====================================================================
    //  抽牌
    // =====================================================================

    /// <summary>抽满一手牌（每个玩家回合开始时由 BattleManager 调用）。</summary>
    public IEnumerator DrawHand()
    {
        if (_deck == null)
        {
            Debug.LogError("[CardManager] _deck 是 null，Start() 里的初始化没执行。", this);
            yield break;
        }

        for (int i = 0; i < DrawPerTurn; i++)
        {
            if (_hand.Count >= MaxHandSize) break;

            CardDef card = _deck.Draw();
            if (card == null) break;

            CardView view = GetFromPool();
            if (view == null) break;

            view.Setup(card, this);
            _hand.Add(view);
        }

        RefreshPileText();
        RefreshPlayableState();
        yield return StartCoroutine(LayoutHand(0.18f));
    }

    // =====================================================================
    //  出牌
    // =====================================================================

    /// <summary>卡牌被点击时由 CardView 回调。</summary>
    public void OnCardClicked(CardView view)
    {
        if (_busy || view == null || view.Data == null) return;

        if (Battle == null || !Battle.IsPlayerTurn) return;
        if (!Battle.CanSpendEnergy(view.Data.Cost)) return;

        StartCoroutine(PlayRoutine(view));
    }

    private IEnumerator PlayRoutine(CardView view)
    {
        _busy = true;
        CardDef data = view.Data;

        // 1) 扣能量
        Battle.SpendEnergy(data.Cost);

        // 2) 从手牌移除并重排
        _hand.Remove(view);
        yield return StartCoroutine(LayoutHand(0.10f));

        // 3) 飞向屏幕中央并放大
        RectTransform rt = view.transform as RectTransform;
        view.SetInteractable(false);

        if (rt != null)
        {
            StartCoroutine(Tween.Scale(rt, Vector3.one, Vector3.one * 1.25f, 0.15f));
            yield return StartCoroutine(Tween.MoveAnchored(rt, rt.anchoredPosition, new Vector2(0f, 380f), 0.15f));
        }

        // 4) 结算效果（由 BattleManager 播战斗演出）
        yield return StartCoroutine(Battle.PlayCard(data));

        // 5) 淡出 → 进"已打出" → 回收
        yield return StartCoroutine(Tween.Fade(view.Group, 1f, 0f, 0.12f));
        _deck.MarkPlayed(data);
        ReturnToPool(view);

        RefreshPileText();
        RefreshPlayableState();

        _busy = false;
    }

    // =====================================================================
    //  弃牌
    // =====================================================================

    /// <summary>弃掉整手牌（玩家回合结束时由 BattleManager 调用）。</summary>
    public IEnumerator DiscardHand()
    {
        if (_hand.Count > 0)
        {
            List<CardView> discarding = new List<CardView>(_hand);
            _hand.Clear();

            for (int i = 0; i < discarding.Count; i++)
            {
                CardView view = discarding[i];
                view.SetInteractable(false);
                StartCoroutine(DiscardVisual(view));
                _deck.Discard(view.Data);
                yield return Tween.WaitRealtime(0.05f);
            }

            yield return Tween.WaitRealtime(0.2f);

            for (int i = 0; i < discarding.Count; i++) ReturnToPool(discarding[i]);
        }

        _deck.FlushPlayed();
        RefreshPileText();
    }

    /// <summary>弃牌动画：缩小 + 飞向左下角（弃牌堆方向）。</summary>
    private IEnumerator DiscardVisual(CardView view)
    {
        RectTransform rt = view.transform as RectTransform;
        if (rt == null) yield break;

        StartCoroutine(Tween.Scale(rt, rt.localScale, Vector3.one * 0.6f, 0.2f));
        yield return StartCoroutine(Tween.MoveAnchored(rt, rt.anchoredPosition, new Vector2(-750f, -300f), 0.2f));
    }

    // =====================================================================
    //  排版（扇形排列）
    // =====================================================================

    private IEnumerator LayoutHand(float duration)
    {
        int count = _hand.Count;
        if (count == 0) yield break;

        float step = CardWidth + CardSpacing;

        float maxWidth = HandArea != null ? HandArea.rect.width - 100f : 1800f;
        if (count * step > maxWidth && count > 1)
        {
            step = Mathf.Max((maxWidth - CardWidth) / (count - 1), 40f);
        }

        float centerIndex = (count - 1) * 0.5f;

        for (int i = 0; i < count; i++)
        {
            CardView view = _hand[i];
            if (view == null) continue;

            RectTransform rt = view.transform as RectTransform;
            if (rt == null) continue;

            float offset = i - centerIndex;

            // X 均匀排开；Y 中间高两边低 → 扇形观感
            float arc = -Mathf.Abs(offset) * 6f;
            Vector2 target = new Vector2(offset * step, HandBaseY + arc);

            view.transform.SetSiblingIndex(i);   // 右边的牌盖住左边的牌
            yield return StartCoroutine(Tween.MoveAnchored(rt, rt.anchoredPosition, target, duration));
        }
    }

    // =====================================================================
    //  状态刷新
    // =====================================================================

    /// <summary>按当前能量刷新"哪些牌能打"（不能打的会变暗）。</summary>
    public void RefreshPlayableState()
    {
        for (int i = 0; i < _hand.Count; i++)
        {
            CardView view = _hand[i];
            if (view == null || view.Data == null) continue;

            bool playable = Battle != null && Battle.IsPlayerTurn && Battle.CanSpendEnergy(view.Data.Cost);
            view.SetInteractable(playable);
        }
    }

    private void RefreshPileText()
    {
        if (_deck == null) return;
        if (DrawPileText != null) DrawPileText.text = "抽牌堆 " + _deck.DrawPileCount;
        if (DiscardPileText != null) DiscardPileText.text = "弃牌堆 " + _deck.DiscardPileCount;
    }

    // =====================================================================
    //  对象池
    // =====================================================================

    private CardView GetFromPool()
    {
        CardView view;

        if (_pool.Count > 0)
        {
            view = _pool[_pool.Count - 1];
            _pool.RemoveAt(_pool.Count - 1);
            view.gameObject.SetActive(true);
        }
        else
        {
            if (CardPrefab == null) return null;
            view = Instantiate(CardPrefab, HandArea);
        }

        view.Init();      // 显式初始化（不依赖 Awake，避免 inactive 时不执行）

        // 初始位置放在手牌区下方，随后由 LayoutHand 滑上来
        RectTransform rt = view.transform as RectTransform;
        if (rt != null) rt.anchoredPosition = new Vector2(0f, -280f);

        return view;
    }

    private void ReturnToPool(CardView view)
    {
        if (view == null) return;

        view.ResetVisual();

        RectTransform rt = view.transform as RectTransform;
        if (rt != null) rt.localScale = Vector3.one;

        view.gameObject.SetActive(false);
        _pool.Add(view);
    }
}