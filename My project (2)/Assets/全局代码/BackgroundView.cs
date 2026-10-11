using UnityEngine;

/// <summary>
/// 战斗背景：按关卡切换背景图，并让它铺满整个游戏画面。
///
/// 【缩放方式：覆盖式（cover）】
///   分别算出"铺满宽度需要的缩放"和"铺满高度需要的缩放"，
///   取【较大的那个】→ 两个方向都铺满，超出部分被裁掉。
///
///   为什么不能只按宽度缩放：
///   背景图比例和屏幕比例不一定一样（你的森林图更宽），
///   只按宽度铺满的话，上下就会露出底幕。
/// </summary>
public class BackgroundView : MonoBehaviour
{
    [Header("引用")]
    [Tooltip("背景的 SpriteRenderer（挂在背景图上）")]
    public SpriteRenderer Renderer;

    [Header("缩放")]
    [Tooltip("勾上：自动铺满画面")]
    public bool AutoFit = true;

    [Tooltip("额外余量（1.05 = 多铺 5%，避免边缘露缝）")]
    public float FitPadding = 1.05f;

    [Header("可选：宽度不足时拉伸底图")]
    [Tooltip("如果图片比例比屏幕更窄，直接按宽度拉伸会变形；" +
             "想让画面完全铺满又不变形，就让它按覆盖式裁剪")]
    public bool CropInsteadOfStretch = true;

    [Header("默认背景")]
    [Tooltip("没指定关卡背景时用这张（留空 = 不改）")]
    public Sprite DefaultBackground;

    private Camera _camera;

    private void Awake()
    {
        if (Renderer == null) Renderer = GetComponent<SpriteRenderer>();
        ResolveCamera();
    }

    private void Start()
    {
        // 场景刚起来时 Screen 尺寸可能还没稳，这里再校准一次
        if (AutoFit && Renderer != null && Renderer.sprite != null) Fit();
    }

    private void ResolveCamera()
    {
        _camera = Camera.main;
        if (_camera == null) _camera = FindObjectOfType<Camera>();
    }

    /// <summary>切换背景图（关卡开始时由 GameFlow 调用）。</summary>
    public void SetBackground(Sprite sprite)
    {
        if (sprite != null) { /* 用传入的 */ }
        else sprite = DefaultBackground;
        if (sprite == null) return;              // 都没配就保持原样
        if (Renderer == null) return;

        Renderer.sprite = sprite;
        Renderer.enabled = true;

        if (AutoFit) Fit();

        Debug.Log("[背景] 切换为 " + sprite.name +
                  "，缩放 " + Renderer.transform.localScale.x.ToString("0.00"));
    }

    /// <summary>铺满画面（保持比例，超出部分裁掉）。</summary>
    private void Fit()
    {
        if (Renderer == null || Renderer.sprite == null) return;
        if (_camera == null) ResolveCamera();
        if (_camera == null) { Debug.LogWarning("[背景] 场景里找不到相机，无法自动缩放"); return; }
        if (!_camera.orthographic) { Debug.LogWarning("[背景] 相机不是正交模式，无法自动缩放"); return; }

        // 相机可见范围（世界单位）
        float halfHeight = _camera.orthographicSize;
        float aspect = (float)Screen.width / Mathf.Max(1, Screen.height);
        float halfWidth = halfHeight * aspect;

        float viewWidth = halfWidth * 2f * FitPadding;
        float viewHeight = halfHeight * 2f * FitPadding;

        // 图片原始世界尺寸（像素 ÷ Pixels Per Unit）
        Sprite sprite = Renderer.sprite;
        float ppu = sprite.pixelsPerUnit > 0.01f ? sprite.pixelsPerUnit : 100f;
        float spriteWidth = sprite.rect.width / ppu;
        float spriteHeight = sprite.rect.height / ppu;
        if (spriteWidth <= 0.001f || spriteHeight <= 0.001f) return;

        // 覆盖式：两个方向都比一遍，取较大的那个
        float scaleX = viewWidth / spriteWidth;
        float scaleY = viewHeight / spriteHeight;
        float scale = Mathf.Max(scaleX, scaleY);

        Renderer.transform.localScale = new Vector3(scale, scale, 1f);

        // 顺便把背景摆到画面中心（避免它偏到一边）
        Vector3 camPos = _camera.transform.position;
        Renderer.transform.position = new Vector3(camPos.x, camPos.y, Renderer.transform.position.z);
    }
}
