using System.Collections.Generic;
using UnityEngine;
public class CardLibrary : MonoBehaviour
{
    [Header("卡牌定义表")]
    public List<CardDef> AllCards = new List<CardDef>();

    [Header("初始牌组")]
    [Tooltip("按顺序列出：哪张牌、几张")]
    public List<StartingCard> StartingDeck = new List<StartingCard>();

    [System.Serializable]
    public class StartingCard
    {
        [Tooltip("要往牌组里放的牌（从 AllCards 里选）")]
        public CardDef Card;

        [Tooltip("放几张")]
        public int Count = 1;
    }

    public List<CardDef> BuildStartingDeck()
    {
        List<CardDef> deck = new List<CardDef>();

        for (int i = 0; i < StartingDeck.Count; i++)
        {
            StartingCard entry = StartingDeck[i];
            if (entry == null || entry.Card == null) continue;

            for (int n = 0; n < Mathf.Max(0, entry.Count); n++)
            {
                //卡牌升级
                deck.Add(Clone(entry.Card));
            }
        }

        return deck;
    }

    private static CardDef Clone(CardDef source)
    {
        return new CardDef
        {
            Id = source.Id,
            Title = source.Title,
            Cost = source.Cost,
            Kind = source.Kind,
            Value = source.Value,
            CardFace = source.CardFace
        };
    }
}