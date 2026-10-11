using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 关卡配置表：所有地图和关卡的集合，并提供"能不能打"的判定。
///
/// 它是唯一知道"解锁关系"的地方 —— 界面只读它，不自己算逻辑。
/// </summary>
public class LevelGraph : MonoBehaviour
{
    [Header("地图")]
    public List<MapDef> Maps = new List<MapDef>();

    [Header("关卡（顺序无所谓，靠 LevelId 关联）")]
    public List<LevelDef> Levels = new List<LevelDef>();

    private readonly Dictionary<string, LevelDef> _byId = new Dictionary<string, LevelDef>();

    private void Awake()
    {
        Rebuild();
    }

    public void Rebuild()
    {
        _byId.Clear();
        for (int i = 0; i < Levels.Count; i++)
        {
            LevelDef level = Levels[i];
            if (level == null || string.IsNullOrEmpty(level.LevelId)) continue;

            if (_byId.ContainsKey(level.LevelId))
            {
                Debug.LogError("[LevelGraph] 关卡 id 重复：" + level.LevelId, this);
                continue;
            }
            _byId[level.LevelId] = level;
        }
        Debug.Log("[LevelGraph] 载入 " + _byId.Count + " 个关卡。");
    }

    public LevelDef GetLevel(string id)
    {
        LevelDef level;
        return _byId.TryGetValue(id, out level) ? level : null;
    }

    /// <summary>取某张地图里的关卡（按 Inspector 顺序）。</summary>
    public List<LevelDef> GetLevelsOfMap(string mapId)
    {
        List<LevelDef> result = new List<LevelDef>();
        for (int i = 0; i < Levels.Count; i++)
        {
            if (Levels[i] != null && Levels[i].MapId == mapId) result.Add(Levels[i]);
        }
        return result;
    }

    /// <summary>
    /// 找出"起始关卡"：没有被任何关卡指向的节点。
    /// 新开一局时把它们解锁，玩家就有第一个能打的关卡。
    /// </summary>
    public List<string> FindStartingLevels()
    {
        HashSet<string> hasIncoming = new HashSet<string>();
        for (int i = 0; i < Levels.Count; i++)
        {
            LevelDef level = Levels[i];
            if (level == null || level.UnlocksLevels == null) continue;
            for (int n = 0; n < level.UnlocksLevels.Count; n++) hasIncoming.Add(level.UnlocksLevels[n]);
        }

        List<string> starts = new List<string>();
        for (int i = 0; i < Levels.Count; i++)
        {
            LevelDef level = Levels[i];
            if (level == null || string.IsNullOrEmpty(level.LevelId)) continue;
            if (!hasIncoming.Contains(level.LevelId)) starts.Add(level.LevelId);
        }
        return starts;
    }

    /// <summary>这一关现在能不能打（解锁了 + 还没通关）。</summary>
    public bool IsPlayable(LevelDef level, RunState run)
    {
        if (level == null || run == null) return false;
        if (run.IsLevelCleared(level.LevelId)) return false;     // 打过就不能重打
        return run.IsLevelUnlocked(level.LevelId);
    }
}