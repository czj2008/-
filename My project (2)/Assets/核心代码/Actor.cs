using System;
using UnityEngine;
public class Actor
{
    //刷新血条
    public event Action<Actor> Changed;

    //死亡时触发
    public event Action<Actor> Died;

    public readonly string Name;

    public int MaxHealth { get; private set; }
    public int Health { get; private set; }
    public int Block { get; private set; }
    public bool IsDead { get { return Health <= 0; } }

    public Actor(int maxHealth, string name)
    {
        MaxHealth = Mathf.Max(1, maxHealth);
        Health = MaxHealth;
        Name = name;
    }
    public void ResetFull()
    {
        Health = MaxHealth;
        Block = 0;
        NotifyChanged();
    }
    public int TakeDamage(int amount)
    {
        if (amount <= 0 || IsDead) return 0;

        int remaining = amount;

        if (Block > 0)
        {
            int absorbed = Mathf.Min(Block, remaining);
            Block -= absorbed;
            remaining -= absorbed;
        }

        int hpLoss = 0;
        if (remaining > 0)
        {
            hpLoss = Mathf.Min(Health, remaining);
            Health -= hpLoss;
        }

        NotifyChanged();

        if (Health <= 0 && Died != null) Died(this);

        return hpLoss;
    }
    public int GainBlock(int amount)
    {
        if (amount <= 0 || IsDead) return 0;
        Block += amount;
        NotifyChanged();
        return amount;
    }
    public void ClearBlock()
    {
        if (Block == 0) return;
        Block = 0;
        NotifyChanged();
    }

    public int Heal(int amount)
    {
        if (amount <= 0 || IsDead) return 0;
        int before = Health;
        Health = Mathf.Min(MaxHealth, Health + amount);
        NotifyChanged();
        return Health - before;
    }
    public void NotifyChanged()
    {
        if (Changed != null) Changed(this);
    }
}
