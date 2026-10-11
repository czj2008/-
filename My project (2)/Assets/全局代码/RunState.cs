using System.Collections.Generic;
using UnityEngine;

public class RunState : MonoBehaviour
{
    [Header("引用")]
    [Tooltip("卡牌图鉴（新开一局时生成初始牌组）")]
    public CardLibrary Library;

    [Tooltip("关卡配置表")]
    public LevelGraph Graph;

    [Header("牌组与血量")]
    public List<CardDef> Deck = new List<CardDef>();
    public int PlayerMaxHealth = 75;
    public int PlayerHealth = 75;

    [Header("货币")]
    public int Gold;

    // 内部：两个集合
    private readonly HashSet<string> _cleared = new HashSet<string>();
    private readonly HashSet<string> _unlocked = new HashSet<string>();
    private readonly List<string> _buffer = new List<string>();     // 存读档用的临时列表

    /// <summary>已通关关卡数（商店涨价用）。</summary>
    public int ClearedCount { get { return _cleared.Count; } }

    public bool IsLevelCleared(string id)
    {
        return !string.IsNullOrEmpty(id) && _cleared.Contains(id);
    }

    public bool IsLevelUnlocked(string id)
    {
        return !string.IsNullOrEmpty(id) && _unlocked.Contains(id);
    }

    // =====================================================================
    //  开新局 / 通关
    // =====================================================================

    public void StartNewRun(int maxHealth, int startingGold)
    {
        PlayerMaxHealth = maxHealth;
        PlayerHealth = maxHealth;
        Gold = startingGold;

        Deck = (Library != null) ? Library.BuildStartingDeck() : new List<CardDef>();

        _cleared.Clear();
        _unlocked.Clear();

        // 解锁所有"起始关卡"
        if (Graph != null)
        {
            List<string> starts = Graph.FindStartingLevels();
            for (int i = 0; i < starts.Count; i++) _unlocked.Add(starts[i]);
            Debug.Log("[RunState] 起始解锁 " + starts.Count + " 个关卡");
        }

        Debug.Log("[RunState] 新的一局：血量 " + PlayerHealth + "/" + PlayerMaxHealth +
                  "，金币 " + Gold + "，牌组 " + Deck.Count + " 张");
    }

    /// <summary>通关一关：标记完成 + 解锁它指向的关卡。</summary>
    public void MarkLevelCleared(string levelId, int goldReward)
    {
        if (string.IsNullOrEmpty(levelId)) return;

        _cleared.Add(levelId);
        _unlocked.Remove(levelId);          // 打过了就不再是"可挑战"
        Gold += goldReward;

        LevelDef level = Graph != null ? Graph.GetLevel(levelId) : null;
        if (level != null && level.UnlocksLevels != null)
        {
            for (int i = 0; i < level.UnlocksLevels.Count; i++)
            {
                string next = level.UnlocksLevels[i];
                if (string.IsNullOrEmpty(next) || _cleared.Contains(next)) continue;
                _unlocked.Add(next);
                Debug.Log("[RunState] 解锁关卡：" + next);
            }
        }
    }

    // =====================================================================
    //  存档
    // =====================================================================

    private const string KEY = "run";

    public void Save()
    {
        _buffer.Clear();
        _buffer.AddRange(_cleared);
        PlayerPrefs.SetString(KEY + ".cleared", string.Join("|", _buffer));

        _buffer.Clear();
        _buffer.AddRange(_unlocked);
        PlayerPrefs.SetString(KEY + ".unlocked", string.Join("|", _buffer));

        _buffer.Clear();
        for (int i = 0; i < Deck.Count; i++)
        {
            if (Deck[i] != null && !string.IsNullOrEmpty(Deck[i].Id)) _buffer.Add(Deck[i].Id);
        }
        PlayerPrefs.SetString(KEY + ".deck", string.Join("|", _buffer));
        PlayerPrefs.SetInt(KEY + ".gold", Gold);
        PlayerPrefs.SetInt(KEY + ".hp", PlayerHealth);
        PlayerPrefs.SetInt(KEY + ".maxHp", PlayerMaxHealth);
        PlayerPrefs.Save();

        Debug.Log("[RunState] 已保存：通关 " + _cleared.Count + " 关，金币 " + Gold + "，牌组 " + Deck.Count + " 张");
    }

    public void Load()
    {
        _cleared.Clear();
        _unlocked.Clear();

        LoadSet(KEY + ".cleared", _cleared);
        LoadSet(KEY + ".unlocked", _unlocked);

        Gold = PlayerPrefs.GetInt(KEY + ".gold", 0);
        PlayerHealth = PlayerPrefs.GetInt(KEY + ".hp", 75);
        PlayerMaxHealth = PlayerPrefs.GetInt(KEY + ".maxHp", 75);

        Debug.Log("[RunState] 已读取：通关 " + _cleared.Count + " 关，金币 " + Gold);

        Deck.Clear();
        string deckRaw = PlayerPrefs.GetString(KEY + ".deck", "");
        if (!string.IsNullOrEmpty(deckRaw))
        {
            string[] ids = deckRaw.Split('|');
            for (int i = 0; i < ids.Length; i++)
            {
                if (string.IsNullOrEmpty(ids[i])) continue;

                CardDef card = Library != null ? Library.FindCardById(ids[i]) : null;
                if (card != null) Deck.Add(card);
                else Debug.LogWarning("[RunState] 读档时找不到卡牌 id：" + ids[i]);
            }
        }
        if (Deck.Count == 0 && Library != null)
        {
            Deck = Library.BuildStartingDeck();
            Debug.Log("[RunState] 存档里没有牌组，使用初始牌组 " + Deck.Count + " 张");
        }

        Debug.Log("[RunState] 已读取：通关 " + _cleared.Count + " 关，金币 " + Gold + "，牌组 " + Deck.Count + " 张");
    }

    private void LoadSet(string key, HashSet<string> target)
    {
        string raw = PlayerPrefs.GetString(key, "");
        if (string.IsNullOrEmpty(raw)) return;

        string[] parts = raw.Split('|');
        for (int i = 0; i < parts.Length; i++)
        {
            if (!string.IsNullOrEmpty(parts[i])) target.Add(parts[i]);
        }
    }

    public bool HasSave()
    {
        return PlayerPrefs.HasKey(KEY + ".cleared") && PlayerPrefs.HasKey(KEY + ".gold");
    }

    public void ClearSave()
    {
        PlayerPrefs.DeleteKey(KEY + ".cleared");
        PlayerPrefs.DeleteKey(KEY + ".unlocked");
        PlayerPrefs.DeleteKey(KEY + ".gold");
        PlayerPrefs.DeleteKey(KEY + ".hp");
        PlayerPrefs.DeleteKey(KEY + ".maxHp");
        PlayerPrefs.DeleteKey(KEY + ".deck");
        PlayerPrefs.Save();
    }
}