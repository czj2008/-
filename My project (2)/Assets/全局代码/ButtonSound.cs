using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 挂在按钮上就自动播放点击音效（不用手连 OnClick）。
/// </summary>
[RequireComponent(typeof(Button))]
public class ButtonSound : MonoBehaviour
{
    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(() => AudioManager.Instance.PlayClick());
    }
}
