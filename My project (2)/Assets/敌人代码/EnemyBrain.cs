using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum IntentType
{
    Attack, 
    Defend
}

public struct Intent
{
    public IntentType Type;
    public int Value;

    public Intent(IntentType type, int value)
    {
        Type = type;
        Value = value;
    }
}

public class EnemyBrain : MonoBehaviour
{

    [System.Serializable]
    public class IntentOption
    {
        public IntentType Type = IntentType.Attack;
        [Tooltip("数值：攻击=伤害，防御=格挡量")]
        public int Value = 6;
        [Tooltip("权重：越大越容易出现")]
        public int Weight = 1;
    }

    [Header("行为候选表")]
    public List<IntentOption> Options = new List<IntentOption>();

    [Header("引用")]
    [Tooltip("头顶的意图提示")]
    public IntentDisplay Display;
    public Intent CurrentIntent { get; private set; }

    public bool IsEmpty { get { return !_decided; } }

    private bool _decided;

    private void Awake()
    {
        if (Display == null) Display = GetComponentInChildren<IntentDisplay>(true);
    }

    public void DecideNextIntent()
    {
        CurrentIntent = PickByWeight();
        _decided = true;
        NotifyIntentChanged();
    }

    private Intent PickByWeight()
    {
        if (Options == null || Options.Count == 0)
        {
            Debug.LogWarning("[EnemyBrain] 候选表是空的，默认使用「攻击 6」。", this);
            return new Intent(IntentType.Attack, 6);
        }

        int totalWeight = 0;
        for (int i = 0; i < Options.Count; i++)
        {
            totalWeight += Mathf.Max(1, Options[i].Weight);
        }

        int roll = Random.Range(0, totalWeight);
        for (int i = 0; i < Options.Count; i++)
        {
            roll -= Mathf.Max(1, Options[i].Weight);
            if (roll < 0)
            {
                return new Intent(Options[i].Type, Options[i].Value);
            }
        }

        IntentOption last = Options[Options.Count - 1];
        return new Intent(last.Type, last.Value);
    }
    public void NotifyIntentChanged()
    {
        if (Display != null) Display.SetIntent(CurrentIntent.Type == IntentType.Attack, CurrentIntent.Value);
    }

    #region 执行意图
    public IEnumerator ExecuteIntent(ActorBinder self, ActorBinder target, BattleManager battle)
    {
        Debug.Log("[敌人] 执行意图：" + CurrentIntent.Type + " " + CurrentIntent.Value);

        switch (CurrentIntent.Type)
        {
            case IntentType.Attack:
                if (target != null && !target.Data.IsDead)
                {
                    yield return battle.StartCoroutine(
                        battle.AttackRoutine(self, target, CurrentIntent.Value, "敌人"));
                }
                break;

            case IntentType.Defend:
                if (self != null)
                {
                    self.GainBlock(CurrentIntent.Value);
                    yield return Tween.WaitRealtime(0.25f);
                }
                break;
        }
    }

    #endregion
    
    [ContextMenu("测试：受到 30 点伤害（含子物体）")]
    private void TestBigHit()
    {
        ActorBinder binder = GetComponentInParent<ActorBinder>();
        if (binder != null)
        {
            int hp = binder.TakeHit(30);
            Debug.Log("[调试] 敌人掉了 " + hp + " 血，剩余 " + binder.Data.Health);
        }
    }

    [ContextMenu("测试：立刻显示 攻击 9")]
    private void TestShowAttack()
    {
        CurrentIntent = new Intent(IntentType.Attack, 9);
        _decided = true;
        NotifyIntentChanged();
    }
}