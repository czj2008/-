using UnityEngine;
using UnityEngine.UI;
public class IntentDisplay : MonoBehaviour
{
    [Header("引用（从 Hierarchy 拖进来）")]
    [Tooltip("Canvas 下的 Icon，用颜色区分攻击/防御")]
    public Image Icon;

    [Tooltip("Canvas 下的 Label")]
    public Text Label;

    [Header("图标贴图")]
    public Sprite AttackIcon;
    public Sprite BlockIcon;

    [Header("图标颜色（攻击=红，防御=蓝）")]
    public Color AttackColor = new Color(0.85f, 0.21f, 0.24f, 1f);
    public Color BlockColor = new Color(0.30f, 0.62f, 0.90f, 1f);
    public void SetIntent(bool isAttack, int value)
    {
        if (Icon != null)
        {
            Icon.sprite = isAttack ? AttackIcon : BlockIcon;
            Icon.color = isAttack ? AttackColor : BlockColor;
            Icon.enabled = true;
        }

        if (Label != null)
        {
            Label.text = isAttack ? "攻击 " + value : "格挡 " + value;
            Label.color = Color.white;
        }

        Punch();
    }

    #region 隐藏意图（战斗结束或正在战斗）
    public void Hide()
    {
        if (Icon != null) Icon.enabled = false;
        if (Label != null) Label.text = "";
    }
    #endregion
    private void Punch()
    {
        if (!gameObject.activeInHierarchy) return;
        StartCoroutine(Tween.ScaleLocal(transform, transform.localScale * 1.22f,
                                        transform.localScale, 0.18f));
    }

    [ContextMenu("测试：显示 攻击 7")]
    private void TestAttack() { SetIntent(true, 7); }

    [ContextMenu("测试：显示 格挡 5")]
    private void TestBlock() { SetIntent(false, 5); }

    [ContextMenu("测试：隐藏")]
    private void TestHide() { Hide(); }
}
