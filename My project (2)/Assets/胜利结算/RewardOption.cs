using UnityEngine;

[System.Serializable]
public class RewardOption
{
    [Tooltip("要掉落的卡牌")]
    public CardDef Card;

    [Tooltip("权重：越大越常见（普通牌给 10，强力牌给 2）")]
    public int Weight = 10;
}
