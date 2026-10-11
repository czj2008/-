using UnityEngine;
public enum ShopItemType
{
    Heal,           // 回血
    MaxHealthUp,    // 最大生命 +N
    AddCard         // 加入一张牌
}

[System.Serializable]
public class ShopItemDef
{
    public string ItemName = "治疗药水";
    public string Description = "恢复 25 点生命";
    public Sprite Icon;

    public ShopItemType Type = ShopItemType.Heal;

    [Tooltip("效果数值：回血量 / 最大生命增加量")]
    public int Value = 25;

    [Tooltip("要加入牌组的牌（只有 AddCard 用）")]
    public CardDef CardToAdd;

    [Header("定价")]
    public int BasePrice = 40;

    [Tooltip("每通关一关涨多少（现在没有关卡，先留着）")]
    public int PriceStepPerLevel = 10;

    [Tooltip("最多买几次（0 = 不限）")]
    public int MaxPurchases = 0;
}