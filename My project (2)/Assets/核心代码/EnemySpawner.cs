using System.Collections.Generic;
using UnityEngine;
public class EnemySpawner : MonoBehaviour
{
    [Header("默认预制体")]
    [Tooltip("模板里没填 Prefab 时用这个")]
    public ActorBinder DefaultPrefab;

    [Tooltip("生成到哪个父物体下（留空 = 生成到自己下面）")]
    public Transform SpawnRoot;

    /// <summary>当前场上所有敌人。</summary>
    public List<ActorBinder> SpawnedEnemies { get; private set; } = new List<ActorBinder>();

    /// <summary>按配置生成一波敌人（会先清空场上旧敌人）。</summary>
    public void SpawnWave(List<EnemyDef> wave)
    {
        ClearAll();

        if (wave == null || wave.Count == 0)
        {
            Debug.LogWarning("[EnemySpawner] 这一波敌人配置是空的。", this);
            return;
        }

        Transform parent = SpawnRoot != null ? SpawnRoot : transform;

        for (int i = 0; i < wave.Count; i++)
        {
            EnemyDef def = wave[i];
            if (def == null) continue;

            ActorBinder prefab = def.Prefab != null ? def.Prefab : DefaultPrefab;
            if (prefab == null)
            {
                Debug.LogError("[EnemySpawner] 模板「" + def.DisplayName + "」没填 Prefab，也没有默认预制体。", this);
                continue;
            }

            // 1) 实例化
            ActorBinder enemy = Instantiate(prefab, parent);
            enemy.gameObject.name = "Enemy_" + def.DisplayName + "_" + i;
            enemy.transform.position = new Vector3(def.Position.x, def.Position.y, 0f);

            // 2) 套用模板
            ApplyDef(enemy, def);

            SpawnedEnemies.Add(enemy);
        }

        Debug.Log("[EnemySpawner] 生成了 " + SpawnedEnemies.Count + " 只敌人。");
    }

    /// <summary>把模板数据套到实例上。</summary>
    private void ApplyDef(ActorBinder enemy, EnemyDef def)
    {
        // ---- 1) 数值：重建 Actor（血量按新上限回满）----
        enemy.Configure(def.MaxHealth, def.DisplayName);

        // ---- 2) 外观 ----
        ActorView view = enemy.GetComponent<ActorView>();
        if (view != null && view.Body != null)
        {
            if (def.BodySprite != null) view.Body.sprite = def.BodySprite;
            view.Body.color = def.BodyColor;
            view.Body.sortingOrder = def.OrderInLayer;
        }

        // ---- 3) 行为表 ----
        EnemyBrain brain = enemy.GetComponent<EnemyBrain>();
        if (brain != null && def.Options != null && def.Options.Count > 0)
        {
            // 深拷贝一份，避免多个实例共享同一个 List 互相影响
            brain.Options = new List<EnemyBrain.IntentOption>();
            for (int i = 0; i < def.Options.Count; i++)
            {
                EnemyBrain.IntentOption src = def.Options[i];
                brain.Options.Add(new EnemyBrain.IntentOption
                {
                    Type = src.Type,
                    Value = src.Value,
                    Weight = src.Weight
                });
            }
        }
    }

    /// <summary>清空场上所有敌人。</summary>
    public void ClearAll()
    {
        for (int i = 0; i < SpawnedEnemies.Count; i++)
        {
            if (SpawnedEnemies[i] != null) Destroy(SpawnedEnemies[i].gameObject);
        }
        SpawnedEnemies.Clear();
    }
}
