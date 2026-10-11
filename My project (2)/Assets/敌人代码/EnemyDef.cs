using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyDef", menuName = "卡牌战斗/敌人模板")]
public class EnemyDef : ScriptableObject
{
    [Header("基本信息")]
    [Tooltip("显示名（日志、结算界面用）")]
    public string DisplayName = "小怪";

    [Tooltip("生成用的预制体（通常就是你的 Enemy.prefab）")]
    public ActorBinder Prefab;

    [Header("数值")]
    public int MaxHealth = 45;

    [Header("外观（留空 = 用预制体原本的样子）")]
    [Tooltip("立绘（可以给同一种敌人换不同图做变体）")]
    public Sprite BodySprite;

    [Tooltip("染色。白色 = 不改色。想做\"红色史莱姆/蓝色史莱姆\"就改这里")]
    public Color BodyColor = Color.white;

    [Header("行为")]
    [Tooltip("候选行为表。空 = 用预制体上 EnemyBrain 原本配的")]
    public List<EnemyBrain.IntentOption> Options = new List<EnemyBrain.IntentOption>();

    [Header("站位（相对战斗中心）")]
    public Vector2 Position = new Vector2(3.2f, -3.6f);

    [Tooltip("图层顺序，越大越靠前（决定敌人之间的前后遮挡）")]
    public int OrderInLayer = 0;
}