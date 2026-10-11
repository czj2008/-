using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 战斗结果展示：用一张美术图表示"胜利"或"失败"，自动淡入 + 弹一下。
///
/// 【为什么要单独一个脚本】
/// 原来是用 Text 显示"胜  利！"，现在改成 Image 换图。
/// 这个脚本不管战斗逻辑，只管"把图显示出来然后淡出"。
/// </summary>
public class BattleResultView : MonoBehaviour
{
    [Header("引用")]
    public Image ResultImage;

    [Header("美术图")]
    public Sprite VictorySprite;
    public Sprite DefeatSprite;

    [Header("动画")]
    [Tooltip("淡入时间")]
    public float FadeInDuration = 0.25f;

    [Tooltip("停留时间")]
    public float HoldDuration = 1.0f;

    [Tooltip("淡出时间")]
    public float FadeOutDuration = 0.3f;

    [Tooltip("出现时的起始缩放（1 = 不缩放）")]
    public float StartScale = 0.75f;

    private Coroutine _routine;

    private void Awake()
    {
        if (ResultImage != null)
        {
            ResultImage.enabled = false;
            SetAlpha(0f);
        }
    }

    /// <summary>
    /// 播放结果演出。GameFlow 会 yield 等它跑完。
    /// </summary>
    public IEnumerator Play(bool playerWon)
    {
        if (ResultImage == null)
        {
            Debug.LogWarning("[结果] Result Image 没拖。", this);
            yield return Tween.WaitRealtime(0.5f);
            yield break;
        }

        // 换图
        ResultImage.sprite = playerWon ? VictorySprite : DefeatSprite;
        if (ResultImage.sprite == null)
        {
            Debug.LogWarning("[结果] " + (playerWon ? "Victory" : "Defeat") + " Sprite 没配。", this);
        }

        ResultImage.enabled = true;

        // 淡入 + 从 0.75 倍弹到 1 倍
        float t = 0f;
        transform.localScale = Vector3.one * StartScale;
        while (t < FadeInDuration)
        {
            t += Time.deltaTime;
            float p = t / FadeInDuration;
            SetAlpha(p);
            transform.localScale = Vector3.one * Mathf.LerpUnclamped(StartScale, 1f, p);
            yield return null;
        }
        SetAlpha(1f);
        transform.localScale = Vector3.one;

        // 停留
        yield return Tween.WaitRealtime(HoldDuration);

        // 淡出
        t = 0f;
        while (t < FadeOutDuration)
        {
            t += Time.deltaTime;
            SetAlpha(1f - t / FadeOutDuration);
            yield return null;
        }

        SetAlpha(0f);
        ResultImage.enabled = false;
    }

    /// <summary>立刻隐藏（开新战斗时清理用）。</summary>
    public void HideImmediate()
    {
        if (_routine != null) StopCoroutine(_routine);
        if (ResultImage != null)
        {
            ResultImage.enabled = false;
            SetAlpha(0f);
        }
        transform.localScale = Vector3.one;
    }

    private void SetAlpha(float alpha)
    {
        if (ResultImage == null) return;
        Color c = ResultImage.color;
        c.a = Mathf.Clamp01(alpha);
        ResultImage.color = c;
    }
}