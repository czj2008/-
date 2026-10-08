using System.Collections.Generic;
using UnityEngine;

public enum CardKind
{
    Attack,   // 攻击
    Skill     // 技能（防御也属于技能）
}

[System.Serializable]
public class CardDef
{
    public string Id = "strike";

    public string Title = "打击";

    public int Cost = 1;

    public CardKind Kind = CardKind.Attack;

    public int Value = 6;

    [Tooltip("卡面")]
    public Sprite CardFace;
}