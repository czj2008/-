using System.Collections;
using UnityEngine;
using UnityEngine.UI;

#region 引用
public class BattleIntro : MonoBehaviour
{
    [Header("引用")]
    [Tooltip("全屏黑幕（开场时压暗屏幕）")]
    public Image DarkOverlay;

    [Tooltip("“战斗开始”美术图（一整张图，含图案和文字）")]
    public Image IntroImage;

    [Header("时间（秒）")]
    [Tooltip("黑幕淡入时间")]
    public float DarkenDuration = 0.30f;

    [Tooltip("黑幕最终不透明度（0 = 全透明，1 = 全黑）")]
    [Range(0f, 1f)] public float DarkenAlpha = 0.85f;

    [Tooltip("美术图淡入时间")]
    public float ImageFadeDuration = 0.25f;

    [Tooltip("美术图停留时间（让人看清）")]
    public float HoldDuration = 0.80f;

    [Tooltip("整体淡出时间")]
    public float FadeOutDuration = 0.35f;

    [Header("弹出动画")]
    [Tooltip("美术图初始缩放（1 = 不缩放，直接淡入）")]
    public float ImageStartScale = 0.72f;

    [Tooltip("从哪个方向滑入：0 = 不滑动，正数从上方、负数从下方（像素）")]
    public float SlideOffsetY = 40f;
    #endregion

    //演出是否正在播放
    public bool IsPlaying { get; private set; }

    private void Awake()
    {
        // 初始状态：全透明（这样在编辑器里摆位置时不会被挡住）
        SetAlpha(DarkOverlay, 0f);
        SetAlpha(IntroImage, 0f);
    }

    public IEnumerator Play()
    {
        IsPlaying = true;

        //准备初始状态
        SetAlpha(DarkOverlay, 0f);
        SetAlpha(IntroImage, 0f);

        RectTransform imageRect = IntroImage != null ? IntroImage.rectTransform : null;
        Vector2 imageHome = imageRect != null ? imageRect.anchoredPosition : Vector2.zero;

        if (imageRect != null)
        {
            imageRect.localScale = Vector3.one * ImageStartScale;
            imageRect.anchoredPosition = imageHome + new Vector2(0f, SlideOffsetY);
        }

        //黑幕淡入
        if (DarkOverlay != null)
        {
            yield return Tween.FadeGraphic(DarkOverlay, 0f, DarkenAlpha, DarkenDuration);
        }
        else
        {
            yield return Tween.WaitRealtime(DarkenDuration);
        }

        //美术图淡入 + 弹到正常大小 + 滑到中心
        if (IntroImage != null)
        {
            StartCoroutine(Tween.FadeGraphic(IntroImage, 0f, 1f, ImageFadeDuration));

            if (imageRect != null)
            {
                StartCoroutine(Tween.Scale(imageRect, Vector3.one * ImageStartScale, Vector3.one, ImageFadeDuration));
                yield return Tween.MoveAnchored(imageRect, imageRect.anchoredPosition, imageHome, ImageFadeDuration);
            }
            else
            {
                yield return Tween.WaitRealtime(ImageFadeDuration);
            }
        }
        else
        {
            yield return Tween.WaitRealtime(ImageFadeDuration);
        }

        //停留
        yield return Tween.WaitRealtime(HoldDuration);

        //美术图淡出 + 黑幕淡出露出战斗画面
        if (IntroImage != null) StartCoroutine(Tween.FadeGraphic(IntroImage, 1f, 0f, FadeOutDuration));

        if (DarkOverlay != null)
        {
            yield return Tween.FadeGraphic(DarkOverlay, DarkenAlpha, 0f, FadeOutDuration);
        }
        else
        {
            yield return Tween.WaitRealtime(FadeOutDuration);
        }

        //收尾
        SetAlpha(IntroImage, 0f);
        if (imageRect != null)
        {
            imageRect.localScale = Vector3.one;
            imageRect.anchoredPosition = imageHome;
        }

        if (DarkOverlay != null)
        {
            SetAlpha(DarkOverlay, 0f);
            DarkOverlay.raycastTarget = false;     // 演出结束，放开点击
        }

        IsPlaying = false;
    }

    private static void SetAlpha(Graphic graphic, float alpha)
    {
        if (graphic == null) return;
        Color c = graphic.color;
        c.a = alpha;
        graphic.color = c;
    }
}