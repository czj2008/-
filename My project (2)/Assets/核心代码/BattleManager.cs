using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public enum BattleState
{
    None,      
    PlayerTurn, 
    EnemyTurn,  
    GameOver    
}
public class BattleManager : MonoBehaviour
{
    #region 引用
    [Header("角色（拖 Hierarchy 里的 Player / Enemy）")]
    public ActorBinder Player;
    public ActorBinder Enemy;

    [Header("敌人 AI")]
    public EnemyBrain EnemyAI;

    [Header("界面引用")]
    [Tooltip("屏幕正上方的回合提示")]
    public Text TurnBanner;

    [Tooltip("结束回合按钮")]
    public Button EndTurnButton;

    [Tooltip("结算文字（默认隐藏，战斗结束才显示）")]
    public Text GameOverText;

    [Header("卡牌系统")]
    public CardManager Cards;

    [Header("能量")]
    [Tooltip("每个玩家回合的能量上限")]
    public int MaxEnergy = 3;

    [Tooltip("能量数字（显示 3 / 3）")]
    public Text EnergyText;

    //本回合剩余能量
    public int Energy { get; private set; }

    [Header("开场演出")]
    [Tooltip("战斗开场演出（留空则跳过，直接用横幅提示）")]
    public BattleIntro Intro;

    [Header("事件")]
    public System.Action<bool> BattleEnded;

    [Tooltip("所有敌人")]
    public List<ActorBinder> Enemies = new List<ActorBinder>();

    #endregion
    public float BattleStartDelay = 0.6f;

    public BattleState State { get; private set; }

    public bool IsPlayerTurn { get { return State == BattleState.PlayerTurn; } }
    //开局
    private void Start()
    {
        // ★ 只检查 Player：敌人是运行时生成的，开局时 Enemies 一定是空的
        if (Player == null)
        {
            Debug.LogError("[Battle] Player 没拖，战斗无法开始。", this);
            enabled = false;
            return;
        }

        Subscribe();                    // ★★★ 就是这一行，你漏了

        Energy = MaxEnergy;

        if (EnergyText != null) EnergyText.text = Energy + " / " + MaxEnergy;
        if (GameOverText != null) GameOverText.gameObject.SetActive(false);

        if (EndTurnButton != null)
        {
            EndTurnButton.onClick.RemoveListener(EndPlayerTurn);
            EndTurnButton.onClick.AddListener(EndPlayerTurn);
        }

        // 【注意】这里不自动开打，由 GameFlow.StartLevel → BeginBattle() 触发
    }
    private void Update()
    {
        if (EnergyText != null)
        {
            string expected = Energy + " / " + MaxEnergy;
            if (EnergyText.text != expected) EnergyText.text = expected;
        }
    }
    private void Subscribe()
    {
        if (Player == null) return;

        Player.HealthChanged -= OnAnyHealthChanged;
        Player.HealthChanged += OnAnyHealthChanged;

        // 敌人不在这里订阅：它们是运行时生成的，
        // SetEnemies() 里会订阅每一只新敌人
    }

    private void OnDestroy()
    {
        if (Player != null) Player.HealthChanged -= OnAnyHealthChanged;
        if (Enemy != null) Enemy.HealthChanged -= OnAnyHealthChanged;
    }

    private void OnAnyHealthChanged(ActorBinder who)
    {
        if (State == BattleState.GameOver) return;

        // ---------- 1) 玩家死了吗 ----------
        if (Player == null) { Debug.LogWarning("[死亡判定] Player 是 null"); return; }
        if (Player.Data == null) { Debug.LogWarning("[死亡判定] Player.Data 是 null"); return; }

        if (Player.Data.IsDead)
        {
            Debug.Log("[死亡判定] 玩家死亡 → 触发失败结算");
            StartCoroutine(GameOverRoutine(false));
            return;
        }

        // ---------- 2) 敌人全死了吗 ----------
        if (Enemies == null || Enemies.Count == 0)
        {
            Debug.LogWarning("[死亡判定] Enemies 列表是空的（长度 " +
                             (Enemies != null ? Enemies.Count : -1) + "），不判定胜利");
            return;
        }

        for (int i = 0; i < Enemies.Count; i++)
        {
            ActorBinder enemy = Enemies[i];

            if (enemy == null)
            {
                Debug.LogWarning("[死亡判定] 第 " + i + " 只敌人引用是 null（被销毁了？），跳过");
                continue;
            }

            if (enemy.Data == null)
            {
                Debug.LogWarning("[死亡判定] " + enemy.name + " 的 Data 是 null，**判定为还活着**（避免误判胜利）");
                return;                      // ← 关键：Data 为空时不能算死
            }

            if (!enemy.Data.IsDead)
            {
                Debug.Log("[死亡判定] " + enemy.name + " 还活着（血量 " + enemy.Data.Health + "），不结算");
                return;
            }

            Debug.Log("[死亡判定] " + enemy.name + " 已死亡");
        }

        // ---------- 3) 全部死亡 → 胜利 ----------
        Debug.Log("[死亡判定] ★ 所有敌人都已死亡 → 触发胜利结算");
        StartCoroutine(GameOverRoutine(true));
    }

    private IEnumerator BattleLoop()
    {
        // ---------- 开场演出 ----------
        if (Enemies != null)
        {
            for (int i = 0; i < Enemies.Count; i++)
            {
                ActorBinder enemy = Enemies[i];
                if (enemy == null) continue;

                EnemyBrain brain = enemy.GetComponent<EnemyBrain>();
                if (brain == null)
                {
                    Debug.LogWarning("[Battle] " + enemy.name + " 上没有 EnemyBrain，无法抽意图。");
                    continue;
                }

                brain.DecideNextIntent();
                Debug.Log("[Battle] 开局已为 " + enemy.name + " 抽定意图");
            }
        }

        if (Intro != null)
        {
            yield return StartCoroutine(Intro.Play());
        }
        else
        {
            SetBanner("战斗开始", new Color(0.95f, 0.90f, 0.55f));
            yield return Tween.WaitRealtime(0.6f);
        }

        // ---------- 正式开打 ----------

        while (State != BattleState.GameOver)
        {
            yield return StartCoroutine(PlayerTurnRoutine());
            if (State == BattleState.GameOver) yield break;
            yield return StartCoroutine(EnemyTurnRoutine());
        }
    }
    //玩家回合
    private IEnumerator PlayerTurnRoutine()
    {

        State = BattleState.PlayerTurn;

        Energy = MaxEnergy;

        Player.TurnStart();          // 清掉玩家自己的格挡
        SetBanner("你的回合", new Color(0.55f, 0.90f, 0.60f));
        SetButtonVisible(true);

        if (Cards != null)
        {
            yield return StartCoroutine(Cards.DrawHand());
            Cards.RefreshPlayableState();
        }

        // 等玩家点"结束回合"
        while (State == BattleState.PlayerTurn) yield return null;

        SetButtonVisible(false);
        if (Cards != null) yield return StartCoroutine(Cards.DiscardHand());
    }
    public void EndPlayerTurn()
    {
        if (State != BattleState.PlayerTurn) return;
        State = BattleState.EnemyTurn;
    }

    //敌人回合
    private IEnumerator EnemyTurnRoutine()
    {
        State = BattleState.EnemyTurn;
        SetBanner("敌方回合", new Color(0.90f, 0.45f, 0.40f));
        yield return Tween.WaitRealtime(0.4f);

        if (Enemies == null || Enemies.Count == 0)
        {
            Debug.LogWarning("[Battle] 敌人列表是空的，跳过敌人回合。");
            yield return Tween.WaitRealtime(0.2f);
            yield break;
        }

        for (int i = 0; i < Enemies.Count; i++)
        {
            ActorBinder enemy = Enemies[i];

            // 逐个字段判空
            if (enemy == null) { Debug.LogWarning("[Battle] 第 " + i + " 只敌人引用是 null，跳过。"); continue; }
            if (enemy.Data == null) { Debug.LogWarning("[Battle] " + enemy.name + " 的 Data 是 null，跳过。"); continue; }
            if (enemy.Data.IsDead) continue;

            EnemyBrain brain = enemy.GetComponent<EnemyBrain>();
            if (brain == null) { Debug.LogWarning("[Battle] " + enemy.name + " 上没有 EnemyBrain，跳过。"); continue; }

            enemy.TurnStart();       // 清自己的格挡

            // 跑意图（带异常保护，出错也不中断整个回合）
            yield return StartCoroutine(ExecuteIntentGuarded(brain, enemy, i));

            if (Player == null || Player.Data == null || Player.Data.IsDead)
            {
                yield return StartCoroutine(GameOverRoutine(false));
                yield break;
            }

            brain.DecideNextIntent();
            yield return Tween.WaitRealtime(0.3f);
        }

        yield return Tween.WaitRealtime(0.2f);
    }

    /// <summary>
    /// 跑一只敌人的意图，捕获异常。
    /// 【为什么必须包一层】协程里抛异常会静默终止，表现就是"敌人回合卡住"。
    /// 包一层能：① 保留其它敌人继续行动 ② 把异常打印出来让你看到。
    /// </summary>
    private IEnumerator ExecuteIntentGuarded(EnemyBrain brain, ActorBinder enemy, int index)
    {
        IEnumerator routine = brain.ExecuteIntent(enemy, Player, this);
        //                                  ↑ 注意：第一个参数传"这只敌人自己"，不是 Enemy 字段

        while (true)
        {
            object current;
            try
            {
                if (!routine.MoveNext()) break;
                current = routine.Current;
            }
            catch (System.Exception e)
            {
                Debug.LogError("[Battle] 第 " + index + " 只敌人（" + enemy.name + "）执行意图时异常：" + e);
                yield break;
            }

            yield return current;
        }
    }

    //攻击
    public IEnumerator AttackRoutine(ActorBinder attacker, ActorBinder target, int damage, string label)
    {
        if (attacker == null || target == null) yield break;

        ActorView view = attacker.View;
        if (view == null) { DealDamage(target, damage, label); yield break; }

        yield return StartCoroutine(view.Lunge());

        DealDamage(target, damage, label);
        yield return Tween.WaitRealtime(0.16f);

    }

    //结算
    private int DealDamage(ActorBinder target, int amount, string label)
    {
        if (target == null) return 0;

        int blockBefore = target.Data.Block;
        int hpLoss = target.TakeHit(amount);

        string message = "[" + label + "] 造成 " + amount + " 点伤害";
        if (hpLoss < amount)
        {
            message += "（格挡吸收 " + (amount - hpLoss) + "）";
        }
        message += "，目标掉血 " + hpLoss + "，剩余 " + target.Data.Health + " / " + target.Data.MaxHealth;
        Debug.Log(message);

        return hpLoss;
    }

    //战斗结束
    private IEnumerator GameOverRoutine(bool playerWon)
    {
        if (State == BattleState.GameOver) yield break;
        State = BattleState.GameOver;
        SetButtonVisible(false);

        SetBanner(playerWon ? "胜  利！" : "失  败……",
                  playerWon ? new Color(0.55f, 0.90f, 0.60f) : new Color(0.90f, 0.40f, 0.40f));

        if (playerWon) AudioManager.Instance.PlayVictory();
        else AudioManager.Instance.PlayDefeat();

        Debug.Log("[Battle] 战斗结束，玩家" + (playerWon ? "获胜" : "失败") + "。");
        yield return Tween.WaitRealtime(0.8f);

        if (BattleEnded != null) BattleEnded(playerWon);
    }


    private void SetBanner(string text, Color color)
    {
        if (TurnBanner == null) return;
        TurnBanner.text = text;
        TurnBanner.color = color;
    }

    private void SetButtonVisible(bool visible)
    {
        if (EndTurnButton != null) EndTurnButton.gameObject.SetActive(visible);
    }

    public bool CanSpendEnergy(int cost)
    {
        return Energy >= cost;
    }

    public void SpendEnergy(int cost)
    {
        Energy = Mathf.Max(0, Energy - cost);
        if (Cards != null) Cards.RefreshPlayableState();
    }

    /// <summary>
    /// 取第一只还活着的敌人（单敌人时就是那唯一一只）。
    /// 【为什么要"活着"】以后加多敌人时，死掉的敌人不该再被打。
    /// </summary>
    private ActorBinder FirstAliveEnemy()
    {
        if (Enemies == null) return null;

        for (int i = 0; i < Enemies.Count; i++)
        {
            ActorBinder enemy = Enemies[i];
            if (enemy == null || enemy.Data == null) continue;
            if (!enemy.Data.IsDead) return enemy;
        }
        return null;
    }
    /// <summary>
    /// 结算一张牌：按顺序执行它的每一条效果。
    ///
    /// 【为什么改成"遍历列表"】
    /// 以前一张牌只有"伤害"或"格挡"一个效果，用 Kind + switch 就够。
    /// 现在要支持"攻击 + 治疗""攻击 + 叠甲"这种复合效果，
    /// 就必须能一条一条按顺序执行。
    /// </summary>
    public IEnumerator PlayCard(CardDef card)
    {
        if (card == null) yield break;

        List<CardEffect> effects = card.GetEffects();

        for (int i = 0; i < effects.Count; i++)
        {
            CardEffect effect = effects[i];
            if (effect == null || effect.Value <= 0) continue;

            switch (effect.Type)
            {
                // ---------- 造成伤害（需要目标） ----------
                case CardEffectType.Damage:
                    {
                        ActorBinder target = FirstAliveEnemy();
                        if (target == null)
                        {
                            Debug.LogWarning("[Battle] 没有可攻击的目标，跳过伤害效果。");
                            break;
                        }

                        Debug.Log("[Battle] 效果 " + (i + 1) + "/" + effects.Count +
                                  "：造成 " + effect.Value + " 点伤害");

                        yield return StartCoroutine(AttackRoutine(Player, target, effect.Value, "玩家"));

                        // 打完可能敌人就死了 → 后面如果有针对敌人的效果就跳过
                        if (target.Data != null && target.Data.IsDead) { /* 继续执行自身效果 */ }
                        break;
                    }

                // ---------- 获得格挡 ----------
                case CardEffectType.Block:
                    Player.GainBlock(effect.Value);
                    Debug.Log("[Battle] 效果 " + (i + 1) + "/" + effects.Count +
                              "：获得 " + effect.Value + " 点格挡，当前 " + Player.Data.Block);
                    yield return Tween.WaitRealtime(0.2f);
                    break;

                // ---------- 治疗 ----------
                case CardEffectType.Heal:
                    {
                        int healed = Player.Heal(effect.Value);
                        Debug.Log("[Battle] 效果 " + (i + 1) + "/" + effects.Count +
                                  "：回复 " + healed + " 点生命，当前 " + Player.Data.Health + " / " + Player.Data.MaxHealth);
                        yield return Tween.WaitRealtime(0.2f);
                        break;
                    }

                // ---------- 抽牌 ----------
                case CardEffectType.Draw:
                    if (Cards != null)
                    {
                        Debug.Log("[Battle] 效果 " + (i + 1) + "/" + effects.Count +
                                  "：抽 " + effect.Value + " 张牌");
                        yield return StartCoroutine(Cards.DrawCards(effect.Value));
                    }
                    break;

                // ---------- 获得能量 ----------
                case CardEffectType.GainEnergy:
                    Energy += effect.Value;
                    if (EnergyText != null) EnergyText.text = Energy + " / " + MaxEnergy;
                    Debug.Log("[Battle] 效果 " + (i + 1) + "/" + effects.Count +
                              "：获得 " + effect.Value + " 点能量，当前 " + Energy);
                    yield return Tween.WaitRealtime(0.15f);
                    break;
            }

            // 中途玩家死了就停下（理论上不会，但保险）
            if (Player.Data != null && Player.Data.IsDead) yield break;
        }
    }
    // =====================================================================
    //  给 GameFlow 用的入口
    // =====================================================================

    /// <summary>战斗前准备玩家：设置血量上限、当前血量、牌组。</summary>
    /// <summary>
    /// 替换敌人列表（由 GameFlow 在生成新敌人后调用）。
    ///
    /// 【为什么要替换而不是一开始就配好】
    /// 敌人是运行时按关卡生成的，每次都不一样。
    /// 替换时要同时做两件事：退订旧敌人的事件、订阅新敌人的事件。
    /// 漏了退订会导致"一个敌人掉血，结算被触发两次"。
    /// </summary>
    public void SetEnemies(List<ActorBinder> enemies)
    {
        // 1) 退订旧敌人
        if (Enemies != null)
        {
            for (int i = 0; i < Enemies.Count; i++)
            {
                if (Enemies[i] != null) Enemies[i].HealthChanged -= OnAnyHealthChanged;
            }
        }

        // 2) 换成新的
        Enemies = (enemies != null) ? new List<ActorBinder>(enemies) : new List<ActorBinder>();

        // 3) 订阅新敌人
        for (int i = 0; i < Enemies.Count; i++)
        {
            if (Enemies[i] == null) continue;
            Enemies[i].HealthChanged -= OnAnyHealthChanged;      // 先退再订，防止重复
            Enemies[i].HealthChanged += OnAnyHealthChanged;
        }

        Debug.Log("[Battle] 敌人列表已更新，共 " + Enemies.Count + " 只");
    }
    public void PreparePlayer(int maxHealth, int currentHealth, System.Collections.Generic.List<CardDef> deck)
    {
        if (Player != null)
        {
            Player.Configure(maxHealth, "玩家");
            Player.Data.SetHealth(currentHealth);
        }

        if (Cards != null && deck != null && deck.Count > 0) Cards.SetDeck(deck);
    }

    /// <summary>开始战斗（GameFlow 调用；可以反复调用，比如重开）。</summary>
    public void BeginBattle()
    {
        StopAllCoroutines();
        State = BattleState.None;
        Energy = MaxEnergy;
        StartCoroutine(BattleLoop());
    }
}