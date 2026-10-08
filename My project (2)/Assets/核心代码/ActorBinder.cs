using System.Collections;
using UnityEngine;

public class ActorBinder : MonoBehaviour
{
    #region 引用
    [Header("数值")]
    [Tooltip("最大血量")]
    public int MaxHealth = 80;

    [Tooltip("角色名")]
    public string ActorName = "角色";

    [Header("受击表现")]
    [Tooltip("受击时闪的颜色")]
    public Color FlashColor = new Color(1f, 0.65f, 0.65f, 1f);

    [Header("引用")]
    [Tooltip("角色外观")]
    public ActorView View;

    [Tooltip("头顶血条")]
    public HealthBar HpBar;
    public Actor Data { get; private set; }

    private Coroutine _hitRoutine;

    #endregion

    #region 状态刷新
    private void Awake()
    {
        if (View == null) View = GetComponent<ActorView>();
        if (HpBar == null) HpBar = GetComponentInChildren<HealthBar>(true);

        Data = new Actor(MaxHealth, ActorName);

        Data.Changed += OnDataChanged;
        Data.Died += OnDied;

        if (View != null) View.Initialize();

        if (HpBar != null) HpBar.Bind(Data);
        else Debug.LogWarning("[" + ActorName + "] 没有找到 HealthBar，血条不会显示。", this);
    }
    #endregion

    private void OnDestroy()
    {
        if (Data != null)
        {
            Data.Changed -= OnDataChanged;
            Data.Died -= OnDied;
        }
    }

    public event System.Action<ActorBinder> HealthChanged;

    private void OnDataChanged(Actor actor)
    {
        if (HpBar != null) HpBar.Refresh();
        if (HealthChanged != null) HealthChanged(this);
    }

    private void OnDied(Actor actor)
    {
        if (View != null) StartCoroutine(View.Die());
    }

    #region 战斗流程代码
    public int TakeHit(int damage)
    {
        int hpLoss = Data.TakeDamage(damage);

        if (View != null)
        {
            if (_hitRoutine != null) StopCoroutine(_hitRoutine);
            _hitRoutine = StartCoroutine(View.HitReaction(FlashColor));
        }

        return hpLoss;
    }

    public void GainBlock(int amount)
    {
        Data.GainBlock(amount);
    }

    public void TurnStart()
    {
        Data.ClearBlock();
    }

    public int Heal(int amount)
    {
        return Data.Heal(amount);
    }
    #endregion

    [ContextMenu("测试：受到 6 点伤害")]
    private void TestHit6() { Debug.Log(ActorName + " 受到 6 点伤害，实际掉血 " + TakeHit(6)); }

    [ContextMenu("测试：受到 20 点伤害")]
    private void TestHit20() { Debug.Log(ActorName + " 受到 20 点伤害，实际掉血 " + TakeHit(20)); }

    [ContextMenu("测试：获得 5 点格挡")]
    private void TestBlock() { GainBlock(5); Debug.Log(ActorName + " 获得 5 点格挡"); }
}
