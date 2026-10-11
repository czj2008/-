using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShopController : MonoBehaviour
{
    [Header("引用")]
    public RunState Run;
    public GameFlow Flow;

    [Header("界面")]
    [Tooltip("整个商店面板的根（默认隐藏）")]
    public GameObject ShopRoot;

    [Tooltip("商品的父物体（需要有 Vertical / Grid Layout Group）")]
    public RectTransform ItemRoot;

    [Tooltip("商品预制体")]
    public Button ItemPrefab;

    [Tooltip("金币显示")]
    public Text GoldText;

    [Tooltip("离开按钮")]
    public Button CloseButton;

    [Tooltip("提示文字（可选，显示“金币不足”这类反馈）")]
    public Text HintText;

    [Header("商品列表")]
    public List<ShopItemDef> Items = new List<ShopItemDef>();

    private readonly List<Button> _itemButtons = new List<Button>();
    private readonly Dictionary<int, int> _purchaseCount = new Dictionary<int, int>();

    //  初始化

    private void Awake()
    {
        if (CloseButton != null)
        {
            CloseButton.onClick.RemoveAllListeners();
            CloseButton.onClick.AddListener(Close);
        }

        if (HintText != null) HintText.text = "";

        BuildItems();
    }

    private void Start()
    {
        Refresh();
    }

    private void BuildItems()
    {
        for (int i = ItemRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(ItemRoot.GetChild(i).gameObject);
        }
        _itemButtons.Clear();

        for (int i = 0; i < Items.Count; i++)
        {
            ShopItemDef item = Items[i];
            Button button = Instantiate(ItemPrefab, ItemRoot);

            // 按名字找子物体（比用数组下标稳，层级怎么摆都不怕）
            SetText(button, "NameText", item.ItemName);
            SetText(button, "DescText", item.Description);
            SetText(button, "PriceText", "");           // 价格在 Refresh 里填

            Image icon = FindImage(button, "Icon");
            if (icon != null && item.Icon != null) icon.sprite = item.Icon;

            int index = i;
            button.onClick.AddListener(() => TryBuy(index));

            _itemButtons.Add(button);
        }
    }

    //  价格与刷新

    //某件商品的当前价格（随进度上涨）
    public int GetPrice(int itemIndex)
    {
        if (itemIndex < 0 || itemIndex >= Items.Count) return 0;

        ShopItemDef item = Items[itemIndex];
        int cleared = Run != null ? Run.ClearedCount : 0;
        return item.BasePrice + item.PriceStepPerLevel * cleared;
    }

    //刷新所有商品的价格和可买状态
    public void Refresh()
    {
        for (int i = 0; i < _itemButtons.Count; i++)
        {
            ShopItemDef item = Items[i];
            int price = GetPrice(i);

            int bought = _purchaseCount.ContainsKey(i) ? _purchaseCount[i] : 0;
            bool soldOut = item.MaxPurchases > 0 && bought >= item.MaxPurchases;
            bool affordable = Run != null && Run.Gold >= price;
            bool usable = CheckUsable(item);            // 条件是否满足（比如满血不能买药）

            _itemButtons[i].interactable = !soldOut && affordable && usable;

            Text priceText = FindText(_itemButtons[i], "PriceText");
            if (priceText != null)
            {
                priceText.text = soldOut ? "已售罄"
                               : !affordable ? price + " 金（不足）"
                               : !usable ? price + " 金（用不上）"
                               : price + " 金";

                priceText.color = soldOut ? new Color(0.6f, 0.6f, 0.6f)
                                : affordable && usable ? new Color(0.95f, 0.85f, 0.45f)
                                : new Color(0.85f, 0.40f, 0.40f);
            }
        }

        RefreshGold();
    }

    private void RefreshGold()
    {
        if (GoldText != null && Run != null) GoldText.text = "金币 " + Run.Gold;
    }

    //这件商品现在"用得上"吗（用不上就不让买，避免浪费钱）
    private bool CheckUsable(ShopItemDef item)
    {
        if (Run == null) return false;

        switch (item.Type)
        {
            case ShopItemType.Heal:
                return Run.PlayerHealth < Run.PlayerMaxHealth;    // 满血不能买
            case ShopItemType.AddCard:
                return item.CardToAdd != null;
            default:
                return true;
        }
    }

    // =====================================================================
    //  购买
    // =====================================================================

    private void TryBuy(int itemIndex)
    {
        ShopItemDef item = Items[itemIndex];
        int price = GetPrice(itemIndex);

        if (Run.Gold < price)
        {
            ShowHint("金币不足，还差 " + (price - Run.Gold) + " 金");
            return;
        }

        if (!CheckUsable(item))
        {
            ShowHint("现在用不上这件商品");
            return;
        }

        if (!ApplyEffect(item))
        {
            ShowHint("购买失败");
            return;
        }

        Run.Gold -= price;

        int bought = _purchaseCount.ContainsKey(itemIndex) ? _purchaseCount[itemIndex] : 0;
        _purchaseCount[itemIndex] = bought + 1;

        ShowHint("已购买「" + item.ItemName + "」");
        Debug.Log("[Shop] 购买「" + item.ItemName + "」，花费 " + price + "，剩余金币 " + Run.Gold);

        if (Run != null) Run.Save();
        Refresh();
    }

    /// <summary>执行商品效果。返回 false = 失败（不扣钱）。</summary>
    private bool ApplyEffect(ShopItemDef item)
    {
        switch (item.Type)
        {
            case ShopItemType.Heal:
                Run.PlayerHealth = Mathf.Min(Run.PlayerMaxHealth, Run.PlayerHealth + item.Value);
                return true;

            case ShopItemType.MaxHealthUp:
                Run.PlayerMaxHealth += item.Value;
                Run.PlayerHealth += item.Value;          // 顺带回这么多血，手感更好
                return true;

            case ShopItemType.AddCard:
                if (item.CardToAdd == null) return false;
                Run.Deck.Add(item.CardToAdd);
                return true;

            default:
                return false;
        }
    }

    //  显示 / 隐藏

    public void Open()
    {
        if (ShopRoot != null) ShopRoot.SetActive(true);
        if (HintText != null) HintText.text = "";
        Refresh();
    }

    public void Close()
    {
        if (ShopRoot != null) ShopRoot.SetActive(false);
        if (Flow != null) Flow.CloseShop();       // 通知总控"我要关掉了"
    }

    //显示一行提示（3 秒后自动消失）
    private void ShowHint(string message)
    {
        if (HintText == null) return;
        HintText.text = message;
        CancelInvoke("ClearHint");
        Invoke("ClearHint", 3f);
    }

    private void ClearHint()
    {
        if (HintText != null) HintText.text = "";
    }



    private static Text FindText(Button button, string childName)
    {
        Transform found = button.transform.Find(childName);
        return found != null ? found.GetComponent<Text>() : null;
    }

    private static Image FindImage(Button button, string childName)
    {
        Transform found = button.transform.Find(childName);
        return found != null ? found.GetComponent<Image>() : null;
    }

    private static void SetText(Button button, string childName, string content)
    {
        Text text = FindText(button, childName);
        if (text != null) text.text = content;
    }
}
