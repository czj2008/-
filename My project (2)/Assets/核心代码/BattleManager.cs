using System.Collections;
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

    [Header("战斗开始演出")]
    [Tooltip("开局等待时间（秒），给玩家一点准备时间")]

    [Header("卡牌系统")]
    public CardManager Cards;

    [Header("能量")]
    [Tooltip("每个玩家回合的能量上限")]
    public int MaxEnergy = 3;

    //本回合剩余能量
    public int Energy { get; private set; }

    #endregion
    public float BattleStartDelay = 0.6f;

    public BattleState State { get; private set; }

    public bool IsPlayerTurn { get { return State == BattleState.PlayerTurn; } }
    //开局
    private void Start()
    {

        if (Player == null || Enemy == null)
        {
            Debug.LogError("[Battle] Player / Enemy 没有拖进 Inspector，战斗无法开始。", this);
            enabled = false;
            return;
        }

        if (GameOverText != null) GameOverText.gameObject.SetActive(false);

        if (EndTurnButton != null)
        {
            EndTurnButton.onClick.RemoveListener(EndPlayerTurn);
            EndTurnButton.onClick.AddListener(EndPlayerTurn);
        }
        Subscribe();
        Energy = MaxEnergy;
        StartCoroutine(BattleLoop());
    }

    private void Subscribe()
    {
        if (Player != null) Player.HealthChanged += OnAnyHealthChanged;
        if (Enemy != null) Enemy.HealthChanged += OnAnyHealthChanged;
    }

    private void OnDestroy()
    {
        if (Player != null) Player.HealthChanged -= OnAnyHealthChanged;
        if (Enemy != null) Enemy.HealthChanged -= OnAnyHealthChanged;
    }

    private void OnAnyHealthChanged(ActorBinder who)
    {
        if (State == BattleState.GameOver) return;

        if (Enemy != null && Enemy.Data.IsDead) StartCoroutine(GameOverRoutine(true));
        else if (Player != null && Player.Data.IsDead) StartCoroutine(GameOverRoutine(false));
    }

    private IEnumerator BattleLoop()
    {
        SetBanner("战斗开始", new Color(0.95f, 0.90f, 0.55f));
        SetButtonVisible(false);
        yield return Tween.WaitRealtime(BattleStartDelay);

        if (EnemyAI != null) EnemyAI.DecideNextIntent();

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
        yield return Tween.WaitRealtime(0.4f);      // 停一下，让玩家意识到"换人了"

        Enemy.TurnStart();                          // 敌人也在自己回合开始清格挡

        if (EnemyAI != null)
        {
            yield return EnemyAI.ExecuteIntent(Enemy, Player, this);
            EnemyAI.DecideNextIntent();
        }

        yield return Tween.WaitRealtime(0.35f);
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

        if (GameOverText != null)
        {
            GameOverText.gameObject.SetActive(true);
            GameOverText.text = playerWon ? "胜  利！" : "失  败……";
            GameOverText.color = playerWon ? new Color(0.55f, 0.90f, 0.60f) : new Color(0.90f, 0.40f, 0.40f);
        }

        Debug.Log("[Battle] 战斗结束，玩家" + (playerWon ? "获胜" : "失败") + "。");
        yield return null;
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

    public IEnumerator PlayCard(CardDef card)
    {
        if (card == null) yield break;

        switch (card.Kind)
        {
            case CardKind.Attack:
                // 复用第 4 步做好的攻击流程（冲过去 → 命中 → 退回）
                yield return StartCoroutine(AttackRoutine(Player, Enemy, card.Value, "玩家"));
                break;

            case CardKind.Skill:
                Player.GainBlock(card.Value);
                Debug.Log("[Battle] 玩家获得 " + card.Value + " 点格挡，当前格挡 " + Player.Data.Block);
                yield return Tween.WaitRealtime(0.2f);
                break;
        }
    }
}