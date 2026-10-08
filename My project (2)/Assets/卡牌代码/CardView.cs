using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 单张手牌：把一份 CardDef 画成"一张卡面图"，并接收点击。
///
/// 【设计选择】卡名、插画、说明全部画在卡面贴图里，所以这里没有卡名/说明的 Text，
/// 代码不参与卡面排版。改卡面 = 换一张图；换数值 = 改 CardDef（图上的文字也要跟着改）。
///
/// 结构（Prefab）：
///   Card                    RectTransform(170x240) + CardView（+ CanvasGroup，运行时自动补）
///   └─ Face      (Image)    铺满整张卡，显示卡面贴图，同时负责接收点击
///       └─ CostText (Text)  可选：想让费用数字动态显示时才需要
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class CardView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("卡面")]
    [Tooltip("显示卡面的 Image（铺满整张卡）")]
    public Image Face;

    [Tooltip("（可选）费用数字。卡面图里已经画了费用就留空")]
    public Text CostText;

    [Header("交互")]
    [Tooltip("鼠标悬停时的放大倍数")]
    public float HoverScale = 1.12f;

    [Tooltip("能量不足时的不透明度（1 = 不变暗）")]
    [Range(0.3f, 1f)] public float DisabledAlpha = 0.8f;

    /// <summary>这张牌代表的数据。</summary>
    public CardDef Data { get; private set; }

    private CardManager _manager;
    private RectTransform _rect;
    private CanvasGroup _group;
    private Coroutine _scaleRoutine;
    private bool _interactable = true;
    private bool _initialized;

    /// <summary>
    /// 整张牌的 CanvasGroup（淡入淡出用）。第一次访问时自动挂载，所以永远不会是 null。
    /// </summary>
    public CanvasGroup Group
    {
        get
        {
            if (_group == null) _group = GetComponent<CanvasGroup>();
            if (_group == null) _group = gameObject.AddComponent<CanvasGroup>();
            return _group;
        }
    }

    // =====================================================================
    //  初始化
    // =====================================================================

    private void Awake()
    {
        Init();
    }

    /// <summary>
    /// 显式初始化。由 CardManager 在 Instantiate 之后立刻调用。
    ///
    /// 【为什么不能只靠 Awake】
    /// 如果 Prefab 是未激活状态，Unity 不会调用 Awake，字段会一直是 null，
    /// 一访问就 NullReferenceException。显式调用可以彻底避免这个问题。
    /// </summary>
    public void Init()
    {
        if (_initialized) return;
        _initialized = true;

        _rect = GetComponent<RectTransform>();
        _group = GetComponent<CanvasGroup>();
        if (_group == null) _group = gameObject.AddComponent<CanvasGroup>();

        // Face 没拖就自动找（第一个 Image 子物体）
        if (Face == null) Face = GetComponentInChildren<Image>(true);

        if (Face == null)
        {
            Debug.LogError("[CardView] 找不到卡面 Image，卡牌不会显示。请把 Face 拖进 Card View 的 Face 槽。", this);
        }
    }

    // =====================================================================
    //  刷新卡面
    // =====================================================================

    /// <summary>用一份数据刷新卡面。每次从对象池取出时调用。</summary>
    public void Setup(CardDef data, CardManager manager)
    {
        Init();

        Data = data;
        _manager = manager;

        // ★ 核心：只换一张图
        if (Face != null)
        {
            Face.sprite = data.CardFace;
            Face.enabled = true;
        }

        if (CostText != null) CostText.text = data.Cost.ToString();

        // 重置状态（对象池复用必须清干净）
        SetInteractable(true);
        SetAlpha(1f);
        if (_rect != null) _rect.localScale = Vector3.one;
    }

    // =====================================================================
    //  状态控制
    // =====================================================================

    /// <summary>能不能点（不是玩家回合 / 能量不足时由 CardManager 调成 false）。</summary>
    public void SetInteractable(bool value)
    {
        _interactable = value;
        SetAlpha(value ? 1f : DisabledAlpha);
    }

    /// <summary>整张牌的透明度。</summary>
    public void SetAlpha(float alpha)
    {
        Group.alpha = Mathf.Clamp01(alpha);
    }

    /// <summary>回收进对象池前重置外观。</summary>
    public void ResetVisual()
    {
        SetAlpha(1f);
        if (_rect != null) _rect.localScale = Vector3.one;
    }

    // =====================================================================
    //  鼠标交互
    // =====================================================================

    public void OnPointerEnter(PointerEventData eventData)
    {
        ScaleTo(HoverScale, 0.08f);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ScaleTo(1f, 0.08f);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!_interactable) return;                       // 能量不足 / 不是玩家回合：不响应
        if (_manager != null) _manager.OnCardClicked(this);
    }

    private void ScaleTo(float scale, float duration)
    {
        if (_rect == null) return;
        if (_scaleRoutine != null) StopCoroutine(_scaleRoutine);
        _scaleRoutine = StartCoroutine(Tween.Scale(_rect, _rect.localScale, Vector3.one * scale, duration));
    }
}