using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tween : MonoBehaviour
{
    /// <summary>世界/本地位移。传 Transform 即可，UI 和 2D 精灵通用（用的是 localPosition）。</summary>
    public static IEnumerator MoveLocal(Transform tr, Vector3 from, Vector3 to, float duration)
    {
        if (tr == null) yield break;

        if (duration <= 0f) { tr.localPosition = to; yield break; }

        float t = 0f;
        tr.localPosition = from;
        while (t < duration)
        {
            t += Time.deltaTime;
            tr.localPosition = Vector3.LerpUnclamped(from, to, t / duration);
            yield return null;
        }
        tr.localPosition = to;
    }

    /// <summary>缩放补间。</summary>
    public static IEnumerator ScaleLocal(Transform tr, Vector3 from, Vector3 to, float duration)
    {
        if (tr == null) yield break;

        if (duration <= 0f) { tr.localScale = to; yield break; }

        float t = 0f;
        tr.localScale = from;
        while (t < duration)
        {
            t += Time.deltaTime;
            tr.localScale = Vector3.LerpUnclamped(from, to, t / duration);
            yield return null;
        }
        tr.localScale = to;
    }

    /// <summary>旋转补间。</summary>
    public static IEnumerator RotateLocal(Transform tr, Quaternion from, Quaternion to, float duration)
    {
        if (tr == null) yield break;

        if (duration <= 0f) { tr.localRotation = to; yield break; }

        float t = 0f;
        tr.localRotation = from;
        while (t < duration)
        {
            t += Time.deltaTime;
            tr.localRotation = Quaternion.SlerpUnclamped(from, to, t / duration);
            yield return null;
        }
        tr.localRotation = to;
    }

    /// <summary>颜色补间（SpriteRenderer / Image 通用）。</summary>
    public static IEnumerator ColorTo(SpriteRenderer sr, Color from, Color to, float duration)
    {
        if (sr == null) yield break;

        float t = 0f;
        sr.color = from;
        while (t < duration)
        {
            t += Time.deltaTime;
            sr.color = Color.LerpUnclamped(from, to, t / duration);
            yield return null;
        }
        sr.color = to;
    }

    /// <summary>等待真实时间（不受 Time.timeScale 影响）——设置界面暂停时也能用。</summary>
    public static IEnumerator WaitRealtime(float seconds)
    {
        float t = 0f;
        while (t < seconds) { t += Time.unscaledDeltaTime; yield return null; }
    }

    /// <summary>UI 节点的缩放补间（RectTransform 版）。</summary>
    public static IEnumerator Scale(RectTransform rt, Vector3 from, Vector3 to, float duration)
    {
        if (rt == null) yield break;

        if (duration <= 0f) { rt.localScale = to; yield break; }

        float t = 0f;
        rt.localScale = from;
        while (t < duration)
        {
            t += Time.deltaTime;
            rt.localScale = Vector3.LerpUnclamped(from, to, t / duration);
            yield return null;
        }
        rt.localScale = to;
    }

    /// <summary>UI 节点的位移补间（anchoredPosition 版）。
    /// 注意：UI 必须用 anchoredPosition，用 localPosition 会算错（尤其在锚点不是中心时）。</summary>
    public static IEnumerator MoveAnchored(RectTransform rt, Vector2 from, Vector2 to, float duration)
    {
        if (rt == null) yield break;

        if (duration <= 0f) { rt.anchoredPosition = to; yield break; }

        float t = 0f;
        rt.anchoredPosition = from;
        while (t < duration)
        {
            t += Time.deltaTime;
            rt.anchoredPosition = Vector2.LerpUnclamped(from, to, t / duration);
            yield return null;
        }
        rt.anchoredPosition = to;
    }
    public static IEnumerator Fade(CanvasGroup group, float from, float to, float duration)
    {
        if (group == null) yield break;

        if (duration <= 0f) { group.alpha = to; yield break; }

        float t = 0f;
        group.alpha = from;
        while (t < duration)
        {
            t += Time.deltaTime;
            group.alpha = Mathf.LerpUnclamped(from, to, t / duration);
            yield return null;
        }
        group.alpha = to;
    }
}
