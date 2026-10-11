using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class LevelDef
{
    [Header("标识")]
    public string LevelId = "l1";
    public string LevelName = "第一关";
    public string MapId = "map_1";

    [Header("敌人 ★")]
    [Tooltip("这一关的敌人（按顺序生成）")]
    public List<EnemyDef> Enemies = new List<EnemyDef>();

    [Header("奖励与解锁")]
    public int GoldReward = 30;
    public List<string> UnlocksLevels = new List<string>();
    public bool IsFinalLevel;
    public Sprite Icon;

    [Header("背景")]
    [Tooltip("这一关的背景图（留空 = 用上一关的 / 默认的）")]
    public Sprite Background;
}

[System.Serializable]
public class MapDef
{
    public string MapId = "map_1";
    public string MapName = "第一章 · 森林";
}