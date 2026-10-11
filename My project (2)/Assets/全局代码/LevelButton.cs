using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class LevelButton : MonoBehaviour
{
    [Header("这一关的 id（要和 LevelGraph 里配的一致）")]
    public string LevelId = "l1";

    [Header("三态美术图")]
    [Tooltip("未解锁")]
    public Sprite LockedSprite;

    [Tooltip("已解锁、可挑战（还没通关）")]
    public Sprite AvailableSprite;

    [Tooltip("已通关")]
    public Sprite ClearedSprite;

    [Header("可选：通关标记（会跟着状态显示/隐藏）")]
    [Tooltip("比如一个小勾、一面旗子，只在已通关时显示")]
    public GameObject ClearedMark;

    [Header("可选：当前关卡指示（比如一个箭头）")]
    [Tooltip("只有这一关是当前进度时显示")]
    public GameObject CurrentMark;

    /// <summary>按钮组件（自动获取）。</summary>
    public Button Button { get; private set; }

    /// <summary>按钮的 Image（换图用）。</summary>
    public Image Image { get; private set; }

    private void Awake()
    {
        Button = GetComponent<Button>();
        Image = GetComponent<Image>();
    }

    /// <summary>按状态换图。由 LevelSelectController 调用。</summary>
    public void ApplyState(bool cleared, bool playable, bool isCurrent)
    {
        // 1) 换整张图
        if (Image != null)
        {
            if (cleared) Image.sprite = ClearedSprite;
            else if (playable) Image.sprite = AvailableSprite;
            else Image.sprite = LockedSprite;

            // 保持图片原色（不染色）——美术图是什么颜色就显示什么颜色
            Image.color = Color.white;

            // 三种状态图没配全时给个提示，避免"按钮变透明了"这种莫名现象
            if (Image.sprite == null)
            {
                Debug.LogWarning("[LevelButton] " + name + " 缺少状态美术图（已通关=" +
                                 (ClearedSprite != null) + "，可挑战=" + (AvailableSprite != null) +
                                 "，未解锁=" + (LockedSprite != null) + "）", this);
            }
        }

        // 2) 可选的通关标记
        if (ClearedMark != null) ClearedMark.SetActive(cleared);

        // 3) 可选的当前进度指示
        if (CurrentMark != null) CurrentMark.SetActive(isCurrent);
    }
}
