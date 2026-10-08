using UnityEngine;
using UnityEngine.UI;
public class HealthBar : MonoBehaviour
{
    #region 引用
    [Header("引用（从 Hierarchy 拖进来）")]
    public Image Fill;
    public Text HpText;

    [Tooltip("格挡数值文字（可选）")]
    public Text BlockText;

    [Header("颜色")]
    public Color NormalColor = new Color(0.83f, 0.24f, 0.26f, 1f);
    public Color LowColor = new Color(0.95f, 0.72f, 0.20f, 1f); 
    [Range(0f, 1f)] public float LowThreshold = 0.3f;

    #endregion

    private Actor _actor;

    public void Bind(Actor actor)
    {
        _actor = actor;
        Refresh();
    }
    public void Refresh()
    {
        if (_actor == null) return;

        float ratio = _actor.MaxHealth <= 0
            ? 0f
            : Mathf.Clamp01((float)_actor.Health / _actor.MaxHealth);

        if (Fill != null)
        {
            Vector3 scale = Fill.transform.localScale;
            scale.x = Mathf.Max(0.0001f, ratio);
            Fill.transform.localScale = scale;

            Fill.color = ratio <= LowThreshold ? LowColor : NormalColor;
        }

        if (HpText != null) HpText.text = _actor.Health + " / " + _actor.MaxHealth;

        bool hasBlock = _actor.Block > 0;
        if (BlockText != null)
        {
            BlockText.gameObject.SetActive(hasBlock);
            if (hasBlock) BlockText.text = "🛡" + _actor.Block;
        }
    }
}