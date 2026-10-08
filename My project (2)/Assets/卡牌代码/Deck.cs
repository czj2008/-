using System.Collections.Generic;
using UnityEngine;
public class Deck
{
    private readonly List<CardDef> _drawPile = new List<CardDef>();     // 末尾 = 牌堆顶
    private readonly List<CardDef> _discardPile = new List<CardDef>();
    private readonly List<CardDef> _played = new List<CardDef>();

    public int DrawPileCount { get { return _drawPile.Count; } }
    public int DiscardPileCount { get { return _discardPile.Count + _played.Count; } }

    /// <summary>用一副牌初始化并洗牌。</summary>
    public void Initialize(IEnumerable<CardDef> cards)
    {
        _drawPile.Clear();
        _discardPile.Clear();
        _played.Clear();

        _drawPile.AddRange(cards);
        Shuffle(_drawPile);
    }

    //抽一张牌。返回 null 表示没牌可抽了
    public CardDef Draw()
    {
        if (_drawPile.Count == 0)
        {
            if (_discardPile.Count == 0) return null;      // 真的没牌了

            _drawPile.AddRange(_discardPile);              // 弃牌堆洗回抽牌堆
            _discardPile.Clear();
            Shuffle(_drawPile);
            Debug.Log("[Deck] 抽牌堆已空，弃牌堆洗回抽牌堆，共 " + _drawPile.Count + " 张。");
        }

        int last = _drawPile.Count - 1;
        CardDef card = _drawPile[last];
        _drawPile.RemoveAt(last);
        return card;
    }

    //标记"这张牌已经打出去了"，回合结束时进弃牌堆
    public void MarkPlayed(CardDef card)
    {
        if (card != null) _played.Add(card);
    }

    //直接丢进弃牌堆（回合结束时弃手牌用）
    public void Discard(CardDef card)
    {
        if (card != null) _discardPile.Add(card);
    }

    //把"打出去的牌"统一送进弃牌堆
    public void FlushPlayed()
    {
        if (_played.Count == 0) return;
        _discardPile.AddRange(_played);
        _played.Clear();
    }

    //洗牌：从后往前，每张和前面随机一张交换
    private static void Shuffle(List<CardDef> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            CardDef temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }
}