using System.Collections.Generic;
using UnityEngine;

public enum CardKind
{
    Attack,
    Skill
}

/// <summary>卡牌效果的类型。</summary>
public enum CardEffectType
{
    Damage,     // 造成伤害（需要目标）
    Block,      // 获得格挡
    Heal,       // 回复生命
    Draw,       // 抽牌
    GainEnergy  // 获得能量
}

/// <summary>一条卡牌效果。</summary>
[System.Serializable]
public class CardEffect
{
    public CardEffectType Type = CardEffectType.Damage;

    [Tooltip("数值：伤害 / 格挡 / 治疗量 / 抽几张 / 几点能量，由 Type 决定含义")]
    public int Value = 1;
}

[System.Serializable]
public class CardDef
{
    public string Id = "strike";

    public string Title = "打击";

    public int Cost = 1;

    [Tooltip("只影响卡牌颜色分类，不影响实际效果")]
    public CardKind Kind = CardKind.Attack;

    [Header("★ 效果列表（按顺序执行，从上往下）")]
    [Tooltip("留空 = 用下面的旧字段 Value（兼容老卡牌）")]
    public List<CardEffect> Effects = new List<CardEffect>();

    [Header("旧字段（Effects 为空时才生效）")]
    public int Value = 6;

    [Tooltip("卡面")]
    public Sprite CardFace;

    /// <summary>
    /// 取这张牌最终要执行的效果列表。
    /// 【为什么这样设计】Effects 为空时自动用旧的 Kind+Value 生成一条，
    /// 这样以前配好的卡牌不用改，新卡牌用 Effects 就行。
    /// </summary>
    public List<CardEffect> GetEffects()
    {
        if (Effects != null && Effects.Count > 0) return Effects;

        // 兜底：把旧的 Kind + Value 转成一条效果
        List<CardEffect> fallback = new List<CardEffect>();
        fallback.Add(new CardEffect
        {
            Type = (Kind == CardKind.Attack) ? CardEffectType.Damage : CardEffectType.Block,
            Value = Value
        });
        return fallback;
    }

    /// <summary>是不是攻击牌（决定要不要选目标）——只要有一条伤害就算。</summary>
    public bool HasDamage()
    {
        List<CardEffect> list = GetEffects();
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].Type == CardEffectType.Damage && list[i].Value > 0) return true;
        }
        return false;
    }
}